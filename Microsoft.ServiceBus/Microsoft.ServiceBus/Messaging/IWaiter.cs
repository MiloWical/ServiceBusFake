using System;

namespace Microsoft.ServiceBus.Messaging
{
	internal interface IWaiter
	{
		bool Cancel();

		bool Signal();
	}
}