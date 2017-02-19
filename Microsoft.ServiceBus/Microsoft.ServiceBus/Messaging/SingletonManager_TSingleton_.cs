using Microsoft.ServiceBus.Common;
using System;
using System.Collections.Generic;

namespace Microsoft.ServiceBus.Messaging
{
	internal abstract class SingletonManager<TSingleton>
	where TSingleton : class
	{
		private readonly object syncRoot;

		private TSingleton singletonInstance;

		private SingletonManager<TSingleton>.SingletonState singletonState;

		private Queue<SingletonManager<TSingleton>.AsyncWaiter> waiters;

		protected TSingleton UnsafeInstance
		{
			get
			{
				return this.singletonInstance;
			}
		}

		public SingletonManager(object syncRoot)
		{
			this.syncRoot = syncRoot;
			this.singletonState = SingletonManager<TSingleton>.SingletonState.NeedCreate;
		}

		public SingletonManager(object syncRoot, TSingleton initialSingleton)
		{
			this.syncRoot = syncRoot;
			this.singletonInstance = initialSingleton;
			this.singletonState = SingletonManager<TSingleton>.SingletonState.Created;
		}

		public IAsyncResult BeginGetInstance(TimeSpan timeout, AsyncCallback callback, object state)
		{
			bool flag = false;
			IAsyncResult singletonValidAsyncResult = null;
			TSingleton tSingleton = default(TSingleton);
			lock (this.syncRoot)
			{
				if (this.singletonState == SingletonManager<TSingleton>.SingletonState.NeedCreate)
				{
					this.singletonState = SingletonManager<TSingleton>.SingletonState.Creating;
					flag = true;
				}
				else if (this.singletonState != SingletonManager<TSingleton>.SingletonState.Creating)
				{
					tSingleton = this.singletonInstance;
				}
				else
				{
					if (this.waiters == null)
					{
						this.waiters = new Queue<SingletonManager<TSingleton>.AsyncWaiter>();
					}
					SingletonManager<TSingleton>.AsyncWaiter asyncWaiter = new SingletonManager<TSingleton>.AsyncWaiter(timeout, callback, state);
					this.waiters.Enqueue(asyncWaiter);
					singletonValidAsyncResult = asyncWaiter;
				}
			}
			if (flag)
			{
				try
				{
					singletonValidAsyncResult = this.OnBeginCreateInstance(timeout, callback, state);
				}
				catch (Exception exception)
				{
					this.CompleteWaiters(default(TSingleton), exception);
					throw;
				}
			}
			else if (singletonValidAsyncResult == null)
			{
				singletonValidAsyncResult = new SingletonManager<TSingleton>.SingletonValidAsyncResult(tSingleton, callback, state);
			}
			return singletonValidAsyncResult;
		}

		private void CompleteWaiters(TSingleton singleton, Exception completeException)
		{
			Queue<SingletonManager<TSingleton>.AsyncWaiter> asyncWaiters;
			lock (this.syncRoot)
			{
				if (completeException != null)
				{
					this.singletonState = SingletonManager<TSingleton>.SingletonState.NeedCreate;
				}
				else
				{
					this.singletonState = SingletonManager<TSingleton>.SingletonState.Created;
					this.singletonInstance = singleton;
				}
				asyncWaiters = this.waiters;
				this.waiters = null;
			}
			if (asyncWaiters != null)
			{
				ExceptionInfo exceptionInfo = new ExceptionInfo(completeException);
				foreach (SingletonManager<TSingleton>.AsyncWaiter asyncWaiter in asyncWaiters)
				{
					asyncWaiter.Signal(singleton, exceptionInfo.CreateException());
				}
			}
		}

		public TSingleton EndGetInstance(IAsyncResult asyncResult)
		{
			TSingleton tSingleton;
			if (asyncResult is SingletonManager<TSingleton>.SingletonValidAsyncResult)
			{
				tSingleton = CompletedAsyncResult<TSingleton>.End(asyncResult);
			}
			else if (!(asyncResult is SingletonManager<TSingleton>.AsyncWaiter))
			{
				tSingleton = default(TSingleton);
				Exception exception = null;
				try
				{
					try
					{
						tSingleton = this.OnEndCreateInstance(asyncResult);
					}
					catch (Exception exception1)
					{
						exception = exception1;
						throw;
					}
				}
				finally
				{
					this.CompleteWaiters(tSingleton, exception);
				}
			}
			else if (!SingletonManager<TSingleton>.AsyncWaiter.End(asyncResult, out tSingleton))
			{
				throw Fx.Exception.AsWarning(new TimeoutException(), null);
			}
			this.OnGetInstance(tSingleton);
			return tSingleton;
		}

		public bool Invalidate(TSingleton instanceToInvalidate)
		{
			if (instanceToInvalidate == null)
			{
				throw new ArgumentNullException("instanceToInvalidate");
			}
			return (object)this.InvalidateCore(instanceToInvalidate) == (object)instanceToInvalidate;
		}

		public bool Invalidate(out TSingleton invalidatedInstance)
		{
			invalidatedInstance = this.InvalidateCore(default(TSingleton));
			return (object)invalidatedInstance != (object)default(TSingleton);
		}

		private TSingleton InvalidateCore(TSingleton instanceToInvalidate)
		{
			TSingleton tSingleton;
			TSingleton tSingleton1;
			lock (this.syncRoot)
			{
				if (this.singletonState == SingletonManager<TSingleton>.SingletonState.Created)
				{
					if ((object)instanceToInvalidate != (object)default(TSingleton))
					{
						TSingleton tSingleton2 = default(TSingleton);
						if ((object)instanceToInvalidate == (object)tSingleton2 || (object)this.singletonInstance != (object)instanceToInvalidate)
						{
							tSingleton1 = default(TSingleton);
							return tSingleton1;
						}
					}
					this.singletonState = SingletonManager<TSingleton>.SingletonState.NeedCreate;
					TSingleton tSingleton3 = this.singletonInstance;
					this.singletonInstance = default(TSingleton);
					tSingleton = tSingleton3;
					return tSingleton;
				}
				tSingleton1 = default(TSingleton);
				return tSingleton1;
			}
			return tSingleton;
		}

		protected abstract IAsyncResult OnBeginCreateInstance(TimeSpan timeout, AsyncCallback callback, object state);

		protected abstract TSingleton OnEndCreateInstance(IAsyncResult asyncResult);

		protected virtual void OnGetInstance(TSingleton instance)
		{
		}

		private class AsyncWaiter : AsyncResult
		{
			private readonly static Action<object> timerCallback;

			private readonly IOThreadTimer timer;

			private bool timedOut;

			private TSingleton @value;

			static AsyncWaiter()
			{
				SingletonManager<TSingleton>.AsyncWaiter.timerCallback = new Action<object>(SingletonManager<TSingleton>.AsyncWaiter.TimerCallback);
			}

			internal AsyncWaiter(TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				if (timeout != TimeSpan.MaxValue)
				{
					this.timer = new IOThreadTimer(SingletonManager<TSingleton>.AsyncWaiter.timerCallback, this, true);
					this.timer.Set(timeout);
				}
			}

			public static bool End(IAsyncResult asyncResult, out TSingleton result)
			{
				SingletonManager<TSingleton>.AsyncWaiter asyncWaiter = AsyncResult.End<SingletonManager<TSingleton>.AsyncWaiter>(asyncResult);
				result = asyncWaiter.@value;
				return !asyncWaiter.timedOut;
			}

			public void Signal(TSingleton result, Exception completeException)
			{
				if (this.timer == null || this.timer.Cancel())
				{
					this.@value = result;
					base.Complete(false, completeException);
				}
			}

			private static void TimerCallback(object state)
			{
				SingletonManager<TSingleton>.AsyncWaiter asyncWaiter = (SingletonManager<TSingleton>.AsyncWaiter)state;
				asyncWaiter.timedOut = true;
				asyncWaiter.Complete(false);
			}
		}

		private enum SingletonState
		{
			NeedCreate,
			Creating,
			Created
		}

		private class SingletonValidAsyncResult : CompletedAsyncResult<TSingleton>
		{
			public SingletonValidAsyncResult(TSingleton data, AsyncCallback callback, object state) : base(data, callback, state)
			{
			}
		}
	}
}