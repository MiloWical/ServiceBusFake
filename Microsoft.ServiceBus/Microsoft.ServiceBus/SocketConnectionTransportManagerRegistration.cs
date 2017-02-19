using Microsoft.ServiceBus.Channels;
using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace Microsoft.ServiceBus
{
	internal class SocketConnectionTransportManagerRegistration : TransportManagerRegistration, IDisposable
	{
		private int connectionBufferSize;

		private IConnectionElement connectionElement;

		private TimeSpan channelInitializationTimeout;

		private TimeSpan idleTimeout;

		private int maxPooledConnections;

		private bool teredoEnabled;

		private int listenBacklog;

		private TimeSpan maxOutputDelay;

		private int maxPendingConnections;

		private int maxPendingAccepts;

		private TransferMode transferMode;

		private SocketConnectionTransportManager transportManager;

		private UriPrefixTable<ITransportManagerRegistration> transportManagerTable;

		public bool TeredoEnabled
		{
			get
			{
				return this.teredoEnabled;
			}
		}

		public SocketConnectionTransportManagerRegistration(Uri listenUri, SocketConnectionChannelListener channelListener) : base(listenUri, channelListener.HostNameComparisonMode)
		{
			this.connectionBufferSize = channelListener.ConnectionBufferSize;
			this.channelInitializationTimeout = channelListener.ChannelInitializationTimeout;
			this.teredoEnabled = channelListener.TeredoEnabled;
			this.listenBacklog = channelListener.ListenBacklog;
			this.maxOutputDelay = channelListener.MaxOutputDelay;
			this.maxPendingConnections = channelListener.MaxPendingConnections;
			this.maxPendingAccepts = channelListener.MaxPendingAccepts;
			this.idleTimeout = channelListener.IdleTimeout;
			this.maxPooledConnections = channelListener.MaxPooledConnections;
			this.transferMode = channelListener.TransferMode;
			this.connectionElement = channelListener.ConnectionElement;
			this.transportManagerTable = channelListener.TransportManagerTable;
		}

		public void Dispose()
		{
			this.transportManager = null;
			this.transportManagerTable.UnregisterUri(base.ListenUri, base.HostNameComparisonMode);
			GC.SuppressFinalize(this);
		}

		private bool IsCompatible(SocketConnectionChannelListener channelListener)
		{
			if (channelListener.InheritBaseAddressSettings)
			{
				return true;
			}
			if (!this.connectionElement.IsCompatible(channelListener.ConnectionElement) || !(this.channelInitializationTimeout == channelListener.ChannelInitializationTimeout) || !(this.idleTimeout == channelListener.IdleTimeout) || this.maxPooledConnections != channelListener.MaxPooledConnections || this.connectionBufferSize != channelListener.ConnectionBufferSize || this.teredoEnabled != channelListener.TeredoEnabled || this.listenBacklog != channelListener.ListenBacklog || this.maxPendingConnections != channelListener.MaxPendingConnections || !(this.maxOutputDelay == channelListener.MaxOutputDelay) || this.maxPendingAccepts != channelListener.MaxPendingAccepts)
			{
				return false;
			}
			return this.transferMode == TransferMode.Buffered == channelListener.TransferMode == TransferMode.Buffered;
		}

		private void ProcessSelection(SocketConnectionChannelListener channelListener, ref SocketConnectionTransportManager transportManager, IList<TransportManager> result)
		{
			if (transportManager == null)
			{
				transportManager = new SocketConnectionTransportManager(this, channelListener);
			}
			result.Add(transportManager);
		}

		public override IList<TransportManager> Select(TransportChannelListener channelListener)
		{
			SocketConnectionChannelListener socketConnectionChannelListener = (SocketConnectionChannelListener)channelListener;
			if (!this.IsCompatible(socketConnectionChannelListener))
			{
				return null;
			}
			IList<TransportManager> transportManagers = new List<TransportManager>();
			this.ProcessSelection(socketConnectionChannelListener, ref this.transportManager, transportManagers);
			return transportManagers;
		}
	}
}