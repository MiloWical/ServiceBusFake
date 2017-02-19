using System;

namespace Microsoft.ServiceBus.Channels
{
	internal delegate IConnectionOrientedTransportFactorySettings TransportSettingsCallback(Uri via);
}