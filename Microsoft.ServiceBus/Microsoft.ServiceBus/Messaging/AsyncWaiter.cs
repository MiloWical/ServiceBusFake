using Microsoft.ServiceBus.Common;
using System;
using System.Threading;

namespace Microsoft.ServiceBus.Messaging
{
	internal sealed class AsyncWaiter : AsyncResult, IWaiter
	{
		private readonly static Action<object> timerCallback;

		private volatile int waitResult;

		private volatile int waiterSignaled;

		private IOThreadTimer ioThreadTimer;

		private bool signalCalled;

		private bool timerCallbackCalled;

		private string dueTime = DateTime.MaxValue.ToString("R");

		static AsyncWaiter()
		{
			AsyncWaiter.timerCallback = new Action<object>(AsyncWaiter.OnTimerCallback);
		}

		public AsyncWaiter(TimeSpan timeout, AsyncCallback callback, object state) : this(timeout, callback, state, true)
		{
		}

		public AsyncWaiter(TimeSpan timeout, AsyncCallback callback, object state, bool startTimer) : base(callback, state)
		{
			if (timeout != TimeSpan.MaxValue && startTimer)
			{
				this.StartTimer(timeout);
			}
		}

		public bool Cancel()
		{
			return this.Cancel(false);
		}

		public bool Cancel(bool synchronous)
		{
			return this.Signal(synchronous, new OperationCanceledException());
		}

		public static new bool End(IAsyncResult result)
		{
			return AsyncResult.End<AsyncWaiter>(result).waiterSignaled == 1;
		}

		private static void OnTimerCallback(object state)
		{
			AsyncWaiter asyncWaiter = (AsyncWaiter)state;
			asyncWaiter.timerCallbackCalled = true;
			if (Interlocked.CompareExchange(ref asyncWaiter.waitResult, 1, 0) == 0)
			{
				asyncWaiter.Complete(false);
			}
		}

		public bool Signal()
		{
			return this.Signal(false, null);
		}

		public bool Signal(bool synchronous)
		{
			return this.Signal(synchronous, null);
		}

		public bool Signal(bool synchronous, Exception exception)
		{
			this.signalCalled = true;
			if ((this.ioThreadTimer == null || !this.ioThreadTimer.Cancel()) && this.ioThreadTimer != null || Interlocked.CompareExchange(ref this.waitResult, 1, 0) != 0)
			{
				return false;
			}
			this.waiterSignaled = 1;
			base.Complete(synchronous, exception);
			return true;
		}

		public void StartTimer(TimeSpan timeout)
		{
			if (timeout != TimeSpan.MaxValue)
			{
				this.ioThreadTimer = new IOThreadTimer(AsyncWaiter.timerCallback, this, true);
				this.ioThreadTimer.Set(timeout);
				DateTime dateTime = Microsoft.ServiceBus.Common.TimeoutHelper.Add(DateTime.UtcNow, timeout);
				this.dueTime = dateTime.ToString("R");
			}
		}
	}
}