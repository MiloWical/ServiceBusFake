using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Net.Security;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Text;

namespace Microsoft.ServiceBus
{
	internal class WebStream : CompositeDuplexStream
	{
		private const int PingFrequency = 5000;

		private const int ThrottleCapacity = 25;

		private const string KerberosAuthType = "kerberos";

		private const string NtlmAuthType = "ntlm";

		private const string BasicAuthType = "basic";

		private const string DigestAuthType = "digest";

		private const int TimeoutForHttpTestingInMiliSecond = 30000;

		private readonly static TimeSpan NaglingDelay;

		private readonly string connectionGroupId;

		private readonly Uri factoryEndpointUri;

		private readonly object thisLock;

		private readonly string webStreamRole;

		private readonly int timeoutForUpDownRequestInMiliSecond = -1;

		private readonly Uri sbUri;

		private volatile bool disposed;

		private volatile bool shutdownRead;

		private volatile bool shutdownWrite;

		private WebStream.ProxyAuthMode proxyAuthMode;

		private ServicePoint sessionServicePoint;

		private HttpWebRequest downstreamRequest;

		private HttpWebRequest upstreamRequest;

		public Uri RemoteReadEndpoint
		{
			get;
			private set;
		}

		public Uri RemoteWriteEndpoint
		{
			get;
			private set;
		}

		static WebStream()
		{
			WebStream.NaglingDelay = TimeSpan.FromMilliseconds(10);
			ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(WebStream.CustomizedCertificateValidator);
		}

		private WebStream(Uri factoryEndpointUri, string webStreamRole, EventTraceActivity activity) : base(activity)
		{
			this.factoryEndpointUri = factoryEndpointUri;
			this.webStreamRole = webStreamRole;
			Guid guid = Guid.NewGuid();
			this.connectionGroupId = string.Concat("WebStream", guid.ToString());
			this.thisLock = new object();
			UriBuilder uriBuilder = new UriBuilder(this.factoryEndpointUri);
			if (factoryEndpointUri.Scheme != Uri.UriSchemeHttps)
			{
				uriBuilder.Scheme = Uri.UriSchemeHttp;
				uriBuilder.Port = RelayEnvironment.RelayHttpPort;
				this.timeoutForUpDownRequestInMiliSecond = 30000;
			}
			else
			{
				uriBuilder.Scheme = Uri.UriSchemeHttps;
				uriBuilder.Port = RelayEnvironment.RelayHttpsPort;
			}
			this.factoryEndpointUri = uriBuilder.Uri;
			this.sbUri = this.factoryEndpointUri;
		}

		public WebStream(Uri factoryEndpointUri, string webStreamRole, bool useHttpsMode, EventTraceActivity activity, Uri sbUri) : base(activity)
		{
			this.factoryEndpointUri = factoryEndpointUri;
			this.webStreamRole = webStreamRole;
			Guid guid = Guid.NewGuid();
			this.connectionGroupId = string.Concat("WebStream", guid.ToString());
			this.thisLock = new object();
			if (this.factoryEndpointUri.Scheme == "sb")
			{
				UriBuilder uriBuilder = new UriBuilder(this.factoryEndpointUri);
				if (!useHttpsMode)
				{
					uriBuilder.Scheme = Uri.UriSchemeHttp;
					uriBuilder.Port = RelayEnvironment.RelayHttpPort;
				}
				else
				{
					uriBuilder.Scheme = Uri.UriSchemeHttps;
					uriBuilder.Port = RelayEnvironment.RelayHttpsPort;
				}
				this.factoryEndpointUri = uriBuilder.Uri;
			}
			this.sbUri = sbUri;
		}

		public override void Close()
		{
			MessagingClientEtwProvider.Provider.WebStreamClose(base.Activity, this.factoryEndpointUri.AbsoluteUri, this.sbUri.AbsoluteUri);
			base.Close();
		}

