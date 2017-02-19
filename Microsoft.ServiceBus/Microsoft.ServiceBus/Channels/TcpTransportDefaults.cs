using Microsoft.ServiceBus;
using System;

namespace Microsoft.ServiceBus.Channels
{
	internal static class TcpTransportDefaults
	{
		internal const int ListenBacklog = 10;

		internal const string ConnectionLeaseTimeoutString = "00:05:00";

		internal const TcpRelayConnectionMode ConnectionMode = TcpRelayConnectionMode.Relayed;

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