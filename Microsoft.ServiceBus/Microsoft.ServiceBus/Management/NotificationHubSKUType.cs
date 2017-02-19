using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Management
{
	public enum NotificationHubSKUType
	{
		[EnumMember]
		Free = 1,
		[EnumMember]
		Basic = 2,
		[EnumMember]
		Standard = 3
	}
}