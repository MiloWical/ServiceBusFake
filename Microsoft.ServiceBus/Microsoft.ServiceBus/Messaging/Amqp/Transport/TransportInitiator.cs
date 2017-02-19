using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
	internal abstract class TransportInitiator
	{
		protected TransportInitiator()
		{
		}

		public abstract bool ConnectAsync(TimeSpan timeout, TransportAsyncCallbackArgs callbackArgs);
	}
}