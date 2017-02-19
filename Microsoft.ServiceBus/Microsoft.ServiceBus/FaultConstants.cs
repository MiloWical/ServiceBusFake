using System;

namespace Microsoft.ServiceBus
{
	internal class FaultConstants
	{
		public const string AddressAlreadyInUseFault = "AddressAlreadyInUseFault";

		public const string AddressNotFoundFault = "AddressNotFoundFault";

		public const string AddressReplacedFault = "AddressReplacedFault";

		public const string AuthorizationFailedFault = "AuthorizationFailedFault";

		public const string ConnectionFailedFault = "ConnectionFailedFault";

		public const string EndpointNotFoundFault = "EndpointNotFoundFault";

		public const string RelayNotFoundFault = "RelayNotFoundFault";

		public const string FaultAction = "http://schemas.microsoft.com/netservices/2009/05/servicebus/relay/FaultAction";

		public const string InvalidRequestFault = "InvalidRequestFault";

		public const string Namespace = "http://schemas.microsoft.com/netservices/2009/05/servicebus/relay";

		public const string QuotaExceededFault = "QuotaExceededFault";

		public const string ServerErrorFault = "ServerErrorFault";

		public const string NoTransportSecurityFault = "NoTransportSecurityFault";

		public FaultConstants()
		{
		}
	}
}