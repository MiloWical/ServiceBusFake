using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging.Channels;
using Microsoft.ServiceBus.Properties;
using System;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Channels
{
	internal abstract class InputQueueChannel<TDisposable> : ChannelBase, ICommunicationObjectInternals, ICommunicationObject
	where TDisposable : class, IDisposable
	{
		private InputQueue<TDisposable> inputQueue;

		private bool aborted;

		private bool closeCalled;

		public int InternalPendingItems
		{
			get
			{
				return this.inputQueue.PendingCount;
			}
		}

		public int PendingItems
		{
			get
			{
				base.ThrowIfDisposedOrNotOpen();
				return this.InternalPendingItems;
			}
		}

		protected InputQueueChannel(ChannelManagerBase channelManager) : base(channelManager)
		{
			this.inputQueue = new InputQueue<TDisposable>();
		}

		protected IAsyncResult BeginDequeue(TimeSpan timeout, AsyncCallback callback, object state)
		{
			this.ThrowIfNotOpened();
			return this.inputQueue.BeginDequeue(timeout, callback, state);
		}

		protected IAsyncResult BeginWaitForItem(TimeSpan timeout, AsyncCallback callback, object state)
		{
			this.ThrowIfNotOpened();
			return this.inputQueue.BeginWaitForItem(timeout, callback, state);
		}

		protected bool Dequeue(TimeSpan timeout, out TDisposable item)
		{
			this.ThrowIfNotOpened();
			bool flag = this.inputQueue.Dequeue(timeout, out item);
			if (item == null)
			{
				this.ThrowIfFaulted();
				this.ThrowIfAborted();
			}
			return flag;
		}

		public void Dispatch()
		{
			this.inputQueue.Dispatch();
		}

		protected bool EndDequeue(IAsyncResult result, out TDisposable item)
		{
			bool flag = this.inputQueue.EndDequeue(result, out item);
			if (item == null)
			{
				this.ThrowIfFaulted();
				this.ThrowIfAborted();
			}
			return flag;
		}

		protected bool EndWaitForItem(IAsyncResult result)
		{
			bool flag = this.inputQueue.EndWaitForItem(result);
			this.ThrowIfFaulted();
			this.ThrowIfAborted();
			return flag;
		}

		public void EnqueueAndDispatch(TDisposable item)
		{
			this.EnqueueAndDispatch(item, null);
		}

		public void EnqueueAndDispatch(TDisposable item, Action dequeuedCallback, bool canDispatchOnThisThread)
		{
			this.OnEnqueueItem(item);
			this.inputQueue.EnqueueAndDispatch(item, dequeuedCallback, canDispatchOnThisThread);
		}

		public void EnqueueAndDispatch(Exception exception, Action dequeuedCallback, bool canDispatchOnThisThread)
		{
			this.inputQueue.EnqueueAndDispatch(exception, dequeuedCallback, canDispatchOnThisThread);
		}

		public void EnqueueAndDispatch(TDisposable item, Action dequeuedCallback)
		{
			this.OnEnqueueItem(item);
			this.inputQueue.EnqueueAndDispatch(item, dequeuedCallback);
		}

		public bool EnqueueWithoutDispatch(Exception exception, Action dequeuedCallback)
		{
			return this.inputQueue.EnqueueWithoutDispatch(exception, dequeuedCallback);
		}

		public bool EnqueueWithoutDispatch(TDisposable item, Action dequeuedCallback)
		{
			this.OnEnqueueItem(item);
			return this.inputQueue.EnqueueWithoutDispatch(item, dequeuedCallback);
		}

		void Microsoft.ServiceBus.Messaging.Channels.ICommunicationObjectInternals.ThrowIfDisposed()
		{
			base.ThrowIfDisposed();
		}

		void Microsoft.ServiceBus.Messaging.Channels.ICommunicationObjectInternals.ThrowIfDisposedOrNotOpen()
		{
			base.ThrowIfDisposedOrNotOpen();
		}

		protected override void OnAbort()
		{
			this.aborted = true;
			this.inputQueue.Close();
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			this.closeCalled = true;
			this.inputQueue.Close();
			return new CompletedAsyncResult(callback, state);
		}

		protected override void OnClose(TimeSpan timeout)
		{
			this.closeCalled = true;
			this.inputQueue.Close();
		}

		protected override void OnClosing()
		{
			base.OnClosing();
			this.inputQueue.Shutdown(() => this.GetPendingException());
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
		}

		protected virtual void OnEnqueueItem(TDisposable item)
		{
		}

		protected override void OnFaulted()
		{
			base.OnFaulted();
			this.inputQueue.Shutdown(() => this.GetPendingException());
		}

		public void Shutdown()
		{
			this.inputQueue.Shutdown();
		}

		internal new void ThrowIfAborted()
		{
			if (this.aborted && !this.closeCalled)
			{
				string communicationObjectAbortedStack2 = Resources.CommunicationObjectAbortedStack2;
				object[] str = new object[] { this.GetCommunicationObjectType().ToString(), string.Empty };
				throw new CommunicationObjectAbortedException(Microsoft.ServiceBus.SR.GetString(communicationObjectAbortedStack2, str));
			}
		}

		protected bool WaitForItem(TimeSpan timeout)
		{
			this.ThrowIfNotOpened();
			bool flag = this.inputQueue.WaitForItem(timeout);
			this.ThrowIfFaulted();
			this.ThrowIfAborted();
			return flag;
		}
	}
}