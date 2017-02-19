using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Name="CompositeAction", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	internal class CompositeAction : RuleAction, IEnumerable<RuleAction>, IEnumerable
	{
		[DataMember(Name="Actions", EmitDefaultValue=false, IsRequired=true, Order=65537)]
		private readonly List<RuleAction> actions;

		public override bool RequiresPreprocessing
		{
			get
			{
				return this.actions.Any<RuleAction>((RuleAction r) => r.RequiresPreprocessing);
			}
		}

		public CompositeAction()
		{
			this.actions = new List<RuleAction>();
		}

		public CompositeAction(IEnumerable<RuleAction> actions)
		{
			this.actions = new List<RuleAction>(actions);
		}

		public void Add(RuleAction action)
		{
			this.actions.Add(action);
		}

		public override BrokeredMessage Execute(BrokeredMessage message)
		{
			foreach (RuleAction action in this.actions)
			{
				action.Execute(message);
			}
			return message;
		}

		internal override BrokeredMessage Execute(BrokeredMessage message, RuleExecutionContext context)
		{
			foreach (RuleAction action in this.actions)
			{
				action.Execute(message, context);
			}
			return message;
		}

		public IEnumerator<RuleAction> GetEnumerator()
		{
			return this.actions.GetEnumerator();
		}

		public override RuleAction Preprocess()
		{
			List<RuleAction> ruleActions = new List<RuleAction>();
			foreach (RuleAction action in this.actions)
			{
				RuleAction ruleAction = action;
				while (ruleAction.RequiresPreprocessing)
				{
					ruleAction = action.Preprocess();
				}
				ruleActions.Add(ruleAction);
			}
			return new CompositeAction(ruleActions);
		}

		IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.actions.GetEnumerator();
		}

		public override void Validate()
		{
			foreach (RuleAction action in this.actions)
			{
				action.Validate();
			}
		}
	}
}