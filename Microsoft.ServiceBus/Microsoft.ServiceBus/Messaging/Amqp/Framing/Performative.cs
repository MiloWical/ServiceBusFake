using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal abstract class Performative : DescribedList
	{
		protected Performative(AmqpSymbol name, ulong code) : base(name, code)
		{
		}
	}
}