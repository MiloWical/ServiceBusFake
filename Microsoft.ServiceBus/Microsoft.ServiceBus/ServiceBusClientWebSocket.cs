using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Transport;
using Microsoft.ServiceBus.Properties;
using Microsoft.ServiceBus.Security;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Microsoft.ServiceBus
{
	internal class ServiceBusClientWebSocket
	{
		private const string HttpGetHeaderFormat = "GET {0} HTTP/1.1\r\n";

		private const string HttpConnectMethod = "CONNECT";

		private const string Http10 = "HTTP/1.0";

		private const string EndOfLineSuffix = "\r\n";

		private const byte FIN = 128;

		private const byte RSV = 0;

		private const byte Mask = 128;

		private const byte PayloadMask = 127;

		private const byte Continuation = 0;

		private const byte Text = 1;

		private const byte Binary = 2;

		private const byte Close = 8;

		private const byte Ping = 9;

		private const byte Pong = 10;

		private const byte MediumSizeFrame = 126;

		private const byte LargeSizeFrame = 127;

		private readonly static byte[] maskingKey;

		private readonly AsyncCallback onWriteComplete;

		private readonly string webSocketRole;

		private readonly string requestPath;

		public EndPoint LocalEndpoint
		{
			get
			{
				System.Net.Sockets.TcpClient tcpClient = this.TcpClient;
				if (tcpClient != null)
				{
					Socket client = tcpClient.Client;
					if (client != null)
					{
						return client.LocalEndPoint;
					}
				}
				return null;
			}
		}

		public EndPoint RemoteEndpoint
		{
			get
			{
				System.Net.Sockets.TcpClient tcpClient = this.TcpClient;
				if (tcpClient != null)
				{
					Socket client = tcpClient.Client;
					if (client != null)
					{
						return client.RemoteEndPoint;
					}
				}
				return null;
			}
		}

		internal ServiceBusClientWebSocket.WebSocketState State
		{
			get;
			private set;
		}

		private System.Net.Sockets.TcpClient TcpClient
		{
			get;
			set;
		}

		private Stream WebSocketStream
		{
			get;
			set;
		}

		static ServiceBusClientWebSocket()
		{
			ServiceBusClientWebSocket.maskingKey = new byte[4];
		}

		public ServiceBusClientWebSocket(string webSocketRole) : this(webSocketRole, "/$servicebus/websocket")
		{
		}

		public ServiceBusClientWebSocket(string webSocketRole, string requestPath)
		{
			this.State = ServiceBusClientWebSocket.WebSocketState.Initial;
			this.webSocketRole = webSocketRole;
			this.requestPath = requestPath;
			this.onWriteComplete = new AsyncCallback(this.OnWriteComplete);
		}

		public void Abort()
		{
			if (this.State == ServiceBusClientWebSocket.WebSocketState.Aborted || this.State == ServiceBusClientWebSocket.WebSocketState.Closed || this.State == ServiceBusClientWebSocket.WebSocketState.Faulted)
			{
				return;
			}
			this.State = ServiceBusClientWebSocket.WebSocketState.Aborted;
			try
			{
				if (this.WebSocketStream != null)
				{
					this.WebSocketStream.Close();
				}
				this.WebSocketStream = null;
				if (this.TcpClient != null)
				{
					this.TcpClient.Close();
				}
				this.TcpClient = null;
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				Fx.Exception.TraceHandled(exception, "ServiceBusClientWebSocket.Abort", null);
			}
		}

		public IAsyncResult BeginClose(AsyncCallback callback, object state)
		{
			this.State = ServiceBusClientWebSocket.WebSocketState.Closed;
			return (new ServiceBusClientWebSocket.CloseAsyncResult(this.WebSocketStream, this.TcpClient, callback, state)).Start();
		}

		public IAsyncResult BeginConnect(string host, int port, TimeSpan timeout, AsyncCallback callback, object state)
		{
			Fx.AssertAndThrow(this.State == ServiceBusClientWebSocket.WebSocketState.Initial, Microsoft.ServiceBus.SR.GetString(Resources.ClientWebSocketNotInInitialState, new object[0]));
			this.State = ServiceBusClientWebSocket.WebSocketState.Connecting;
			return (new ServiceBusClientWebSocket.ConnectAsyncResult(host, port, timeout, this.webSocketRole, this.requestPath, callback, state)).Start();
		}

		public IAsyncResult BeginReceive(byte[] buffer, int offset, int size, TimeSpan timeout, AsyncCallback callback, object state)
		{
			Fx.AssertAndThrow(this.State == ServiceBusClientWebSocket.WebSocketState.Open, Microsoft.ServiceBus.SR.GetString(Resources.ClientWebSocketNotInOpenStateDuringReceive, new object[0]));
			this.TcpClient.Client.ReceiveTimeout = TimeoutHelper.ToMilliseconds(timeout);
			return (new ServiceBusClientWebSocket.ReceiveAsyncResult(this.WebSocketStream, buffer, offset, timeout, callback, state)).Start();
		}

		public IAsyncResult BeginSend(byte[] buffer, int offset, int size, ServiceBusClientWebSocket.WebSocketMessageType webSocketMessageType, TimeSpan timeout, AsyncCallback callback, object state)
		{
			Fx.AssertAndThrow(this.State == ServiceBusClientWebSocket.WebSocketState.Open, Microsoft.ServiceBus.SR.GetString(Resources.ClientWebSocketNotInOpenStateDuringSend, new object[0]));
			this.TcpClient.Client.SendTimeout = TimeoutHelper.ToMilliseconds(timeout);
			return (new ServiceBusClientWebSocket.SendAsyncResult(this.WebSocketStream, buffer, offset, size, webSocketMessageType, timeout, callback, state)).Start();
		}

		private static bool CustomizedCertificateValidator(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			return SecureSocketUtil.CustomizedCertificateValidator(sender, certificate, chain, sslPolicyErrors, RelayEnvironment.RelayHostRootName);
		}

		public void EndClose(IAsyncResult result)
		{
			bool flag = false;
			try
			{
				try
				{
					AsyncResult<ServiceBusClientWebSocket.CloseAsyncResult>.End(result);
					if (this.WebSocketStream != null)
					{
						this.WebSocketStream.Close();
					}
					if (this.TcpClient != null)
					{
						this.TcpClient.Close();
					}
					flag = true;
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					Fx.Exception.TraceHandled(exception, "ServiceBusClientWebSocket.EndClose", null);
				}
			}
			finally
			{
				if (!flag)
				{
					this.Fault();
				}
			}
		}

		public void EndConnect(IAsyncResult result)
		{
			bool flag = false;
			try
			{
				ServiceBusClientWebSocket.ConnectAsyncResult connectAsyncResult = AsyncResult<ServiceBusClientWebSocket.ConnectAsyncResult>.End(result);
				this.WebSocketStream = connectAsyncResult.WebSocketStream;
				this.TcpClient = connectAsyncResult.TcpClient;
				this.State = ServiceBusClientWebSocket.WebSocketState.Open;
				flag = true;
			}
			finally
			{
				if (!flag)
				{
					this.Fault();
				}
			}
		}

		public int EndReceive(IAsyncResult result)
		{
			int totalBytesRead;
			bool flag = false;
			try
			{
				ServiceBusClientWebSocket.ReceiveAsyncResult receiveAsyncResult = AsyncResult<ServiceBusClientWebSocket.ReceiveAsyncResult>.End(result);
				flag = true;
				if (receiveAsyncResult.ConnectionClosed)
				{
					this.State = ServiceBusClientWebSocket.WebSocketState.Closed;
					if (this.WebSocketStream != null)
					{
						this.WebSocketStream.Close();
					}
					if (this.TcpClient != null)
					{
						this.TcpClient.Close();
					}
				}
				totalBytesRead = receiveAsyncResult.TotalBytesRead;
			}
			finally
			{
				if (!flag)
				{
					this.Fault();
				}
			}
			return totalBytesRead;
		}

		public void EndSend(IAsyncResult result)
		{
			bool flag = false;
			try
			{
				AsyncResult<ServiceBusClientWebSocket.SendAsyncResult>.End(result);
				flag = true;
			}
			finally
			{
				if (!flag)
				{
					this.Fault();
				}
			}
		}

		private void Fault()
		{
			this.State = ServiceBusClientWebSocket.WebSocketState.Faulted;
			if (this.WebSocketStream != null)
			{
				this.WebSocketStream.Close();
				this.WebSocketStream = null;
			}
			if (this.TcpClient != null)
			{
				this.TcpClient.Close();
				this.TcpClient = null;
			}
		}

		private static int GetSocketTimeoutInMilliSeconds(TimeSpan timeout)
		{
			if (timeout == TimeSpan.MaxValue)
			{
				return -1;
			}
			if (timeout == TimeSpan.Zero)
			{
				return 1;
			}
			long num = Ticks.FromTimeSpan(timeout);
			if (num / (long)10000 > (long)2147483647)
			{
				return 2147483647;
			}
			return Ticks.ToMilliseconds(num);
		}

		private void HandleOperationComplete(IAsyncResult result, bool syncComplete)
		{
			TransportAsyncCallbackArgs asyncState = null;
			try
			{
				asyncState = (TransportAsyncCallbackArgs)result.AsyncState;
				this.WebSocketStream.EndWrite(result);
				asyncState.BytesTransfered = asyncState.Count;
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				asyncState.Exception = exception;
			}
			asyncState.CompletedSynchronously = syncComplete;
			if (!syncComplete && asyncState.CompletedCallback != null)
			{
				asyncState.CompletedCallback(asyncState);
			}
		}

		public static bool IsSupportingScheme(Uri factoryEndpointUri, out Exception exception)
		{
			bool clientWebSocket;
			exception = null;
			try
			{
				using (WebSocketConnectAsyncResult webSocketConnectAsyncResult = (new WebSocketConnectAsyncResult(factoryEndpointUri, TimeSpan.FromMinutes(1), "wsrelayedconnection", null, null)).RunSynchronously())
				{
					clientWebSocket = webSocketConnectAsyncResult.ClientWebSocket != null;
				}
			}
			catch (Exception exception2)
			{
				Exception exception1 = exception2;
				if (Fx.IsFatal(exception1))
				{
					throw;
				}
				exception = exception1;
				clientWebSocket = false;
			}
			return clientWebSocket;
		}

		private static void MaskWebSocketData(byte[] buffer, int offset, int size)
		{
			ConnectionUtilities.ValidateBufferBounds(buffer, offset, size);
			for (int i = 0; i < size; i++)
			{
				buffer[i + offset] = (byte)(buffer[i + offset] ^ ServiceBusClientWebSocket.maskingKey[i % 4]);
			}
		}

		private void OnWriteComplete(IAsyncResult result)
		{
			if (!result.CompletedSynchronously)
			{
				this.HandleOperationComplete(result, false);
			}
		}

		private static bool ParseWebSocketFrameHeader(byte[] buffer, out byte payloadLength, out bool pongFrame)
		{
			payloadLength = 0;
			if ((buffer[0] & 128) != 128)
			{
				throw Fx.Exception.AsError(new NotImplementedException("Client Websocket implementation lacks fragmentation support"), null);
			}
			bool flag = true;
			int num = buffer[0] & 15;
			pongFrame = false;
			switch (num)
			{
				case 0:
				{
					if (!flag)
					{
						if ((buffer[1] & 128) == 128)
						{
							return false;
						}
						payloadLength = (byte)(buffer[1] & 127);
						return true;
					}
					return false;
				}
				case 1:
				case 2:
				{
					if ((buffer[1] & 128) == 128)
					{
						return false;
					}
					payloadLength = (byte)(buffer[1] & 127);
					return true;
				}
				case 3:
				case 4:
				case 5:
				case 6:
				case 7:
				{
					return false;
				}
				case 8:
				{
					return false;
				}
				case 9:
				{
					throw Fx.Exception.AsError(new NotImplementedException("Client Websocket implementation lacks ping message support"), null);
				}
				case 10:
				{
					pongFrame = true;
					if ((buffer[1] & 128) == 128)
					{
						return false;
					}
					payloadLength = (byte)(buffer[1] & 127);
					return true;
				}
				default:
				{
					return false;
				}
			}
		}

		private byte[] PrepareBigByteArray(byte[] buffer, int offset, int count)
		{
			Fx.AssertAndThrow(count > 0, "count should be greater than zero");
			byte[] numArray = ServiceBusClientWebSocket.PrepareWebSocketHeader(count, ServiceBusClientWebSocket.WebSocketMessageType.Binary);
			byte[] numArray1 = DiagnosticUtility.Utility.AllocateByteArray((int)numArray.Length + count);
			Buffer.BlockCopy(numArray, 0, numArray1, 0, (int)numArray.Length);
			Buffer.BlockCopy(buffer, offset, numArray1, (int)numArray.Length, count);
			ServiceBusClientWebSocket.MaskWebSocketData(numArray1, (int)numArray.Length, count);
			return numArray1;
		}

		private static byte PrepareOctet0(ServiceBusClientWebSocket.WebSocketMessageType webSocketMessageType)
		{
			byte num = 128;
			if (!webSocketMessageType.Equals(ServiceBusClientWebSocket.WebSocketMessageType.Binary))
			{
				num = (!webSocketMessageType.Equals(ServiceBusClientWebSocket.WebSocketMessageType.Text) ? (byte)(num | 8) : (byte)(num | 1));
			}
			else
			{
				num = (byte)(num | 2);
			}
			return num;
		}

		private static byte[] PrepareWebSocketHeader(int bufferLength, ServiceBusClientWebSocket.WebSocketMessageType webSocketMessageType)
		{
			byte[] numArray;
			if (bufferLength >= 126)
			{
				numArray = (bufferLength > 65535 ? new byte[] { ServiceBusClientWebSocket.PrepareOctet0(webSocketMessageType), 255, 0, 0, 0, 0, (byte)(bufferLength >> 24 & 255), (byte)(bufferLength >> 16 & 255), (byte)(bufferLength >> 8 & 255), (byte)(bufferLength & 255), ServiceBusClientWebSocket.maskingKey[0], ServiceBusClientWebSocket.maskingKey[1], ServiceBusClientWebSocket.maskingKey[2], ServiceBusClientWebSocket.maskingKey[3] } : new byte[] { ServiceBusClientWebSocket.PrepareOctet0(webSocketMessageType), 254, (byte)(bufferLength >> 8 & 255), (byte)(bufferLength & 255), ServiceBusClientWebSocket.maskingKey[0], ServiceBusClientWebSocket.maskingKey[1], ServiceBusClientWebSocket.maskingKey[2], ServiceBusClientWebSocket.maskingKey[3] });
			}
			else
			{
				numArray = new byte[] { ServiceBusClientWebSocket.PrepareOctet0(webSocketMessageType), (byte)(bufferLength | 128), ServiceBusClientWebSocket.maskingKey[0], ServiceBusClientWebSocket.maskingKey[1], ServiceBusClientWebSocket.maskingKey[2], ServiceBusClientWebSocket.maskingKey[3] };
			}
			return numArray;
		}

		public bool WriteAsync(TransportAsyncCallbackArgs args)
		{
			byte[] numArray;
			if (args.Buffer != null)
			{
				numArray = this.PrepareBigByteArray(args.Buffer, args.Offset, args.Count);
			}
			else if (args.ByteBufferList.Count != 1)
			{
				byte[][] numArray1 = new byte[args.ByteBufferList.Count][];
				int length = 0;
				for (int i = 0; i < args.ByteBufferList.Count; i++)
				{
					numArray1[i] = ServiceBusClientWebSocket.PrepareWebSocketHeader(args.ByteBufferList[i].Length, ServiceBusClientWebSocket.WebSocketMessageType.Binary);
					length = length + (int)numArray1[i].Length;
				}
				numArray = DiagnosticUtility.Utility.AllocateByteArray(length + args.Count);
				int num = 0;
				for (int j = 0; j < args.ByteBufferList.Count; j++)
				{
					Buffer.BlockCopy(numArray1[j], 0, numArray, num, (int)numArray1[j].Length);
					num = num + (int)numArray1[j].Length;
					Buffer.BlockCopy(args.ByteBufferList[j].Buffer, args.ByteBufferList[j].Offset, numArray, num, args.ByteBufferList[j].Length);
					ServiceBusClientWebSocket.MaskWebSocketData(numArray, num, args.ByteBufferList[j].Length);
					num = num + args.ByteBufferList[j].Length;
				}
			}
			else
			{
				numArray = this.PrepareBigByteArray(args.ByteBufferList[0].Buffer, args.ByteBufferList[0].Offset, args.ByteBufferList[0].Length);
			}
			IAsyncResult asyncResult = this.WebSocketStream.BeginWrite(numArray, 0, (int)numArray.Length, this.onWriteComplete, args);
			bool completedSynchronously = asyncResult.CompletedSynchronously;
			if (completedSynchronously)
			{
				this.HandleOperationComplete(asyncResult, true);
			}
			return !completedSynchronously;
		}

		private sealed class CloseAsyncResult : IteratorAsyncResult<ServiceBusClientWebSocket.CloseAsyncResult>
		{
			private readonly Stream webSocketStream;

			private readonly System.Net.Sockets.TcpClient tcpClient;

			public CloseAsyncResult(Stream webSocketStream, System.Net.Sockets.TcpClient tcpClient, AsyncCallback callback, object state) : base(TimeSpan.FromSeconds(5), callback, state)
			{
				this.webSocketStream = webSocketStream;
				this.tcpClient = tcpClient;
			}

			protected override IEnumerator<IteratorAsyncResult<ServiceBusClientWebSocket.CloseAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				if (this.tcpClient.Connected)
				{
					byte[] numArray = ServiceBusClientWebSocket.PrepareWebSocketHeader(0, ServiceBusClientWebSocket.WebSocketMessageType.Close);
					ServiceBusClientWebSocket.CloseAsyncResult closeAsyncResult = this;
					IteratorAsyncResult<ServiceBusClientWebSocket.CloseAsyncResult>.BeginCall beginCall = (ServiceBusClientWebSocket.CloseAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.webSocketStream.BeginWrite(numArray, 0, (int)numArray.Length, c, s);
					yield return closeAsyncResult.CallAsync(beginCall, (ServiceBusClientWebSocket.CloseAsyncResult thisPtr, IAsyncResult r) => thisPtr.webSocketStream.EndWrite(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				}
			}
		}

		private sealed class ConnectAsyncResult : IteratorAsyncResult<ServiceBusClientWebSocket.ConnectAsyncResult>
		{
			private const string HostHeaderPrefix = "Host: ";

			private const string Separator = ": ";

			private const string Upgrade = "Upgrade";

			private const string Websocket = "websocket";

			private const string ConnectionHeaderName = "Connection";

			private readonly static SHA1CryptoServiceProvider sha1CryptoServiceProvider;

			private readonly int port;

			private readonly string webSocketRole;

			private readonly string requestPath;

			private string host;

			private string webSocketKey;

			internal System.Net.Sockets.TcpClient TcpClient
			{
				get;
				private set;
			}

			internal Stream WebSocketStream
			{
				get;
				private set;
			}

			static ConnectAsyncResult()
			{
				ServiceBusClientWebSocket.ConnectAsyncResult.sha1CryptoServiceProvider = ServiceBusClientWebSocket.ConnectAsyncResult.InitCryptoServiceProvider();
			}

			public ConnectAsyncResult(string host, int port, TimeSpan timeout, string webSocketRole, string requestPath, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.host = host;
				this.port = port;
				this.webSocketRole = webSocketRole;
				this.requestPath = requestPath;
			}

			private void Abort()
			{
				if (this.TcpClient != null)
				{
					if (this.WebSocketStream != null)
					{
						this.WebSocketStream.Close();
					}
					this.WebSocketStream = null;
					this.TcpClient.Close();
					this.TcpClient = null;
				}
			}

			private string BuildUpgradeRequest()
			{
				Guid guid = Guid.NewGuid();
				this.webSocketKey = Convert.ToBase64String(guid.ToByteArray());
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendFormat("GET {0} HTTP/1.1\r\n", this.requestPath);
				stringBuilder.Append("Host: ").Append(this.host).Append("\r\n");
				stringBuilder.Append("Upgrade").Append(": ").Append("websocket").Append("\r\n");
				stringBuilder.Append("Connection").Append(": ").Append("Upgrade").Append("\r\n");
				stringBuilder.Append("Sec-WebSocket-Key").Append(": ").Append(this.webSocketKey).Append("\r\n");
				if (!string.IsNullOrEmpty(this.webSocketRole))
				{
					stringBuilder.Append("Sec-WebSocket-Protocol").Append(": ").Append(this.webSocketRole).Append("\r\n");
				}
				stringBuilder.Append("Sec-WebSocket-Version").Append(": ").Append("13").Append("\r\n");
				stringBuilder.Append("\r\n");
				return stringBuilder.ToString();
			}

			private static string ComputeHash(string key)
			{
				byte[] numArray;
				string str = string.Concat(key, "258EAFA5-E914-47DA-95CA-C5AB0DC85B11");
				byte[] bytes = Encoding.ASCII.GetBytes(str);
				lock (ServiceBusClientWebSocket.ConnectAsyncResult.sha1CryptoServiceProvider)
				{
					numArray = ServiceBusClientWebSocket.ConnectAsyncResult.sha1CryptoServiceProvider.ComputeHash(bytes);
				}
				return Convert.ToBase64String(numArray);
			}

			protected override IEnumerator<IteratorAsyncResult<ServiceBusClientWebSocket.ConnectAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				bool flag = false;
				try
				{
					if (this.host.Equals("127.0.0.1"))
					{
						this.host = RelayEnvironment.RelayHostRootName;
					}
					yield return this.GetConnectTcpStep();
					if (base.LastAsyncStepException == null)
					{
						SslStream sslStream = new SslStream(this.TcpClient.GetStream(), false, new RemoteCertificateValidationCallback(ServiceBusClientWebSocket.CustomizedCertificateValidator));
						yield return base.CallAsync((ServiceBusClientWebSocket.ConnectAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => sslStream.BeginAuthenticateAsClient(thisPtr.host, c, s), (ServiceBusClientWebSocket.ConnectAsyncResult thisPtr, IAsyncResult r) => sslStream.EndAuthenticateAsClient(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
						if (base.LastAsyncStepException == null)
						{
							this.WebSocketStream = sslStream;
							string str = this.BuildUpgradeRequest();
							byte[] bytes = Encoding.ASCII.GetBytes(str);
							this.TcpClient.Client.SendTimeout = ServiceBusClientWebSocket.GetSocketTimeoutInMilliSeconds(base.RemainingTime());
							ServiceBusClientWebSocket.ConnectAsyncResult connectAsyncResult = this;
							IteratorAsyncResult<ServiceBusClientWebSocket.ConnectAsyncResult>.BeginCall beginCall = (ServiceBusClientWebSocket.ConnectAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.WebSocketStream.BeginWrite(bytes, 0, (int)bytes.Length, c, s);
							yield return connectAsyncResult.CallAsync(beginCall, (ServiceBusClientWebSocket.ConnectAsyncResult thisPtr, IAsyncResult r) => thisPtr.WebSocketStream.EndWrite(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
							if (base.LastAsyncStepException == null)
							{
								byte[] numArray = new byte[8192];
								ServiceBusClientWebSocket.HttpResponse httpResponse = new ServiceBusClientWebSocket.HttpResponse(this.TcpClient, this.WebSocketStream, numArray);
								yield return base.CallAsync((ServiceBusClientWebSocket.ConnectAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => httpResponse.BeginRead(t, c, s), (ServiceBusClientWebSocket.ConnectAsyncResult thisPtr, IAsyncResult r) => httpResponse.EndRead(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
								if (base.LastAsyncStepException != null)
								{
									Exception lastAsyncStepException = base.LastAsyncStepException;
									if (this.TcpClient.Connected)
									{
										ServiceBusClientWebSocket.ConnectAsyncResult connectAsyncResult1 = this;
										IteratorAsyncResult<ServiceBusClientWebSocket.ConnectAsyncResult>.BeginCall beginCall1 = (ServiceBusClientWebSocket.ConnectAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.TcpClient.Client.BeginDisconnect(false, c, s);
										yield return connectAsyncResult1.CallAsync(beginCall1, (ServiceBusClientWebSocket.ConnectAsyncResult thisPtr, IAsyncResult r) => thisPtr.TcpClient.Client.EndDisconnect(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
									}
									base.Complete(new IOException(lastAsyncStepException.Message, lastAsyncStepException));
								}
								else if (httpResponse.StatusCode != HttpStatusCode.SwitchingProtocols)
								{
									if (this.TcpClient.Connected)
									{
										ServiceBusClientWebSocket.ConnectAsyncResult connectAsyncResult2 = this;
										IteratorAsyncResult<ServiceBusClientWebSocket.ConnectAsyncResult>.BeginCall beginCall2 = (ServiceBusClientWebSocket.ConnectAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.TcpClient.Client.BeginDisconnect(false, c, s);
										yield return connectAsyncResult2.CallAsync(beginCall2, (ServiceBusClientWebSocket.ConnectAsyncResult thisPtr, IAsyncResult r) => thisPtr.TcpClient.Client.EndDisconnect(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
									}
									base.Complete(new IOException(string.Concat(Resources.ServerRejectedUpgradeRequest, " ", httpResponse)));
								}
								else if (this.VerifyWebSocketUpgradeResponse(httpResponse.Headers))
								{
									flag = true;
								}
								else
								{
									if (this.TcpClient.Connected)
									{
										ServiceBusClientWebSocket.ConnectAsyncResult connectAsyncResult3 = this;
										IteratorAsyncResult<ServiceBusClientWebSocket.ConnectAsyncResult>.BeginCall beginCall3 = (ServiceBusClientWebSocket.ConnectAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.TcpClient.Client.BeginDisconnect(false, c, s);
										yield return connectAsyncResult3.CallAsync(beginCall3, (ServiceBusClientWebSocket.ConnectAsyncResult thisPtr, IAsyncResult r) => thisPtr.TcpClient.Client.EndDisconnect(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
									}
									base.Complete(new IOException(Resources.UpgradeProtocolNotSupported));
								}
							}
							else
							{
								Exception exception = base.LastAsyncStepException;
								if (this.TcpClient.Connected)
								{
									ServiceBusClientWebSocket.ConnectAsyncResult connectAsyncResult4 = this;
									IteratorAsyncResult<ServiceBusClientWebSocket.ConnectAsyncResult>.BeginCall beginCall4 = (ServiceBusClientWebSocket.ConnectAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.TcpClient.Client.BeginDisconnect(false, c, s);
									yield return connectAsyncResult4.CallAsync(beginCall4, (ServiceBusClientWebSocket.ConnectAsyncResult thisPtr, IAsyncResult r) => thisPtr.TcpClient.Client.EndDisconnect(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
								}
								base.Complete(new IOException(exception.Message, exception));
							}
						}
						else
						{
							Exception lastAsyncStepException1 = base.LastAsyncStepException;
							if (this.TcpClient.Connected)
							{
								ServiceBusClientWebSocket.ConnectAsyncResult connectAsyncResult5 = this;
								IteratorAsyncResult<ServiceBusClientWebSocket.ConnectAsyncResult>.BeginCall beginCall5 = (ServiceBusClientWebSocket.ConnectAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.TcpClient.Client.BeginDisconnect(false, c, s);
								yield return connectAsyncResult5.CallAsync(beginCall5, (ServiceBusClientWebSocket.ConnectAsyncResult thisPtr, IAsyncResult r) => thisPtr.TcpClient.Client.EndDisconnect(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
							}
							base.Complete(new IOException(lastAsyncStepException1.Message, lastAsyncStepException1));
						}
					}
					else
					{
						Exception exception1 = base.LastAsyncStepException;
						if (this.TcpClient != null && this.TcpClient.Connected)
						{
							ServiceBusClientWebSocket.ConnectAsyncResult connectAsyncResult6 = this;
							IteratorAsyncResult<ServiceBusClientWebSocket.ConnectAsyncResult>.BeginCall beginCall6 = (ServiceBusClientWebSocket.ConnectAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.TcpClient.Client.BeginDisconnect(false, c, s);
							yield return connectAsyncResult6.CallAsync(beginCall6, (ServiceBusClientWebSocket.ConnectAsyncResult thisPtr, IAsyncResult r) => thisPtr.TcpClient.Client.EndDisconnect(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
						}
						base.Complete(new IOException(exception1.Message, exception1));
					}
				}
				finally
				{
					if (!flag)
					{
						this.Abort();
					}
				}
			}

			private IteratorAsyncResult<ServiceBusClientWebSocket.ConnectAsyncResult>.AsyncStep GetConnectTcpStep()
			{
				Uri proxy;
				IWebProxy defaultWebProxy = WebRequest.DefaultWebProxy;
				Uri uri = (new UriBuilder(Uri.UriSchemeHttps, this.host, this.port)).Uri;
				if (defaultWebProxy != null)
				{
					proxy = defaultWebProxy.GetProxy(uri);
				}
				else
				{
					proxy = null;
				}
				Uri uri1 = proxy;
				if (!uri.Equals(uri1))
				{
					return base.CallAsync((ServiceBusClientWebSocket.ConnectAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => (new ServiceBusClientWebSocket.ConnectTcpProxyAsyncResult(uri, uri1, t, c, s)).Start(), (ServiceBusClientWebSocket.ConnectAsyncResult thisPtr, IAsyncResult r) => thisPtr.TcpClient = AsyncResult<ServiceBusClientWebSocket.ConnectTcpProxyAsyncResult>.End(r).TcpClient, IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				}
				this.TcpClient = new System.Net.Sockets.TcpClient();
				return base.CallAsync((ServiceBusClientWebSocket.ConnectAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.TcpClient.BeginConnect(thisPtr.host, thisPtr.port, c, s), (ServiceBusClientWebSocket.ConnectAsyncResult thisPtr, IAsyncResult r) => thisPtr.TcpClient.EndConnect(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
			}

			private static SHA1CryptoServiceProvider InitCryptoServiceProvider()
			{
				return new SHA1CryptoServiceProvider();
			}

			private bool VerifyWebSocketUpgradeResponse(NameValueCollection webSocketHeaders)
			{
				string str = webSocketHeaders.Get("Upgrade");
				string str1 = str;
				if (str == null)
				{
					return false;
				}
				if (!string.Equals(str1, "websocket", StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}
				string str2 = webSocketHeaders.Get("Connection");
				string str3 = str2;
				if (str2 == null)
				{
					return false;
				}
				if (!string.Equals(str3, "Upgrade", StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}
				string str4 = webSocketHeaders.Get("Sec-WebSocket-Accept");
				string str5 = str4;
				if (str4 == null)
				{
					return false;
				}
				if (!ServiceBusClientWebSocket.ConnectAsyncResult.ComputeHash(this.webSocketKey).Equals(str5, StringComparison.Ordinal))
				{
					return false;
				}
				if (!string.IsNullOrEmpty(this.webSocketRole))
				{
					string str6 = webSocketHeaders.Get("Sec-WebSocket-Protocol");
					string str7 = str6;
					if (str6 == null)
					{
						return false;
					}
					if (!this.webSocketRole.Equals(str7))
					{
						return false;
					}
				}
				return true;
			}
		}

		private sealed class ConnectTcpProxyAsyncResult : IteratorAsyncResult<ServiceBusClientWebSocket.ConnectTcpProxyAsyncResult>
		{
			private readonly static Action<AsyncResult, Exception> completing;

			private readonly Uri destinationAddress;

			private readonly Uri proxyAddress;

			private readonly byte[] buffer;

			private Microsoft.ServiceBus.Security.SafeFreeCredentials credentials;

			private Microsoft.ServiceBus.Security.SafeDeleteContext securityContext;

			private int byteCount;

			private Stream Stream
			{
				get;
				set;
			}

			public System.Net.Sockets.TcpClient TcpClient
			{
				get;
				private set;
			}

			static ConnectTcpProxyAsyncResult()
			{
				ServiceBusClientWebSocket.ConnectTcpProxyAsyncResult.completing = new Action<AsyncResult, Exception>(ServiceBusClientWebSocket.ConnectTcpProxyAsyncResult.Completing);
			}

			public ConnectTcpProxyAsyncResult(Uri destinationAddress, Uri proxyAddress, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.destinationAddress = destinationAddress;
				this.proxyAddress = proxyAddress;
				this.buffer = Fx.AllocateByteArray(8192);
				ServiceBusClientWebSocket.ConnectTcpProxyAsyncResult connectTcpProxyAsyncResult = this;
				connectTcpProxyAsyncResult.OnCompleting = (Action<AsyncResult, Exception>)Delegate.Combine(connectTcpProxyAsyncResult.OnCompleting, ServiceBusClientWebSocket.ConnectTcpProxyAsyncResult.completing);
			}

			private static string BuildConnectRequest(Uri address, string proxyAuthorization)
			{
				StringBuilder stringBuilder = new StringBuilder();
				CultureInfo invariantCulture = CultureInfo.InvariantCulture;
				object[] host = new object[] { "CONNECT", address.Host, address.Port, "HTTP/1.0", "\r\n" };
				stringBuilder.AppendFormat(invariantCulture, "{0} {1}:{2} {3}{4}", host);
				stringBuilder.Append("Proxy-Connection: Keep-Alive").Append("\r\n");
				if (!string.IsNullOrEmpty(proxyAuthorization))
				{
					stringBuilder.Append("Proxy-Authorization: ").Append(proxyAuthorization).Append("\r\n");
				}
				stringBuilder.Append("\r\n");
				return stringBuilder.ToString();
			}

			private static void Completing(AsyncResult asyncResult, Exception completeException)
			{
				ServiceBusClientWebSocket.ConnectTcpProxyAsyncResult connectTcpProxyAsyncResult = (ServiceBusClientWebSocket.ConnectTcpProxyAsyncResult)asyncResult;
				if (connectTcpProxyAsyncResult.credentials != null)
				{
					connectTcpProxyAsyncResult.credentials.Close();
				}
				if (connectTcpProxyAsyncResult.securityContext != null)
				{
					connectTcpProxyAsyncResult.securityContext.Close();
				}
				if (completeException != null)
				{
					if (connectTcpProxyAsyncResult.TcpClient != null)
					{
						connectTcpProxyAsyncResult.TcpClient.Close();
					}
					if (!(completeException is IOException))
					{
						throw Fx.Exception.AsWarning(new IOException(completeException.Message, completeException), connectTcpProxyAsyncResult.Activity);
					}
				}
			}

			protected override IEnumerator<IteratorAsyncResult<ServiceBusClientWebSocket.ConnectTcpProxyAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				bool flag = false;
				string str = null;
				do
				{
					if (this.Stream == null)
					{
						ServiceBusClientWebSocket.ConnectTcpProxyAsyncResult connectTcpProxyAsyncResult = this;
						System.Net.Sockets.TcpClient tcpClient = new System.Net.Sockets.TcpClient()
						{
							SendTimeout = ServiceBusClientWebSocket.GetSocketTimeoutInMilliSeconds(base.RemainingTime())
						};
						connectTcpProxyAsyncResult.TcpClient = tcpClient;
						ServiceBusClientWebSocket.ConnectTcpProxyAsyncResult connectTcpProxyAsyncResult1 = this;
						IteratorAsyncResult<ServiceBusClientWebSocket.ConnectTcpProxyAsyncResult>.BeginCall beginCall = (ServiceBusClientWebSocket.ConnectTcpProxyAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.TcpClient.BeginConnect(thisPtr.proxyAddress.Host, thisPtr.proxyAddress.Port, c, s);
						yield return connectTcpProxyAsyncResult1.CallAsync(beginCall, (ServiceBusClientWebSocket.ConnectTcpProxyAsyncResult thisPtr, IAsyncResult a) => thisPtr.TcpClient.EndConnect(a), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
						this.Stream = this.TcpClient.GetStream();
					}
					string str1 = ServiceBusClientWebSocket.ConnectTcpProxyAsyncResult.BuildConnectRequest(this.destinationAddress, str);
					this.byteCount = Encoding.ASCII.GetBytes(str1, 0, str1.Length, this.buffer, 0);
					this.TcpClient.Client.SendTimeout = ServiceBusClientWebSocket.GetSocketTimeoutInMilliSeconds(base.RemainingTime());
					ServiceBusClientWebSocket.ConnectTcpProxyAsyncResult connectTcpProxyAsyncResult2 = this;
					IteratorAsyncResult<ServiceBusClientWebSocket.ConnectTcpProxyAsyncResult>.BeginCall beginCall1 = (ServiceBusClientWebSocket.ConnectTcpProxyAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.Stream.BeginWrite(thisPtr.buffer, 0, thisPtr.byteCount, c, s);
					yield return connectTcpProxyAsyncResult2.CallAsync(beginCall1, (ServiceBusClientWebSocket.ConnectTcpProxyAsyncResult thisPtr, IAsyncResult a) => thisPtr.Stream.EndWrite(a), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
					ServiceBusClientWebSocket.HttpResponse httpResponse = new ServiceBusClientWebSocket.HttpResponse(this.TcpClient, this.Stream, this.buffer);
					yield return base.CallAsync((ServiceBusClientWebSocket.ConnectTcpProxyAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => httpResponse.BeginRead(t, c, s), (ServiceBusClientWebSocket.ConnectTcpProxyAsyncResult thisPtr, IAsyncResult a) => httpResponse.EndRead(a), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
					if (httpResponse.StatusCode != HttpStatusCode.OK)
					{
						if (httpResponse.StatusCode != HttpStatusCode.ProxyAuthenticationRequired)
						{
							throw Fx.Exception.AsWarning(new IOException(httpResponse.ToString()), this.Activity);
						}
						string item = httpResponse.Headers[HttpResponseHeader.ProxyAuthenticate];
						Fx.AssertAndThrow(!string.IsNullOrEmpty(item), "No Proxy-Authenticate Header was received!");
						char[] chrArray = new char[] { ',' };
						string str2 = item.Split(chrArray)[0];
						string[] strArrays = str2.Split(new char[] { ' ' });
						string str3 = strArrays[0];
						if (this.credentials == null)
						{
							IntPtr zero = IntPtr.Zero;
							this.credentials = SspiWrapper.AcquireCredentialsHandle(str3, Microsoft.ServiceBus.Security.CredentialUse.Outbound, ref zero);
						}
						Microsoft.ServiceBus.Security.SecurityBuffer securityBuffer = null;
						if ((int)strArrays.Length > 1)
						{
							string str4 = strArrays[1];
							securityBuffer = new Microsoft.ServiceBus.Security.SecurityBuffer(Convert.FromBase64String(str4), Microsoft.ServiceBus.Security.BufferType.Token);
						}
						SspiContextFlags sspiContextFlag = SspiContextFlags.AllocateMemory;
						Microsoft.ServiceBus.Security.SecurityBuffer securityBuffer1 = new Microsoft.ServiceBus.Security.SecurityBuffer(0, Microsoft.ServiceBus.Security.BufferType.Token);
						Microsoft.ServiceBus.Security.SecurityStatus securityStatu = (Microsoft.ServiceBus.Security.SecurityStatus)SspiWrapper.InitializeSecurityContext(this.credentials, ref this.securityContext, null, sspiContextFlag, Microsoft.ServiceBus.Security.Endianness.Network, securityBuffer, securityBuffer1, ref sspiContextFlag);
						if (securityStatu != Microsoft.ServiceBus.Security.SecurityStatus.OK)
						{
							if (securityStatu != Microsoft.ServiceBus.Security.SecurityStatus.ContinueNeeded)
							{
								Win32Exception win32Exception = new Win32Exception((int)securityStatu);
								throw Fx.Exception.AsWarning(new IOException(win32Exception.Message, win32Exception), this.Activity);
							}
							string base64String = Convert.ToBase64String(securityBuffer1.token, securityBuffer1.offset, securityBuffer1.size);
							str = string.Concat(str3, " ", base64String);
							if (!string.Equals(httpResponse.Headers["Proxy-Connection"], "close", StringComparison.OrdinalIgnoreCase))
							{
								continue;
							}
							this.Stream.Close();
							this.Stream = null;
							this.TcpClient.Close();
						}
						else
						{
							flag = true;
						}
					}
					else
					{
						flag = true;
					}
				}
				while (!flag);
			}
		}

		private class HttpResponse
		{
			private int bodyStartIndex;

			private byte[] Buffer
			{
				get;
				set;
			}

			public WebHeaderCollection Headers
			{
				get;
				private set;
			}

			public HttpStatusCode StatusCode
			{
				get;
				private set;
			}

			public string StatusDescription
			{
				get;
				private set;
			}

			private Stream Stream
			{
				get;
				set;
			}

			private System.Net.Sockets.TcpClient TcpClient
			{
				get;
				set;
			}

			private int TotalBytesRead
			{
				get;
				set;
			}

			public HttpResponse(System.Net.Sockets.TcpClient tcpClient, Stream stream, byte[] buffer)
			{
				this.TcpClient = tcpClient;
				this.Stream = stream;
				this.Buffer = buffer;
			}

			public IAsyncResult BeginRead(TimeSpan timeout, AsyncCallback callback, object state)
			{
				return new ServiceBusClientWebSocket.HttpResponse.ReadResponseAsyncResult(this, timeout, callback, state);
			}

			public void EndRead(IAsyncResult result)
			{
				AsyncResult<ServiceBusClientWebSocket.HttpResponse.ReadResponseAsyncResult>.End(result);
			}

			public override string ToString()
			{
				return string.Concat((int)this.StatusCode, " ", this.StatusDescription);
			}

			private bool TryParseBuffer()
			{
				string item;
				if (this.bodyStartIndex == 0)
				{
					int num = ByteUtility.IndexOfAsciiChar(this.Buffer, 0, this.TotalBytesRead, ' ');
					if (num == -1)
					{
						return false;
					}
					int num1 = ByteUtility.IndexOfAsciiChar(this.Buffer, num + 1, this.TotalBytesRead - (num + 1), ' ');
					if (num1 == -1)
					{
						return false;
					}
					string str = Encoding.ASCII.GetString(this.Buffer, num + 1, num1 - (num + 1));
					this.StatusCode = (HttpStatusCode)int.Parse(str, CultureInfo.InvariantCulture);
					int num2 = ByteUtility.IndexOfAsciiChars(this.Buffer, num1 + 1, this.TotalBytesRead - (num1 + 1), '\r', '\n');
					if (num2 == -1)
					{
						return false;
					}
					this.StatusDescription = Encoding.ASCII.GetString(this.Buffer, num1 + 1, num2 - (num1 + 1));
					this.Headers = new WebHeaderCollection();
					while (true)
					{
						int num3 = num2 + 2;
						if (num3 >= this.TotalBytesRead)
						{
							return false;
						}
						if (this.Buffer[num3] == 13 && this.Buffer[num3 + 1] == 10)
						{
							this.bodyStartIndex = num3 + 2;
							item = this.Headers[HttpResponseHeader.ContentLength];
							if (!string.IsNullOrEmpty(item) && item != "0" && int.Parse(item, CultureInfo.InvariantCulture) > this.TotalBytesRead - this.bodyStartIndex)
							{
								return false;
							}
							return true;
						}
						int num4 = ByteUtility.IndexOfAsciiChars(this.Buffer, num3, this.TotalBytesRead - num3, ':', ' ');
						if (num4 == -1)
						{
							return false;
						}
						string str1 = Encoding.ASCII.GetString(this.Buffer, num3, num4 - num3);
						num2 = ByteUtility.IndexOfAsciiChars(this.Buffer, num4 + 2, this.TotalBytesRead - (num4 + 2), '\r', '\n');
						if (num2 == -1)
						{
							break;
						}
						string str2 = Encoding.ASCII.GetString(this.Buffer, num4 + 2, num2 - (num4 + 2));
						this.Headers.Add(str1, str2);
					}
					return false;
				}
				item = this.Headers[HttpResponseHeader.ContentLength];
				if (!string.IsNullOrEmpty(item) && item != "0" && int.Parse(item, CultureInfo.InvariantCulture) > this.TotalBytesRead - this.bodyStartIndex)
				{
					return false;
				}
				return true;
			}

			private sealed class ReadResponseAsyncResult : IteratorAsyncResult<ServiceBusClientWebSocket.HttpResponse.ReadResponseAsyncResult>
			{
				private int bytesRead;

				private ServiceBusClientWebSocket.HttpResponse HttpResponse
				{
					get;
					set;
				}

				public ReadResponseAsyncResult(ServiceBusClientWebSocket.HttpResponse httpResponse, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
				{
					this.HttpResponse = httpResponse;
					base.Start();
				}

				protected override IEnumerator<IteratorAsyncResult<ServiceBusClientWebSocket.HttpResponse.ReadResponseAsyncResult>.AsyncStep> GetAsyncSteps()
				{
					do
					{
						this.HttpResponse.TcpClient.Client.ReceiveTimeout = ServiceBusClientWebSocket.GetSocketTimeoutInMilliSeconds(base.RemainingTime());
						this.bytesRead = 0;
						ServiceBusClientWebSocket.HttpResponse.ReadResponseAsyncResult readResponseAsyncResult = this;
						IteratorAsyncResult<ServiceBusClientWebSocket.HttpResponse.ReadResponseAsyncResult>.BeginCall beginCall = (ServiceBusClientWebSocket.HttpResponse.ReadResponseAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.HttpResponse.Stream.BeginRead(thisPtr.HttpResponse.Buffer, thisPtr.HttpResponse.TotalBytesRead, (int)thisPtr.HttpResponse.Buffer.Length - thisPtr.HttpResponse.TotalBytesRead, c, s);
						yield return readResponseAsyncResult.CallAsync(beginCall, (ServiceBusClientWebSocket.HttpResponse.ReadResponseAsyncResult thisPtr, IAsyncResult a) => thisPtr.bytesRead = thisPtr.HttpResponse.Stream.EndRead(a), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
						ServiceBusClientWebSocket.HttpResponse httpResponse = this.HttpResponse;
						httpResponse.TotalBytesRead = httpResponse.TotalBytesRead + this.bytesRead;
					}
					while (this.bytesRead != 0 && !this.HttpResponse.TryParseBuffer());
					if (this.HttpResponse.TotalBytesRead == 0)
					{
						SocketException socketException = new SocketException(10061);
						throw Fx.Exception.AsWarning(new IOException(socketException.Message, socketException), null);
					}
				}
			}
		}

		private sealed class ReceiveAsyncResult : IteratorAsyncResult<ServiceBusClientWebSocket.ReceiveAsyncResult>
		{
			private readonly Stream webSocketStream;

			private readonly byte[] buffer;

			private readonly int startOffset;

			private readonly byte[] header;

			private int bytesRead;

			internal bool ConnectionClosed
			{
				get;
				private set;
			}

			internal int TotalBytesRead
			{
				get;
				private set;
			}

			public ReceiveAsyncResult(Stream webSocketStream, byte[] buffer, int startOffset, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.webSocketStream = webSocketStream;
				this.buffer = buffer;
				this.startOffset = startOffset;
				this.ConnectionClosed = false;
			}

			protected override IEnumerator<IteratorAsyncResult<ServiceBusClientWebSocket.ReceiveAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				bool flag;
				byte num;
				while (true)
				{
					this.TotalBytesRead = 0;
					do
					{
						ServiceBusClientWebSocket.ReceiveAsyncResult receiveAsyncResult = this;
						IteratorAsyncResult<ServiceBusClientWebSocket.ReceiveAsyncResult>.BeginCall beginCall = (ServiceBusClientWebSocket.ReceiveAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.webSocketStream.BeginRead(thisPtr.header, thisPtr.TotalBytesRead, (int)thisPtr.header.Length - thisPtr.TotalBytesRead, c, s);
						yield return receiveAsyncResult.CallAsync(beginCall, (ServiceBusClientWebSocket.ReceiveAsyncResult thisPtr, IAsyncResult r) => thisPtr.bytesRead = thisPtr.webSocketStream.EndRead(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
						ServiceBusClientWebSocket.ReceiveAsyncResult totalBytesRead = this;
						totalBytesRead.TotalBytesRead = totalBytesRead.TotalBytesRead + this.bytesRead;
					}
					while (this.TotalBytesRead < (int)this.header.Length);
					if (ServiceBusClientWebSocket.ParseWebSocketFrameHeader(this.header, out num, out flag))
					{
						if (flag && num > 0)
						{
							this.TotalBytesRead = 0;
							byte[] numArray = new byte[num];
							while (this.TotalBytesRead < num)
							{
								ServiceBusClientWebSocket.ReceiveAsyncResult receiveAsyncResult1 = this;
								IteratorAsyncResult<ServiceBusClientWebSocket.ReceiveAsyncResult>.BeginCall beginCall1 = (ServiceBusClientWebSocket.ReceiveAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.webSocketStream.BeginRead(numArray, thisPtr.TotalBytesRead, num - thisPtr.TotalBytesRead, c, s);
								yield return receiveAsyncResult1.CallAsync(beginCall1, (ServiceBusClientWebSocket.ReceiveAsyncResult thisPtr, IAsyncResult r) => thisPtr.bytesRead = thisPtr.webSocketStream.EndRead(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
								ServiceBusClientWebSocket.ReceiveAsyncResult totalBytesRead1 = this;
								totalBytesRead1.TotalBytesRead = totalBytesRead1.TotalBytesRead + this.bytesRead;
							}
						}
						if (!flag)
						{
							this.TotalBytesRead = 0;
							this.bytesRead = 0;
							if ((int)this.buffer.Length < num)
							{
								throw Fx.Exception.AsError(new InvalidOperationException(Resources.SizeExceedsRemainingBufferSpace), null);
							}
							if (num >= 126)
							{
								switch (num)
								{
									case 126:
									{
										do
										{
											ServiceBusClientWebSocket.ReceiveAsyncResult receiveAsyncResult2 = this;
											IteratorAsyncResult<ServiceBusClientWebSocket.ReceiveAsyncResult>.BeginCall beginCall2 = (ServiceBusClientWebSocket.ReceiveAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.webSocketStream.BeginRead(thisPtr.header, thisPtr.TotalBytesRead, (int)thisPtr.header.Length - thisPtr.TotalBytesRead, c, s);
											yield return receiveAsyncResult2.CallAsync(beginCall2, (ServiceBusClientWebSocket.ReceiveAsyncResult thisPtr, IAsyncResult r) => thisPtr.bytesRead = thisPtr.webSocketStream.EndRead(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
											ServiceBusClientWebSocket.ReceiveAsyncResult totalBytesRead2 = this;
											totalBytesRead2.TotalBytesRead = totalBytesRead2.TotalBytesRead + this.bytesRead;
										}
										while (this.TotalBytesRead < (int)this.header.Length);
										this.TotalBytesRead = 0;
										ushort num1 = (ushort)(this.header[0] << 8 | this.header[1]);
										if ((int)this.buffer.Length < num1)
										{
											throw Fx.Exception.AsError(new InvalidOperationException(Resources.SizeExceedsRemainingBufferSpace), null);
										}
										while (this.TotalBytesRead < num1)
										{
											ServiceBusClientWebSocket.ReceiveAsyncResult receiveAsyncResult3 = this;
											IteratorAsyncResult<ServiceBusClientWebSocket.ReceiveAsyncResult>.BeginCall beginCall3 = (ServiceBusClientWebSocket.ReceiveAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.webSocketStream.BeginRead(thisPtr.buffer, thisPtr.startOffset + thisPtr.TotalBytesRead, num1 - thisPtr.TotalBytesRead, c, s);
											yield return receiveAsyncResult3.CallAsync(beginCall3, (ServiceBusClientWebSocket.ReceiveAsyncResult thisPtr, IAsyncResult r) => thisPtr.bytesRead = thisPtr.webSocketStream.EndRead(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
											ServiceBusClientWebSocket.ReceiveAsyncResult totalBytesRead3 = this;
											totalBytesRead3.TotalBytesRead = totalBytesRead3.TotalBytesRead + this.bytesRead;
										}
										break;
									}
									case 127:
									{
										byte[] numArray1 = new byte[8];
										do
										{
											ServiceBusClientWebSocket.ReceiveAsyncResult receiveAsyncResult4 = this;
											IteratorAsyncResult<ServiceBusClientWebSocket.ReceiveAsyncResult>.BeginCall beginCall4 = (ServiceBusClientWebSocket.ReceiveAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.webSocketStream.BeginRead(numArray1, thisPtr.TotalBytesRead, (int)numArray1.Length - thisPtr.TotalBytesRead, c, s);
											yield return receiveAsyncResult4.CallAsync(beginCall4, (ServiceBusClientWebSocket.ReceiveAsyncResult thisPtr, IAsyncResult r) => thisPtr.bytesRead = thisPtr.webSocketStream.EndRead(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
											ServiceBusClientWebSocket.ReceiveAsyncResult totalBytesRead4 = this;
											totalBytesRead4.TotalBytesRead = totalBytesRead4.TotalBytesRead + this.bytesRead;
										}
										while (this.TotalBytesRead < (int)numArray1.Length);
										this.TotalBytesRead = 0;
										uint num2 = (uint)(numArray1[4] << 24 | numArray1[5] << 16 | numArray1[6] << 8 | numArray1[7]);
										if ((long)((int)this.buffer.Length) < (ulong)num2)
										{
											throw Fx.Exception.AsError(new InvalidOperationException(Resources.SizeExceedsRemainingBufferSpace), null);
										}
										while ((long)this.TotalBytesRead < (ulong)num2)
										{
											ServiceBusClientWebSocket.ReceiveAsyncResult receiveAsyncResult5 = this;
											IteratorAsyncResult<ServiceBusClientWebSocket.ReceiveAsyncResult>.BeginCall beginCall5 = (ServiceBusClientWebSocket.ReceiveAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.webSocketStream.BeginRead(thisPtr.buffer, thisPtr.startOffset + thisPtr.TotalBytesRead, (int)((ulong)num2 - (long)thisPtr.TotalBytesRead), c, s);
											yield return receiveAsyncResult5.CallAsync(beginCall5, (ServiceBusClientWebSocket.ReceiveAsyncResult thisPtr, IAsyncResult r) => thisPtr.bytesRead = thisPtr.webSocketStream.EndRead(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
											ServiceBusClientWebSocket.ReceiveAsyncResult totalBytesRead5 = this;
											totalBytesRead5.TotalBytesRead = totalBytesRead5.TotalBytesRead + this.bytesRead;
										}
										break;
									}
								}
							}
							else
							{
								while (this.TotalBytesRead < num)
								{
									ServiceBusClientWebSocket.ReceiveAsyncResult receiveAsyncResult6 = this;
									IteratorAsyncResult<ServiceBusClientWebSocket.ReceiveAsyncResult>.BeginCall beginCall6 = (ServiceBusClientWebSocket.ReceiveAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.webSocketStream.BeginRead(thisPtr.buffer, thisPtr.startOffset + thisPtr.TotalBytesRead, num - thisPtr.TotalBytesRead, c, s);
									yield return receiveAsyncResult6.CallAsync(beginCall6, (ServiceBusClientWebSocket.ReceiveAsyncResult thisPtr, IAsyncResult r) => thisPtr.bytesRead = thisPtr.webSocketStream.EndRead(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
									ServiceBusClientWebSocket.ReceiveAsyncResult totalBytesRead6 = this;
									totalBytesRead6.TotalBytesRead = totalBytesRead6.TotalBytesRead + this.bytesRead;
								}
								break;
							}
						}
					}
					else
					{
						this.ConnectionClosed = true;
						byte[] numArray2 = ServiceBusClientWebSocket.PrepareWebSocketHeader(0, ServiceBusClientWebSocket.WebSocketMessageType.Close);
						ServiceBusClientWebSocket.ReceiveAsyncResult receiveAsyncResult7 = this;
						IteratorAsyncResult<ServiceBusClientWebSocket.ReceiveAsyncResult>.BeginCall beginCall7 = (ServiceBusClientWebSocket.ReceiveAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.webSocketStream.BeginWrite(numArray2, 0, (int)numArray2.Length, c, s);
						yield return receiveAsyncResult7.CallAsync(beginCall7, (ServiceBusClientWebSocket.ReceiveAsyncResult thisPtr, IAsyncResult r) => thisPtr.webSocketStream.EndWrite(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
						break;
					}
				}
			}
		}

		private sealed class SendAsyncResult : IteratorAsyncResult<ServiceBusClientWebSocket.SendAsyncResult>
		{
			private readonly Stream webSocketStream;

			private readonly ServiceBusClientWebSocket.WebSocketMessageType webSocketMessageType;

			private readonly int offset;

			private readonly int size;

			private byte[] buffer;

			public SendAsyncResult(Stream webSocketStream, byte[] buffer, int offset, int size, ServiceBusClientWebSocket.WebSocketMessageType webSocketMessageType, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.webSocketStream = webSocketStream;
				this.buffer = buffer;
				this.offset = offset;
				this.size = size;
				this.webSocketMessageType = webSocketMessageType;
			}

			protected override IEnumerator<IteratorAsyncResult<ServiceBusClientWebSocket.SendAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				byte[] numArray = ServiceBusClientWebSocket.PrepareWebSocketHeader(this.size, this.webSocketMessageType);
				ServiceBusClientWebSocket.SendAsyncResult sendAsyncResult = this;
				IteratorAsyncResult<ServiceBusClientWebSocket.SendAsyncResult>.BeginCall beginCall = (ServiceBusClientWebSocket.SendAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.webSocketStream.BeginWrite(numArray, 0, (int)numArray.Length, c, s);
				yield return sendAsyncResult.CallAsync(beginCall, (ServiceBusClientWebSocket.SendAsyncResult thisPtr, IAsyncResult r) => thisPtr.webSocketStream.EndWrite(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				ServiceBusClientWebSocket.MaskWebSocketData(this.buffer, this.offset, this.size);
				ServiceBusClientWebSocket.SendAsyncResult sendAsyncResult1 = this;
				IteratorAsyncResult<ServiceBusClientWebSocket.SendAsyncResult>.BeginCall beginCall1 = (ServiceBusClientWebSocket.SendAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.webSocketStream.BeginWrite(thisPtr.buffer, thisPtr.offset, thisPtr.size, c, s);
				yield return sendAsyncResult1.CallAsync(beginCall1, (ServiceBusClientWebSocket.SendAsyncResult thisPtr, IAsyncResult r) => thisPtr.webSocketStream.EndWrite(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
			}
		}

		public enum WebSocketMessageType
		{
			Binary,
			Close,
			Text
		}

		public enum WebSocketState
		{
			Initial,
			Connecting,
			Open,
			Closed,
			Aborted,
			Faulted
		}
	}
}