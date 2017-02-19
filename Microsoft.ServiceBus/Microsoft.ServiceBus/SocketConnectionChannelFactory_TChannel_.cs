using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;

namespace Microsoft.ServiceBus
{
	internal class SocketConnectionChannelFactory<TChannel> : Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelFactory<TChannel>, ISocketConnectionChannelFactorySettings, Microsoft.ServiceBus.Channels.IConnectionOrientedTransportChannelFactorySettings, Microsoft.ServiceBus.Channels.IConnectionOrientedTransportFactorySettings, Microsoft.ServiceBus.Channels.ITransportFactorySettings, IDefaultCommunicationTimeouts, Microsoft.ServiceBus.Channels.IConnectionOrientedConnectionSettings
	{
		private static SocketConnectionPoolRegistry connectionPoolRegistry;

		private TimeSpan leaseTimeout;

		private IConnectionElement connectionElement;

		private System.ServiceModel.Channels.MessageVersion messageVersion;

		private ISecurityCapabilities securityCapabilities;

		private bool enableKeepAlive;

		public TimeSpan LeaseTimeout
		{
			get
			{
				return this.leaseTimeout;
			}
		}

		public override string Scheme
		{
			get
			{
				return "sb";
			}
		}

		static SocketConnectionChannelFactory()
		{
			SocketConnectionChannelFactory<TChannel>.connectionPoolRegistry = new SocketConnectionPoolRegistry();
		}

		public SocketConnectionChannelFactory(SocketConnectionBindingElement bindingElement, BindingContext context, bool enableKeepAlive) : base(bindingElement, context, bindingElement.ConnectionPoolSettings.GroupName, bindingElement.ConnectionPoolSettings.IdleTimeout, bindingElement.ConnectionPoolSettings.MaxOutboundConnectionsPerEndpoint)
		{
			this.enableKeepAlive = enableKeepAlive;
			this.connectionElement = bindingElement.ConnectionElement;
			this.leaseTimeout = bindingElement.ConnectionPoolSettings.LeaseTimeout;
			this.messageVersion = context.Binding.MessageVersion;
			this.securityCapabilities = bindingElement.GetProperty<ISecurityCapabilities>(context);
		}

		internal override Microsoft.ServiceBus.Channels.IConnectionInitiator GetConnectionInitiator()
		{
			Microsoft.ServiceBus.Channels.IConnectionInitiator connectionInitiator = this.connectionElement.CreateInitiator(base.ConnectionBufferSize);
			return new Microsoft.ServiceBus.Channels.BufferedConnectionInitiator(connectionInitiator, base.MaxOutputDelay, base.ConnectionBufferSize);
		}

		internal override Microsoft.ServiceBus.Channels.ConnectionPool GetConnectionPool()
		{
			return SocketConnectionChannelFactory<TChannel>.connectionPoolRegistry.Lookup(this);
		}

		public override T GetProperty<T>()
		where T : class
		{
			if (typeof(T) != typeof(ISecurityCapabilities))
			{
				return base.GetProperty<T>();
			}
			return (T)this.securityCapabilities;
		}

		protected override TChannel OnCreateChannel(EndpointAddress address, Uri via)
		{
			return this.WrapChannel(base.OnCreateChannel(address, via));
		}

		internal override void ReleaseConnectionPool(Microsoft.ServiceBus.Channels.ConnectionPool pool, TimeSpan timeout)
		{
			SocketConnectionChannelFactory<TChannel>.connectionPoolRegistry.Release(pool, timeout);
		}

		private TChannel WrapChannel(TChannel innerChannel)
		{
			if (typeof(TChannel) == typeof(IDuplexSessionChannel))
			{
				return (TChannel)(new SocketConnectionChannelFactory<TChannel>.DuplexSessionChannel(this, (IDuplexSessionChannel)(object)innerChannel, this.messageVersion, this.enableKeepAlive));
			}
			if (typeof(TChannel) != typeof(IRequestChannel))
			{
				throw Fx.Exception.AsError(new NotSupportedException(SRClient.NotSupportedTypeofChannel), null);
			}
			return innerChannel;
		}

