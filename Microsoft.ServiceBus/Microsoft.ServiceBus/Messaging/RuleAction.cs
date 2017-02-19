using Microsoft.ServiceBus;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Name="RuleAction", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	[KnownType(typeof(CompositeAction))]
	[KnownType(typeof(EmptyRuleAction))]
	[KnownType(typeof(RuleCreationAction))]
	[KnownType(typeof(SqlRuleAction))]
	[KnownType(typeof(DateTimeOffset))]
	public abstract class RuleAction : IExtensibleDataObject
	{
		public abstract bool RequiresPreprocessing
		{
			get;
		}

		ExtensionDataObject System.Runtime.Serialization.IExtensibleDataObject.ExtensionData
		{
			get;
			set;
		}

		internal RuleAction()
		{
		}

		public abstract BrokeredMessage Execute(BrokeredMessage message);

		internal abstract BrokeredMessage Execute(BrokeredMessage message, RuleExecutionContext context);

		internal virtual bool IsValidForVersion(ApiVersion version)
		{
			return true;
		}

		public abstract RuleAction Preprocess();

		internal virtual void UpdateForVersion(ApiVersion version, RuleAction existingAction = null)
		{
		}

		public abstract void Validate();
	}
}