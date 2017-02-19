using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus
{
	[DataContract(Name="RelayType", Namespace="http://schemas.microsoft.com/netservices/2009/05/servicebus/connect")]
	public enum RelayType
	{
		[EnumMember]
		None,
		[EnumMember]
		NetTcp,
		[EnumMember]
		Http,
		[EnumMember]
		NetEvent,
		[EnumMember]
		NetOneway
	}
}