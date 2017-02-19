using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging.Channels;
using System;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Channels
{
	internal class InputQueueChannelAcceptor<TChannel> : Microsoft.ServiceBus.Channels.ChannelAcceptor<TChannel>
	where TChannel : class, IChannel
	{
		private readonly InputQueue<TChannel> channelQueue;

		private readonly Func<Exception> pendingExceptionGenerator;

		public int PendingCount
		{
			get
			{
				return this.channelQueue.PendingCount;
			}
		}

		public InputQueueChannelAcceptor(ChannelManagerBase channelManager, Func<Exception> pendingExceptionGenerator) : base(channelManager)
		{
			this.channelQueue = new InputQueue<TChannel>();
			this.pendingExceptionGenerator = pendingExceptionGenerator;
		}

		public override TChannel AcceptChannel(TimeSpan timeout)
		{
			this.ThrowIfNotOpened();
			return this.channelQueue.Dequeue(timeout);
		}

		public override IAsyncResult BeginAcceptChannel(TimeSpan timeout, AsyncCallback callback, object state)
		{
			this.ThrowIfNotOpened();
			return this.channelQueue.BeginDequeue(timeout, callback, state);
		}

		public override IAsyncResult BeginWaitForChannel(TimeSpan timeout, AsyncCallback callback, object state)
		{
			this.ThrowIfNotOpened();
			return this.channelQueue.BeginWaitForItem(timeout, callback, state);
		}

		public void Dispatch()
		{
			this.channelQueue.Dispatch();
		}

		public override TChannel EndAcceptChannel(IAsyncResult result)
		{
			return this.channelQueue.EndDequeue(result);
		}

		public override bool EndWaitForChannel(IAsyncResult result)
		{
			return this.channelQueue.EndWaitForItem(result);
		}

		public void EnqueueAndDispatch(TChannel channel)
		{
			this.channelQueue.EnqueueAndDispatch(channel);
		}

		public void EnqueueAndDispatch(TChannel channel, Action dequeuedCallback)
		{
			this.channelQueue.EnqueueAndDispatch(channel, dequeuedCallback);
		}

		public void EnqueueAndDispatch(TChannel channel, Action dequeuedCallback, bool canDispatchOnThisThread)
		{
			this.channelQueue.EnqueueAndDispatch(channel, dequeuedCallback, canDispatchOnThisThread);
		}

		public virtual void EnqueueAndDispatch(Exception exception, Action dequeuedCallback, bool canDispatchOnThisThread)
		{
			this.channelQueue.EnqueueAndDispatch(exception, dequeuedCallback, canDispatchOnThisThread);
		}

		public bool EnqueueWithoutDispatch(TChannel channel, Action dequeuedCallback)
		{
			return this.channelQueue.EnqueueWithoutDispatch(channel, dequeuedCallback);
		}

		public virtual bool EnqueueWithoutDispatch(Exception exception, Action dequeuedCallback)
		{
			return this.channelQueue.EnqueueWithoutDispatch(exception, dequeuedCallback);
		}

		public void FaultQueue()
		{
			base.Fault();
		}

		protected override void OnClosed()
		{
			base.OnClosed();
			this.channelQueue.Dispose();
		}

		protected override void OnFaulted()
		{
			this.channelQueue.Shutdown(this.pendingExceptionGenerator);
			base.OnFaulted();
		}

		public override bool WaitForChannel(TimeSpan timeout)
		{
			this.ThrowIfNotOpened();
			return this.channelQueue.WaitForItem(timeout);
		}
	}
}