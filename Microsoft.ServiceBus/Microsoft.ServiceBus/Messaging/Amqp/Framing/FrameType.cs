using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal enum FrameType : byte
	{
		Amqp,
		Sasl
	}
}