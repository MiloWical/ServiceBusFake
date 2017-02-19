using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Tracing;
using System;
using System.IdentityModel.Tokens;
using System.Net.Sockets;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;

namespace Microsoft.ServiceBus
{
	internal class RelayedSocketInitiator : Microsoft.ServiceBus.Channels.IConnectionInitiator
	{
		private const int MaxRedirectDepth = 3;

		private readonly int bufferSize;

		private readonly SocketMessageHelper messageHelper;

		private readonly SocketSecurityRole socketSecurityMode;

		private readonly TokenProvider tokenProvider;

		public RelayedSocketInitiator(int bufferSize, TokenProvider tokenProvider, SocketSecurityRole socketSecurityMode)
		{
			this.bufferSize = bufferSize;
			this.tokenProvider = tokenProvider;
			this.socketSecurityMode = socketSecurityMode;
			if (this.tokenProvider != null)
			{
				this.socketSecurityMode = SocketSecurityRole.SslClient;
			}
			this.messageHelper = new SocketMessageHelper();
		}

		public IAsyncResult BeginConnect(Uri uri, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult<Microsoft.ServiceBus.Channels.IConnection>(this.Connect(uri, timeout), callback, state);
		}

		public Microsoft.ServiceBus.Channels.IConnection Connect(Uri uri, TimeSpan timeout)
		{
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			Uri uri1 = uri;
			for (int i = 0; i < 3; i++)
			{
				EventTraceActivity eventTraceActivity = new EventTraceActivity();
				Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				try
				{
					socket.Connect(uri1.Host, 9352);
					Microsoft.ServiceBus.Channels.IConnection socketConnection = new Microsoft.ServiceBus.Channels.SocketConnection(socket, this.bufferSize, eventTraceActivity);
					byte[] bytes = Encoding.UTF8.GetBytes(ConnectConstants.ConnectType);
					byte[] numArray = BitConverter.GetBytes((int)bytes.Length);
					socketConnection.Write(numArray, 0, (int)numArray.Length, true, timeoutHelper.RemainingTime());
					socketConnection.Write(bytes, 0, (int)bytes.Length, true, timeoutHelper.RemainingTime());
					byte[] bytes1 = Encoding.UTF8.GetBytes(uri1.ToString());
					byte[] numArray1 = BitConverter.GetBytes((int)bytes1.Length);
					socketConnection.Write(numArray1, 0, (int)numArray1.Length, true, timeoutHelper.RemainingTime());
					socketConnection.Write(bytes1, 0, (int)bytes1.Length, true, timeoutHelper.RemainingTime());
					socketConnection = SecureSocketUtil.InitiateSecureClientUpgradeIfNeeded(socketConnection, null, this.socketSecurityMode, uri.Host, timeoutHelper.RemainingTime());
					Message message = Message.CreateMessage(this.messageHelper.MessageVersion, "RelayedConnect", new ConnectMessage(uri));
					TrackingIdHeader.TryAddOrUpdate(message.Headers, eventTraceActivity.ActivityId.ToString());
					if (this.tokenProvider != null)
					{
						SecurityToken token = this.tokenProvider.GetToken(RelayedHttpUtility.ConvertToHttpUri(uri).ToString(), "Send", false, timeoutHelper.RemainingTime());
						message.Headers.Add(new RelayTokenHeader(token));
					}
					this.messageHelper.SendMessage(socketConnection, message, timeoutHelper.RemainingTime());
					Message message1 = this.messageHelper.ReceiveMessage(socketConnection, timeoutHelper.RemainingTime());
					using (message1)
					{
						if (message1.Headers.Action == "Redirect")
						{
							uri1 = message1.GetBody<RedirectMessage>().Uri;
							socket.Close();
							goto Label1;
						}
						else if (message1.IsFault)
						{
							MessageFault messageFault = MessageFault.CreateFault(message1, 65536);
							throw Fx.Exception.AsError(ErrorUtility.ConvertToError(messageFault), eventTraceActivity);
						}
					}
					return socketConnection;
				}
				catch
				{
					socket.Close();
					throw;
				}
			Label1:
			}
			throw Fx.Exception.AsError(new CommunicationException(SRClient.MaxRedirectsExceeded(3)), null);
		}

		public Microsoft.ServiceBus.Channels.IConnection EndConnect(IAsyncResult result)
		{
			return CompletedAsyncResult<Microsoft.ServiceBus.Channels.IConnection>.End(result);
		}
	}
}