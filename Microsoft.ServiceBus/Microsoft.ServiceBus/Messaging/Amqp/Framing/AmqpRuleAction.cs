using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal abstract class AmqpRuleAction : DescribedList
	{
		protected AmqpRuleAction(string Name, ulong code) : base(Name, code)
		{
		}
	}
}