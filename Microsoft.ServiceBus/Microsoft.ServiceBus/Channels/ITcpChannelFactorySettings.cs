using System;
using System.ServiceModel;

namespace Microsoft.ServiceBus.Channels
{
	internal interface ITcpChannelFactorySettings : IConnectionOrientedTransportChannelFactorySettings, IConnectionOrientedTransportFactorySettings, ITransportFactorySettings, IDefaultCommunicationTimeouts, IConnectionOrientedConnectionSettings
	{
		TimeSpan LeaseTimeout
		{
			get;
		}
	}
}