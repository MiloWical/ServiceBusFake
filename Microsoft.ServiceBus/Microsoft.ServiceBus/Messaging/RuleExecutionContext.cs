using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging
{
	internal sealed class RuleExecutionContext
	{
		private readonly IRuleManager ruleManager;

		public string RuleName
		{
			get;
			private set;
		}

		public string SubscriptionName
		{
			get;
			private set;
		}

		public RuleExecutionContext(string subscriptionName, string ruleName, IRuleManager ruleManager)
		{
			if (string.IsNullOrEmpty(subscriptionName))
			{
				throw new ArgumentNullException("subscriptionName");
			}
			if (string.IsNullOrEmpty(ruleName))
			{
				throw new ArgumentNullException("subscriptionName");
			}
			if (ruleManager == null)
			{
				throw new ArgumentNullException("ruleManager");
			}
			this.SubscriptionName = subscriptionName;
			this.RuleName = ruleName;
			this.ruleManager = ruleManager;
		}

		public void AddRule(RuleDescription description)
		{
			this.ruleManager.AddRule(this.SubscriptionName, description);
		}
	}
}