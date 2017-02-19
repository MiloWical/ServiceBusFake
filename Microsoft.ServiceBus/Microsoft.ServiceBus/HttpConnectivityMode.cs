using System;

namespace Microsoft.ServiceBus
{
	internal enum HttpConnectivityMode
	{
		Http,
		Https,
		HttpsWebSocket,
		AutoDetect
	}
}