using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Tracing;
using System;
using System.IdentityModel.Tokens;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus
{
	internal class WebStreamConnectionInitiator : Microsoft.ServiceBus.Channels.IConnectionInitiator
	{
		private readonly int bufferSize;

		private readonly SocketSecurityRole socketSecurityRole;

		private readonly TokenProvider tokenProvider;

		private readonly bool useHttpsMode;

		public WebStreamConnectionInitiator(TokenProvider tokenProvider, SocketSecurityRole socketSecurityRole, int bufferSize, bool useHttpsMode)
		{
			this.tokenProvider = tokenProvider;
			this.socketSecurityRole = socketSecurityRole;
			this.bufferSize = bufferSize;
			this.useHttpsMode = useHttpsMode;
			if (tokenProvider != null)
			{
				this.socketSecurityRole = SocketSecurityRole.SslClient;
			}
		}

		public IAsyncResult BeginConnect(Uri uri, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult<Microsoft.ServiceBus.Channels.IConnection>(this.Connect(uri, timeout), callback, state);
		}

		public Microsoft.ServiceBus.Channels.IConnection Connect(Uri uri, TimeSpan timeout)
		{
			EventTraceActivity eventTraceActivity = new EventTraceActivity();
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			WebStream webStream = (new WebStream(uri, "connection", this.useHttpsMode, eventTraceActivity, uri)).Open();
			Microsoft.ServiceBus.Channels.IConnection webStreamConnection = new WebStreamConnection(uri, this.bufferSize, eventTraceActivity, webStream, uri);
			webStreamConnection = SecureSocketUtil.InitiateSecureClientUpgradeIfNeeded(webStreamConnection, null, this.socketSecurityRole, uri.Host, timeoutHelper.RemainingTime());
			SocketMessageHelper socketMessageHelper = new SocketMessageHelper();
			Message message = Message.CreateMessage(socketMessageHelper.MessageVersion, "RelayedConnect", new ConnectMessage(uri));
			TrackingIdHeader.TryAddOrUpdate(message.Headers, eventTraceActivity.ActivityId.ToString());
			if (this.tokenProvider != null)
			{
				string absoluteUri = RelayedHttpUtility.ConvertToHttpUri(uri).AbsoluteUri;
				SecurityToken token = this.tokenProvider.GetToken(absoluteUri, "Send", false, timeoutHelper.RemainingTime());
				message.Headers.Add(new RelayTokenHeader(token));
			}
			socketMessageHelper.SendMessage(webStreamConnection, message, timeoutHelper.RemainingTime());
			Message message1 = socketMessageHelper.ReceiveMessage(webStreamConnection, timeoutHelper.RemainingTime());
			if (message1.IsFault)
			{
				throw ErrorUtility.ConvertToError(MessageFault.CreateFault(message1, 65536));
			}
			return webStreamConnection;
		}

		public Microsoft.ServiceBus.Channels.IConnection EndConnect(IAsyncResult result)
		{
			return CompletedAsyncResult<Microsoft.ServiceBus.Channels.IConnection>.End(result);
		}
	}
}