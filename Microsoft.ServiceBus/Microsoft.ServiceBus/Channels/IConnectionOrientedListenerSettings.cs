using System;

namespace Microsoft.ServiceBus.Channels
{
	internal interface IConnectionOrientedListenerSettings : IConnectionOrientedConnectionSettings
	{
		TimeSpan ChannelInitializationTimeout
		{
			get;
		}

		int MaxPendingAccepts
		{
			get;
		}

		int MaxPendingConnections
		{
			get;
		}

		int MaxPooledConnections
		{
			get;
		}
	}
}