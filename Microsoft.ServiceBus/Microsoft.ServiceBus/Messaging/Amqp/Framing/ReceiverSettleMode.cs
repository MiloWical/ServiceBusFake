using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal enum ReceiverSettleMode : byte
	{
		First,
		Second
	}
}