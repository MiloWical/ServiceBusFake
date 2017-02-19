using System;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal interface ILinkFactory
	{
		IAsyncResult BeginOpenLink(AmqpLink link, TimeSpan timeout, AsyncCallback callback, object state);

		AmqpLink CreateLink(AmqpSession session, AmqpLinkSettings settings);

		void EndOpenLink(IAsyncResult result);
	}
}