using System;

namespace Microsoft.ServiceBus.Common
{
	internal abstract class AsyncResult<TAsyncResult> : AsyncResult
	where TAsyncResult : AsyncResult<TAsyncResult>
	{
		protected AsyncResult(AsyncCallback callback, object state) : base(callback, state)
		{
		}

		public static new TAsyncResult End(IAsyncResult asyncResult)
		{
			return AsyncResult.End<TAsyncResult>(asyncResult);
		}
	}
}