		private class DuplexSessionChannel : Microsoft.ServiceBus.Channels.LayeredChannel<IDuplexSessionChannel>, IDuplexSessionChannel, IDuplexChannel, IInputChannel, IOutputChannel, IChannel, ICommunicationObject, ISessionChannel<IDuplexSession>
		{
			private readonly static Action<object> pingCallbackStatic;

			private readonly IDuplexSessionChannel innerChannel;

			private readonly System.ServiceModel.Channels.MessageVersion messageVersion;

			private readonly IOThreadTimer pingTimer;

			private readonly bool enableKeepAlive;

			private bool suppressPing;

			private int messagesInFlight;

			public EndpointAddress LocalAddress
			{
				get
				{
					return this.innerChannel.LocalAddress;
				}
			}

			public EndpointAddress RemoteAddress
			{
				get
				{
					return this.innerChannel.RemoteAddress;
				}
			}

			public IDuplexSession Session
			{
				get
				{
					return this.innerChannel.Session;
				}
			}

			public Uri Via
			{
				get
				{
					return this.innerChannel.Via;
				}
			}

			static DuplexSessionChannel()
			{
				SocketConnectionChannelFactory<TChannel>.DuplexSessionChannel.pingCallbackStatic = new Action<object>(SocketConnectionChannelFactory<TChannel>.DuplexSessionChannel.PingCallbackStatic);
			}

			public DuplexSessionChannel(ChannelManagerBase channelManager, IDuplexSessionChannel innerChannel, System.ServiceModel.Channels.MessageVersion messageVersion, bool enableKeepAlive) : base(channelManager, innerChannel)
			{
				this.innerChannel = innerChannel;
				this.messageVersion = messageVersion;
				this.enableKeepAlive = enableKeepAlive;
				this.pingTimer = new IOThreadTimer(SocketConnectionChannelFactory<TChannel>.DuplexSessionChannel.pingCallbackStatic, this, false);
				this.suppressPing = false;
			}

			public IAsyncResult BeginReceive(AsyncCallback callback, object state)
			{
				return this.innerChannel.BeginReceive(callback, state);
			}

			public IAsyncResult BeginReceive(TimeSpan timeout, AsyncCallback callback, object state)
			{
				return this.innerChannel.BeginReceive(timeout, callback, state);
			}

			public IAsyncResult BeginSend(Message message, AsyncCallback callback, object state)
			{
				IAsyncResult asyncResult;
				if (this.enableKeepAlive && Interlocked.Increment(ref this.messagesInFlight) == 1)
				{
					this.suppressPing = true;
					this.pingTimer.Cancel();
				}
				try
				{
					asyncResult = this.innerChannel.BeginSend(message, callback, state);
				}
				catch
				{
					if (this.enableKeepAlive && Interlocked.Decrement(ref this.messagesInFlight) == 0)
					{
						this.pingTimer.Set(TimeSpan.FromSeconds(30));
						this.suppressPing = false;
					}
					throw;
				}
				return asyncResult;
			}

			public IAsyncResult BeginSend(Message message, TimeSpan timeout, AsyncCallback callback, object state)
			{
				IAsyncResult asyncResult;
				if (this.enableKeepAlive && Interlocked.Increment(ref this.messagesInFlight) == 1)
				{
					this.suppressPing = true;
					this.pingTimer.Cancel();
				}
				try
				{
					asyncResult = this.innerChannel.BeginSend(message, timeout, callback, state);
				}
				catch
				{
					if (this.enableKeepAlive && Interlocked.Decrement(ref this.messagesInFlight) == 0)
					{
						this.pingTimer.Set(TimeSpan.FromSeconds(30));
						this.suppressPing = false;
					}
					throw;
				}
				return asyncResult;
			}

			public IAsyncResult BeginTryReceive(TimeSpan timeout, AsyncCallback callback, object state)
			{
				return this.innerChannel.BeginTryReceive(timeout, callback, state);
			}

			public IAsyncResult BeginWaitForMessage(TimeSpan timeout, AsyncCallback callback, object state)
			{
				return this.innerChannel.BeginWaitForMessage(timeout, callback, state);
			}

			public Message EndReceive(IAsyncResult result)
			{
				return this.innerChannel.EndReceive(result);
			}

