using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Name="FilterTemplate", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	[KnownType(typeof(CorrelationFilterTemplate))]
	[KnownType(typeof(SqlFilterTemplate))]
	internal abstract class FilterTemplate
	{
		internal FilterTemplate()
		{
		}

		public abstract Filter Create();

		internal abstract void Validate(ICollection<PropertyReference> initializationList);
	}
}