using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus
{
	internal class RelayedOnewayChannelFactory : ChannelFactoryBase<IOutputChannel>
	{
		private BindingContext context;

		private RelayedOnewayTransportBindingElement transportBindingElement;

		public RelayedOnewayChannelFactory(BindingContext context, RelayedOnewayTransportBindingElement transportBindingElement) : base(context.Binding)
		{
			this.context = context;
			this.transportBindingElement = transportBindingElement;
		}

		public override T GetProperty<T>()
		where T : class
		{
			if (typeof(T) != typeof(MessageVersion))
			{
				return base.GetProperty<T>();
			}
			return (T)this.context.Binding.MessageVersion;
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult(callback, state);
		}

		protected override IOutputChannel OnCreateChannel(EndpointAddress remoteAddress, Uri via)
		{
			return new RelayedOnewayChannelFactory.RelayedOnewayOutputChannel(this, this.context, this.transportBindingElement, remoteAddress, via);
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
		}

		protected override void OnOpen(TimeSpan timeout)
		{
		}

		private class RelayedOnewayOutputChannel : ChannelBase, IOutputChannel, IChannel, ICommunicationObject
		{
			private IRelayedOnewaySender connection;

			private EndpointAddress to;

			private Uri via;

			public EndpointAddress RemoteAddress
			{
				get
				{
					return this.to;
				}
			}

			public Uri Via
			{
				get
				{
					return this.via;
				}
			}

			public RelayedOnewayOutputChannel(RelayedOnewayChannelFactory channelFactory, BindingContext context, RelayedOnewayTransportBindingElement transportBindingElement, EndpointAddress to, Uri via) : base(channelFactory)
			{
				this.to = to;
				this.via = via;
				this.connection = RelayedOnewayManager.CreateConnection(context, transportBindingElement, via);
			}

			public IAsyncResult BeginSend(Message message, TimeSpan timeout, AsyncCallback callback, object state)
			{
				TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
				message = this.PrepareMessage(message);
				return this.connection.BeginSend(message, timeoutHelper.RemainingTime(), callback, state);
			}

			public IAsyncResult BeginSend(Message message, AsyncCallback callback, object state)
			{
				return this.BeginSend(message, base.DefaultSendTimeout, callback, state);
			}

			public void EndSend(IAsyncResult result)
			{
				this.connection.EndSend(result);
			}

			public override T GetProperty<T>()
			where T : class
			{
				if (typeof(T) != typeof(IConnectionStatus))
				{
					return base.GetProperty<T>();
				}
				return (T)this.connection;
			}

			protected override void OnAbort()
			{
				IRelayedOnewaySender relayedOnewaySender;
				lock (base.ThisLock)
				{
					relayedOnewaySender = this.connection;
					this.connection = null;
				}
				if (relayedOnewaySender != null)
				{
					relayedOnewaySender.Abort();
				}
			}

			protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
			{
				IRelayedOnewaySender relayedOnewaySender;
				lock (base.ThisLock)
				{
					relayedOnewaySender = this.connection;
					this.connection = null;
				}
				if (relayedOnewaySender == null)
				{
					return new CompletedAsyncResult(callback, state);
				}
				IRelayedOnewaySender relayedOnewaySender1 = relayedOnewaySender;
				IRelayedOnewaySender relayedOnewaySender2 = relayedOnewaySender;
				return new DelegatingAsyncResult(new Func<TimeSpan, AsyncCallback, object, IAsyncResult>(relayedOnewaySender1.BeginClose), new Action<IAsyncResult>(relayedOnewaySender2.EndClose), timeout, callback, state);
			}

			protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
			{
				IRelayedOnewaySender relayedOnewaySender = this.connection;
				if (relayedOnewaySender == null)
				{
					return new CompletedAsyncResult(callback, state);
				}
				IRelayedOnewaySender relayedOnewaySender1 = relayedOnewaySender;
				IRelayedOnewaySender relayedOnewaySender2 = relayedOnewaySender;
				return new DelegatingAsyncResult(new Func<TimeSpan, AsyncCallback, object, IAsyncResult>(relayedOnewaySender1.BeginOpen), new Action<IAsyncResult>(relayedOnewaySender2.EndOpen), timeout, callback, state);
			}

			protected override void OnClose(TimeSpan timeout)
			{
				TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
				IRelayedOnewaySender relayedOnewaySender = null;
				lock (base.ThisLock)
				{
					relayedOnewaySender = this.connection;
					this.connection = null;
				}
				if (relayedOnewaySender != null)
				{
					relayedOnewaySender.Close(timeoutHelper.RemainingTime());
				}
			}

			protected override void OnEndClose(IAsyncResult result)
			{
				if (!(result is DelegatingAsyncResult))
				{
					CompletedAsyncResult.End(result);
					return;
				}
				AsyncResult<DelegatingAsyncResult>.End(result);
			}

			protected override void OnEndOpen(IAsyncResult result)
			{
				if (!(result is DelegatingAsyncResult))
				{
					CompletedAsyncResult.End(result);
					return;
				}
				AsyncResult<DelegatingAsyncResult>.End(result);
			}

			protected override void OnOpen(TimeSpan timeout)
			{
				IRelayedOnewaySender relayedOnewaySender = this.connection;
				if (relayedOnewaySender != null)
				{
					relayedOnewaySender.Open(timeout);
				}
			}

			private Message PrepareMessage(Message message)
			{
				this.to.ApplyTo(message);
				return message;
			}

			public void Send(Message message, TimeSpan timeout)
			{
				TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
				message = this.PrepareMessage(message);
				this.connection.Send(message, timeoutHelper.RemainingTime());
			}

			public void Send(Message message)
			{
				this.Send(message, base.DefaultSendTimeout);
			}
		}
	}
}