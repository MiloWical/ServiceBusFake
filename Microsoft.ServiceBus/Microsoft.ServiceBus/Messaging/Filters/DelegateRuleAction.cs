using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;

namespace Microsoft.ServiceBus.Messaging.Filters
{
	internal sealed class DelegateRuleAction : RuleAction
	{
		private readonly Action<BrokeredMessage, IDictionary<string, object>> action;

		private readonly IDictionary<string, object> parameters;

		public override bool RequiresPreprocessing
		{
			get
			{
				return false;
			}
		}

		public DelegateRuleAction(Action<BrokeredMessage, IDictionary<string, object>> action, IDictionary<string, object> parameters)
		{
			if (action == null)
			{
				throw new ArgumentNullException("action");
			}
			this.action = action;
			this.parameters = parameters;
		}

		public override BrokeredMessage Execute(BrokeredMessage message)
		{
			BrokeredMessage brokeredMessage;
			try
			{
				this.action(message, this.parameters);
				brokeredMessage = message;
			}
			catch (RuleActionException ruleActionException)
			{
				throw;
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw new RuleActionException(exception.Message, exception);
				}
				throw;
			}
			return brokeredMessage;
		}

		internal override BrokeredMessage Execute(BrokeredMessage message, RuleExecutionContext context)
		{
			return this.Execute(message);
		}

		public override RuleAction Preprocess()
		{
			return this;
		}

		public override void Validate()
		{
		}
	}
}