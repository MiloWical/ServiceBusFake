using System;

namespace Microsoft.ServiceBus.Common
{
	internal class ChainedAsyncResult : AsyncResult
	{
		private ChainedBeginHandler begin2;

		private ChainedEndHandler end1;

		private ChainedEndHandler end2;

		private TimeoutHelper timeoutHelper;

		private static AsyncCallback begin1Callback;

		private static AsyncCallback begin2Callback;

		static ChainedAsyncResult()
		{
			ChainedAsyncResult.begin1Callback = new AsyncCallback(ChainedAsyncResult.Begin1Callback);
			ChainedAsyncResult.begin2Callback = new AsyncCallback(ChainedAsyncResult.Begin2Callback);
		}

		protected ChainedAsyncResult(TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
		{
			this.timeoutHelper = new TimeoutHelper(timeout);
		}

		public ChainedAsyncResult(TimeSpan timeout, AsyncCallback callback, object state, ChainedBeginHandler begin1, ChainedEndHandler end1, ChainedBeginHandler begin2, ChainedEndHandler end2) : base(callback, state)
		{
			this.timeoutHelper = new TimeoutHelper(timeout);
			this.Begin(begin1, end1, begin2, end2);
		}

		protected void Begin(ChainedBeginHandler beginOne, ChainedEndHandler endOne, ChainedBeginHandler beginTwo, ChainedEndHandler endTwo)
		{
			this.end1 = endOne;
			this.begin2 = beginTwo;
			this.end2 = endTwo;
			IAsyncResult asyncResult = beginOne(this.timeoutHelper.RemainingTime(), ChainedAsyncResult.begin1Callback, this);
			if (!asyncResult.CompletedSynchronously)
			{
				return;
			}
			if (this.Begin1Completed(asyncResult))
			{
				base.Complete(true);
			}
		}

		private static void Begin1Callback(IAsyncResult result)
		{
			if (result.CompletedSynchronously)
			{
				return;
			}
			ChainedAsyncResult asyncState = (ChainedAsyncResult)result.AsyncState;
			bool flag = false;
			Exception exception = null;
			try
			{
				flag = asyncState.Begin1Completed(result);
			}
			catch (Exception exception2)
			{
				Exception exception1 = exception2;
				if (Fx.IsFatal(exception1))
				{
					throw;
				}
				flag = true;
				exception = exception1;
			}
			if (flag)
			{
				asyncState.Complete(false, exception);
			}
		}

		private bool Begin1Completed(IAsyncResult result)
		{
			this.end1(result);
			result = this.begin2(this.timeoutHelper.RemainingTime(), ChainedAsyncResult.begin2Callback, this);
			if (!result.CompletedSynchronously)
			{
				return false;
			}
			this.end2(result);
			return true;
		}

		private static void Begin2Callback(IAsyncResult result)
		{
			if (result.CompletedSynchronously)
			{
				return;
			}
			ChainedAsyncResult asyncState = (ChainedAsyncResult)result.AsyncState;
			Exception exception = null;
			try
			{
				asyncState.end2(result);
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
			asyncState.Complete(false, exception);
		}

		public static new void End(IAsyncResult result)
		{
			AsyncResult.End<ChainedAsyncResult>(result);
		}
	}
}