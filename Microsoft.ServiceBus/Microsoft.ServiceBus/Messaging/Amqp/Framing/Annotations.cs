using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class Annotations : RestrictedMap<AmqpSymbol>
	{
		public Annotations()
		{
		}
	}
}