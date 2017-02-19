using Microsoft.ServiceBus;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Name="Filter", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	[KnownType(typeof(CorrelationFilter))]
	[KnownType(typeof(FalseFilter))]
	[KnownType(typeof(SqlFilter))]
	[KnownType(typeof(TrueFilter))]
	[KnownType(typeof(DateTimeOffset))]
	public abstract class Filter : IExtensibleDataObject
	{
		internal abstract Microsoft.ServiceBus.Messaging.FilterType FilterType
		{
			get;
		}

		public abstract bool RequiresPreprocessing
		{
			get;
		}

		ExtensionDataObject System.Runtime.Serialization.IExtensibleDataObject.ExtensionData
		{
			get;
			set;
		}

		internal Filter()
		{
		}

		internal virtual bool IsValidForVersion(ApiVersion version)
		{
			return true;
		}

		public abstract bool Match(BrokeredMessage message);

		public abstract Filter Preprocess();

		internal virtual void UpdateForVersion(ApiVersion version, Filter existingFilter = null)
		{
		}

		public abstract void Validate();
	}
}