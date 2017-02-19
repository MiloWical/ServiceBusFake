using System;

namespace Microsoft.ServiceBus.Common
{
	internal delegate IAsyncResult ChainedBeginHandler(TimeSpan timeout, AsyncCallback asyncCallback, object state);
}