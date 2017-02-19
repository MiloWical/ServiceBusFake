using System;

namespace Microsoft.ServiceBus.Messaging
{
	internal delegate void EndBatchedDelegate(IAsyncResult result, bool forceCleanUp);
}