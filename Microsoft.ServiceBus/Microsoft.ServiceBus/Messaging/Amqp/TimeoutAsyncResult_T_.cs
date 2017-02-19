using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using System;
using System.Threading;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal abstract class TimeoutAsyncResult<T> : AsyncResult
	where T : class
	{
		private readonly static Action<object> timerCallback;

		private readonly TimeSpan timeout;

		private readonly IOThreadTimer timer;

		private int completed;

		protected abstract T Target
		{
			get;
		}

		static TimeoutAsyncResult()
		{
			TimeoutAsyncResult<T>.timerCallback = new Action<object>(TimeoutAsyncResult<T>.OnTimerCallback);
		}

		protected TimeoutAsyncResult(TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
		{
			if (timeout != TimeSpan.MaxValue)
			{
				this.timeout = timeout;
				this.timer = new IOThreadTimer(TimeoutAsyncResult<T>.timerCallback, this, true);
				this.timer.Set(timeout);
			}
		}

		private void CompleteInternal(bool syncComplete, Exception exception)
		{
			if (Interlocked.CompareExchange(ref this.completed, 1, 0) == 0)
			{
				if (exception == null)
				{
					base.Complete(syncComplete);
					return;
				}
				base.Complete(syncComplete, exception);
			}
		}

		protected virtual void CompleteOnTimer()
		{
			this.CompleteInternal(false, new TimeoutException(SRAmqp.AmqpTimeout(this.timeout, this.Target)));
		}

		protected void CompleteSelf(bool syncComplete)
		{
			this.CompleteSelf(syncComplete, null);
		}

		protected void CompleteSelf(bool syncComplete, Exception exception)
		{
			if (this.timer != null)
			{
				this.timer.Cancel();
			}
			this.CompleteInternal(syncComplete, exception);
		}

		private static void OnTimerCallback(object state)
		{
			((TimeoutAsyncResult<T>)state).CompleteOnTimer();
		}
	}
}