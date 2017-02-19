using System;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal interface IAmqpChannelEvents
	{
		event EventHandler LinkClosed;

		event EventHandler LinkOpened;

		event EventHandler SessionClosed;

		event EventHandler SessionOpened;
	}
}