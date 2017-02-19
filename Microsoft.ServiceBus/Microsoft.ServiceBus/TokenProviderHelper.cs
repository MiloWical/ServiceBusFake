using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IdentityModel.Tokens;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Web;

namespace Microsoft.ServiceBus
{
	internal static class TokenProviderHelper
	{
		internal static IAsyncResult BeginGetAccessTokenByAssertion(Uri requestUri, string appliesTo, string requestToken, string simpleAuthAssertionFormat, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return TokenProviderHelper.BeginGetAccessTokenCore(requestUri, appliesTo, requestToken, simpleAuthAssertionFormat, timeout, callback, state);
		}

		internal static IAsyncResult BeginGetAccessTokenCore(Uri requestUri, string appliesTo, string requestToken, string simpleAuthAssertionFormat, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new TokenProviderHelper.TokenRequestAsyncResult(requestUri, appliesTo, requestToken, simpleAuthAssertionFormat, timeout, callback, state);
		}

		private static Exception ConvertException(Uri requestUri, Exception innerException)
		{
			string tokenProviderFailedSecurityToken = Resources.TokenProviderFailedSecurityToken;
			object[] absoluteUri = new object[] { requestUri.AbsoluteUri, null };
			absoluteUri[1] = (innerException == null ? "Unknown" : innerException.Message);
			SecurityTokenException securityTokenException = new SecurityTokenException(Microsoft.ServiceBus.SR.GetString(tokenProviderFailedSecurityToken, absoluteUri), innerException);
			if (DiagnosticUtility.ShouldTraceInformation)
			{
				DiagnosticUtility.ExceptionUtility.TraceHandledException(securityTokenException, TraceEventType.Information);
			}
			return securityTokenException;
		}

		private static DateTime ConvertExpiry(string expiresIn)
		{
			double num = Convert.ToDouble(expiresIn, CultureInfo.InvariantCulture);
			return DateTime.UtcNow + TimeSpan.FromSeconds(num);
		}

		private static int ConvertToInt32(TimeSpan timeout)
		{
			int num;
			try
			{
				num = Convert.ToInt32(timeout.TotalMilliseconds, CultureInfo.InvariantCulture);
			}
			catch (OverflowException overflowException)
			{
				throw new ArgumentException(SRClient.TimeoutExceeded, overflowException);
			}
			return num;
		}

		private static Exception ConvertWebException(Uri requestUri, WebException exception)
		{
			Exception securityTokenException;
			Exception exception1;
			object[] absoluteUri;
			object[] objArray;
			string tokenProviderFailedSecurityToken;
			string tokenProviderTimeout;
			string empty = string.Empty;
			if (exception == null || exception.Response == null)
			{
				empty = (exception == null ? "Unknown" : exception.Message);
			}
			else
			{
				using (exception.Response)
				{
					using (StreamReader streamReader = new StreamReader(exception.Response.GetResponseStream()))
					{
						empty = streamReader.ReadToEnd();
					}
					HttpWebResponse response = exception.Response as HttpWebResponse;
					if (response == null || response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Continue)
					{
						tokenProviderFailedSecurityToken = Resources.TokenProviderFailedSecurityToken;
						absoluteUri = new object[] { requestUri.AbsoluteUri, empty };
						securityTokenException = new SecurityTokenException(Microsoft.ServiceBus.SR.GetString(tokenProviderFailedSecurityToken, absoluteUri), exception);
						if (exception != null && (exception.Status == WebExceptionStatus.Timeout || exception.Status == WebExceptionStatus.RequestCanceled))
						{
							tokenProviderTimeout = Resources.TokenProviderTimeout;
							objArray = new object[] { requestUri.AbsoluteUri };
							securityTokenException = new TimeoutException(Microsoft.ServiceBus.SR.GetString(tokenProviderTimeout, objArray), securityTokenException);
						}
						if (DiagnosticUtility.ShouldTraceInformation)
						{
							DiagnosticUtility.ExceptionUtility.TraceHandledException(securityTokenException, TraceEventType.Information);
						}
						return securityTokenException;
					}
					else
					{
						string str = Resources.TokenProviderFailedSecurityToken;
						object[] absoluteUri1 = new object[] { requestUri.AbsoluteUri, empty };
						Exception internalSecurityTokenException = new TokenProviderHelper.InternalSecurityTokenException(Microsoft.ServiceBus.SR.GetString(str, absoluteUri1), response.StatusCode, exception);
						HttpStatusCode statusCode = response.StatusCode;
						if (statusCode != HttpStatusCode.NotFound)
						{
							if (statusCode != HttpStatusCode.RequestTimeout)
							{
								switch (statusCode)
								{
									case HttpStatusCode.BadGateway:
									case HttpStatusCode.ServiceUnavailable:
									{
										goto Label2;
									}
									case HttpStatusCode.GatewayTimeout:
									{
										break;
									}
									default:
									{
										goto Label1;
									}
								}
							}
							string tokenProviderTimeout1 = Resources.TokenProviderTimeout;
							object[] objArray1 = new object[] { requestUri.AbsoluteUri };
							internalSecurityTokenException = new TimeoutException(Microsoft.ServiceBus.SR.GetString(tokenProviderTimeout1, objArray1), exception);
							goto Label1;
						}
					Label2:
						string tokenProviderServiceUnavailable = Resources.TokenProviderServiceUnavailable;
						object[] absoluteUri2 = new object[] { requestUri.AbsoluteUri };
						internalSecurityTokenException = new TokenProviderException(Microsoft.ServiceBus.SR.GetString(tokenProviderServiceUnavailable, absoluteUri2), exception);
					Label1:
						if (DiagnosticUtility.ShouldTraceInformation)
						{
							DiagnosticUtility.ExceptionUtility.TraceHandledException(internalSecurityTokenException, TraceEventType.Information);
						}
						exception1 = internalSecurityTokenException;
					}
				}
				return exception1;
			}
			tokenProviderFailedSecurityToken = Resources.TokenProviderFailedSecurityToken;
			absoluteUri = new object[] { requestUri.AbsoluteUri, empty };
			securityTokenException = new SecurityTokenException(Microsoft.ServiceBus.SR.GetString(tokenProviderFailedSecurityToken, absoluteUri), exception);
			if (exception != null && (exception.Status == WebExceptionStatus.Timeout || exception.Status == WebExceptionStatus.RequestCanceled))
			{
				tokenProviderTimeout = Resources.TokenProviderTimeout;
				objArray = new object[] { requestUri.AbsoluteUri };
				securityTokenException = new TimeoutException(Microsoft.ServiceBus.SR.GetString(tokenProviderTimeout, objArray), securityTokenException);
			}
			if (DiagnosticUtility.ShouldTraceInformation)
			{
				DiagnosticUtility.ExceptionUtility.TraceHandledException(securityTokenException, TraceEventType.Information);
			}
			return securityTokenException;
		}