		private static string CloseResponseInWebException(Exception e)
		{
			string str;
			WebException webException = e as WebException;
			if (webException == null || webException.Response == null)
			{
				return string.Empty;
			}
			try
			{
				using (WebResponse response = webException.Response)
				{
					using (Stream responseStream = response.GetResponseStream())
					{
						using (StreamReader streamReader = new StreamReader(responseStream))
						{
							str = string.Concat(" -- Response:", streamReader.ReadToEnd());
						}
					}
				}
			}
			catch (Exception exception)
			{
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				Fx.Exception.TraceHandled(e, "WebStream.CloseResponseInWebException", null);
				return string.Empty;
			}
			return str;
		}

		private void ConfigurePreauthorizationRequest(HttpWebRequest request, WebStream.ProxyAuthMode authMode)
		{
			switch (authMode)
			{
				case WebStream.ProxyAuthMode.Kerberos:
				{
					request.PreAuthenticate = true;
					request.UnsafeAuthenticatedConnectionSharing = true;
					return;
				}
				case WebStream.ProxyAuthMode.Ntlm:
				{
					request.UnsafeAuthenticatedConnectionSharing = true;
					return;
				}
				default:
				{
					return;
				}
			}
		}

		private void ConfigureProxy(HttpWebRequest request, WebStream.ProxyAuthMode mode)
		{
			IWebProxy proxy = request.Proxy;
			Uri uri = request.Proxy.GetProxy(request.RequestUri);
			switch (mode)
			{
				case WebStream.ProxyAuthMode.None:
				{
					request.Proxy = new WebStream.ForcedCredentialWebProxy(proxy, null);
					return;
				}
				case WebStream.ProxyAuthMode.Kerberos:
				{
					CredentialCache credentialCaches = new CredentialCache()
					{
						{ uri, "kerberos", proxy.Credentials.GetCredential(uri, "kerberos") }
					};
					request.Proxy = new WebStream.ForcedCredentialWebProxy(proxy, credentialCaches);
					request.PreAuthenticate = true;
					return;
				}
				case WebStream.ProxyAuthMode.Ntlm:
				{
					CredentialCache credentialCaches1 = new CredentialCache()
					{
						{ uri, "ntlm", proxy.Credentials.GetCredential(uri, "ntlm") }
					};
					request.Proxy = new WebStream.ForcedCredentialWebProxy(proxy, credentialCaches1);
					return;
				}
				case WebStream.ProxyAuthMode.Other:
				{
					CredentialCache credentialCaches2 = new CredentialCache()
					{
						{ uri, "basic", proxy.Credentials.GetCredential(uri, "basic") },
						{ uri, "digest", proxy.Credentials.GetCredential(uri, "digest") }
					};
					request.Proxy = new WebStream.ForcedCredentialWebProxy(proxy, credentialCaches2);
					return;
				}
				default:
				{
					return;
				}
			}
		}

		private bool Connect()
		{
			Uri uri;
			Uri uri1;
			this.CreateSession(out uri, out uri1);
			this.StartSession(uri, uri1);
			if (uri.Scheme != Uri.UriSchemeHttp)
			{
				return true;
			}
			return this.VerifySession();
		}

