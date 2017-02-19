using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal enum SenderSettleMode : byte
	{
		Unsettled,
		Settled,
		Mixed
	}
}