		private static IDictionary<string, string> Decode(string encodedString)
		{
			IDictionary<string, string> strs = new Dictionary<string, string>();
			if (!string.IsNullOrWhiteSpace(encodedString))
			{
				foreach (string str in encodedString.Split(new char[] { '&' }, StringSplitOptions.None))
				{
					char[] chrArray = new char[] { '=' };
					string[] strArrays = str.Split(chrArray, StringSplitOptions.None);
					if ((int)strArrays.Length != 2)
					{
						throw new FormatException(SRClient.InvalidEncoding);
					}
					strs.Add(HttpUtility.UrlDecode(strArrays[0]), HttpUtility.UrlDecode(strArrays[1]));
				}
			}
			return strs;
		}

		internal static TokenProviderHelper.TokenResult<SecurityToken> EndGetAccessTokenByAssertion(IAsyncResult result)
		{
			string str;
			string str1;
			string str2 = TokenProviderHelper.EndGetAccessTokenCore(result, out str, out str1);
			DateTime dateTime = TokenProviderHelper.ConvertExpiry(str);
			SimpleWebSecurityToken simpleWebSecurityToken = new SimpleWebSecurityToken(str2, dateTime, str1);
			return new TokenProviderHelper.TokenResult<SecurityToken>()
			{
				CacheUntil = dateTime,
				Token = simpleWebSecurityToken
			};
		}

		internal static string EndGetAccessTokenCore(IAsyncResult result, out string expiresIn, out string audience)
		{
			TokenProviderHelper.TokenRequestAsyncResult tokenRequestAsyncResult = AsyncResult<TokenProviderHelper.TokenRequestAsyncResult>.End(result);
			expiresIn = tokenRequestAsyncResult.ExpiresIn;
			audience = tokenRequestAsyncResult.Audience;
			return tokenRequestAsyncResult.AccessToken;
		}