		private PumpStream CreateDownStreamRequest(Uri endpointLocation)
		{
			PumpStream pumpStream;
			try
			{
				this.downstreamRequest = (HttpWebRequest)WebRequest.Create(endpointLocation);
				this.downstreamRequest.Method = "GET";
				this.downstreamRequest.ConnectionGroupName = this.connectionGroupId;
				this.downstreamRequest.SendChunked = false;
				this.downstreamRequest.Timeout = this.timeoutForUpDownRequestInMiliSecond;
				this.downstreamRequest.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
				this.downstreamRequest.Headers.Add("X-WSCONNECT", this.webStreamRole);
				this.downstreamRequest.Headers.Add("X-PROCESS-AT", "http://schemas.microsoft.com/netservices/2009/05/servicebus/connect/roles/relay");
				if (this.downstreamRequest.Proxy != null)
				{
					this.ConfigureProxy(this.downstreamRequest, this.proxyAuthMode);
				}
				this.downstreamRequest.ServicePoint.UseNagleAlgorithm = false;
				this.downstreamRequest.ServicePoint.Expect100Continue = false;
				this.downstreamRequest.ServicePoint.ConnectionLimit = 2048;
				Stream responseStream = ((HttpWebResponse)this.downstreamRequest.GetResponse()).GetResponseStream();
				ThrottledPipeStream throttledPipeStream = new ThrottledPipeStream(25, WebStream.NaglingDelay);
				Stream stream = responseStream;
				ThrottledPipeStream throttledPipeStream1 = throttledPipeStream;
				FramingInputPump framingInputPump = new FramingInputPump(new BufferRead(stream.Read), new BufferWrite(throttledPipeStream1.Write), new Action(throttledPipeStream.WriteEndOfStream), base.Activity, this.factoryEndpointUri);
				ReadPumpStream readPumpStream = new ReadPumpStream(responseStream, throttledPipeStream, framingInputPump)
				{
					PumpCompletedEvent = new Action(this.OnReadStreamCompleted)
				};
				readPumpStream.BeginRunPump();
				pumpStream = readPumpStream;
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Fx.Exception.AsError(new CommunicationException(SRClient.DownstreamConnection, exception), null);
				}
				throw;
			}
			return pumpStream;
		}

