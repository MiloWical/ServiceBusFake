using Microsoft.ServiceBus.Channels;
using System;
using System.ServiceModel;

namespace Microsoft.ServiceBus
{
	internal class SocketConnectionPoolRegistry : ConnectionPoolRegistry
	{
		public SocketConnectionPoolRegistry()
		{
		}

		protected override ConnectionPool CreatePool(IConnectionOrientedTransportChannelFactorySettings settings)
		{
			return new SocketConnectionPoolRegistry.SocketConnectionConnectionPool((ISocketConnectionChannelFactorySettings)settings);
		}

		private class SocketConnectionConnectionPool : ConnectionPool
		{
			public SocketConnectionConnectionPool(ISocketConnectionChannelFactorySettings settings) : base(settings, settings.LeaseTimeout)
			{
			}

			protected override string GetPoolKey(EndpointAddress address, Uri via)
			{
				return via.ToString();
			}

			public override bool IsCompatible(IConnectionOrientedTransportChannelFactorySettings settings)
			{
				ISocketConnectionChannelFactorySettings socketConnectionChannelFactorySetting = (ISocketConnectionChannelFactorySettings)settings;
				if (base.LeaseTimeout != socketConnectionChannelFactorySetting.LeaseTimeout)
				{
					return false;
				}
				return base.IsCompatible(settings);
			}
		}
	}
}