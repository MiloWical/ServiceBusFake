using System;
using System.Collections.Generic;

namespace Microsoft.ServiceBus.Messaging
{
	internal delegate IComparable GroupByKeySelectorDelegate<TItem>(IEnumerable<TItem> batchedObjects);
}