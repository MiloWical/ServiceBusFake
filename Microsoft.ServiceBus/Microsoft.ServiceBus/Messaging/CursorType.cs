using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Name="CursorType", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public enum CursorType
	{
		[EnumMember]
		Server,
		[EnumMember]
		Client
	}
}