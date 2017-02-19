using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading;

namespace Microsoft.ServiceBus.Common
{
	internal class AsyncWaitHandle
	{
		private static Action<object> timerCompleteCallback;

		private List<AsyncWaitHandle.AsyncWaiter> asyncWaiters;

		private volatile bool isSignaled;

		private EventResetMode resetMode;

		private object syncObject;

		public AsyncWaitHandle() : this(EventResetMode.AutoReset)
		{
		}

		public AsyncWaitHandle(EventResetMode resetMode)
		{
			this.resetMode = resetMode;
			this.syncObject = new object();
		}

		private static void OnTimerComplete(object state)
		{
			AsyncWaitHandle.AsyncWaiter asyncWaiter = (AsyncWaitHandle.AsyncWaiter)state;
			AsyncWaitHandle parent = asyncWaiter.Parent;
			bool flag = false;
			lock (parent.syncObject)
			{
				if (parent.asyncWaiters != null && parent.asyncWaiters.Remove(asyncWaiter))
				{
					asyncWaiter.TimedOut = true;
					flag = true;
				}
			}
			asyncWaiter.CancelTimer();
			if (flag)
			{
				asyncWaiter.Call();
			}
		}

		public void Reset()
		{
			this.isSignaled = false;
		}

		public void Set()
		{
			List<AsyncWaitHandle.AsyncWaiter> asyncWaiters = null;
			AsyncWaitHandle.AsyncWaiter item = null;
			if (!this.isSignaled)
			{
				lock (this.syncObject)
				{
					if (!this.isSignaled)
					{
						if (this.resetMode == EventResetMode.ManualReset)
						{
							this.isSignaled = true;
							Monitor.PulseAll(this.syncObject);
							asyncWaiters = this.asyncWaiters;
							this.asyncWaiters = null;
						}
						else if (this.asyncWaiters == null || this.asyncWaiters.Count <= 0)
						{
							this.isSignaled = true;
						}
						else
						{
							item = this.asyncWaiters[0];
							this.asyncWaiters.RemoveAt(0);
						}
					}
				}
			}
			if (asyncWaiters != null)
			{
				foreach (AsyncWaitHandle.AsyncWaiter asyncWaiter in asyncWaiters)
				{
					asyncWaiter.CancelTimer();
					asyncWaiter.Call();
				}
			}
			if (item != null)
			{
				item.CancelTimer();
				item.Call();
			}
		}

		public bool WaitAsync(Action<object, TimeoutException> callback, object state, TimeSpan timeout)
		{
			bool flag;
			if (!this.isSignaled || this.isSignaled && this.resetMode == EventResetMode.AutoReset)
			{
				lock (this.syncObject)
				{
					if (this.isSignaled && this.resetMode == EventResetMode.AutoReset)
					{
						this.isSignaled = false;
					}
					else if (!this.isSignaled)
					{
						AsyncWaitHandle.AsyncWaiter asyncWaiter = new AsyncWaitHandle.AsyncWaiter(this, callback, state);
						if (this.asyncWaiters == null)
						{
							this.asyncWaiters = new List<AsyncWaitHandle.AsyncWaiter>();
						}
						this.asyncWaiters.Add(asyncWaiter);
						if (timeout != TimeSpan.MaxValue)
						{
							if (AsyncWaitHandle.timerCompleteCallback == null)
							{
								AsyncWaitHandle.timerCompleteCallback = new Action<object>(AsyncWaitHandle.OnTimerComplete);
							}
							asyncWaiter.SetTimer(AsyncWaitHandle.timerCompleteCallback, asyncWaiter, timeout);
						}
						flag = false;
						return flag;
					}
					return true;
				}
				return flag;
			}
			return true;
		}

		private class AsyncWaiter : ActionItem
		{
			[SecurityCritical]
			private Action<object, TimeoutException> callback;

			[SecurityCritical]
			private object state;

			private IOThreadTimer timer;

			private TimeSpan originalTimeout;

			public AsyncWaitHandle Parent
			{
				get;
				private set;
			}

			public bool TimedOut
			{
				get;
				set;
			}

			public AsyncWaiter(AsyncWaitHandle parent, Action<object, TimeoutException> callback, object state)
			{
				this.Parent = parent;
				this.callback = callback;
				this.state = state;
			}

			public void Call()
			{
				base.Schedule();
			}

			public void CancelTimer()
			{
				if (this.timer != null)
				{
					this.timer.Cancel();
					this.timer = null;
				}
			}

			[SecurityCritical]
			protected override void Invoke()
			{
				TimeoutException timeoutException;
				Action<object, TimeoutException> action = this.callback;
				object obj = this.state;
				if (this.TimedOut)
				{
					timeoutException = new TimeoutException(SRCore.TimeoutOnOperation(this.originalTimeout));
				}
				else
				{
					timeoutException = null;
				}
				action(obj, timeoutException);
			}

			public void SetTimer(Action<object> timerCallback, object timerState, TimeSpan timeout)
			{
				if (this.timer != null)
				{
					throw Fx.Exception.AsError(new InvalidOperationException(SRCore.MustCancelOldTimer), null);
				}
				this.originalTimeout = timeout;
				this.timer = new IOThreadTimer(timerCallback, timerState, false);
				this.timer.Set(timeout);
			}
		}
	}
}