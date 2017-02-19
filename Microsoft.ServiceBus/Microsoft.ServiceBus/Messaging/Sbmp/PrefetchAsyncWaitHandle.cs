using Microsoft.ServiceBus.Common;
using System;
using System.Threading;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	internal sealed class PrefetchAsyncWaitHandle
	{
		private readonly AsyncWaitHandle waitHandle;

		public PrefetchAsyncWaitHandle()
		{
			this.waitHandle = new AsyncWaitHandle(EventResetMode.AutoReset);
		}

		public AsyncResult BeginWait(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new PrefetchAsyncWaitHandle.WaitAsyncResult(this.waitHandle, callback, state, timeout);
		}

		public void EndWait(IAsyncResult result)
		{
			PrefetchAsyncWaitHandle.WaitAsyncResult.End(result);
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

			static WaitAsyncResult()
			{
				PrefetchAsyncWaitHandle.WaitAsyncResult.onWaitCallback = new Action<object, TimeoutException>(PrefetchAsyncWaitHandle.WaitAsyncResult.OnWaitCallback);
			}

			public WaitAsyncResult(AsyncWaitHandle waitHandle, AsyncCallback callback, object state, TimeSpan timeout) : base(callback, state)
			{
				this.waitHandle = waitHandle;
				if (this.waitHandle.WaitAsync(PrefetchAsyncWaitHandle.WaitAsyncResult.onWaitCallback, this, timeout))
				{
					base.Complete(true);
				}
			}

			public static new void End(IAsyncResult result)
			{
				AsyncResult.End<PrefetchAsyncWaitHandle.WaitAsyncResult>(result);
			}

			private static void OnWaitCallback(object state, TimeoutException ex)
			{
				((PrefetchAsyncWaitHandle.WaitAsyncResult)state).Complete(false, ex);
			}
		}
	}
}