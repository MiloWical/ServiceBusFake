using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	[KnownType(typeof(SqlRuleActionTemplate))]
	internal abstract class RuleActionTemplate
	{
		internal RuleActionTemplate()
		{
		}

		public abstract RuleAction Create();

		internal abstract void Validate(ICollection<PropertyReference> initializationList);
	}
}