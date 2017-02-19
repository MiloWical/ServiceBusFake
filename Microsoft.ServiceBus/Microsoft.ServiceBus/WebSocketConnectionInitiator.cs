using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Tracing;
using System;
using System.IdentityModel.Tokens;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus
{
	internal class WebSocketConnectionInitiator : Microsoft.ServiceBus.Channels.IConnectionInitiator
	{
		private readonly TokenProvider tokenProvider;

		private readonly int bufferSize;

		public WebSocketConnectionInitiator(TokenProvider tokenProvider, int bufferSize)
		{
			this.tokenProvider = tokenProvider;
			this.bufferSize = bufferSize;
		}

		public IAsyncResult BeginConnect(Uri uri, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return (new WebSocketConnectAsyncResult(uri, timeout, "wsrelayedconnection", callback, state)).Start();
		}

		public Microsoft.ServiceBus.Channels.IConnection Connect(Uri uri, TimeSpan timeout)
		{
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			WebSocketConnectAsyncResult webSocketConnectAsyncResult = (new WebSocketConnectAsyncResult(uri, timeout, "wsrelayedconnection", null, null)).RunSynchronously();
			ClientWebSocketConnection clientWebSocketConnection = new ClientWebSocketConnection(webSocketConnectAsyncResult.ClientWebSocket, this.bufferSize, uri, new EventTraceActivity());
			MessagingClientEtwProvider.Provider.WebSocketConnectionEstablished(clientWebSocketConnection.Activity, uri.AbsoluteUri);
			this.SendRelayedConnectAndReceiveResponse(clientWebSocketConnection, timeoutHelper);
			return clientWebSocketConnection;
		}

		public Microsoft.ServiceBus.Channels.IConnection EndConnect(IAsyncResult result)
		{
			WebSocketConnectAsyncResult webSocketConnectAsyncResult = AsyncResult<WebSocketConnectAsyncResult>.End(result);
			TimeoutHelper timeoutHelper = new TimeoutHelper(webSocketConnectAsyncResult.TimeRemaining());
			ClientWebSocketConnection clientWebSocketConnection = null;
			if (result.IsCompleted)
			{
				clientWebSocketConnection = new ClientWebSocketConnection(webSocketConnectAsyncResult.ClientWebSocket, this.bufferSize, webSocketConnectAsyncResult.Uri, new EventTraceActivity());
				MessagingClientEtwProvider.Provider.WebSocketConnectionEstablished(clientWebSocketConnection.Activity, webSocketConnectAsyncResult.Uri.AbsoluteUri);
				this.SendRelayedConnectAndReceiveResponse(clientWebSocketConnection, timeoutHelper);
			}
			return clientWebSocketConnection;
		}

		private void SendRelayedConnectAndReceiveResponse(ClientWebSocketConnection connection, TimeoutHelper timeoutHelper)
		{
			SocketMessageHelper socketMessageHelper = new SocketMessageHelper();
			Message message = Message.CreateMessage(socketMessageHelper.MessageVersion, "RelayedConnect", new ConnectMessage(connection.Uri));
			TrackingIdHeader.TryAddOrUpdate(message.Headers, connection.Activity.ActivityId.ToString());
			if (this.tokenProvider != null)
			{
				string absoluteUri = RelayedHttpUtility.ConvertToHttpUri(connection.Uri).AbsoluteUri;
				SecurityToken token = this.tokenProvider.GetToken(absoluteUri, "Send", false, timeoutHelper.RemainingTime());
				message.Headers.Add(new RelayTokenHeader(token));
			}
			socketMessageHelper.SendMessage(connection, message, timeoutHelper.RemainingTime());
			Message message1 = socketMessageHelper.ReceiveMessage(connection, timeoutHelper.RemainingTime());
			if (message1.IsFault)
			{
				throw ErrorUtility.ConvertToError(MessageFault.CreateFault(message1, 65536));
			}
		}
	}
}