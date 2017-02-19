using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Tracing;
using System;

namespace Microsoft.ServiceBus
{
	internal class WebSocketOnewayConnectionInitiator : IConnectionInitiator
	{
		private readonly int bufferSize;

		private readonly string webSocketRole;

		public WebSocketOnewayConnectionInitiator(string webSocketRole, int bufferSize)
		{
			this.webSocketRole = webSocketRole;
			this.bufferSize = bufferSize;
		}

		public IAsyncResult BeginConnect(Uri uri, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult<IConnection>(this.Connect(uri, timeout), callback, state);
		}

		public IConnection Connect(Uri uri, TimeSpan timeout)
		{
			WebSocketConnectAsyncResult webSocketConnectAsyncResult = (new WebSocketConnectAsyncResult(uri, timeout, this.webSocketRole, null, null)).RunSynchronously();
			IConnection clientWebSocketConnection = new ClientWebSocketConnection(webSocketConnectAsyncResult.ClientWebSocket, this.bufferSize, uri, new EventTraceActivity());
			MessagingClientEtwProvider.Provider.WebSocketConnectionEstablished(clientWebSocketConnection.Activity, webSocketConnectAsyncResult.Uri.AbsoluteUri);
			return clientWebSocketConnection;
		}

		public IConnection EndConnect(IAsyncResult result)
		{
			return CompletedAsyncResult<IConnection>.End(result);
		}
	}
}