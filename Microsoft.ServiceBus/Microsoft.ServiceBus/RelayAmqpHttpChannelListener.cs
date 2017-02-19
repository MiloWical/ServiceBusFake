using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus
{
	internal class RelayAmqpHttpChannelListener : ChannelListenerBaseInternals<IReplyChannel>
	{
		private readonly InputQueue<IReplyChannel> singletonAcceptor;

		public override System.Uri Uri
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public RelayAmqpHttpChannelListener(BindingContext context) : base(context.Binding)
		{
			this.singletonAcceptor = new InputQueue<IReplyChannel>();
		}

		protected override void OnAbort()
		{
			this.singletonAcceptor.Close();
		}

		protected override IReplyChannel OnAcceptChannel(TimeSpan timeout)
		{
			return this.singletonAcceptor.Dequeue(timeout);
		}

		protected override IAsyncResult OnBeginAcceptChannel(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.singletonAcceptor.BeginDequeue(timeout, callback, state);
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			this.singletonAcceptor.Close();
			return new CompletedAsyncResult(callback, state);
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult(callback, state);
		}

		protected override IAsyncResult OnBeginWaitForChannel(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.singletonAcceptor.BeginWaitForItem(timeout, callback, state);
		}

		protected override void OnClose(TimeSpan timeout)
		{
			this.singletonAcceptor.Close();
		}

		protected override IReplyChannel OnEndAcceptChannel(IAsyncResult result)
		{
			return this.singletonAcceptor.EndDequeue(result);
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
		}

		protected override bool OnEndWaitForChannel(IAsyncResult result)
		{
			return this.singletonAcceptor.EndWaitForItem(result);
		}

		private void OnNewChannelNeeded(object sender, EventArgs args)
		{
			if (!base.IsDisposed && CommunicationState.Opened == base.State)
			{
				IReplyChannel relayAmqpHttpChannel = new RelayAmqpHttpChannel(this);
				relayAmqpHttpChannel.SafeAddClosed(new EventHandler(this.OnNewChannelNeeded));
				this.singletonAcceptor.EnqueueAndDispatch(relayAmqpHttpChannel);
			}
		}

		protected override void OnOpen(TimeSpan timeout)
		{
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			this.OnNewChannelNeeded(this, EventArgs.Empty);
		}

		protected override bool OnWaitForChannel(TimeSpan timeout)
		{
			return this.singletonAcceptor.WaitForItem(timeout);
		}
	}
}