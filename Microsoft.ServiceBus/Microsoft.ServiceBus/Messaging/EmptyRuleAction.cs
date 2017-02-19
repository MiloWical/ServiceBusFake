using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Name="EmptyRuleAction", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	internal sealed class EmptyRuleAction : RuleAction
	{
		internal readonly static EmptyRuleAction Default;

		public override bool RequiresPreprocessing
		{
			get
			{
				return false;
			}
		}

		static EmptyRuleAction()
		{
			EmptyRuleAction.Default = new EmptyRuleAction();
		}

		public EmptyRuleAction()
		{
		}

		public override BrokeredMessage Execute(BrokeredMessage message)
		{
			return message;
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