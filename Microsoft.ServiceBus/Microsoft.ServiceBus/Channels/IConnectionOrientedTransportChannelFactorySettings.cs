using System;
using System.ServiceModel;

namespace Microsoft.ServiceBus.Channels
{
	internal interface IConnectionOrientedTransportChannelFactorySettings : IConnectionOrientedTransportFactorySettings, ITransportFactorySettings, IDefaultCommunicationTimeouts, IConnectionOrientedConnectionSettings
	{
		string ConnectionPoolGroupName
		{
			get;
		}

		int MaxOutboundConnectionsPerEndpoint
		{
			get;
		}
	}
}