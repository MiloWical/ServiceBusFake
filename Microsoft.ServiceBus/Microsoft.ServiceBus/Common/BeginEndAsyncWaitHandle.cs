using Microsoft.ServiceBus.Tracing;
using System;
using System.Threading;

namespace Microsoft.ServiceBus.Common
{
	internal sealed class BeginEndAsyncWaitHandle
	{
		private readonly AsyncWaitHandle waitHandle;

		public BeginEndAsyncWaitHandle()
		{
			this.waitHandle = new AsyncWaitHandle(EventResetMode.AutoReset);
		}

		public IAsyncResult BeginWait(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.BeginWait(null, timeout, callback, state);
		}

		public IAsyncResult BeginWait(EventTraceActivity activity, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new BeginEndAsyncWaitHandle.WaitAsyncResult(this.waitHandle, activity, callback, state, timeout);
		}

		public void EndWait(IAsyncResult result)
		{
			BeginEndAsyncWaitHandle.WaitAsyncResult.End(result);
		}

		public void Reset()
		{
			this.waitHandle.Reset();
		}

		public void Set()
		{
			this.waitHandle.Set();
		}

		private sealed class WaitAsyncResult : AsyncResult
		{
			private readonly static Action<object, TimeoutException> onWaitCallback;

			private readonly AsyncWaitHandle waitHandle;

			private readonly EventTraceActivity activity;

			protected internal override EventTraceActivity Activity
			{
				get
				{
					return this.activity;
				}
			}

			static WaitAsyncResult()
			{
				BeginEndAsyncWaitHandle.WaitAsyncResult.onWaitCallback = new Action<object, TimeoutException>(BeginEndAsyncWaitHandle.WaitAsyncResult.OnWaitCallback);
			}

			public WaitAsyncResult(AsyncWaitHandle waitHandle, EventTraceActivity activity, AsyncCallback callback, object state, TimeSpan timeout) : base(callback, state)
			{
				this.waitHandle = waitHandle;
				this.activity = activity;
				if (this.waitHandle.WaitAsync(BeginEndAsyncWaitHandle.WaitAsyncResult.onWaitCallback, this, timeout))
				{
					base.Complete(true);
				}
			}

			public static new void End(IAsyncResult result)
			{
				AsyncResult.End<BeginEndAsyncWaitHandle.WaitAsyncResult>(result);
			}

			private static void OnWaitCallback(object state, TimeoutException ex)
			{
				((BeginEndAsyncWaitHandle.WaitAsyncResult)state).Complete(false, ex);
			}
		}
	}
}