using Microsoft.ServiceBus;
using System;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Channels
{
	internal static class TransportDefaults
	{
		internal const System.ServiceModel.HostNameComparisonMode HostNameComparisonMode = System.ServiceModel.HostNameComparisonMode.Exact;

		internal const TokenImpersonationLevel ImpersonationLevel = TokenImpersonationLevel.Identification;

		internal const bool ManualAddressing = false;

		internal const long MaxReceivedMessageSize = 65536L;

		internal const int MaxDrainSize = 65536;

		internal const long MaxBufferPoolSize = 524288L;

		internal const int MaxBufferSize = 65536;

		internal const bool RequireClientCertificate = false;

		internal const int MaxFaultSize = 65536;

		internal const int MaxSecurityFaultSize = 16384;

		internal const int MaxRMFaultSize = 65536;

		internal static MessageEncoderFactory GetDefaultMessageEncoderFactory()
		{
			return ClientMessageUtility.DefaultBinaryMessageEncoderFactory;
		}
	}
}