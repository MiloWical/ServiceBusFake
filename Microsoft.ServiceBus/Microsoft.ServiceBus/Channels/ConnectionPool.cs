using System;

namespace Microsoft.ServiceBus.Channels
{
	internal abstract class ConnectionPool : IdlingCommunicationPool<string, IConnection>
	{
		private int connectionBufferSize;

		private TimeSpan maxOutputDelay;

		private string name;

		public string Name
		{
			get
			{
				return this.name;
			}
		}

		protected ConnectionPool(IConnectionOrientedTransportChannelFactorySettings settings, TimeSpan leaseTimeout) : base(settings.MaxOutboundConnectionsPerEndpoint, settings.IdleTimeout, leaseTimeout)
		{
			this.connectionBufferSize = settings.ConnectionBufferSize;
			this.maxOutputDelay = settings.MaxOutputDelay;
			this.name = settings.ConnectionPoolGroupName;
		}

		protected override void AbortItem(IConnection item)
		{
			item.Abort();
		}

		protected override void CloseItem(IConnection item, TimeSpan timeout)
		{
			item.Close(timeout);
		}

		public virtual bool IsCompatible(IConnectionOrientedTransportChannelFactorySettings settings)
		{
			if (!(this.name == settings.ConnectionPoolGroupName) || this.connectionBufferSize != settings.ConnectionBufferSize || base.MaxIdleConnectionPoolCount != settings.MaxOutboundConnectionsPerEndpoint || !(base.IdleTimeout == settings.IdleTimeout))
			{
				return false;
			}
			return this.maxOutputDelay == settings.MaxOutputDelay;
		}
	}
}