using System;

namespace Microsoft.ServiceBus
{
	internal static class WebSocketConstants
	{
		internal const string Version = "13";

		internal const string GetRequestUri = "/$servicebus/websocket";

		internal static class Headers
		{
			public const string SecWebSocketAccept = "Sec-WebSocket-Accept";

			public const string SecWebSocketProtocol = "Sec-WebSocket-Protocol";

			public const string SecWebSocketKey = "Sec-WebSocket-Key";

			public const string SecWebSocketVersion = "Sec-WebSocket-Version";
		}

		internal static class SubProtocols
		{
			public const string RelayedConnection = "wsrelayedconnection";

			public const string RelayedOneway = "wsrelayedoneway";

			public const string Amqp = "wsrelayedamqp";

			public const string Amqp10 = "AMQPWSB10";
		}
	}
}