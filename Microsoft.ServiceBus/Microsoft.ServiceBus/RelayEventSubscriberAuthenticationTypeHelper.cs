using System;

namespace Microsoft.ServiceBus
{
	internal static class RelayEventSubscriberAuthenticationTypeHelper
	{
		internal static bool IsDefined(RelayEventSubscriberAuthenticationType value)
		{
			if (value == RelayEventSubscriberAuthenticationType.RelayAccessToken)
			{
				return true;
			}
			return value == RelayEventSubscriberAuthenticationType.None;
		}
	}
}