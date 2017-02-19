using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging
{
	internal sealed class BatchManagerAsyncResult<T> : AsyncResult
	{
		private readonly static AsyncResult.AsyncCompletion onBatchedCompletion;

		private BatchManager<T> BatchManager
		{
			get;
			set;
		}

		static BatchManagerAsyncResult()
		{
			BatchManagerAsyncResult<T>.onBatchedCompletion = new AsyncResult.AsyncCompletion(BatchManagerAsyncResult<T>.OnBatchedCallback);
		}

		public BatchManagerAsyncResult(TrackingContext trackingContext, BatchManager<T> batchManager, IEnumerable<T> msgs, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
		{
			if (trackingContext == null)
			{
				throw Fx.Exception.ArgumentNull("trackingContext");
			}
			this.BatchManager = batchManager;
			if (base.SyncContinue(this.BatchManager.BeginBatchedOperation(trackingContext, msgs, timeout, base.PrepareAsyncCompletion(BatchManagerAsyncResult<T>.onBatchedCompletion), this)))
			{
				base.Complete(true);
			}
		}

		public static new void End(IAsyncResult result)
		{
			AsyncResult.End<BatchManagerAsyncResult<T>>(result);
		}

		private static bool OnBatchedCallback(IAsyncResult result)
		{
			((BatchManagerAsyncResult<T>)result.AsyncState).BatchManager.EndBatchedOperation(result);
			return true;
		}
	}
}