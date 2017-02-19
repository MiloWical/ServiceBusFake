using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class Fields : RestrictedMap<AmqpSymbol>
	{
		public Fields()
		{
		}
	}
}