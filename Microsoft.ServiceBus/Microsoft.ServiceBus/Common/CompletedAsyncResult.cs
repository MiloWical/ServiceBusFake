using System;

namespace Microsoft.ServiceBus.Common
{
	[Serializable]
	internal class CompletedAsyncResult : AsyncResult
	{
		public CompletedAsyncResult(AsyncCallback callback, object state) : base(callback, state)
		{
			base.Complete(true);
		}

		public CompletedAsyncResult(Exception exception, AsyncCallback callback, object state) : base(callback, state)
		{
			base.Complete(true, exception);
		}

		public static new void End(IAsyncResult result)
		{
			Fx.AssertAndThrowFatal(result.IsCompleted, "CompletedAsyncResult was not completed!");
			AsyncResult.End<CompletedAsyncResult>(result);
		}
	}
}