			public void EndSend(IAsyncResult result)
			{
				try
				{
					this.innerChannel.EndSend(result);
				}
				finally
				{
					if (this.enableKeepAlive && Interlocked.Decrement(ref this.messagesInFlight) == 0)
					{
						this.pingTimer.Set(TimeSpan.FromSeconds(30));
						this.suppressPing = false;
					}
				}
			}

			public bool EndTryReceive(IAsyncResult result, out Message message)
			{
				return this.innerChannel.EndTryReceive(result, out message);
			}

			public bool EndWaitForMessage(IAsyncResult result)
			{
				return this.innerChannel.EndWaitForMessage(result);
			}

			protected override void OnAbort()
			{
				if (this.enableKeepAlive)
				{
					this.suppressPing = true;
					this.pingTimer.Cancel();
				}
				base.OnAbort();
			}

			protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
			{
				if (this.enableKeepAlive)
				{
					this.suppressPing = true;
					this.pingTimer.Cancel();
				}
				return base.OnBeginClose(timeout, callback, state);
			}

			protected override void OnClose(TimeSpan timeout)
			{
				if (this.enableKeepAlive)
				{
					this.suppressPing = true;
					this.pingTimer.Cancel();
				}
				base.OnClose(timeout);
			}

			protected override void OnOpened()
			{
				base.OnOpened();
				if (this.enableKeepAlive)
				{
					this.pingTimer.Set(TimeSpan.FromSeconds(30));
					this.suppressPing = false;
				}
			}

			private void PingCallback()
			{
				if (!this.suppressPing && base.State != CommunicationState.Closing && base.State != CommunicationState.Closed && base.State != CommunicationState.Faulted)
				{
					try
					{
						Message message = Message.CreateMessage(this.messageVersion, "Ping", new PingMessage());
						Interlocked.Increment(ref this.messagesInFlight);
						this.Send(message, TimeSpan.FromSeconds(10));
						Interlocked.Decrement(ref this.messagesInFlight);
						lock (base.ThisLock)
						{
							if (!this.suppressPing && base.State != CommunicationState.Closing && base.State != CommunicationState.Closed && base.State != CommunicationState.Faulted)
							{
								this.pingTimer.Set(TimeSpan.FromSeconds(30));
							}
						}
					}
					catch (Exception exception1)
					{
						Exception exception = exception1;
						base.Abort();
						if (Fx.IsFatal(exception))
						{
							throw;
						}
					}
				}
			}

			private static void PingCallbackStatic(object state)
			{
				((SocketConnectionChannelFactory<TChannel>.DuplexSessionChannel)state).PingCallback();
			}

			public Message Receive()
			{
				return this.innerChannel.Receive();
			}

			public Message Receive(TimeSpan timeout)
			{
				return this.innerChannel.Receive(timeout);
			}

			public void Send(Message message)
			{
				if (this.enableKeepAlive && Interlocked.Increment(ref this.messagesInFlight) == 1)
				{
					this.suppressPing = true;
					this.pingTimer.Cancel();
				}
				try
				{
					this.innerChannel.Send(message);
				}
				finally
				{
					if (this.enableKeepAlive && Interlocked.Decrement(ref this.messagesInFlight) == 0)
					{
						this.pingTimer.Set(TimeSpan.FromSeconds(30));
						this.suppressPing = false;
					}
				}
			}

			public void Send(Message message, TimeSpan timeout)
			{
				if (this.enableKeepAlive && Interlocked.Increment(ref this.messagesInFlight) == 1)
				{
					this.suppressPing = true;
					this.pingTimer.Cancel();
				}
				try
				{
					this.innerChannel.Send(message, timeout);
				}
				finally
				{
					if (this.enableKeepAlive && Interlocked.Decrement(ref this.messagesInFlight) == 0)
					{
						this.pingTimer.Set(TimeSpan.FromSeconds(30));
						this.suppressPing = false;
					}
				}
			}

			public bool TryReceive(TimeSpan timeout, out Message message)
			{
				return this.innerChannel.TryReceive(timeout, out message);
			}

			public bool WaitForMessage(TimeSpan timeout)
			{
				return this.innerChannel.WaitForMessage(timeout);
			}
		}
	}
}