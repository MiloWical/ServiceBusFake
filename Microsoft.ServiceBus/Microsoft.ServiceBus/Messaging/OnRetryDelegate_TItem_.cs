using System;
using System.Collections.Generic;

namespace Microsoft.ServiceBus.Messaging
{
	internal delegate bool OnRetryDelegate<TItem>(IEnumerable<TItem> batchItem, Exception exception, bool isMultiBatchCommand);
}