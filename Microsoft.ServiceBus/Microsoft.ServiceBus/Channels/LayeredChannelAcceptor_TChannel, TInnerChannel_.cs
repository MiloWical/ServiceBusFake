using System;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Channels
{
	internal abstract class LayeredChannelAcceptor<TChannel, TInnerChannel> : Microsoft.ServiceBus.Channels.ChannelAcceptor<TChannel>
	where TChannel : class, IChannel
	where TInnerChannel : class, IChannel
	{
		private IChannelListener<TInnerChannel> innerListener;

		protected LayeredChannelAcceptor(ChannelManagerBase channelManager, IChannelListener<TInnerChannel> innerListener) : base(channelManager)
		{
			this.innerListener = innerListener;
		}

		public override TChannel AcceptChannel(TimeSpan timeout)
		{
			TInnerChannel tInnerChannel = this.innerListener.AcceptChannel(timeout);
			if (tInnerChannel == null)
			{
				return default(TChannel);
			}
			return this.OnAcceptChannel(tInnerChannel);
		}

		public override IAsyncResult BeginAcceptChannel(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.innerListener.BeginAcceptChannel(timeout, callback, state);
		}

		public override IAsyncResult BeginWaitForChannel(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.innerListener.BeginWaitForChannel(timeout, callback, state);
		}

		public override TChannel EndAcceptChannel(IAsyncResult result)
		{
			TInnerChannel tInnerChannel = this.innerListener.EndAcceptChannel(result);
			if (tInnerChannel == null)
			{
				return default(TChannel);
			}
			return this.OnAcceptChannel(tInnerChannel);
		}

		public override bool EndWaitForChannel(IAsyncResult result)
		{
			return this.innerListener.EndWaitForChannel(result);
		}

		protected abstract TChannel OnAcceptChannel(TInnerChannel innerChannel);

		public override bool WaitForChannel(TimeSpan timeout)
		{
			return this.innerListener.WaitForChannel(timeout);
		}
	}
}