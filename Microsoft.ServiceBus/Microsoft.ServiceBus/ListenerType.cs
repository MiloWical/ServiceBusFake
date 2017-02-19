using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus
{
	[DataContract(Name="ListenerType", Namespace="http://schemas.microsoft.com/netservices/2009/05/servicebus/connect")]
	internal enum ListenerType
	{
		[EnumMember]
		None,
		[EnumMember]
		Unicast,
		[EnumMember]
		Multicast,
		[EnumMember]
		DirectConnection,
		[EnumMember]
		HybridConnection,
		[EnumMember]
		RelayedConnection,
		[EnumMember]
		RelayedHttp,
		[EnumMember]
		Junction,
		[EnumMember]
		RoutedHttp
	}
}