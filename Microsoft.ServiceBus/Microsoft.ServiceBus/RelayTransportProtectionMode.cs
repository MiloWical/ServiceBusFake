using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus
{
	[DataContract(Name="RelayTransportProtectionMode", Namespace="http://schemas.microsoft.com/netservices/2009/05/servicebus/connect")]
	public enum RelayTransportProtectionMode
	{
		[EnumMember]
		None,
		[EnumMember]
		ListenerOnly,
		[EnumMember]
		EndToEnd
	}
}