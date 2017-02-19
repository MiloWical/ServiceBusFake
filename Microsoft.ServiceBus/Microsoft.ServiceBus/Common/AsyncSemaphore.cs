using Microsoft.ServiceBus.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.ServiceBus.Common
{
	internal sealed class AsyncSemaphore
	{
		private readonly int maximumCount;

		private readonly LinkedList<AsyncSemaphore.SemaphoreWaiter> waiters;

		private int count;

		private object SyncRoot
		{
			get
			{
				return this.waiters;
			}
		}

		public AsyncSemaphore(int maximumCount) : this(maximumCount, maximumCount)
		{
		}

		public AsyncSemaphore(int initialCount, int maximumCount)
		{
			if (initialCount <= 0)
			{
				throw new ArgumentOutOfRangeException("initialCount", (object)initialCount, Resources.ValueMustBePositive);
			}
			if (maximumCount <= 0)
			{
				throw new ArgumentOutOfRangeException("maximumCount", (object)maximumCount, Resources.ValueMustBePositive);
			}
			if (initialCount > maximumCount)
			{
				CultureInfo currentCulture = CultureInfo.CurrentCulture;
				string valueMustBeInRange = Resources.ValueMustBeInRange;
				object[] objArray = new object[] { 0, maximumCount };
				string str = string.Format(currentCulture, valueMustBeInRange, objArray);
				throw new ArgumentOutOfRangeException("initialCount", (object)initialCount, str);
			}
			this.count = initialCount;
			this.maximumCount = maximumCount;
			this.waiters = new LinkedList<AsyncSemaphore.SemaphoreWaiter>();
		}

		public IAsyncResult BeginEnter(AsyncCallback callback, object state)
		{
			return this.BeginEnter(TimeSpan.MaxValue, callback, state);
		}

		public IAsyncResult BeginEnter(TimeSpan timeout, AsyncCallback callback, object state)
		{
			AsyncSemaphore.SemaphoreWaiter semaphoreWaiter;
			bool flag;
			lock (this.SyncRoot)
			{
				if (this.count <= 0)
				{
					flag = false;
					semaphoreWaiter = new AsyncSemaphore.SemaphoreWaiter(this, timeout, callback, state);
					semaphoreWaiter.StartTimer(this.waiters.AddLast(semaphoreWaiter));
				}
				else
				{
					flag = true;
					AsyncSemaphore asyncSemaphore = this;
					asyncSemaphore.count = asyncSemaphore.count - 1;
					semaphoreWaiter = new AsyncSemaphore.SemaphoreWaiter(this, TimeSpan.MaxValue, callback, state);
				}
			}
			if (flag)
			{
				semaphoreWaiter.Signal(true, true);
			}
			return semaphoreWaiter;
		}

		public bool EndEnter(IAsyncResult asyncResult)
		{
			return AsyncSemaphore.SemaphoreWaiter.End(asyncResult);
		}

		public void Exit()
		{
			AsyncSemaphore.SemaphoreWaiter value = null;
			lock (this.SyncRoot)
			{
				if (this.count == this.maximumCount)
				{
					throw new SemaphoreFullException(SRCore.AsyncSemaphoreExitCalledWithoutEnter);
				}
				if (this.waiters.Count <= 0)
				{
					AsyncSemaphore asyncSemaphore = this;
					asyncSemaphore.count = asyncSemaphore.count + 1;
				}
				else
				{
					LinkedListNode<AsyncSemaphore.SemaphoreWaiter> first = this.waiters.First;
					this.waiters.Remove(first);
					value = first.Value;
				}
			}
			if (value != null)
			{
				value.Signal(false, true);
			}
		}

		private void HandleTimeout(AsyncSemaphore.SemaphoreWaiter waiter)
		{
			bool flag = false;
			lock (this.SyncRoot)
			{
				if (waiter.Node.List != null)
				{
					flag = true;
					this.waiters.Remove(waiter.Node);
				}
			}
			if (flag)
			{
				waiter.Signal(false, false);
			}
		}

		public bool TryEnter()
		{
			bool flag;
			lock (this.SyncRoot)
			{
				if (this.count <= 0)
				{
					return false;
				}
				else
				{
					AsyncSemaphore asyncSemaphore = this;
					asyncSemaphore.count = asyncSemaphore.count - 1;
					flag = true;
				}
			}
			return flag;
		}

		private sealed class SemaphoreWaiter : AsyncResult<AsyncSemaphore.SemaphoreWaiter>
		{
			private readonly static Action<object> OnTimeoutElapsedStaticDelegate;

			private readonly AsyncSemaphore owner;

			private readonly TimeSpan timeout;

			private IOThreadTimer timer;

			private bool result;

			public LinkedListNode<AsyncSemaphore.SemaphoreWaiter> Node
			{
				get;
				private set;
			}

			static SemaphoreWaiter()
			{
				AsyncSemaphore.SemaphoreWaiter.OnTimeoutElapsedStaticDelegate = new Action<object>(AsyncSemaphore.SemaphoreWaiter.OnTimeoutElapsed);
			}

			public SemaphoreWaiter(AsyncSemaphore owner, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				this.owner = owner;
				this.timeout = timeout;
				this.result = true;
			}

			public static new bool End(IAsyncResult asyncResult)
			{
				return AsyncResult<AsyncSemaphore.SemaphoreWaiter>.End(asyncResult).result;
			}

			private static void OnTimeoutElapsed(object state)
			{
				AsyncSemaphore.SemaphoreWaiter semaphoreWaiter = (AsyncSemaphore.SemaphoreWaiter)state;
				semaphoreWaiter.owner.HandleTimeout(semaphoreWaiter);
			}

			public void Signal(bool completedSynchronously, bool result)
			{
				this.result = result;
				if (completedSynchronously)
				{
					base.Complete(true);
					return;
				}
				IOThreadScheduler.ScheduleCallbackNoFlow((object o) => ((AsyncSemaphore.SemaphoreWaiter)o).Complete(false), this);
			}

			public void StartTimer(LinkedListNode<AsyncSemaphore.SemaphoreWaiter> node)
			{
				this.Node = node;
				this.timer = new IOThreadTimer(AsyncSemaphore.SemaphoreWaiter.OnTimeoutElapsedStaticDelegate, this, true);
				this.timer.SetIfValid(this.timeout);
			}
		}
	}
}