		private static void ExtractAccessToken(string urlParameters, out string token, out string expiresIn, out string audience)
		{
			token = null;
			expiresIn = null;
			audience = null;
			if (urlParameters != null)
			{
				string[] strArrays = urlParameters.Split(new char[] { '&' });
				for (int i = 0; i < (int)strArrays.Length; i++)
				{
					string str = strArrays[i];
					string[] strArrays1 = str.Split(new char[] { '=' });
					if ((int)strArrays1.Length != 2)
					{
						string tokenProviderInvalidTokenParameter = Resources.TokenProviderInvalidTokenParameter;
						object[] objArray = new object[] { str };
						TokenProviderHelper.ThrowException(Microsoft.ServiceBus.SR.GetString(tokenProviderInvalidTokenParameter, objArray));
					}
					else
					{
						string str1 = strArrays1[0].Trim();
						if (str1 == "wrap_access_token")
						{
							token = HttpUtility.UrlDecode(strArrays1[1].Trim());
							audience = TokenProviderHelper.ExtractAudience(token);
						}
						else if (str1 == "wrap_access_token_expires_in")
						{
							expiresIn = HttpUtility.UrlDecode(strArrays1[1].Trim());
						}
					}
				}
			}
			if (string.IsNullOrEmpty(token))
			{
				TokenProviderHelper.ThrowException(Microsoft.ServiceBus.SR.GetString(Resources.TokenProviderEmptyToken, new object[0]));
			}
			if (string.IsNullOrEmpty(expiresIn))
			{
				TokenProviderHelper.ThrowException(Microsoft.ServiceBus.SR.GetString(Resources.TokenProviderEmptyExpiration, new object[0]));
			}
			if (string.IsNullOrEmpty(audience))
			{
				TokenProviderHelper.ThrowException(Microsoft.ServiceBus.SR.GetString(Resources.TokenProviderEmptyAudience, new object[0]));
			}
		}

		private static string ExtractAudience(string token)
		{
			string str = null;
			if (token != null)
			{
				string[] strArrays = token.Split(new char[] { '&' });
				for (int i = 0; i < (int)strArrays.Length; i++)
				{
					string str1 = strArrays[i];
					string[] strArrays1 = str1.Split(new char[] { '=' });
					if ((int)strArrays1.Length != 2)
					{
						string tokenProviderInvalidTokenParameter = Resources.TokenProviderInvalidTokenParameter;
						object[] objArray = new object[] { str1 };
						TokenProviderHelper.ThrowException(Microsoft.ServiceBus.SR.GetString(tokenProviderInvalidTokenParameter, objArray));
					}
					else if (strArrays1[0].Trim() == "Audience")
					{
						str = HttpUtility.UrlDecode(strArrays1[1].Trim());
					}
				}
			}
			if (string.IsNullOrEmpty(str))
			{
				TokenProviderHelper.ThrowException(Microsoft.ServiceBus.SR.GetString(Resources.TokenProviderEmptyAudience, new object[0]));
			}
			return str;
		}

		private static void ExtractExpiresInAndAudience(string simpleWebToken, out DateTime expiresIn, out string audience)
		{
			expiresIn = DateTime.MinValue;
			audience = null;
			if (string.IsNullOrWhiteSpace(simpleWebToken))
			{
				TokenProviderHelper.ThrowException(Microsoft.ServiceBus.SR.GetString(Resources.TokenProviderEmptyToken, new object[0]));
			}
			else
			{
				IDictionary<string, string> strs = TokenProviderHelper.Decode(simpleWebToken);
				string item = strs["ExpiresOn"];
				if (string.IsNullOrWhiteSpace(item))
				{
					TokenProviderHelper.ThrowException(Microsoft.ServiceBus.SR.GetString(Resources.TokenProviderEmptyExpiration, new object[0]));
				}
				expiresIn = TokenConstants.WrapBaseTime + TimeSpan.FromSeconds(double.Parse(HttpUtility.UrlDecode(item.Trim()), CultureInfo.InvariantCulture));
				audience = strs["Audience"];
				if (string.IsNullOrWhiteSpace(item))
				{
					TokenProviderHelper.ThrowException(Microsoft.ServiceBus.SR.GetString(Resources.TokenProviderEmptyAudience, new object[0]));
					return;
				}
			}
		}

		internal static string ExtractSolutionFromHostname(string hostname)
		{
			if (string.IsNullOrEmpty(hostname))
			{
				throw new ArgumentException(SRClient.NullHostname);
			}
			string lowerInvariant = hostname.ToLowerInvariant();
			string str = RelayEnvironment.RelayHostRootName.ToLowerInvariant();
			if (!lowerInvariant.EndsWith(str, StringComparison.Ordinal))
			{
				throw new ArgumentException(SRClient.MismatchServiceBusDomain(lowerInvariant, str));
			}
			string str1 = lowerInvariant.Replace(str, string.Empty);
			char[] chrArray = new char[] { '.' };
			string[] strArrays = str1.Split(chrArray, StringSplitOptions.RemoveEmptyEntries);
			if ((int)strArrays.Length > 1)
			{
				throw new ArgumentException(SRClient.UnsupportedServiceBusDomainPrefix(lowerInvariant, str));
			}
			string empty = string.Empty;
			if ((int)strArrays.Length == 1)
			{
				empty = strArrays[0];
			}
			return empty;
		}

