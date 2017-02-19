using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections.Generic;

namespace Microsoft.ServiceBus.Messaging
{
	internal delegate IAsyncResult BeginBatchedDelegate<TItem>(TrackingContext trackingContext, IEnumerable<TItem> batchItems, string transactionId, TimeSpan timeout, AsyncCallback callback, object state);
}