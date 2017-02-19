using System;

namespace Microsoft.ServiceBus
{
	internal class RouterConstants
	{
		public const string AddressAlreadyInUseFault = "AddressAlreadyInUseFault";

		public const string CreateRoute = "CreateRoute";

		public const string CreateRouteReply = "CreateRouteReply";

		public const string DeleteRoute = "DeleteRoute";

		public const string DeleteRouteReply = "DeleteRouteReply";

		public const string GetRoute = "GetRoute";

		public const string GetRouteReply = "GetRouteReply";

		public const string Namespace = "http://schemas.microsoft.com/netservices/2009/05/servicebus/connect";

		public const string RouteContract = "RouteContract";

		public const string RouteDescription = "RouteDescription";

		public const string RouteEndpoint = "RouteEndpoint";

		public const string RouteFactoryContract = "RouteFactoryContract";

		public const string RouteRequestContract = "RouteRequestContract";

		public const string RouteSendContract = "RouteSendContract";

		public const string HttpHeadersCollectionElement = "HttpHeaders";

		public const string HttpHeaderElement = "HttpHeader";

		public RouterConstants()
		{
		}
	}
}