using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus
{
	[DataContract(Name="DiscoveryType", Namespace="http://schemas.microsoft.com/netservices/2009/05/servicebus/connect")]
	public enum DiscoveryType
	{
		[EnumMember]
		Public,
		[EnumMember]
		Private
	}
}