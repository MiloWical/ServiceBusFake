using System;

namespace Microsoft.ServiceBus.Messaging
{
	internal enum BrokeredMessageFormat : byte
	{
		Sbmp,
		Amqp,
		PassthroughAmqp,
		AmqpEventData
	}
}