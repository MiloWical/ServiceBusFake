using System;

namespace Microsoft.ServiceBus.Common
{
	internal delegate void ChainedEndHandler(IAsyncResult result);
}