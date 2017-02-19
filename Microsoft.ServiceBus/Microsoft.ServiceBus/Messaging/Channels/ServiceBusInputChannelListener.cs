using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Messaging.Channels
{
	internal sealed class ServiceBusInputChannelListener : ServiceBusChannelListener<IInputChannel>
	{
		private readonly InputQueue<IInputChannel> singletonAcceptor;

		public ServiceBusInputChannelListener(BindingContext context, NetMessagingTransportBindingElement transport) : base(context, transport)
		{
			this.singletonAcceptor = new InputQueue<IInputChannel>();
		}

		protected override void OnAbort()
		{
			this.singletonAcceptor.Close();
			base.OnAbort();
		}

		protected override IInputChannel OnAcceptChannel(TimeSpan timeout)
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
			return base.OnBeginClose(timeout, callback, state);
		}

		protected override IAsyncResult OnBeginWaitForChannel(TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new NotSupportedException(), null);
		}

		protected override void OnClose(TimeSpan timeout)
		{
			this.singletonAcceptor.Close();
			base.OnClose(timeout);
		}

		protected override IInputChannel OnEndAcceptChannel(IAsyncResult result)
		{
			return this.singletonAcceptor.EndDequeue(result);
		}

		protected override bool OnEndWaitForChannel(IAsyncResult result)
		{
			throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new NotSupportedException(), null);
		}

		private void OnNewChannelNeeded(object sender, EventArgs args)
		{
			if (!base.IsDisposed && CommunicationState.Opened == base.State)
			{
				IInputChannel serviceBusInputChannel = new ServiceBusInputChannel(this);
				serviceBusInputChannel.SafeAddClosed(new EventHandler(this.OnNewChannelNeeded));
				this.singletonAcceptor.EnqueueAndDispatch(serviceBusInputChannel);
			}
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			this.OnNewChannelNeeded(this, EventArgs.Empty);
		}

		protected override bool OnWaitForChannel(TimeSpan timeout)
		{
			throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new NotSupportedException(), null);
		}
	}
}