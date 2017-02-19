using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class FilterSet : RestrictedMap<AmqpSymbol>
	{
		public FilterSet()
		{
		}
	}
}