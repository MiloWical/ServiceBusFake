using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using System;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Channels
{
	internal abstract class SingletonChannelAcceptor<ChannelInterfaceType, TChannel, QueueItemType> : Microsoft.ServiceBus.Channels.InputQueueChannelAcceptor<ChannelInterfaceType>
	where ChannelInterfaceType : class, IChannel
	where TChannel : Microsoft.ServiceBus.Channels.InputQueueChannel<QueueItemType>
	where QueueItemType : class, IDisposable
	{
		private TChannel currentChannel;

		private object currentChannelLock;

		private static Action<object> onInvokeDequeuedCallback;

		public SingletonChannelAcceptor(ChannelManagerBase channelManager, Func<Exception> pendingExceptionGenerator) : base(channelManager, pendingExceptionGenerator)
		{
		}

		public override ChannelInterfaceType AcceptChannel(TimeSpan timeout)
		{
			this.EnsureChannelAvailable();
			return base.AcceptChannel(timeout);
		}

		public override IAsyncResult BeginAcceptChannel(TimeSpan timeout, AsyncCallback callback, object state)
		{
			this.EnsureChannelAvailable();
			return base.BeginAcceptChannel(timeout, callback, state);
		}

		public void DispatchItems()
		{
			TChannel tChannel = this.EnsureChannelAvailable();
			if (tChannel != null)
			{
				tChannel.Dispatch();
			}
		}

		public void Enqueue(QueueItemType item)
		{
			this.Enqueue(item, null);
		}

		public void Enqueue(QueueItemType item, Action dequeuedCallback)
		{
			this.Enqueue(item, dequeuedCallback, true);
		}

		public void Enqueue(QueueItemType item, Action dequeuedCallback, bool canDispatchOnThisThread)
		{
			TChannel tChannel = this.EnsureChannelAvailable();
			if (DiagnosticUtility.ShouldTraceInformation)
			{
				this.OnTraceMessageReceived(item);
			}
			if (tChannel != null)
			{
				tChannel.EnqueueAndDispatch(item, dequeuedCallback, canDispatchOnThisThread);
				return;
			}
			Microsoft.ServiceBus.Channels.SingletonChannelAcceptor<ChannelInterfaceType, TChannel, QueueItemType>.InvokeDequeuedCallback(dequeuedCallback, canDispatchOnThisThread);
			item.Dispose();
		}

		public void Enqueue(Exception exception, Action dequeuedCallback)
		{
			this.Enqueue(exception, dequeuedCallback, true);
		}

		public void Enqueue(Exception exception, Action dequeuedCallback, bool canDispatchOnThisThread)
		{
			TChannel tChannel = this.EnsureChannelAvailable();
			if (tChannel == null)
			{
				Microsoft.ServiceBus.Channels.SingletonChannelAcceptor<ChannelInterfaceType, TChannel, QueueItemType>.InvokeDequeuedCallback(dequeuedCallback, canDispatchOnThisThread);
				return;
			}
			tChannel.EnqueueAndDispatch(exception, dequeuedCallback, canDispatchOnThisThread);
		}

		public void EnqueueAndDispatch(QueueItemType item, Action dequeuedCallback, bool canDispatchOnThisThread)
		{
			TChannel tChannel = this.EnsureChannelAvailable();
			if (DiagnosticUtility.ShouldTraceInformation)
			{
				this.OnTraceMessageReceived(item);
			}
			if (tChannel != null)
			{
				tChannel.EnqueueAndDispatch(item, dequeuedCallback, canDispatchOnThisThread);
				return;
			}
			Microsoft.ServiceBus.Channels.SingletonChannelAcceptor<ChannelInterfaceType, TChannel, QueueItemType>.InvokeDequeuedCallback(dequeuedCallback, canDispatchOnThisThread);
			item.Dispose();
		}

		public override void EnqueueAndDispatch(Exception exception, Action dequeuedCallback, bool canDispatchOnThisThread)
		{
			TChannel tChannel = this.EnsureChannelAvailable();
			if (tChannel == null)
			{
				Microsoft.ServiceBus.Channels.SingletonChannelAcceptor<ChannelInterfaceType, TChannel, QueueItemType>.InvokeDequeuedCallback(dequeuedCallback, canDispatchOnThisThread);
				return;
			}
			tChannel.EnqueueAndDispatch(exception, dequeuedCallback, canDispatchOnThisThread);
		}

		public bool EnqueueWithoutDispatch(QueueItemType item, Action dequeuedCallback)
		{
			TChannel tChannel = this.EnsureChannelAvailable();
			if (DiagnosticUtility.ShouldTraceInformation)
			{
				this.OnTraceMessageReceived(item);
			}
			if (tChannel != null)
			{
				return tChannel.EnqueueWithoutDispatch(item, dequeuedCallback);
			}
			Microsoft.ServiceBus.Channels.SingletonChannelAcceptor<ChannelInterfaceType, TChannel, QueueItemType>.InvokeDequeuedCallback(dequeuedCallback, false);
			item.Dispose();
			return false;
		}

		public override bool EnqueueWithoutDispatch(Exception exception, Action dequeuedCallback)
		{
			TChannel tChannel = this.EnsureChannelAvailable();
			if (tChannel == null)
			{
				Microsoft.ServiceBus.Channels.SingletonChannelAcceptor<ChannelInterfaceType, TChannel, QueueItemType>.InvokeDequeuedCallback(dequeuedCallback, false);
				return false;
			}
			return tChannel.EnqueueWithoutDispatch(exception, dequeuedCallback);
		}

		private TChannel EnsureChannelAvailable()
		{
			TChannel tChannel;
			bool flag = false;
			TChannel tChannel1 = this.currentChannel;
			TChannel tChannel2 = tChannel1;
			if (tChannel1 == null)
			{
				lock (this.currentChannelLock)
				{
					if (!base.IsDisposed)
					{
						TChannel tChannel3 = this.currentChannel;
						tChannel2 = tChannel3;
						if (tChannel3 == null)
						{
							tChannel2 = this.OnCreateChannel();
							tChannel2.Closed += new EventHandler(this.OnChannelClosed);
							this.currentChannel = tChannel2;
							flag = true;
						}
						if (flag)
						{
							base.EnqueueAndDispatch((ChannelInterfaceType)(object)tChannel2);
						}
						return tChannel2;
					}
					else
					{
						tChannel = default(TChannel);
					}
				}
				return tChannel;
			}
			if (flag)
			{
				base.EnqueueAndDispatch((ChannelInterfaceType)(object)tChannel2);
			}
			return tChannel2;
		}

		protected TChannel GetCurrentChannel()
		{
			return this.currentChannel;
		}

		private static void InvokeDequeuedCallback(Action dequeuedCallback, bool canDispatchOnThisThread)
		{
			if (canDispatchOnThisThread)
			{
				dequeuedCallback();
				return;
			}
			if (Microsoft.ServiceBus.Channels.SingletonChannelAcceptor<ChannelInterfaceType, TChannel, QueueItemType>.onInvokeDequeuedCallback == null)
			{
				Microsoft.ServiceBus.Channels.SingletonChannelAcceptor<ChannelInterfaceType, TChannel, QueueItemType>.onInvokeDequeuedCallback = new Action<object>(Microsoft.ServiceBus.Channels.SingletonChannelAcceptor<ChannelInterfaceType, TChannel, QueueItemType>.OnInvokeDequeuedCallback);
			}
			IOThreadScheduler.ScheduleCallbackNoFlow(Microsoft.ServiceBus.Channels.SingletonChannelAcceptor<ChannelInterfaceType, TChannel, QueueItemType>.onInvokeDequeuedCallback, dequeuedCallback);
		}

		protected void OnChannelClosed(object sender, EventArgs args)
		{
			IChannel channel = (IChannel)sender;
			lock (this.currentChannelLock)
			{
				if (channel == (object)this.currentChannel)
				{
					this.currentChannel = default(TChannel);
				}
			}
		}

		protected abstract TChannel OnCreateChannel();

		private static void OnInvokeDequeuedCallback(object state)
		{
			((Action)state)();
		}

		protected abstract void OnTraceMessageReceived(QueueItemType item);
	}
}