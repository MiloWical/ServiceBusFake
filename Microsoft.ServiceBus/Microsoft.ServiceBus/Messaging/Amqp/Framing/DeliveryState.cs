using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal abstract class DeliveryState : DescribedList
	{
		public DeliveryState(AmqpSymbol name, ulong code) : base(name, code)
		{
		}
	}
}