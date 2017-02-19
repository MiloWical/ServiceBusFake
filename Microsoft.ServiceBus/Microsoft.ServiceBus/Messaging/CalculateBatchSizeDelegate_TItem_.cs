using System;
using System.Collections.Generic;

namespace Microsoft.ServiceBus.Messaging
{
	internal delegate int CalculateBatchSizeDelegate<TItem>(IEnumerable<TItem> batchItems);
}