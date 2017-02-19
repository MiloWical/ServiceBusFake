using Microsoft.ServiceBus.Common;
using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Channels
{
	internal abstract class DelegatingTransportChannelListener<TChannel> : ChannelListenerBaseInternals<TChannel>
	where TChannel : class, IChannel
	{
		private Microsoft.ServiceBus.Channels.IChannelAcceptor<TChannel> channelAcceptor;

		public Microsoft.ServiceBus.Channels.IChannelAcceptor<TChannel> Acceptor
		{
			get
			{
				return this.channelAcceptor;
			}
			set
			{
				this.channelAcceptor = value;
			}
		}

		protected DelegatingTransportChannelListener(IDefaultCommunicationTimeouts timeouts) : base(timeouts)
		{
		}

		protected override void OnAbort()
		{
			if (this.channelAcceptor != null)
			{
				this.channelAcceptor.Abort();
			}
		}

		protected override TChannel OnAcceptChannel(TimeSpan timeout)
		{
			return this.channelAcceptor.AcceptChannel(timeout);
		}

		protected override IAsyncResult OnBeginAcceptChannel(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.channelAcceptor.BeginAcceptChannel(timeout, callback, state);
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new DelegatingCloseAsyncResult(this.channelAcceptor, timeout, callback, state);
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new DelegatingCloseAsyncResult(this.channelAcceptor, timeout, callback, state);
		}

		protected override IAsyncResult OnBeginWaitForChannel(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.channelAcceptor.BeginWaitForChannel(timeout, callback, state);
		}

		protected override void OnClose(TimeSpan timeout)
		{
			this.channelAcceptor.Close(timeout);
		}

		protected override TChannel OnEndAcceptChannel(IAsyncResult result)
		{
			return this.channelAcceptor.EndAcceptChannel(result);
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			AsyncResult<DelegatingCloseAsyncResult>.End(result);
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			AsyncResult<DelegatingCloseAsyncResult>.End(result);
		}

		protected override bool OnEndWaitForChannel(IAsyncResult result)
		{
			return this.channelAcceptor.EndWaitForChannel(result);
		}

		protected override void OnOpen(TimeSpan timeout)
		{
			this.channelAcceptor.Open(timeout);
		}

		protected override bool OnWaitForChannel(TimeSpan timeout)
		{
			return this.channelAcceptor.WaitForChannel(timeout);
		}
	}
}