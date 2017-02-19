using System;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	[Flags]
	internal enum SectionFlag
	{
		Header = 1,
		DeliveryAnnotations = 2,
		MessageAnnotations = 4,
		Properties = 8,
		ApplicationProperties = 16,
		Data = 32,
		AmqpSequence = 64,
		AmqpValue = 128,
		Body = 224,
		Immutable = 248,
		Footer = 256,
		Mutable = 263,
		NonBody = 287,
		All = 511
	}
}