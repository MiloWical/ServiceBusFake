using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Name="EntityStatus", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public enum EntityStatus
	{
		[EnumMember]
		Active,
		[EnumMember]
		Disabled,
		[EnumMember]
		Restoring,
		[EnumMember]
		SendDisabled,
		[EnumMember]
		ReceiveDisabled,
		[EnumMember]
		Creating,
		[EnumMember]
		Deleting
	}
}