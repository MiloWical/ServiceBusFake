using System;

namespace Microsoft.ServiceBus
{
	internal static class WebStreamConstants
	{
		internal static class Headers
		{
			public const string WebStreamCreate = "X-WSCREATE";

			public const string WebStreamConnect = "X-WSCONNECT";

			public const string WebStreamEndpoint1 = "X-WSENDPT1";

			public const string WebStreamEndpoint2 = "X-WSENDPT2";
		}

		internal static class Roles
		{
			public const string RelayedConnection = "connection";

			public const string Oneway = "oneway";

			public const string Messaging = "messaging";
		}
	}
}