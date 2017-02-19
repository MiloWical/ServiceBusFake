using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging.Channels;
using System;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Channels
{
	internal class TransportReplyChannelAcceptor : Microsoft.ServiceBus.Channels.ReplyChannelAcceptor
	{
		private Microsoft.ServiceBus.Channels.TransportManagerContainer transportManagerContainer;

		private Microsoft.ServiceBus.Channels.TransportChannelListener listener;

		public TransportReplyChannelAcceptor(Microsoft.ServiceBus.Channels.TransportChannelListener listener) : base(listener, () => listener.GetPendingException())
		{
			this.listener = listener;
		}

		private IAsyncResult DummyBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult(callback, state);
		}

		private void DummyEndClose(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
		}

		protected override void OnAbort()
		{
			base.OnAbort();
			if (this.transportManagerContainer != null && !this.TransferTransportManagers())
			{
				this.transportManagerContainer.Close(this.DefaultCloseTimeout);
			}
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			Microsoft.ServiceBus.Common.ChainedBeginHandler chainedBeginHandler = new Microsoft.ServiceBus.Common.ChainedBeginHandler(this.DummyBeginClose);
			Microsoft.ServiceBus.Common.ChainedEndHandler chainedEndHandler = new Microsoft.ServiceBus.Common.ChainedEndHandler(this.DummyEndClose);
			if (this.transportManagerContainer != null && !this.TransferTransportManagers())
			{
				chainedBeginHandler = new Microsoft.ServiceBus.Common.ChainedBeginHandler(this.transportManagerContainer.BeginClose);
				chainedEndHandler = new Microsoft.ServiceBus.Common.ChainedEndHandler(this.transportManagerContainer.EndClose);
			}
			return new Microsoft.ServiceBus.Common.ChainedAsyncResult(timeout, callback, state, new Microsoft.ServiceBus.Common.ChainedBeginHandler(this.OnBeginClose), new Microsoft.ServiceBus.Common.ChainedEndHandler(this.OnEndClose), chainedBeginHandler, chainedEndHandler);
		}

		protected override void OnClose(TimeSpan timeout)
		{
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			base.OnClose(timeoutHelper.RemainingTime());
			if (this.transportManagerContainer != null && !this.TransferTransportManagers())
			{
				this.transportManagerContainer.Close(timeoutHelper.RemainingTime());
			}
		}

		protected override Microsoft.ServiceBus.Channels.ReplyChannel OnCreateChannel()
		{
			return new Microsoft.ServiceBus.Channels.TransportReplyChannelAcceptor.TransportReplyChannel(base.ChannelManager, null);
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			Microsoft.ServiceBus.Common.ChainedAsyncResult.End(result);
		}

		protected override void OnOpening()
		{
			base.OnOpening();
			this.transportManagerContainer = this.listener.GetTransportManagers();
			this.listener = null;
		}

		private bool TransferTransportManagers()
		{
			Microsoft.ServiceBus.Channels.TransportReplyChannelAcceptor.TransportReplyChannel currentChannel = (Microsoft.ServiceBus.Channels.TransportReplyChannelAcceptor.TransportReplyChannel)base.GetCurrentChannel();
			if (currentChannel == null)
			{
				return false;
			}
			return currentChannel.TransferTransportManagers(this.transportManagerContainer);
		}

		protected class TransportReplyChannel : Microsoft.ServiceBus.Channels.ReplyChannel
		{
			private Microsoft.ServiceBus.Channels.TransportManagerContainer transportManagerContainer;

			public TransportReplyChannel(ChannelManagerBase channelManager, EndpointAddress localAddress) : base(channelManager, localAddress)
			{
			}

			private IAsyncResult DummyBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
			{
				return new CompletedAsyncResult(callback, state);
			}

			private void DummyEndClose(IAsyncResult result)
			{
				CompletedAsyncResult.End(result);
			}

			protected override void OnAbort()
			{
				if (this.transportManagerContainer != null)
				{
					this.transportManagerContainer.Close(this.DefaultCloseTimeout);
				}
				base.OnAbort();
			}

			protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
			{
				Microsoft.ServiceBus.Common.ChainedBeginHandler chainedBeginHandler = new Microsoft.ServiceBus.Common.ChainedBeginHandler(this.DummyBeginClose);
				Microsoft.ServiceBus.Common.ChainedEndHandler chainedEndHandler = new Microsoft.ServiceBus.Common.ChainedEndHandler(this.DummyEndClose);
				if (this.transportManagerContainer != null)
				{
					chainedBeginHandler = new Microsoft.ServiceBus.Common.ChainedBeginHandler(this.transportManagerContainer.BeginClose);
					chainedEndHandler = new Microsoft.ServiceBus.Common.ChainedEndHandler(this.transportManagerContainer.EndClose);
				}
				return new Microsoft.ServiceBus.Common.ChainedAsyncResult(timeout, callback, state, chainedBeginHandler, chainedEndHandler, new Microsoft.ServiceBus.Common.ChainedBeginHandler(this.OnBeginClose), new Microsoft.ServiceBus.Common.ChainedEndHandler(this.OnEndClose));
			}

			protected override void OnClose(TimeSpan timeout)
			{
				TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
				if (this.transportManagerContainer != null)
				{
					this.transportManagerContainer.Close(timeoutHelper.RemainingTime());
				}
				base.OnClose(timeoutHelper.RemainingTime());
			}

			protected override void OnEndClose(IAsyncResult result)
			{
				Microsoft.ServiceBus.Common.ChainedAsyncResult.End(result);
			}

			public bool TransferTransportManagers(Microsoft.ServiceBus.Channels.TransportManagerContainer transportManagerContainer)
			{
				bool flag;
				lock (base.ThisLock)
				{
					if (base.State == CommunicationState.Opened)
					{
						this.transportManagerContainer = transportManagerContainer;
						flag = true;
					}
					else
					{
						flag = false;
					}
				}
				return flag;
			}
		}
	}
}