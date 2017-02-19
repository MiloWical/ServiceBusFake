using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.ServiceModel;

namespace Microsoft.ServiceBus
{
	internal sealed class WebSocketConnectAsyncResult : IteratorAsyncResult<WebSocketConnectAsyncResult>, IDisposable
	{
		private readonly TimeSpan timeout;

		private readonly string webSocketRole;

		internal ServiceBusClientWebSocket ClientWebSocket
		{
			get;
			private set;
		}

		internal System.Uri Uri
		{
			get;
			private set;
		}

		public WebSocketConnectAsyncResult(System.Uri uri, TimeSpan timeout, string webSocketRole, AsyncCallback callback, object state) : base(timeout, callback, state)
		{
			this.Uri = uri;
			this.timeout = timeout;
			this.webSocketRole = webSocketRole;
		}

		public void Dispose()
		{
			if (this.ClientWebSocket != null)
			{
				this.ClientWebSocket.Abort();
			}
		}

		protected override IEnumerator<IteratorAsyncResult<WebSocketConnectAsyncResult>.AsyncStep> GetAsyncSteps()
		{
			this.ClientWebSocket = new ServiceBusClientWebSocket(this.webSocketRole);
			yield return base.CallAsync((WebSocketConnectAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => this.ClientWebSocket.BeginConnect(thisPtr.Uri.Host, RelayEnvironment.RelayHttpsPort, thisPtr.timeout, c, s), (WebSocketConnectAsyncResult thisPtr, IAsyncResult r) => this.ClientWebSocket.EndConnect(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
			if (base.LastAsyncStepException != null)
			{
				base.Complete(new CommunicationException(base.LastAsyncStepException.Message, base.LastAsyncStepException));
			}
		}

		public TimeSpan TimeRemaining()
		{
			return base.RemainingTime();
		}
	}
}