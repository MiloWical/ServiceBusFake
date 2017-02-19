using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging.Channels;
using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Channels
{
	internal abstract class ChannelAcceptor<TChannel> : CommunicationObject, Microsoft.ServiceBus.Channels.IChannelAcceptor<TChannel>, ICommunicationObjectInternals, ICommunicationObject
	where TChannel : class, IChannel
	{
		private ChannelManagerBase channelManager;

		protected ChannelManagerBase ChannelManager
		{
			get
			{
				return this.channelManager;
			}
		}

		protected override TimeSpan DefaultCloseTimeout
		{
			get
			{
				return ((IDefaultCommunicationTimeouts)this.channelManager).CloseTimeout;
			}
		}

		protected override TimeSpan DefaultOpenTimeout
		{
			get
			{
				return ((IDefaultCommunicationTimeouts)this.channelManager).OpenTimeout;
			}
		}

		protected ChannelAcceptor(ChannelManagerBase channelManager)
		{
			this.channelManager = channelManager;
		}

		public abstract TChannel AcceptChannel(TimeSpan timeout);

		public abstract IAsyncResult BeginAcceptChannel(TimeSpan timeout, AsyncCallback callback, object state);

		public abstract IAsyncResult BeginWaitForChannel(TimeSpan timeout, AsyncCallback callback, object state);

		public abstract TChannel EndAcceptChannel(IAsyncResult result);

		public abstract bool EndWaitForChannel(IAsyncResult result);

		void Microsoft.ServiceBus.Messaging.Channels.ICommunicationObjectInternals.ThrowIfDisposed()
		{
			base.ThrowIfDisposed();
		}

		void Microsoft.ServiceBus.Messaging.Channels.ICommunicationObjectInternals.ThrowIfDisposedOrNotOpen()
		{
			base.ThrowIfDisposedOrNotOpen();
		}

		protected override void OnAbort()
		{
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult(callback, state);
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult(callback, state);
		}

		protected override void OnClose(TimeSpan timeout)
		{
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
		}

		protected override void OnOpen(TimeSpan timeout)
		{
		}

		public abstract bool WaitForChannel(TimeSpan timeout);
	}
}