using System;

namespace Microsoft.ServiceBus
{
	internal class DirectConnectionConstants
	{
		public const string NamespaceUri = "http://schemas.microsoft.com/netservices/2009/05/servicebus/directconnect";

		public const string Abort = "Abort";

		public const string Connect = "Connect";

		public const string ConnectRetry = "ConnectRetry";

		public const string ConnectResponse = "ConnectResponse";

		public const string SwitchRoles = "SwitchRoles";

		public DirectConnectionConstants()
		{
		}

		internal static class Actions
		{
			public const string AbortRequest = "http://schemas.microsoft.com/netservices/2009/05/servicebus/directconnect/Abort";

			public const string ConnectRequest = "http://schemas.microsoft.com/netservices/2009/05/servicebus/directconnect/Connect";

			public const string ConnectRetryRequest = "http://schemas.microsoft.com/netservices/2009/05/servicebus/directconnect/ConnectRetry";

			public const string ConnectResponseRequest = "http://schemas.microsoft.com/netservices/2009/05/servicebus/directconnect/ConnectResponse";

			public const string SwitchRolesRequest = "http://schemas.microsoft.com/netservices/2009/05/servicebus/directconnect/SwitchRoles";
		}
	}
}