using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using System;

namespace Microsoft.ServiceBus
{
	internal class SocketConnectionTransportManager : ConnectionOrientedTransportManager<SocketConnectionChannelListener>
	{
		private ConnectionDemuxer connectionDemuxer;

		private IConnectionElement connectionElement;

		private IConnectionListener connectionListener;

		private int listenBacklog;

		private Uri listenUri;

		private SocketConnectionTransportManagerRegistration registration;

		public int ListenBacklog
		{
			get
			{
				return this.listenBacklog;
			}
		}

		internal override string Scheme
		{
			get
			{
				return "sb";
			}
		}

		public SocketConnectionTransportManager(SocketConnectionTransportManagerRegistration registration, SocketConnectionChannelListener channelListener)
		{
			base.ApplyListenerSettings(channelListener);
			this.listenUri = channelListener.BaseUri;
			this.listenBacklog = channelListener.ListenBacklog;
			this.registration = registration;
			this.connectionElement = channelListener.ConnectionElement;
		}

		internal override void OnClose(TimeSpan timeout)
		{
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			try
			{
				this.connectionDemuxer.Close(timeoutHelper.RemainingTime());
			}
			catch (Exception exception)
			{
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				this.connectionDemuxer.Abort();
			}
			try
			{
				this.connectionListener.Close(timeoutHelper.RemainingTime());
			}
			catch (Exception exception1)
			{
				if (Fx.IsFatal(exception1))
				{
					throw;
				}
				this.connectionListener.Abort();
			}
			this.registration.Dispose();
			this.registration = null;
		}

		internal override void OnOpen(TimeSpan timeout)
		{
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			IConnectionListener connectionListener = this.connectionElement.CreateListener(base.ConnectionBufferSize, this.listenUri);
			this.connectionListener = new BufferedConnectionListener(connectionListener, base.MaxOutputDelay, base.ConnectionBufferSize);
			this.connectionDemuxer = new ConnectionDemuxer(this.connectionListener, base.MaxPendingAccepts, base.MaxPendingConnections, base.ChannelInitializationTimeout, base.IdleTimeout, base.MaxPooledConnections, new TransportSettingsCallback(this.OnGetTransportFactorySettings), new SingletonPreambleDemuxCallback(this.OnGetSingletonMessageHandler), new ServerSessionPreambleDemuxCallback(this.OnHandleServerSessionPreamble), new ErrorCallback(this.OnDemuxerError));
			bool flag = false;
			try
			{
				this.connectionDemuxer.Open(timeoutHelper.RemainingTime());
				flag = true;
			}
			finally
			{
				if (!flag)
				{
					if (this.registration != null)
					{
						this.registration.Dispose();
						this.registration = null;
					}
					this.connectionDemuxer.Abort();
				}
			}
		}
	}
}