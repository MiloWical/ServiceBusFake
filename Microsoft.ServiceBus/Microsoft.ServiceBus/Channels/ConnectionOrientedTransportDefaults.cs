using Microsoft.ServiceBus;
using System;
using System.Net.Security;
using System.ServiceModel;

namespace Microsoft.ServiceBus.Channels
{
	internal static class ConnectionOrientedTransportDefaults
	{
		internal const int ConnectionBufferSize = 65536;

		internal const string ConnectionPoolGroupName = "default";

		internal const System.ServiceModel.HostNameComparisonMode HostNameComparisonMode = System.ServiceModel.HostNameComparisonMode.StrongWildcard;

		internal const string IdleTimeoutString = "00:02:00";

		internal const string ChannelInitializationTimeoutString = "00:01:00";

		internal const int MaxContentTypeSize = 256;

		internal const int MaxOutboundConnectionsPerEndpoint = 10;

		internal const int MaxPendingConnections = 10;

		internal const string MaxOutputDelayString = "00:00:00.2";

		internal const int MaxPendingAccepts = 1;

		internal const int MaxViaSize = 2048;

		internal const System.Net.Security.ProtectionLevel ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;

		internal const System.ServiceModel.TransferMode TransferMode = System.ServiceModel.TransferMode.Buffered;

		internal const TransportClientCredentialType CredentialType = TransportClientCredentialType.Unauthenticated;

		internal static TimeSpan ChannelInitializationTimeout
		{
			get
			{
				return Microsoft.ServiceBus.Channels.TimeSpanHelper.FromMinutes(1, "00:01:00");
			}
		}

		internal static TimeSpan IdleTimeout
		{
			get
			{
				return Microsoft.ServiceBus.Channels.TimeSpanHelper.FromMinutes(2, "00:02:00");
			}
		}

		internal static TimeSpan MaxOutputDelay
		{
			get
			{
				return Microsoft.ServiceBus.Channels.TimeSpanHelper.FromMilliseconds(200, "00:00:00.2");
			}
		}
	}
}