using System;

namespace Microsoft.ServiceBus.Channels
{
	internal delegate void ConnectionAvailableCallback(IConnection connection, Action connectionDequeuedCallback);
}