		private void CreateSession(out Uri endpointLocation1, out Uri endpointLocation2)
		{
			WebStream.ProxyAuthMode[] proxyAuthModeArray = new WebStream.ProxyAuthMode[] { WebStream.ProxyAuthMode.None, WebStream.ProxyAuthMode.Kerberos, WebStream.ProxyAuthMode.Ntlm, WebStream.ProxyAuthMode.Other };
			this.proxyAuthMode = WebStream.ProxyAuthMode.None;
			string empty = string.Empty;
			WebStream.ProxyAuthMode[] proxyAuthModeArray1 = proxyAuthModeArray;
			int num = 0;
		Label1:
			while (num < (int)proxyAuthModeArray1.Length)
			{
				WebStream.ProxyAuthMode proxyAuthMode = proxyAuthModeArray1[num];
				switch (proxyAuthMode)
				{
					case WebStream.ProxyAuthMode.Kerberos:
					{
						if (empty.Contains("KERBEROS"))
						{
							break;
						}
						goto Label0;
					}
					case WebStream.ProxyAuthMode.Ntlm:
					{
						if (!empty.Contains("NTLM"))
						{
							goto Label0;
						}
						else
						{
							break;
						}
					}
				}
				try
				{
					HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(this.factoryEndpointUri);
					httpWebRequest.KeepAlive = false;
					httpWebRequest.Method = "POST";
					httpWebRequest.ContentType = "text/plain";
					httpWebRequest.Headers.Add("X-WSCREATE", this.webStreamRole);
					httpWebRequest.Headers.Add("X-PROCESS-AT", "http://schemas.microsoft.com/netservices/2009/05/servicebus/connect/roles/relay");
					if (httpWebRequest.Proxy != null)
					{
						this.ConfigureProxy(httpWebRequest, proxyAuthMode);
					}
					httpWebRequest.ServicePoint.ConnectionLimit = 2048;
					using (Stream requestStream = httpWebRequest.GetRequestStream())
					{
						using (StreamWriter streamWriter = new StreamWriter(requestStream, Encoding.UTF8))
						{
							streamWriter.Write(string.Empty);
							streamWriter.Flush();
						}
					}
					HttpWebResponse response = (HttpWebResponse)httpWebRequest.GetResponse();
					if (response.StatusCode != HttpStatusCode.Created || response.Headers["X-WSENDPT1"] == null || response.Headers["X-WSENDPT2"] == null)
					{
						throw Fx.Exception.AsWarning(new CommunicationException(SRClient.FaultyEndpointResponse), null);
					}
					if (!Uri.TryCreate(response.Headers["X-WSENDPT1"], UriKind.Absolute, out endpointLocation1))
					{
						throw Fx.Exception.AsWarning(new CommunicationException(SRClient.URIEndpoint), null);
					}
					if (!Uri.TryCreate(response.Headers["X-WSENDPT2"], UriKind.Absolute, out endpointLocation2))
					{
						throw Fx.Exception.AsWarning(new CommunicationException(SRClient.URIEndpoint), null);
					}
					response.Close();
					this.proxyAuthMode = proxyAuthMode;
				}
				catch (WebException webException1)
				{
					WebException webException = webException1;
					HttpWebResponse httpWebResponse = webException.Response as HttpWebResponse;
					if (httpWebResponse == null || httpWebResponse.StatusCode != HttpStatusCode.ProxyAuthenticationRequired)
					{
						string str = WebStream.CloseResponseInWebException(webException);
						throw Fx.Exception.AsWarning(new CommunicationException(string.Concat(SRClient.FactoryEndpoint, str), webException), null);
					}
					empty = httpWebResponse.Headers["Proxy-Authenticate"].ToUpperInvariant();
					goto Label0;
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (!Fx.IsFatal(exception))
					{
						throw Fx.Exception.AsError(new CommunicationException(SRClient.FactoryEndpoint, exception), null);
					}
					throw;
				}
				return;
			}
			ExceptionTrace exceptionTrace = Fx.Exception;
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] objArray = new object[] { empty };
			throw exceptionTrace.AsError(new CommunicationException(string.Format(invariantCulture, "Failed to authenticate with proxy supporting modes '{0}'.", objArray)), null);
		Label0:
			num++;
			goto Label1;
		}

		private PumpStream CreateUpStreamRequest(Uri endpointLocation)
		{
			PumpStream pumpStream;
			try
			{
				this.PreauthorizeUpStreamRequestIfNeeded(endpointLocation, this.proxyAuthMode);
				this.upstreamRequest = (HttpWebRequest)WebRequest.Create(endpointLocation);
				this.upstreamRequest.Method = "POST";
				this.upstreamRequest.ConnectionGroupName = this.connectionGroupId;
				this.upstreamRequest.SendChunked = true;
				this.upstreamRequest.Timeout = this.timeoutForUpDownRequestInMiliSecond;
				this.upstreamRequest.ServicePoint.ConnectionLimit = 2048;
				if (endpointLocation.Scheme == Uri.UriSchemeHttps && this.upstreamRequest.ServicePoint.ProtocolVersion != HttpVersion.Version11)
				{
					try
					{
						typeof(ServicePoint).GetProperty("HttpBehaviour", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(this.upstreamRequest.ServicePoint, (byte)0, null);
					}
					catch (Exception exception1)
					{
						Exception exception = exception1;
						if (!Fx.IsFatal(exception))
						{
							this.upstreamRequest = null;
							throw Fx.Exception.AsError(new CommunicationException(SRClient.UpstreamConnection, exception), null);
						}
						throw;
					}
				}
				this.upstreamRequest.ServicePoint.UseNagleAlgorithm = false;
				this.upstreamRequest.ServicePoint.Expect100Continue = false;
				this.upstreamRequest.ContentType = "application/octet-stream";
				this.upstreamRequest.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
				this.upstreamRequest.AllowWriteStreamBuffering = false;
				this.upstreamRequest.Headers.Add("X-WSCONNECT", this.webStreamRole);
				this.upstreamRequest.Headers.Add("X-PROCESS-AT", "http://schemas.microsoft.com/netservices/2009/05/servicebus/connect/roles/relay");
				if (this.upstreamRequest.Proxy != null)
				{
					this.ConfigureProxy(this.upstreamRequest, this.proxyAuthMode);
				}
				this.ConfigurePreauthorizationRequest(this.upstreamRequest, this.proxyAuthMode);
				this.upstreamRequest.ServicePoint.UseNagleAlgorithm = false;
				this.upstreamRequest.ServicePoint.Expect100Continue = false;
				this.upstreamRequest.ServicePoint.ConnectionLimit = 2048;
				this.sessionServicePoint = this.upstreamRequest.ServicePoint;
				Stream requestStream = this.upstreamRequest.GetRequestStream();
				ThrottledPipeStream throttledPipeStream = new ThrottledPipeStream(25, WebStream.NaglingDelay);
				ThrottledPipeStream throttledPipeStream1 = throttledPipeStream;
				Stream stream = requestStream;
				FramingOutputPump framingOutputPump = new FramingOutputPump(new BufferRead(throttledPipeStream1.Read), new BufferWrite(stream.Write), 5000, base.Activity, this.factoryEndpointUri);
				PumpStream writePumpStream = new WritePumpStream(throttledPipeStream, requestStream, framingOutputPump)
				{
					PumpCompletedEvent = new Action(this.OnWriteStreamCompleted)
				};
				writePumpStream.BeginRunPump();
				pumpStream = writePumpStream;
			}
			catch (Exception exception3)
			{
				Exception exception2 = exception3;
				if (!Fx.IsFatal(exception2))
				{
					string str = WebStream.CloseResponseInWebException(exception2);
					throw Fx.Exception.AsError(new CommunicationException(string.Concat(SRClient.UpstreamConnection, str), exception2), null);
				}
				throw;
			}
			return pumpStream;
		}

		private static bool CustomizedCertificateValidator(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			return SecureSocketUtil.CustomizedCertificateValidator(sender, certificate, chain, sslPolicyErrors, RelayEnvironment.RelayHostRootName);
		}

		protected override void Dispose(bool disposing)
		{
			MessagingClientEtwProvider.Provider.WebStreamDispose(base.Activity, this.factoryEndpointUri.AbsoluteUri, this.sbUri.AbsoluteUri);
			try
			{
				if (disposing)
				{
					this.Reset();
					if (this.sessionServicePoint != null)
					{
						this.sessionServicePoint.CloseConnectionGroup(this.connectionGroupId);
					}
				}
			}
			finally
			{
				base.Dispose(disposing);
			}
		}

		public static bool IsSupportingScheme(Uri factoryEndpointUri, out Exception exception)
		{
			bool flag;
			exception = null;
			try
			{
				using (WebStream webStream = new WebStream(factoryEndpointUri, "connection", new EventTraceActivity()))
				{
					flag = webStream.Connect();
				}
			}
			catch (WebException webException1)
			{
				WebException webException = webException1;
				string str = WebStream.CloseResponseInWebException(webException);
				exception = new CommunicationException(string.Concat(webException.Message, str), webException);
				flag = false;
			}
			catch (Exception exception2)
			{
				Exception exception1 = exception2;
				if (Fx.IsFatal(exception1))
				{
					throw;
				}
				exception = exception1;
				flag = false;
			}
			return flag;
		}

		private void OnReadStreamCompleted()
		{
			MessagingClientEtwProvider.Provider.WebStreamReadStreamCompleted(base.Activity, this.factoryEndpointUri.AbsoluteUri, this.sbUri.AbsoluteUri);
			this.OnStreamCompleted();
		}

		private void OnStreamCompleted()
		{
			try
			{
				this.Reset();
			}
			catch (WebException webException1)
			{
				WebException webException = webException1;
				ExceptionTrace exception = Fx.Exception;
				object[] absoluteUri = new object[] { "WebStream.OnStreamCompleted uri: ", this.factoryEndpointUri.AbsoluteUri, " sbUri: ", this.sbUri };
				exception.TraceHandled(webException, string.Concat(absoluteUri), base.Activity);
			}
			catch (Exception exception2)
			{
				Exception exception1 = exception2;
				if (Fx.IsFatal(exception1))
				{
					throw;
				}
				ExceptionTrace exceptionTrace = Fx.Exception;
				object[] objArray = new object[] { "WebStream.OnStreamCompleted uri: ", this.factoryEndpointUri.AbsoluteUri, " sbUri: ", this.sbUri };
				exceptionTrace.TraceHandled(exception1, string.Concat(objArray), base.Activity);
			}
		}

		private void OnWriteStreamCompleted()
		{
			MessagingClientEtwProvider.Provider.WebStreamWriteStreamCompleted(base.Activity, this.factoryEndpointUri.AbsoluteUri, this.sbUri.AbsoluteUri);
			this.OnStreamCompleted();
		}

		public WebStream Open()
		{
			int num;
			Exception communicationException = null;
			int num1 = 5;
			do
			{
				int num2 = num1 - 1;
				try
				{
					MessagingClientEtwProvider.Provider.WebStreamConnecting(base.Activity, this.factoryEndpointUri.AbsoluteUri, this.sbUri.AbsoluteUri, num2);
					if (this.Connect())
					{
						MessagingClientEtwProvider.Provider.WebStreamConnectCompleted(base.Activity, this.factoryEndpointUri.AbsoluteUri, this.sbUri.AbsoluteUri, num2);
						return this;
					}
				}
				catch (WebException webException1)
				{
					WebException webException = webException1;
					if (Fx.IsFatal(webException))
					{
						throw;
					}
					string str = WebStream.CloseResponseInWebException(webException);
					communicationException = new CommunicationException(string.Concat(webException.Message, str), webException);
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					communicationException = exception;
				}
				string str1 = (communicationException != null ? communicationException.ToString() : "Failed to connect. No exception thrown.");
				MessagingClientEtwProvider.Provider.WebStreamConnectFailed(base.Activity, this.factoryEndpointUri.AbsoluteUri, this.sbUri.AbsoluteUri, num2, str1);
				this.Reset();
				this.disposed = false;
				num = num1 - 1;
				num1 = num;
			}
			while (num > 0);
			this.disposed = true;
			throw Fx.Exception.AsError(new CommunicationException(SRClient.HTTPConnectivityMode, communicationException), null);
		}

		private void PreauthorizeUpStreamRequestIfNeeded(Uri endpointLocation, WebStream.ProxyAuthMode authMode)
		{
			switch (authMode)
			{
				case WebStream.ProxyAuthMode.Kerberos:
				case WebStream.ProxyAuthMode.Ntlm:
				{
					try
					{
						HttpWebRequest requestCachePolicy = (HttpWebRequest)WebRequest.Create(endpointLocation);
						requestCachePolicy.Method = "HEAD";
						requestCachePolicy.ConnectionGroupName = this.connectionGroupId;
						requestCachePolicy.Timeout = -1;
						requestCachePolicy.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
						requestCachePolicy.AllowWriteStreamBuffering = false;
						requestCachePolicy.Headers.Add("X-WSCONNECT", this.webStreamRole);
						requestCachePolicy.Headers.Add("X-PROCESS-AT", "http://schemas.microsoft.com/netservices/2009/05/servicebus/connect/roles/relay");
						if (requestCachePolicy.Proxy != null)
						{
							this.ConfigureProxy(requestCachePolicy, authMode);
						}
						this.ConfigurePreauthorizationRequest(requestCachePolicy, authMode);
						requestCachePolicy.ServicePoint.UseNagleAlgorithm = false;
						requestCachePolicy.ServicePoint.Expect100Continue = false;
						requestCachePolicy.GetResponse();
					}
					catch (WebException webException)
					{
					}
					return;
				}
				default:
				{
					return;
				}
			}
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			if (!this.shutdownRead)
			{
				return base.Read(buffer, offset, count);
			}
			MessagingClientEtwProvider.Provider.WebStreamReturningZero(base.Activity, this.factoryEndpointUri.AbsoluteUri, this.sbUri.AbsoluteUri);
			return 0;
		}

		private void Reset()
		{
			MessagingClientEtwProvider.Provider.WebStreamReset(base.Activity, this.factoryEndpointUri.AbsoluteUri, this.sbUri.AbsoluteUri);
			if (!this.disposed)
			{
				lock (this.thisLock)
				{
					if (!this.disposed)
					{
						this.disposed = true;
						this.Shutdown();
						int num = 0;
						bool flag = (bool)num;
						this.shutdownWrite = (bool)num;
						this.shutdownRead = flag;
					}
				}
			}
		}

		public override void Shutdown()
		{
			MessagingClientEtwProvider.Provider.WebStreamShutdown(base.Activity, this.factoryEndpointUri.AbsoluteUri, this.sbUri.AbsoluteUri);
			lock (this.thisLock)
			{
				if (!this.shutdownWrite)
				{
					this.shutdownWrite = true;
					if (this.upstreamRequest != null)
					{
						this.upstreamRequest.Abort();
						this.upstreamRequest = null;
						if (this.outputStream != null)
						{
							((PumpStream)this.outputStream).Shutdown();
							this.outputStream = null;
						}
					}
				}
				if (!this.shutdownRead)
				{
					this.shutdownRead = true;
					if (this.downstreamRequest != null)
					{
						this.downstreamRequest.Abort();
						this.downstreamRequest = null;
						this.inputStream = null;
					}
				}
			}
		}

		private void StartSession(Uri readEndpoint, Uri writeEndpoint)
		{
			lock (this.thisLock)
			{
				this.RemoteReadEndpoint = readEndpoint;
				this.RemoteWriteEndpoint = writeEndpoint;
			}
			this.outputStream = this.CreateUpStreamRequest(writeEndpoint);
			this.inputStream = this.CreateDownStreamRequest(readEndpoint);
		}

		private bool VerifySession()
		{
			byte[] numArray = new byte[] { 1, 2, 3, 4 };
			byte[] numArray1 = new byte[(int)numArray.Length];
			int readTimeout = this.ReadTimeout;
			int writeTimeout = this.WriteTimeout;
			this.ReadTimeout = (int)TimeSpan.FromSeconds(30).TotalMilliseconds;
			this.WriteTimeout = (int)TimeSpan.FromSeconds(30).TotalMilliseconds;
			this.Write(numArray, 0, (int)numArray.Length);
			if (this.Read(numArray1, 0, (int)numArray1.Length) < (int)numArray1.Length)
			{
				return false;
			}
			for (int i = 0; i < (int)numArray.Length; i++)
			{
				if (numArray[i] != numArray1[i])
				{
					return false;
				}
			}
			this.Write(numArray, 0, (int)numArray.Length);
			if (this.Read(numArray1, 0, (int)numArray1.Length) < (int)numArray1.Length)
			{
				return false;
			}
			for (int j = 0; j < (int)numArray.Length; j++)
			{
				if (numArray[j] != numArray1[j])
				{
					return false;
				}
			}
			this.ReadTimeout = readTimeout;
			this.WriteTimeout = writeTimeout;
			return true;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			if (this.shutdownWrite)
			{
				throw Fx.Exception.AsWarning(new CommunicationException(SRClient.WebStreamShutdown), null);
			}
			base.Write(buffer, offset, count);
		}

		private class ForcedCredentialWebProxy : IWebProxy
		{
			private readonly IWebProxy innerProxy;

			private readonly ICredentials innerCredentials;

			public ICredentials Credentials
			{
				get
				{
					return this.innerCredentials;
				}
				set
				{
					throw Fx.Exception.AsError(new NotImplementedException(), null);
				}
			}

			public ForcedCredentialWebProxy(IWebProxy proxy, ICredentials credentials)
			{
				this.innerProxy = proxy;
				this.innerCredentials = credentials;
			}

			public Uri GetProxy(Uri destination)
			{
				return this.innerProxy.GetProxy(destination);
			}

			public bool IsBypassed(Uri host)
			{
				return this.innerProxy.IsBypassed(host);
			}
		}

		private enum ProxyAuthMode
		{
			None,
			Kerberos,
			Ntlm,
			Other
		}
	}
}