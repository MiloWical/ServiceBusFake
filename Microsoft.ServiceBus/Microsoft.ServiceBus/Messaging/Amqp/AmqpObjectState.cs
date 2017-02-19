using System;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal enum AmqpObjectState
	{
		Start,
		HeaderSent,
		OpenPipe,
		OpenClosePipe,
		HeaderReceived,
		HeaderExchanged,
		OpenSent,
		OpenReceived,
		ClosePipe,
		Opened,
		CloseSent,
		CloseReceived,
		End,
		Faulted
	}
}