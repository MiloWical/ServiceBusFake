using Microsoft.ServiceBus.Channels;
using System;
using System.ServiceModel;

namespace Microsoft.ServiceBus
{
	internal interface ISocketConnectionChannelFactorySettings : IConnectionOrientedTransportChannelFactorySettings, IConnectionOrientedTransportFactorySettings, ITransportFactorySettings, IDefaultCommunicationTimeouts, IConnectionOrientedConnectionSettings
	{
		TimeSpan LeaseTimeout
		{
			get;
		}
	}
}