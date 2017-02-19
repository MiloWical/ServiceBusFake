using System;

namespace Microsoft.ServiceBus
{
	internal enum InternalConnectivityMode
	{
		Tcp,
		Http,
		Https,
		HttpsWebSocket
	}
}