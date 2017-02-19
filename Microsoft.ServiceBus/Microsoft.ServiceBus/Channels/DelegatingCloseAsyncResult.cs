using Microsoft.ServiceBus.Common;
using System;
using System.ServiceModel;

namespace Microsoft.ServiceBus.Channels
{
	internal class DelegatingCloseAsyncResult : AsyncResult<DelegatingCloseAsyncResult>
	{
		private ICommunicationObject innerObject;

		public DelegatingCloseAsyncResult(ICommunicationObject innerObject, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
		{
			this.innerObject = innerObject;
			IAsyncResult asyncResult = this.innerObject.BeginClose(timeout, new AsyncCallback(DelegatingCloseAsyncResult.CloseCallback), this);
			if (asyncResult.CompletedSynchronously)
			{
				this.CloseCompleted(asyncResult, true);
			}
		}

		private static void CloseCallback(IAsyncResult result)
		{
			if (result.CompletedSynchronously)
			{
				return;
			}
			((DelegatingCloseAsyncResult)result.AsyncState).CloseCompleted(result, false);
		}

		private void CloseCompleted(IAsyncResult result, bool completedSynchronously)
		{
			Exception exception = null;
			try
			{
				this.innerObject.EndClose(result);
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