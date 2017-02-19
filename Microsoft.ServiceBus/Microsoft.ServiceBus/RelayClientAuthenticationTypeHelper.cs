using System;

namespace Microsoft.ServiceBus
{
	internal static class RelayClientAuthenticationTypeHelper
	{
		internal static bool IsDefined(RelayClientAuthenticationType value)
		{
			if (value == RelayClientAuthenticationType.RelayAccessToken)
			{
				return true;
			}
			return value == RelayClientAuthenticationType.None;
		}
	}
}