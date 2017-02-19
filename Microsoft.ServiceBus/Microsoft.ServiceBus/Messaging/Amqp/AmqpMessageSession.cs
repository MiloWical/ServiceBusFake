using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal sealed class AmqpMessageSession : MessageSession
	{
		public AmqpMessageSession(ReceiveMode receiveMode, string sessionId, MessageReceiver innerReceiver) : base(receiveMode, sessionId, DateTime.MinValue, innerReceiver)
		{
		}

		protected override IAsyncResult OnBeginGetState(TrackingContext trackingContext, TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new NotImplementedException();
		}

		protected override IAsyncResult OnBeginPeek(TrackingContext trackingContext, long fromSequenceNumber, int messageCount, TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new NotImplementedException();
		}

		protected override IAsyncResult OnBeginRenewLock(TrackingContext trackingContext, TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new NotImplementedException();
		}

		protected override IAsyncResult OnBeginSetState(TrackingContext trackingContext, Stream stream, TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new NotImplementedException();
		}

		protected override Stream OnEndGetState(IAsyncResult result)
		{
			throw new NotImplementedException();
		}

		protected override IEnumerable<BrokeredMessage> OnEndPeek(IAsyncResult result)
		{
			throw new NotImplementedException();
		}

		protected override DateTime OnEndRenewLock(IAsyncResult result)
		{
			throw new NotImplementedException();
		}

		protected override void OnEndSetState(IAsyncResult result)
		{
			throw new NotImplementedException();
		}
	}
}