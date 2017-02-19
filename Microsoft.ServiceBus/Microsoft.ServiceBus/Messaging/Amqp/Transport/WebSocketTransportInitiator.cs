using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Tracing;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
	internal sealed class WebSocketTransportInitiator : TransportInitiator
	{
		private readonly static AsyncCallback onConnectComplete;

		private readonly Uri uri;

		private readonly WebSocketTransportSettings webSocketTransportSettings;

		private TransportAsyncCallbackArgs callbackArgs;

		static WebSocketTransportInitiator()
		{
			WebSocketTransportInitiator.onConnectComplete = new AsyncCallback(WebSocketTransportInitiator.OnConnectComplete);
		}

		internal WebSocketTransportInitiator(Uri uri, WebSocketTransportSettings webSocketTransportSettings)
		{
			this.uri = uri;
			this.webSocketTransportSettings = webSocketTransportSettings;
		}

		private void Complete(IAsyncResult connectAsyncResult, bool completeSynchronously)
		{
			EventTraceActivity eventTraceActivity = new EventTraceActivity();
			TransportBase clientWebSocketTransport = null;
			try
			{
				try
				{
					ServiceBusClientWebSocket clientWebSocket = AsyncResult<WebSocketConnectAsyncResult>.End(connectAsyncResult).ClientWebSocket;
					clientWebSocketTransport = new ClientWebSocketTransport(clientWebSocket, this.uri, this.webSocketTransportSettings, eventTraceActivity);
					clientWebSocketTransport.Open();
					MessagingClientEtwProvider.Provider.WebSocketTransportEstablished(eventTraceActivity, this.uri.AbsoluteUri);
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					this.callbackArgs.Exception = exception;
					if (clientWebSocketTransport != null)
					{
						clientWebSocketTransport.SafeClose(exception);
					}
					clientWebSocketTransport = null;
				}
			}
			finally
			{
				this.callbackArgs.Transport = clientWebSocketTransport;
				this.callbackArgs.CompletedSynchronously = completeSynchronously;
				if (!completeSynchronously)
				{
					this.callbackArgs.CompletedCallback(this.callbackArgs);
				}
			}
		}

		public override bool ConnectAsync(TimeSpan timeout, TransportAsyncCallbackArgs callbackArgs)
		{
			this.callbackArgs = callbackArgs;
			IAsyncResult asyncResult = (new WebSocketConnectAsyncResult(this.uri, timeout, "wsrelayedamqp", WebSocketTransportInitiator.onConnectComplete, this)).Start();
			if (!asyncResult.IsCompleted)
			{
				return true;
			}
			this.Complete(asyncResult, true);
			return false;
		}

		private static void OnConnectComplete(IAsyncResult result)
		{
			if (result.CompletedSynchronously)
			{
				return;
			}
			((WebSocketTransportInitiator)result.AsyncState).Complete(result, false);
		}
	}
}