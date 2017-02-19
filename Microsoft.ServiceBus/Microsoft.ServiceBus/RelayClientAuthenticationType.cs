using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus
{
	[DataContract(Name="RelayClientAuthenticationType", Namespace="http://schemas.microsoft.com/netservices/2009/05/servicebus/connect")]
	public enum RelayClientAuthenticationType
	{
		[EnumMember]
		RelayAccessToken,
		[EnumMember]
		None
	}
}