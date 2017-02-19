using System;

namespace Microsoft.ServiceBus.Messaging
{
	internal interface IRuleManager
	{
		void AddRule(string subscriptionName, RuleDescription rule);
	}
}