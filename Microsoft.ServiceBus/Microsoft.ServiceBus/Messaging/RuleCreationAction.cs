using Microsoft.ServiceBus;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Name="RuleCreationAction", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	[KnownType(typeof(PropertyReference))]
	internal class RuleCreationAction : RuleAction
	{
		[DataMember(Name="ActionTemplate", Order=65537, EmitDefaultValue=false, IsRequired=false)]
		public RuleActionTemplate ActionTemplate
		{
			get;
			set;
		}

		[DataMember(Name="FilterTemplate", Order=65537, EmitDefaultValue=false, IsRequired=true)]
		public Microsoft.ServiceBus.Messaging.FilterTemplate FilterTemplate
		{
			get;
			set;
		}

		[DataMember(Name="Initializes", Order=65537, EmitDefaultValue=false, IsRequired=false)]
		public ICollection<PropertyReference> Initializes
		{
			get;
			private set;
		}

		public override bool RequiresPreprocessing
		{
			get
			{
				return false;
			}
		}

		[DataMember(Name="Tag", Order=65537, EmitDefaultValue=false, IsRequired=false)]
		public object Tag
		{
			get;
			set;
		}

		public RuleCreationAction()
		{
			this.Initializes = new List<PropertyReference>();
		}

		public override BrokeredMessage Execute(BrokeredMessage message)
		{
			throw new NotSupportedException();
		}

		internal override BrokeredMessage Execute(BrokeredMessage message, RuleExecutionContext context)
		{
			RuleAction ruleAction;
			this.Validate();
			foreach (PropertyReference initialize in this.Initializes)
			{
				if (initialize.PropertyName.Scope != PropertyScope.System)
				{
					initialize.Value = message.Properties[initialize.PropertyName.Name];
				}
				else
				{
					initialize.Value = message.GetSystemProperty(initialize.PropertyName.Name);
				}
			}
			Filter filter = this.FilterTemplate.Create();
			if (this.ActionTemplate != null)
			{
				ruleAction = this.ActionTemplate.Create();
			}
			else
			{
				ruleAction = null;
			}
			RuleAction ruleAction1 = ruleAction;
			Guid guid = Guid.NewGuid();
			RuleDescription ruleDescription = new RuleDescription(guid.ToString(), filter)
			{
				Action = ruleAction1,
				Tag = PropertyReference.GetValue<string>(this.Tag)
			};
			context.AddRule(ruleDescription);
			return message;
		}

		[OnDeserialized]
		private void OnDeserialized(StreamingContext context)
		{
			if (this.Initializes == null)
			{
				this.Initializes = new Collection<PropertyReference>();
			}
		}

		public override RuleAction Preprocess()
		{
			return this;
		}

		public override void Validate()
		{
			if (this.FilterTemplate == null)
			{
				throw new RuleActionException(SRClient.RuleCreationActionRequiresFilterTemplate);
			}
			this.FilterTemplate.Validate(this.Initializes);
			if (this.ActionTemplate != null)
			{
				this.ActionTemplate.Validate(this.Initializes);
			}
			PropertyReference tag = this.Tag as PropertyReference;
			if (tag != null && !this.Initializes.Contains(tag))
			{
				throw new RuleActionException(SRClient.PropertyReferenceUsedWithoutInitializes(tag));
			}
		}
	}
}