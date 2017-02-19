using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading;

namespace Microsoft.ServiceBus.Channels
{
	internal class ThreadNeutralSemaphore
	{
		private readonly static TimeSpan TraceDelay;

		private readonly object ThisLock = new object();

		private readonly bool traceOnFailureToEnter;

		private readonly string semaphoreName;

		private readonly int maxCount;

		private readonly IOThreadTimer delayTraceTimer;

		private int count;

		private int failureCount;

		private Queue<ThreadNeutralSemaphore.Waiter> waiters;

		private bool aborted;

		private bool delayTraceTimerStarted;

		public int Count
		{
			get
			{
				int num;
				lock (this.ThisLock)
				{
					num = this.count;
				}
				return num;
			}
		}

		private Queue<ThreadNeutralSemaphore.Waiter> Waiters
		{
			get
			{
				if (this.waiters == null)
				{
					this.waiters = new Queue<ThreadNeutralSemaphore.Waiter>();
				}
				return this.waiters;
			}
		}

		static ThreadNeutralSemaphore()
		{
			ThreadNeutralSemaphore.TraceDelay = TimeSpan.FromMinutes(3);
		}

		public ThreadNeutralSemaphore(int maxCount) : this(maxCount, false, string.Empty)
		{
		}

		public ThreadNeutralSemaphore(int maxCount, bool traceOnFailureToEnter, string semaphoreName)
		{
			if (maxCount < 1)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("maxCount", (object)maxCount, Microsoft.ServiceBus.SR.GetString(Resources.ValueMustBePositive, new object[0])));
			}
			this.maxCount = maxCount;
			this.traceOnFailureToEnter = traceOnFailureToEnter;
			this.semaphoreName = semaphoreName;
			this.delayTraceTimer = new IOThreadTimer(new Action<object>(this.OnDelayTraceTimerCallback), null, false);
		}

		public void Abort()
		{
			lock (this.ThisLock)
			{
				if (!this.aborted)
				{
					this.aborted = true;
					if (this.waiters != null)
					{
						while (this.waiters.Count > 0)
						{
							this.waiters.Dequeue().Abort();
						}
					}
				}
			}
		}

		internal static TimeoutException CreateEnterTimedOutException(TimeSpan timeout)
		{
			string threadAcquisitionTimedOut = Resources.ThreadAcquisitionTimedOut;
			object[] objArray = new object[] { timeout };
			return new TimeoutException(Microsoft.ServiceBus.SR.GetString(threadAcquisitionTimedOut, objArray));
		}

		private static CommunicationObjectAbortedException CreateObjectAbortedException()
		{
			return new CommunicationObjectAbortedException(Microsoft.ServiceBus.SR.GetString(Resources.ThreadNeutralSemaphoreAborted, new object[0]));
		}

		public bool Enter(Action<object> callback, object state)
		{
			bool flag;
			if (callback == null)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("callback");
			}
			lock (this.ThisLock)
			{
				if (this.count >= this.maxCount)
				{
					if (this.traceOnFailureToEnter)
					{
						this.IncrementFailureCount();
					}
					this.Waiters.Enqueue(new ThreadNeutralSemaphore.AsyncWaiter(callback, state));
					flag = false;
				}
				else
				{
					ThreadNeutralSemaphore threadNeutralSemaphore = this;
					threadNeutralSemaphore.count = threadNeutralSemaphore.count + 1;
					flag = true;
				}
			}
			return flag;
		}

		public void Enter()
		{
			ThreadNeutralSemaphore.SyncWaiter syncWaiter = this.EnterCore();
			if (syncWaiter != null)
			{
				syncWaiter.Wait();
			}
		}

		public void Enter(TimeSpan timeout)
		{
			if (!this.TryEnter(timeout))
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(ThreadNeutralSemaphore.CreateEnterTimedOutException(timeout));
			}
		}

		private ThreadNeutralSemaphore.SyncWaiter EnterCore()
		{
			ThreadNeutralSemaphore.SyncWaiter syncWaiter;
			lock (this.ThisLock)
			{
				if (this.aborted)
				{
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(ThreadNeutralSemaphore.CreateObjectAbortedException());
				}
				if (this.count >= this.maxCount)
				{
					if (this.traceOnFailureToEnter)
					{
						this.IncrementFailureCount();
					}
					ThreadNeutralSemaphore.SyncWaiter syncWaiter1 = new ThreadNeutralSemaphore.SyncWaiter(this);
					this.Waiters.Enqueue(syncWaiter1);
					return syncWaiter1;
				}
				else
				{
					ThreadNeutralSemaphore threadNeutralSemaphore = this;
					threadNeutralSemaphore.count = threadNeutralSemaphore.count + 1;
					syncWaiter = null;
				}
			}
			return syncWaiter;
		}

		public void Exit()
		{
			ThreadNeutralSemaphore.Waiter waiter;
			lock (this.ThisLock)
			{
				if (this.count == 0)
				{
					string str = Microsoft.ServiceBus.SR.GetString(Resources.InvalidLockOperation, new object[0]);
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new SynchronizationLockException(str));
				}
				if (this.waiters == null || this.waiters.Count == 0)
				{
					ThreadNeutralSemaphore threadNeutralSemaphore = this;
					threadNeutralSemaphore.count = threadNeutralSemaphore.count - 1;
					return;
				}
				else
				{
					waiter = this.waiters.Dequeue();
				}
			}
			waiter.Signal();
		}

		private void IncrementFailureCount()
		{
			lock (this.ThisLock)
			{
				if (!this.delayTraceTimerStarted)
				{
					this.delayTraceTimer.Set(ThreadNeutralSemaphore.TraceDelay);
					this.delayTraceTimerStarted = true;
				}
				ThreadNeutralSemaphore threadNeutralSemaphore = this;
				threadNeutralSemaphore.failureCount = threadNeutralSemaphore.failureCount + 1;
			}
		}

		private void OnDelayTraceTimerCallback(object o)
		{
			int num = 0;
			lock (this.ThisLock)
			{
				num = this.failureCount;
				this.failureCount = 0;
				this.delayTraceTimerStarted = false;
			}
			if (num > 0)
			{
				MessagingClientEtwProvider.Provider.EventWriteThreadNeutralSemaphoreEnterFailed(this.semaphoreName, num, ThreadNeutralSemaphore.TraceDelay.TotalMilliseconds);
			}
		}

		private bool RemoveWaiter(ThreadNeutralSemaphore.Waiter waiter)
		{
			bool flag = false;
			lock (this.ThisLock)
			{
				for (int i = this.Waiters.Count; i > 0; i--)
				{
					ThreadNeutralSemaphore.Waiter waiter1 = this.Waiters.Dequeue();
					if (!object.ReferenceEquals(waiter1, waiter))
					{
						this.Waiters.Enqueue(waiter1);
					}
					else
					{
						flag = true;
					}
				}
			}
			return flag;
		}

		public bool TryEnter()
		{
			bool flag;
			lock (this.ThisLock)
			{
				if (this.count >= this.maxCount)
				{
					if (this.traceOnFailureToEnter)
					{
						this.IncrementFailureCount();
					}
					flag = false;
				}
				else
				{
					ThreadNeutralSemaphore threadNeutralSemaphore = this;
					threadNeutralSemaphore.count = threadNeutralSemaphore.count + 1;
					flag = true;
				}
			}
			return flag;
		}

		public bool TryEnter(TimeSpan timeout)
		{
			ThreadNeutralSemaphore.SyncWaiter syncWaiter = this.EnterCore();
			if (syncWaiter == null)
			{
				return true;
			}
			return syncWaiter.Wait(timeout);
		}

		private class AsyncWaiter : ThreadNeutralSemaphore.Waiter
		{
			private Action<object> callback;

			private object state;

			public AsyncWaiter(Action<object> callback, object state)
			{
				this.callback = callback;
				this.state = state;
			}

			public override void Abort()
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException(Microsoft.ServiceBus.SR.GetString(Resources.ThreadNeutralSemaphoreAsyncAbort, new object[0])));
			}

			public override void Signal()
			{
				IOThreadScheduler.ScheduleCallbackNoFlow(this.callback, this.state);
			}
		}

		private class SyncWaiter : ThreadNeutralSemaphore.Waiter
		{
			private ThreadNeutralSemaphore parent;

			private AutoResetEvent waitHandle;

			private bool aborted;

			public SyncWaiter(ThreadNeutralSemaphore parent)
			{
				this.waitHandle = new AutoResetEvent(false);
				this.parent = parent;
			}

			public override void Abort()
			{
				this.aborted = true;
				this.waitHandle.Set();
			}

			public override void Signal()
			{
				this.waitHandle.Set();
			}

			public void Wait()
			{
				this.waitHandle.WaitOne();
				this.waitHandle.Close();
				if (this.aborted)
				{
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(ThreadNeutralSemaphore.CreateObjectAbortedException());
				}
			}

			public bool Wait(TimeSpan timeout)
			{
				bool flag = true;
				if (!Microsoft.ServiceBus.Common.TimeoutHelper.WaitOne(this.waitHandle, timeout))
				{
					if (this.parent.RemoveWaiter(this))
					{
						flag = false;
					}
					else
					{
						this.waitHandle.WaitOne();
					}
				}
				this.waitHandle.Close();
				if (this.aborted)
				{
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(ThreadNeutralSemaphore.CreateObjectAbortedException());
				}
				return flag;
			}
		}

		private abstract class Waiter
		{
			protected Waiter()
			{
			}

			public abstract void Abort();

			public abstract void Signal();
		}
	}
}