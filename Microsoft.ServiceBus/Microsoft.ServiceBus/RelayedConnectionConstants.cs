using System;

namespace Microsoft.ServiceBus
{
	internal class RelayedConnectionConstants
	{
		public const string NamespaceUri = "http://schemas.microsoft.com/netservices/2009/05/servicebus/relayedconnect";

		public RelayedConnectionConstants()
		{
		}

		internal static class Actions
		{
			public const string RelayedConnectRequest = "http://schemas.microsoft.com/netservices/2009/05/servicebus/relayedconnect/RelayedConnect";
		}
	}
}