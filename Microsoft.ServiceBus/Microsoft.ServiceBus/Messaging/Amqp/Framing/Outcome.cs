using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal abstract class Outcome : DeliveryState
	{
		protected Outcome(AmqpSymbol name, ulong code) : base(name, code)
		{
		}
	}
}