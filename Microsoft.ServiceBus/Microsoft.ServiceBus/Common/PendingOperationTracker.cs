using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;

namespace Microsoft.ServiceBus.Common
{
	internal sealed class PendingOperationTracker
	{
		private readonly List<AsyncWaiter> waiters;

		private readonly object syncLock;

		private long pendingOperationCount;

		private bool rejectNewRequests;

		public bool RejectNewRequests
		{
			get
			{
				bool flag;
				lock (this.syncLock)
				{
					flag = this.rejectNewRequests;
				}
				return flag;
			}
		}

		public PendingOperationTracker()
		{
			this.waiters = new List<AsyncWaiter>(1);
			this.syncLock = new object();
		}

		public IAsyncResult BeginWaitPendingOperations(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new PendingOperationTracker.WaitPendingOperationsAsyncResult(this, timeout, callback, state);
		}

		public void DecrementOperationCount()
		{
			this.DecrementOperationCount((long)1);
		}

		private void DecrementOperationCount(long count)
		{
			List<AsyncWaiter> asyncWaiters = null;
			lock (this.syncLock)
			{
				PendingOperationTracker pendingOperationTracker = this;
				pendingOperationTracker.pendingOperationCount = pendingOperationTracker.pendingOperationCount - count;
				if (this.waiters.Count != 0)
				{
					asyncWaiters = new List<AsyncWaiter>(this.waiters);
					this.waiters.Clear();
				}
			}
			if (asyncWaiters != null)
			{
				foreach (AsyncWaiter asyncWaiter in asyncWaiters)
				{
					asyncWaiter.Signal();
				}
			}
		}

		public void EndWaitPendingOperations(IAsyncResult result)
		{
			AsyncResult<PendingOperationTracker.WaitPendingOperationsAsyncResult>.End(result);
		}

		public void IncrementOperationCount()
		{
			this.IncrementOperationCount((long)1);
		}

		public void IncrementOperationCount(long count)
		{
			lock (this.syncLock)
			{
				if (this.rejectNewRequests)
				{
					throw new OperationCanceledException("Object is in the process of idling");
				}
				PendingOperationTracker pendingOperationTracker = this;
				pendingOperationTracker.pendingOperationCount = pendingOperationTracker.pendingOperationCount + count;
			}
		}

		private sealed class WaitPendingOperationsAsyncResult : AsyncResult<PendingOperationTracker.WaitPendingOperationsAsyncResult>
		{
			private readonly static AsyncCallback StaticOnWaiterCompleted;

			private readonly PendingOperationTracker owner;

			private readonly TimeSpan timeout;

			static WaitPendingOperationsAsyncResult()
			{
				PendingOperationTracker.WaitPendingOperationsAsyncResult.StaticOnWaiterCompleted = new AsyncCallback(PendingOperationTracker.WaitPendingOperationsAsyncResult.OnWaiterCompleted);
			}

			public WaitPendingOperationsAsyncResult(PendingOperationTracker owner, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				this.owner = owner;
				this.timeout = timeout;
				AsyncWaiter asyncWaiter = null;
				lock (this.owner.syncLock)
				{
					this.owner.rejectNewRequests = true;
					if (this.owner.pendingOperationCount > (long)0)
					{
						asyncWaiter = new AsyncWaiter(timeout, PendingOperationTracker.WaitPendingOperationsAsyncResult.StaticOnWaiterCompleted, this);
						this.owner.waiters.Add(asyncWaiter);
					}
				}
				if (asyncWaiter == null)
				{
					base.Complete(true);
				}
			}

			private static void OnWaiterCompleted(IAsyncResult result)
			{
				PendingOperationTracker.WaitPendingOperationsAsyncResult asyncState = (PendingOperationTracker.WaitPendingOperationsAsyncResult)result.AsyncState;
				if (AsyncWaiter.End(result))
				{
					asyncState.Complete(result.CompletedSynchronously);
					return;
				}
				TimeoutException timeoutException = new TimeoutException(SRCore.TimeoutOnOperation(asyncState.timeout));
				asyncState.Complete(result.CompletedSynchronously, timeoutException);
			}
		}
	}
}