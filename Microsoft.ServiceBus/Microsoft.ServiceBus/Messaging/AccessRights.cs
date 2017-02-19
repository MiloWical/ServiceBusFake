using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Name="AccessRights", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public enum AccessRights
	{
		[EnumMember]
		Manage,
		[EnumMember]
		Send,
		[EnumMember]
		Listen
	}
}