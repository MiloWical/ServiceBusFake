using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging.Channels;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus
{
	internal abstract class SocketConnectionChannelListener<TChannel, TChannelAcceptor> : SocketConnectionChannelListener, IChannelListener<TChannel>, IChannelListener, ICommunicationObject
	where TChannel : class, IChannel
	where TChannelAcceptor : Microsoft.ServiceBus.Channels.ChannelAcceptor<TChannel>
	{
		protected abstract TChannelAcceptor ChannelAcceptor
		{
			get;
		}

		protected SocketConnectionChannelListener(SocketConnectionBindingElement bindingElement, BindingContext context) : base(bindingElement, context)
		{
		}

		public TChannel AcceptChannel()
		{
			return this.AcceptChannel(this.DefaultReceiveTimeout);
		}

		public TChannel AcceptChannel(TimeSpan timeout)
		{
			this.ThrowIfNotOpened();
			return this.WrapChannel(this.ChannelAcceptor.AcceptChannel(timeout));
		}

		public IAsyncResult BeginAcceptChannel(AsyncCallback callback, object state)
		{
			return this.BeginAcceptChannel(this.DefaultReceiveTimeout, callback, state);
		}

		public IAsyncResult BeginAcceptChannel(TimeSpan timeout, AsyncCallback callback, object state)
		{
			this.ThrowIfNotOpened();
			return this.ChannelAcceptor.BeginAcceptChannel(timeout, callback, state);
		}

		public TChannel EndAcceptChannel(IAsyncResult result)
		{
			this.ThrowPending();
			return this.WrapChannel(this.ChannelAcceptor.EndAcceptChannel(result));
		}

		private void HookupConnectionStatusEvents()
		{
			IConnectionStatus property = this.GetProperty<IConnectionStatus>();
			if (property != null)
			{
				property.Offline += new EventHandler(this.OnConnectionOffline);
			}
		}

		protected override void OnAbort()
		{
			this.ChannelAcceptor.Abort();
			base.OnAbort();
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			Microsoft.ServiceBus.Common.ChainedBeginHandler chainedBeginHandler = new Microsoft.ServiceBus.Common.ChainedBeginHandler(this.OnBeginClose);
			Microsoft.ServiceBus.Common.ChainedEndHandler chainedEndHandler = new Microsoft.ServiceBus.Common.ChainedEndHandler(this.OnEndClose);
			ICommunicationObject[] channelAcceptor = new ICommunicationObject[] { this.ChannelAcceptor };
			return new Microsoft.ServiceBus.Channels.ChainedCloseAsyncResult(timeout, callback, state, chainedBeginHandler, chainedEndHandler, channelAcceptor);
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			Microsoft.ServiceBus.Common.ChainedBeginHandler chainedBeginHandler = new Microsoft.ServiceBus.Common.ChainedBeginHandler(this.OnBeginOpen);
			Microsoft.ServiceBus.Common.ChainedEndHandler chainedEndHandler = new Microsoft.ServiceBus.Common.ChainedEndHandler(this.OnEndOpen);
			ICommunicationObject[] channelAcceptor = new ICommunicationObject[] { this.ChannelAcceptor };
			return new Microsoft.ServiceBus.Channels.ChainedOpenAsyncResult(timeout, callback, state, chainedBeginHandler, chainedEndHandler, channelAcceptor);
		}

		protected override IAsyncResult OnBeginWaitForChannel(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.ChannelAcceptor.BeginWaitForChannel(timeout, callback, state);
		}

		protected override void OnClose(TimeSpan timeout)
		{
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			this.ChannelAcceptor.Close(timeoutHelper.RemainingTime());
			base.OnClose(timeoutHelper.RemainingTime());
		}

		private void OnConnectionOffline(object sender, EventArgs args)
		{
			if (base.State == CommunicationState.Opened)
			{
				base.Fault();
			}
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			Microsoft.ServiceBus.Common.ChainedAsyncResult.End(result);
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			Microsoft.ServiceBus.Common.ChainedAsyncResult.End(result);
			this.HookupConnectionStatusEvents();
		}

		protected override bool OnEndWaitForChannel(IAsyncResult result)
		{
			return this.ChannelAcceptor.EndWaitForChannel(result);
		}

		protected override void OnOpen(TimeSpan timeout)
		{
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			base.OnOpen(timeoutHelper.RemainingTime());
			this.ChannelAcceptor.Open(timeoutHelper.RemainingTime());
			this.HookupConnectionStatusEvents();
		}

		protected override bool OnWaitForChannel(TimeSpan timeout)
		{
			return this.ChannelAcceptor.WaitForChannel(timeout);
		}

		private TChannel WrapChannel(TChannel innerChannel)
		{
			if (innerChannel == null)
			{
				return default(TChannel);
			}
			if (typeof(TChannel) == typeof(IDuplexSessionChannel))
			{
				return (TChannel)(new SocketConnectionChannelListener<TChannel, TChannelAcceptor>.DuplexSessionChannel(this, (IDuplexSessionChannel)(object)innerChannel));
			}
			if (typeof(TChannel) != typeof(IReplyChannel))
			{
				throw Fx.Exception.AsError(new NotSupportedException(SRClient.NotSupportedTypeofChannel), null);
			}
			return innerChannel;
		}

		private class DuplexSessionChannel : Microsoft.ServiceBus.Channels.LayeredChannel<IDuplexSessionChannel>, IDuplexSessionChannel, IDuplexChannel, IInputChannel, IOutputChannel, IChannel, ICommunicationObject, ISessionChannel<IDuplexSession>
		{
			private IDuplexSessionChannel innerChannel;

			private EventTraceActivity Activity
			{
				get;
				set;
			}

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

			public System.Uri Via
			{
				get
				{
					return this.innerChannel.Via;
				}
			}

			public DuplexSessionChannel(ChannelManagerBase channelManager, IDuplexSessionChannel innerChannel) : base(channelManager, innerChannel)
			{
				this.innerChannel = innerChannel;
				this.Activity = innerChannel.GetProperty<EventTraceActivity>() ?? new EventTraceActivity();
			}

			public IAsyncResult BeginReceive(AsyncCallback callback, object state)
			{
				return this.BeginReceive(base.DefaultReceiveTimeout, callback, state);
			}

			public IAsyncResult BeginReceive(TimeSpan timeout, AsyncCallback callback, object state)
			{
				return new SocketConnectionChannelListener<TChannel, TChannelAcceptor>.DuplexSessionChannel.ReceiveAsyncResult(this, timeout, callback, state);
			}

			public IAsyncResult BeginSend(Message message, AsyncCallback callback, object state)
			{
				return this.innerChannel.BeginSend(message, callback, state);
			}

			public IAsyncResult BeginSend(Message message, TimeSpan timeout, AsyncCallback callback, object state)
			{
				return this.innerChannel.BeginSend(message, timeout, callback, state);
			}

			public IAsyncResult BeginTryReceive(TimeSpan timeout, AsyncCallback callback, object state)
			{
				return new SocketConnectionChannelListener<TChannel, TChannelAcceptor>.DuplexSessionChannel.TryReceiveAsyncResult(this.innerChannel, timeout, callback, state);
			}

			public IAsyncResult BeginWaitForMessage(TimeSpan timeout, AsyncCallback callback, object state)
			{
				return this.innerChannel.BeginWaitForMessage(timeout, callback, state);
			}

			public Message EndReceive(IAsyncResult result)
			{
				return AsyncResult<SocketConnectionChannelListener<TChannel, TChannelAcceptor>.DuplexSessionChannel.ReceiveAsyncResult>.End(result).Message;
			}

			public void EndSend(IAsyncResult result)
			{
				this.innerChannel.EndSend(result);
			}

			public bool EndTryReceive(IAsyncResult result, out Message message)
			{
				return SocketConnectionChannelListener<TChannel, TChannelAcceptor>.DuplexSessionChannel.TryReceiveAsyncResult.End(result, out message);
			}

			public bool EndWaitForMessage(IAsyncResult result)
			{
				return this.innerChannel.EndWaitForMessage(result);
			}

			public override T GetProperty<T>()
			where T : class
			{
				if (typeof(T) != typeof(EventTraceActivity))
				{
					return base.GetProperty<T>();
				}
				return (T)this.Activity;
			}

			public Message Receive()
			{
				return this.Receive(base.DefaultReceiveTimeout);
			}

			public Message Receive(TimeSpan timeout)
			{
				Message message;
				TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
				do
				{
					message = this.innerChannel.Receive(timeoutHelper.RemainingTime());
				}
				while (message != null && message.Headers.Action == "Ping");
				return message;
			}

			public void Send(Message message)
			{
				this.innerChannel.Send(message);
			}

			public void Send(Message message, TimeSpan timeout)
			{
				this.innerChannel.Send(message, timeout);
			}

			public bool TryReceive(TimeSpan timeout, out Message message)
			{
				Message message1;
				bool flag;
				TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
				do
				{
					flag = this.innerChannel.TryReceive(timeoutHelper.RemainingTime(), out message1);
				}
				while (flag && message1 != null && message1.Headers.Action == "Ping");
				message = message1;
				return flag;
			}

			public bool WaitForMessage(TimeSpan timeout)
			{
				return this.innerChannel.WaitForMessage(timeout);
			}

			private sealed class ReceiveAsyncResult : AsyncResult<SocketConnectionChannelListener<TChannel, TChannelAcceptor>.DuplexSessionChannel.ReceiveAsyncResult>
			{
				private readonly static AsyncCallback receiveCallback;

				private static Action<object> receiveStatic;

				private readonly SocketConnectionChannelListener<TChannel, TChannelAcceptor>.DuplexSessionChannel channel;

				private TimeoutHelper timeoutHelper;

				protected internal override EventTraceActivity Activity
				{
					get
					{
						return this.channel.Activity;
					}
				}

				public Message Message
				{
					get;
					private set;
				}

				static ReceiveAsyncResult()
				{
					SocketConnectionChannelListener<TChannel, TChannelAcceptor>.DuplexSessionChannel.ReceiveAsyncResult.receiveCallback = new AsyncCallback(SocketConnectionChannelListener<TChannel, TChannelAcceptor>.DuplexSessionChannel.ReceiveAsyncResult.ReceiveCallback);
				}

				public ReceiveAsyncResult(SocketConnectionChannelListener<TChannel, TChannelAcceptor>.DuplexSessionChannel channel, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
				{
					this.channel = channel;
					this.timeoutHelper = new TimeoutHelper(timeout);
					this.Receive(true);
				}

				private void Receive(bool calledSync)
				{
					IAsyncResult asyncResult = this.channel.innerChannel.BeginReceive(this.timeoutHelper.RemainingTime(), SocketConnectionChannelListener<TChannel, TChannelAcceptor>.DuplexSessionChannel.ReceiveAsyncResult.receiveCallback, this);
					if (asyncResult.CompletedSynchronously)
					{
						this.ReceiveComplete(asyncResult, calledSync);
					}
				}

				private static void ReceiveCallback(IAsyncResult result)
				{
					if (result.CompletedSynchronously)
					{
						return;
					}
					((SocketConnectionChannelListener<TChannel, TChannelAcceptor>.DuplexSessionChannel.ReceiveAsyncResult)result.AsyncState).ReceiveComplete(result, false);
				}

				private void ReceiveComplete(IAsyncResult result, bool completedSynchronously)
				{
					Exception exception = null;
					try
					{
						Message message = this.channel.innerChannel.EndReceive(result);
						if (message == null || !(message.Headers.Action == "Ping"))
						{
							this.Message = message;
						}
						else
						{
							if (SocketConnectionChannelListener<TChannel, TChannelAcceptor>.DuplexSessionChannel.ReceiveAsyncResult.receiveStatic == null)
							{
								SocketConnectionChannelListener<TChannel, TChannelAcceptor>.DuplexSessionChannel.ReceiveAsyncResult.receiveStatic = new Action<object>(SocketConnectionChannelListener<TChannel, TChannelAcceptor>.DuplexSessionChannel.ReceiveAsyncResult.ReceiveStatic);
							}
							IOThreadScheduler.ScheduleCallbackNoFlow(SocketConnectionChannelListener<TChannel, TChannelAcceptor>.DuplexSessionChannel.ReceiveAsyncResult.receiveStatic, this);
							return;
						}
					}
					catch (Exception exception2)
					{
						Exception exception1 = exception2;
						if (Fx.IsFatal(exception1))
						{
							throw;
						}
						exception = exception1;
					}
					base.Complete(completedSynchronously, exception);
				}

				private static void ReceiveStatic(object state)
				{
					SocketConnectionChannelListener<TChannel, TChannelAcceptor>.DuplexSessionChannel.ReceiveAsyncResult receiveAsyncResult = (SocketConnectionChannelListener<TChannel, TChannelAcceptor>.DuplexSessionChannel.ReceiveAsyncResult)state;
					try
					{
						receiveAsyncResult.Receive(false);
					}
					catch (Exception exception1)
					{
						Exception exception = exception1;
						if (Fx.IsFatal(exception))
						{
							throw;
						}
						receiveAsyncResult.Complete(false, exception);
					}
				}
			}

			private sealed class TryReceiveAsyncResult : AsyncResult<SocketConnectionChannelListener<TChannel, TChannelAcceptor>.DuplexSessionChannel.TryReceiveAsyncResult>
			{
				private readonly static AsyncCallback receiveCallback;

				private static Action<object> receiveStatic;

				private IDuplexSessionChannel innerChannel;

				private bool success;

				private TimeoutHelper timeoutHelper;

				public Message Message
				{
					get;
					private set;
				}

				static TryReceiveAsyncResult()
				{
					SocketConnectionChannelListener<TChannel, TChannelAcceptor>.DuplexSessionChannel.TryReceiveAsyncResult.receiveCallback = new AsyncCallback(SocketConnectionChannelListener<TChannel, TChannelAcceptor>.DuplexSessionChannel.TryReceiveAsyncResult.ReceiveCallback);
				}

				public TryReceiveAsyncResult(IDuplexSessionChannel innerChannel, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
				{
					this.innerChannel = innerChannel;
					this.timeoutHelper = new TimeoutHelper(timeout);
					this.Receive(true);
				}

				public static bool End(IAsyncResult result, out Message message)
				{
					SocketConnectionChannelListener<TChannel, TChannelAcceptor>.DuplexSessionChannel.TryReceiveAsyncResult tryReceiveAsyncResult = AsyncResult.End<SocketConnectionChannelListener<TChannel, TChannelAcceptor>.DuplexSessionChannel.TryReceiveAsyncResult>(result);
					message = tryReceiveAsyncResult.Message;
					return tryReceiveAsyncResult.success;
				}

				private void Receive(bool calledSync)
				{
					IAsyncResult asyncResult = this.innerChannel.BeginTryReceive(this.timeoutHelper.RemainingTime(), SocketConnectionChannelListener<TChannel, TChannelAcceptor>.DuplexSessionChannel.TryReceiveAsyncResult.receiveCallback, this);
					if (asyncResult.CompletedSynchronously)
					{
						this.ReceiveComplete(asyncResult, calledSync);
					}
				}

				private static void ReceiveCallback(IAsyncResult result)
				{
					if (result.CompletedSynchronously)
					{
						return;
					}
					((SocketConnectionChannelListener<TChannel, TChannelAcceptor>.DuplexSessionChannel.TryReceiveAsyncResult)result.AsyncState).ReceiveComplete(result, false);
				}

				private void ReceiveComplete(IAsyncResult result, bool completedSynchronously)
				{
					Message message;
					Exception exception = null;
					try
					{
						bool flag = this.innerChannel.EndTryReceive(result, out message);
						if (!flag || message == null || !(message.Headers.Action == "Ping"))
						{
							this.success = flag;
							this.Message = message;
						}
						else
						{
							if (SocketConnectionChannelListener<TChannel, TChannelAcceptor>.DuplexSessionChannel.TryReceiveAsyncResult.receiveStatic == null)
							{
								SocketConnectionChannelListener<TChannel, TChannelAcceptor>.DuplexSessionChannel.TryReceiveAsyncResult.receiveStatic = new Action<object>(SocketConnectionChannelListener<TChannel, TChannelAcceptor>.DuplexSessionChannel.TryReceiveAsyncResult.ReceiveStatic);
							}
							IOThreadScheduler.ScheduleCallbackNoFlow(SocketConnectionChannelListener<TChannel, TChannelAcceptor>.DuplexSessionChannel.TryReceiveAsyncResult.receiveStatic, this);
							return;
						}
					}
					catch (Exception exception2)
					{
						Exception exception1 = exception2;
						if (Fx.IsFatal(exception1))
						{
							throw;
						}
						exception = exception1;
					}
					base.Complete(completedSynchronously, exception);
				}

				private static void ReceiveStatic(object state)
				{
					SocketConnectionChannelListener<TChannel, TChannelAcceptor>.DuplexSessionChannel.TryReceiveAsyncResult tryReceiveAsyncResult = (SocketConnectionChannelListener<TChannel, TChannelAcceptor>.DuplexSessionChannel.TryReceiveAsyncResult)state;
					try
					{
						tryReceiveAsyncResult.Receive(false);
					}
					catch (Exception exception1)
					{
						Exception exception = exception1;
						if (Fx.IsFatal(exception))
						{
							throw;
						}
						tryReceiveAsyncResult.Complete(false, exception);
					}
				}
			}
		}
	}
}