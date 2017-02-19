using Microsoft.ServiceBus.Common;
using System;

namespace Microsoft.ServiceBus.Channels
{
	internal class DelegatingAsyncResult : AsyncResult<DelegatingAsyncResult>
	{
		private Action<IAsyncResult> end;

		public DelegatingAsyncResult(Func<TimeSpan, AsyncCallback, object, IAsyncResult> begin, Action<IAsyncResult> end, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
		{
			this.end = end;
			IAsyncResult asyncResult = begin(timeout, new AsyncCallback(DelegatingAsyncResult.Callback), this);
			if (asyncResult.CompletedSynchronously)
			{
				this.Completed(asyncResult, true);
			}
		}

		private static void Callback(IAsyncResult result)
		{
			if (result.CompletedSynchronously)
			{
				return;
			}
			((DelegatingAsyncResult)result.AsyncState).Completed(result, false);
		}

		private void Completed(IAsyncResult result, bool completedSynchronously)
		{
			Exception exception = null;
			try
			{
				this.end(result);
			}
			catch (Exception exception2)
			{
				Exception exception1 = exception2;
				if (Fx.IsFatal(exception1))
				{
					throw;
				}
				exception = exception1;
			}
			base.Complete(completedSynchronously, exception);
		}
	}
}