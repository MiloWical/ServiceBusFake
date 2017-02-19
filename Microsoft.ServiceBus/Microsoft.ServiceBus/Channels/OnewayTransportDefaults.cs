using Microsoft.ServiceBus;
using System;

namespace Microsoft.ServiceBus.Channels
{
	internal static class OnewayTransportDefaults
	{
		internal const string ConnectionLeaseTimeoutString = "00:05:00";

		internal const RelayedOnewayConnectionMode ConnectionMode = RelayedOnewayConnectionMode.Unicast;

		internal const RelayClientAuthenticationType DefaultRelayClientAuthenticationType = RelayClientAuthenticationType.RelayAccessToken;

		internal static TimeSpan ConnectionLeaseTimeout
		{
			get
			{
				return TimeSpanHelper.FromMinutes(5, "00:05:00");
			}
		}
	}
}