		internal static TokenProviderHelper.TokenResult<SecurityToken> GetAccessTokenByAssertion(Uri requestUri, string appliesTo, string requestToken, string simpleAuthAssertionFormat, TimeSpan timeout)
		{
			string str;
			string str1;
			string accessTokenCore = TokenProviderHelper.GetAccessTokenCore(requestUri, appliesTo, requestToken, simpleAuthAssertionFormat, timeout, out str, out str1);
			DateTime dateTime = TokenProviderHelper.ConvertExpiry(str);
			SimpleWebSecurityToken simpleWebSecurityToken = new SimpleWebSecurityToken(accessTokenCore, dateTime, str1);
			return new TokenProviderHelper.TokenResult<SecurityToken>()
			{
				CacheUntil = dateTime,
				Token = simpleWebSecurityToken
			};
		}

		private static string GetAccessTokenCore(Uri requestUri, string appliesTo, string requestToken, string simpleAuthAssertionFormat, TimeSpan timeout, out string expiresIn, out string audience)
		{
			StringBuilder stringBuilder = new StringBuilder();
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] objArray = new object[] { "wrap_scope", HttpUtility.UrlEncode(appliesTo) };
			stringBuilder.AppendFormat(invariantCulture, "{0}={1}", objArray);
			stringBuilder.Append('&');
			CultureInfo cultureInfo = CultureInfo.InvariantCulture;
			object[] objArray1 = new object[] { "wrap_assertion_format", simpleAuthAssertionFormat };
			stringBuilder.AppendFormat(cultureInfo, "{0}={1}", objArray1);
			stringBuilder.Append('&');
			CultureInfo invariantCulture1 = CultureInfo.InvariantCulture;
			object[] objArray2 = new object[] { "wrap_assertion", HttpUtility.UrlEncode(requestToken) };
			stringBuilder.AppendFormat(invariantCulture1, "{0}={1}", objArray2);
			byte[] bytes = Encoding.UTF8.GetBytes(stringBuilder.ToString());
			string str = null;
			expiresIn = null;
			audience = null;
			try
			{
				HttpWebRequest servicePointMaxIdleTimeMilliSeconds = WebRequest.Create(requestUri) as HttpWebRequest;
				servicePointMaxIdleTimeMilliSeconds.ServicePoint.MaxIdleTime = Constants.ServicePointMaxIdleTimeMilliSeconds;
				servicePointMaxIdleTimeMilliSeconds.AllowAutoRedirect = true;
				servicePointMaxIdleTimeMilliSeconds.MaximumAutomaticRedirections = 1;
				servicePointMaxIdleTimeMilliSeconds.Method = "POST";
				servicePointMaxIdleTimeMilliSeconds.ContentType = "application/x-www-form-urlencoded";
				servicePointMaxIdleTimeMilliSeconds.ContentLength = (long)((int)bytes.Length);
				try
				{
					servicePointMaxIdleTimeMilliSeconds.Timeout = Convert.ToInt32(timeout.TotalMilliseconds, CultureInfo.InvariantCulture);
				}
				catch (OverflowException overflowException)
				{
					throw new ArgumentException(SRClient.TimeoutExceeded, overflowException);
				}
				using (Stream requestStream = servicePointMaxIdleTimeMilliSeconds.GetRequestStream())
				{
					requestStream.Write(bytes, 0, (int)bytes.Length);
				}
				using (HttpWebResponse response = (HttpWebResponse)servicePointMaxIdleTimeMilliSeconds.GetResponse())
				{
					using (Stream responseStream = response.GetResponseStream())
					{
						using (StreamReader streamReader = new StreamReader(responseStream, Encoding.UTF8))
						{
							TokenProviderHelper.ExtractAccessToken(streamReader.ReadToEnd(), out str, out expiresIn, out audience);
						}
					}
				}
			}
			catch (ArgumentException argumentException)
			{
				TokenProviderHelper.ThrowException(requestUri, argumentException);
			}
			catch (WebException webException)
			{
				TokenProviderHelper.ThrowException(requestUri, webException);
			}
			return str;
		}

