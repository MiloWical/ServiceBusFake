using System;

namespace Microsoft.ServiceBus
{
	internal static class ProtocolVersioningHelper
	{
		private const string NetServices = "netservices";

		public const string ServiceBusNetService = "servicebus";

		public const string UsageService = "usage";

		public const string VersionYear = "2009";

		public const string VersionMonth = "05";

		public const string ConfigNamePostfix = "2009.05";

		public const int CurrentVersionOrder = 1;

		private const string NameSpacePrefix = "http://schemas.microsoft.com/netservices/2009/05";

		public const string ServiceBusNameSpace = "http://schemas.microsoft.com/netservices/2009/05/servicebus";

		public const string UsageNameSpace = "http://schemas.microsoft.com/netservices/2009/05/usage";
	}
}