using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Name="EntityAvailabilityStatus", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public enum EntityAvailabilityStatus
	{
		[EnumMember]
		Unknown,
		[EnumMember]
		Available,
		[EnumMember]
		Limited,
		[EnumMember]
		Restoring
	}
}