		internal static TokenProviderHelper.TokenResult<string> GetHttpAuthAccessTokenByAssertion(Uri requestUri, string appliesTo, string requestToken, string simpleAuthAssertionFormat, TimeSpan timeout)
		{
			string str;
			string str1;
			string accessTokenCore = TokenProviderHelper.GetAccessTokenCore(requestUri, appliesTo, requestToken, simpleAuthAssertionFormat, timeout, out str, out str1);
			DateTime dateTime = TokenProviderHelper.ConvertExpiry(str);
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] objArray = new object[] { "WRAP", "access_token", accessTokenCore };
			string str2 = string.Format(invariantCulture, "{0} {1}=\"{2}\"", objArray);
			return new TokenProviderHelper.TokenResult<string>()
			{
				CacheUntil = dateTime,
				Token = str2
			};
		}

		public static string GetOAuthAccessTokenCore(IEnumerator<Uri> stsUris, Func<Uri, Uri> uriBuilder, string requestToken, TimeSpan timeout, out DateTime expiresIn, out string audience)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(requestToken);
			string end = null;
			expiresIn = DateTime.MinValue;
			audience = null;
			bool flag = stsUris.MoveNext();
			while (flag)
			{
				Uri uri = uriBuilder(stsUris.Current);
				try
				{
					HttpWebRequest servicePointMaxIdleTimeMilliSeconds = WebRequest.Create(uri) as HttpWebRequest;
					servicePointMaxIdleTimeMilliSeconds.ServicePoint.MaxIdleTime = Constants.ServicePointMaxIdleTimeMilliSeconds;
					servicePointMaxIdleTimeMilliSeconds.AllowAutoRedirect = true;
					servicePointMaxIdleTimeMilliSeconds.MaximumAutomaticRedirections = 1;
					servicePointMaxIdleTimeMilliSeconds.Method = "POST";
					servicePointMaxIdleTimeMilliSeconds.ContentType = "application/x-www-form-urlencoded";
					servicePointMaxIdleTimeMilliSeconds.ContentLength = (long)((int)bytes.Length);
					servicePointMaxIdleTimeMilliSeconds.Timeout = TokenProviderHelper.ConvertToInt32(timeout);
					using (Stream requestStream = servicePointMaxIdleTimeMilliSeconds.GetRequestStream())
					{
						requestStream.Write(bytes, 0, (int)bytes.Length);
					}
					using (HttpWebResponse response = servicePointMaxIdleTimeMilliSeconds.GetResponse() as HttpWebResponse)
					{
						using (Stream responseStream = response.GetResponseStream())
						{
							using (StreamReader streamReader = new StreamReader(responseStream, Encoding.UTF8))
							{
								end = streamReader.ReadToEnd();
								TokenProviderHelper.ExtractExpiresInAndAudience(end, out expiresIn, out audience);
							}
						}
					}
					flag = false;
				}
				catch (ArgumentException argumentException)
				{
					TokenProviderHelper.ThrowException(uri, argumentException);
				}
				catch (WebException webException1)
				{
					WebException webException = webException1;
					flag = stsUris.MoveNext();
					if (!flag)
					{
						TokenProviderHelper.ThrowException(uri, webException);
					}
				}
			}
			return end;
		}

		internal static Uri GetStsUri(Uri stsUri, string appliesTo)
		{
			if (stsUri != null)
			{
				return new Uri(stsUri, "WRAPv0.9/");
			}
			string str = TokenProviderHelper.ExtractSolutionFromHostname((new Uri(appliesTo)).Host);
			return ServiceBusEnvironment.CreateAccessControlUri(str);
		}

		public static string GetWindowsAccessTokenCore(IEnumerator<Uri> stsUris, Func<Uri, Uri> uriBuilder, string requestToken, TimeSpan timeout, out DateTime expiresIn, out string audience)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(requestToken);
			string end = null;
			expiresIn = DateTime.MinValue;
			audience = null;
			bool flag = stsUris.MoveNext();
			while (flag)
			{
				Uri uri = uriBuilder(stsUris.Current);
				try
				{
					HttpWebRequest servicePointMaxIdleTimeMilliSeconds = WebRequest.Create(uri) as HttpWebRequest;
					servicePointMaxIdleTimeMilliSeconds.ServicePoint.MaxIdleTime = Constants.ServicePointMaxIdleTimeMilliSeconds;
					servicePointMaxIdleTimeMilliSeconds.AllowAutoRedirect = true;
					servicePointMaxIdleTimeMilliSeconds.MaximumAutomaticRedirections = 1;
					servicePointMaxIdleTimeMilliSeconds.Method = "POST";
					servicePointMaxIdleTimeMilliSeconds.ContentType = "application/x-www-form-urlencoded";
					servicePointMaxIdleTimeMilliSeconds.ContentLength = (long)((int)bytes.Length);
					servicePointMaxIdleTimeMilliSeconds.Timeout = TokenProviderHelper.ConvertToInt32(timeout);
					servicePointMaxIdleTimeMilliSeconds.UseDefaultCredentials = true;
					AuthenticationManager.CustomTargetNameDictionary[uri.AbsoluteUri] = "HTTP\\";
					using (Stream requestStream = servicePointMaxIdleTimeMilliSeconds.GetRequestStream())
					{
						requestStream.Write(bytes, 0, (int)bytes.Length);
					}
					using (HttpWebResponse response = servicePointMaxIdleTimeMilliSeconds.GetResponse() as HttpWebResponse)
					{
						using (Stream responseStream = response.GetResponseStream())
						{
							using (StreamReader streamReader = new StreamReader(responseStream, Encoding.UTF8))
							{
								end = streamReader.ReadToEnd();
								TokenProviderHelper.ExtractExpiresInAndAudience(end, out expiresIn, out audience);
							}
						}
					}
					flag = false;
				}
				catch (ArgumentException argumentException)
				{
					TokenProviderHelper.ThrowException(uri, argumentException);
				}
				catch (WebException webException1)
				{
					WebException webException = webException1;
					flag = stsUris.MoveNext();
					if (!flag)
					{
						TokenProviderHelper.ThrowException(uri, webException);
					}
				}
			}
			return end;
		}

		private static void ThrowException(string message)
		{
			SecurityTokenException securityTokenException = new SecurityTokenException(message);
			if (DiagnosticUtility.ShouldTraceInformation)
			{
				DiagnosticUtility.ExceptionUtility.TraceHandledException(securityTokenException, TraceEventType.Information);
			}
			throw securityTokenException;
		}

		private static void ThrowException(Uri requestUri, WebException exception)
		{
			throw TokenProviderHelper.ConvertWebException(requestUri, exception);
		}

		private static void ThrowException(Uri requestUri, Exception innerException)
		{
			throw TokenProviderHelper.ConvertException(requestUri, innerException);
		}

		[Serializable]
		internal class InternalSecurityTokenException : SecurityTokenException
		{
			public HttpStatusCode StatusCode
			{
				get;
				private set;
			}

			public InternalSecurityTokenException(string message, HttpStatusCode statusCode, Exception innerException) : base(message, innerException)
			{
				this.StatusCode = statusCode;
			}
		}

		private sealed class StreamReaderAsyncResult : IteratorAsyncResult<TokenProviderHelper.StreamReaderAsyncResult>
		{
			private const int ReadBufferSize = 1024;

			private readonly Stream inputStream;

			private readonly MemoryStream outputStream;

			private readonly byte[] buffer;

			private int bytesRead;

			public StreamReaderAsyncResult(Stream inputStream, MemoryStream outputStream, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.inputStream = inputStream;
				this.outputStream = outputStream;
				this.buffer = new byte[1024];
				this.bytesRead = -1;
				base.Start();
			}

			protected override IEnumerator<IteratorAsyncResult<TokenProviderHelper.StreamReaderAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				while (this.bytesRead != 0)
				{
					TokenProviderHelper.StreamReaderAsyncResult streamReaderAsyncResult = this;
					IteratorAsyncResult<TokenProviderHelper.StreamReaderAsyncResult>.BeginCall beginCall = (TokenProviderHelper.StreamReaderAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.inputStream.BeginRead(thisPtr.buffer, 0, 1024, c, s);
					yield return streamReaderAsyncResult.CallAsync(beginCall, (TokenProviderHelper.StreamReaderAsyncResult thisPtr, IAsyncResult r) => thisPtr.bytesRead = thisPtr.inputStream.EndRead(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
					this.outputStream.Write(this.buffer, 0, this.bytesRead);
				}
			}
		}

		private sealed class TokenRequestAsyncResult : IteratorAsyncResult<TokenProviderHelper.TokenRequestAsyncResult>
		{
			private readonly static Action<object> onCancelTimer;

			private readonly Uri requestUri;

			private readonly string appliesTo;

			private readonly string requestToken;

			private readonly string simpleAuthAssertionFormat;

			private Stream requestStream;

			private HttpWebRequest request;

			private HttpWebResponse response;

			private string expiresIn;

			private string audience;

			private string accessToken;

			private byte[] body;

			private Stream sourceStream;

			private MemoryStream tmpStream;

			private IOThreadTimer requestCancelTimer;

			private volatile bool requestTimedOut;

			public string AccessToken
			{
				get
				{
					return this.accessToken;
				}
			}

			public string Audience
			{
				get
				{
					return this.audience;
				}
			}

			public string ExpiresIn
			{
				get
				{
					return this.expiresIn;
				}
			}

			static TokenRequestAsyncResult()
			{
				TokenProviderHelper.TokenRequestAsyncResult.onCancelTimer = new Action<object>(TokenProviderHelper.TokenRequestAsyncResult.OnCancelTimer);
			}

			public TokenRequestAsyncResult(Uri requestUri, string appliesTo, string requestToken, string simpleAuthAssertionFormat, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.requestUri = requestUri;
				this.appliesTo = appliesTo;
				this.requestToken = requestToken;
				this.simpleAuthAssertionFormat = simpleAuthAssertionFormat;
				base.Start();
			}

			private Exception ConvertException(Exception exception)
			{
				if (exception is ArgumentException)
				{
					return TokenProviderHelper.ConvertException(this.requestUri, exception);
				}
				if (exception is WebException)
				{
					return TokenProviderHelper.ConvertWebException(this.requestUri, (WebException)exception);
				}
				if (!(exception is IOException) || !this.requestTimedOut)
				{
					return exception;
				}
				return new TimeoutException(SRClient.OperationRequestTimedOut(base.OriginalTimeout), exception);
			}

			protected override IEnumerator<IteratorAsyncResult<TokenProviderHelper.TokenRequestAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				try
				{
					StringBuilder stringBuilder = new StringBuilder();
					CultureInfo invariantCulture = CultureInfo.InvariantCulture;
					object[] objArray = new object[] { "wrap_scope", HttpUtility.UrlEncode(this.appliesTo) };
					stringBuilder.AppendFormat(invariantCulture, "{0}={1}", objArray);
					stringBuilder.Append('&');
					CultureInfo cultureInfo = CultureInfo.InvariantCulture;
					object[] objArray1 = new object[] { "wrap_assertion_format", this.simpleAuthAssertionFormat };
					stringBuilder.AppendFormat(cultureInfo, "{0}={1}", objArray1);
					stringBuilder.Append('&');
					CultureInfo invariantCulture1 = CultureInfo.InvariantCulture;
					object[] objArray2 = new object[] { "wrap_assertion", HttpUtility.UrlEncode(this.requestToken) };
					stringBuilder.AppendFormat(invariantCulture1, "{0}={1}", objArray2);
					this.body = Encoding.UTF8.GetBytes(stringBuilder.ToString());
					this.request = (HttpWebRequest)WebRequest.Create(this.requestUri);
					this.request.ServicePoint.MaxIdleTime = Constants.ServicePointMaxIdleTimeMilliSeconds;
					this.request.AllowAutoRedirect = true;
					this.request.MaximumAutomaticRedirections = 1;
					this.request.Method = "POST";
					this.request.ContentType = "application/x-www-form-urlencoded";
					this.request.ContentLength = (long)((int)this.body.Length);
					try
					{
						HttpWebRequest num = this.request;
						TimeSpan timeSpan = base.RemainingTime();
						num.Timeout = Convert.ToInt32(timeSpan.TotalMilliseconds, CultureInfo.InvariantCulture);
					}
					catch (OverflowException overflowException)
					{
						throw new ArgumentException(SRClient.TimeoutExceeded, overflowException);
					}
				}
				catch (ArgumentException argumentException)
				{
					base.Complete(this.ConvertException(argumentException));
					goto Label0;
				}
				try
				{
					this.requestCancelTimer = new IOThreadTimer(TokenProviderHelper.TokenRequestAsyncResult.onCancelTimer, this, true);
					try
					{
						TimeSpan timeSpan1 = base.RemainingTime();
						if (timeSpan1 > TimeSpan.Zero)
						{
							TokenProviderHelper.TokenRequestAsyncResult tokenRequestAsyncResult = this;
							IteratorAsyncResult<TokenProviderHelper.TokenRequestAsyncResult>.BeginCall beginCall = (TokenProviderHelper.TokenRequestAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => {
								IAsyncResult asyncResult = thisPtr.request.BeginGetRequestStream(c, s);
								thisPtr.requestCancelTimer.Set(timeSpan1);
								return asyncResult;
							};
							yield return tokenRequestAsyncResult.CallAsync(beginCall, (TokenProviderHelper.TokenRequestAsyncResult thisPtr, IAsyncResult r) => thisPtr.requestStream = thisPtr.request.EndGetRequestStream(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
							if (base.LastAsyncStepException == null)
							{
								TokenProviderHelper.TokenRequestAsyncResult tokenRequestAsyncResult1 = this;
								IteratorAsyncResult<TokenProviderHelper.TokenRequestAsyncResult>.BeginCall beginCall1 = (TokenProviderHelper.TokenRequestAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.requestStream.BeginWrite(thisPtr.body, 0, (int)thisPtr.body.Length, c, s);
								yield return tokenRequestAsyncResult1.CallAsync(beginCall1, (TokenProviderHelper.TokenRequestAsyncResult thisPtr, IAsyncResult r) => thisPtr.requestStream.EndWrite(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
								if (base.LastAsyncStepException == null)
								{
									goto Label1;
								}
								base.Complete(this.ConvertException(base.LastAsyncStepException));
								goto Label0;
							}
							else
							{
								base.Complete(this.ConvertException(base.LastAsyncStepException));
								goto Label0;
							}
						}
						else
						{
							base.Complete(new TimeoutException(SRClient.OperationRequestTimedOut(base.OriginalTimeout)));
							goto Label0;
						}
					}
					finally
					{
						if (this.requestStream != null)
						{
							this.requestStream.Dispose();
						}
						this.requestCancelTimer.Cancel();
					}
				Label1:
					TimeSpan timeSpan2 = base.RemainingTime();
					if (timeSpan2 > TimeSpan.Zero)
					{
						TokenProviderHelper.TokenRequestAsyncResult tokenRequestAsyncResult2 = this;
						IteratorAsyncResult<TokenProviderHelper.TokenRequestAsyncResult>.BeginCall beginCall2 = (TokenProviderHelper.TokenRequestAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => {
							IAsyncResult asyncResult = thisPtr.request.BeginGetResponse(c, s);
							thisPtr.requestCancelTimer.Set(timeSpan2);
							return asyncResult;
						};
						yield return tokenRequestAsyncResult2.CallAsync(beginCall2, (TokenProviderHelper.TokenRequestAsyncResult thisPtr, IAsyncResult r) => thisPtr.response = (HttpWebResponse)thisPtr.request.EndGetResponse(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
						if (base.LastAsyncStepException == null)
						{
							TokenProviderHelper.TokenRequestAsyncResult tokenRequestAsyncResult3 = this;
							Stream responseStream = this.response.GetResponseStream();
							Stream stream = responseStream;
							tokenRequestAsyncResult3.sourceStream = responseStream;
							using (stream)
							{
								TokenProviderHelper.TokenRequestAsyncResult tokenRequestAsyncResult4 = this;
								MemoryStream memoryStream = new MemoryStream();
								MemoryStream memoryStream1 = memoryStream;
								tokenRequestAsyncResult4.tmpStream = memoryStream;
								using (memoryStream1)
								{
									TokenProviderHelper.TokenRequestAsyncResult tokenRequestAsyncResult5 = this;
									IteratorAsyncResult<TokenProviderHelper.TokenRequestAsyncResult>.BeginCall streamReaderAsyncResult = (TokenProviderHelper.TokenRequestAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => new TokenProviderHelper.StreamReaderAsyncResult(thisPtr.sourceStream, thisPtr.tmpStream, t, c, s);
									yield return tokenRequestAsyncResult5.CallAsync(streamReaderAsyncResult, (TokenProviderHelper.TokenRequestAsyncResult thisPtr, IAsyncResult r) => AsyncResult<TokenProviderHelper.StreamReaderAsyncResult>.End(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
									if (base.LastAsyncStepException == null)
									{
										try
										{
											this.tmpStream.Position = (long)0;
											using (StreamReader streamReader = new StreamReader(this.tmpStream, Encoding.UTF8))
											{
												string end = streamReader.ReadToEnd();
												TokenProviderHelper.ExtractAccessToken(end, out this.accessToken, out this.expiresIn, out this.audience);
											}
										}
										catch (Exception exception1)
										{
											Exception exception = exception1;
											if (Fx.IsFatal(exception))
											{
												throw;
											}
											base.Complete(this.ConvertException(exception));
										}
									}
									else
									{
										base.Complete(this.ConvertException(base.LastAsyncStepException));
									}
								}
							}
						}
						else
						{
							base.Complete(this.ConvertException(base.LastAsyncStepException));
						}
					}
					else
					{
						base.Complete(new TimeoutException(SRClient.OperationRequestTimedOut(base.OriginalTimeout)));
					}
				}
				finally
				{
					if (this.requestCancelTimer != null)
					{
						this.requestCancelTimer.Cancel();
					}
				}
			Label0:
				yield break;
			}

			private static void OnCancelTimer(object state)
			{
				TokenProviderHelper.TokenRequestAsyncResult tokenRequestAsyncResult = (TokenProviderHelper.TokenRequestAsyncResult)state;
				tokenRequestAsyncResult.requestTimedOut = true;
				tokenRequestAsyncResult.request.Abort();
			}
		}

		internal class TokenResult<TToken>
		{
			public DateTime CacheUntil
			{
				get;
				set;
			}

			public TToken Token
			{
				get;
				set;
			}

			public TokenResult()
			{
			}
		}
	}
}