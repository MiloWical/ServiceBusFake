using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using System.Transactions;

namespace Microsoft.ServiceBus.Messaging
{
	internal sealed class BatchManager<TItem> : CommunicationObject
	{
		private static string nullKey;

		private static Action<object> flushTimerCallback;

		private static AsyncCallback flushCompleteCallback;

		private static AsyncCallback transactionObjectCompleteCallback;

		private readonly TransactionCompletedEventHandler transactionCompletedEventHandler;

		private GroupByKeySelectorDelegate<TItem> groupByKeySelector;

		private Queue<BatchManager<TItem>.BatchedObjectsAsyncResult> batchedResults;

		private Dictionary<string, Queue<BatchManager<TItem>.BatchedObjectsAsyncResult>> transactionalBatchResults;

		private TimeSpan flushInterval;

		private long flushThreshold;

		private IOThreadTimer flushTimer;

		private bool isTimerSet;

		private long maximumBatchSize;

		private int pendingBatchSize;

		private object syncLock;

		private object syncTransactionLock;

		private int batchOverheadSize;

		private BeginBatchedDelegate<TItem> BatchedBegin
		{
			get;
			set;
		}

		private EndBatchedDelegate BatchedEnd
		{
			get;
			set;
		}

		public CalculateBatchSizeDelegate<TItem> CalculateBatchSize
		{
			get;
			set;
		}

		protected override TimeSpan DefaultCloseTimeout
		{
			get
			{
				return Constants.DefaultOperationTimeout;
			}
		}

		protected override TimeSpan DefaultOpenTimeout
		{
			get
			{
				return Constants.DefaultOperationTimeout;
			}
		}

		public TimeSpan FlushInterval
		{
			get
			{
				return this.flushInterval;
			}
			set
			{
				base.ThrowIfDisposedOrImmutable();
				this.flushInterval = value;
			}
		}

		public GroupByKeySelectorDelegate<TItem> GroupByKeySelector
		{
			get
			{
				return this.groupByKeySelector;
			}
			set
			{
				if (value != this.groupByKeySelector)
				{
					if (value == null)
					{
						this.StartBatchOperation = new BatchManager<TItem>.StartBatchOperationDelegate(BatchManager<TItem>.PerformFlushAsyncResult.StartBatchOperation);
					}
					else
					{
						this.StartBatchOperation = new BatchManager<TItem>.StartBatchOperationDelegate(BatchManager<TItem>.PerformFlushAsyncResult.StartGroupByBatchOperation);
					}
				}
				this.groupByKeySelector = value;
			}
		}

		private OnRetryDelegate<TItem> OnRetry
		{
			get;
			set;
		}

		private TransactionStateChangedDelegate OnTransactionStateChanged
		{
			get;
			set;
		}

		private BatchManager<TItem>.StartBatchOperationDelegate StartBatchOperation
		{
			get;
			set;
		}

		static BatchManager()
		{
			BatchManager<TItem>.nullKey = Guid.NewGuid().ToString();
			BatchManager<TItem>.flushTimerCallback = new Action<object>(BatchManager<TItem>.OnFlushTimerFired);
			BatchManager<TItem>.flushCompleteCallback = new AsyncCallback(BatchManager<TItem>.OnFlushComplete);
			BatchManager<TItem>.transactionObjectCompleteCallback = new AsyncCallback(BatchManager<TItem>.OnTransactionObjectCompleteCallback);
		}

		public BatchManager(BeginBatchedDelegate<TItem> batchedBeginOperation, EndBatchedDelegate batchedEndOperation, CalculateBatchSizeDelegate<TItem> calculateBatchSize, OnRetryDelegate<TItem> onRetryOperation, TransactionStateChangedDelegate transactionStateChanged, TransactionCompletedEventHandler transactionCompletedEventHandler, long flushThreshold, long maximumBatchSize, int batchOverheadSize)
		{
			this.StartBatchOperation = new BatchManager<TItem>.StartBatchOperationDelegate(BatchManager<TItem>.PerformFlushAsyncResult.StartBatchOperation);
			this.BatchedBegin = batchedBeginOperation;
			this.BatchedEnd = batchedEndOperation;
			this.CalculateBatchSize = calculateBatchSize;
			this.OnRetry = onRetryOperation;
			this.OnTransactionStateChanged = transactionStateChanged;
			this.transactionCompletedEventHandler = transactionCompletedEventHandler;
			this.batchedResults = new Queue<BatchManager<TItem>.BatchedObjectsAsyncResult>();
			this.transactionalBatchResults = new Dictionary<string, Queue<BatchManager<TItem>.BatchedObjectsAsyncResult>>();
			this.flushTimer = new IOThreadTimer(BatchManager<TItem>.flushTimerCallback, this, false);
			this.flushThreshold = flushThreshold;
			this.maximumBatchSize = maximumBatchSize;
			this.pendingBatchSize = batchOverheadSize;
			this.batchOverheadSize = batchOverheadSize;
			this.syncLock = new object();
			this.syncTransactionLock = new object();
		}

		public BatchManager(BeginBatchedDelegate<TItem> batchedBeginOperation, EndBatchedDelegate batchedEndOperation, CalculateBatchSizeDelegate<TItem> calculateBatchSize, OnRetryDelegate<TItem> onRetryOperation, TransactionStateChangedDelegate transactionStateChanged, TransactionCompletedEventHandler transactionCompletedEventHandler, long flushThreshold, long maximumBatchSize) : this(batchedBeginOperation, batchedEndOperation, calculateBatchSize, onRetryOperation, transactionStateChanged, transactionCompletedEventHandler, flushThreshold, maximumBatchSize, 0)
		{
		}

		public IAsyncResult BeginBatchedOperation(TrackingContext trackingContext, IEnumerable<TItem> batchItems, TimeSpan timeout, AsyncCallback callback, object state)
		{
			base.ThrowIfDisposedOrNotOpen();
			if (!batchItems.Any<TItem>())
			{
				return new CompletedAsyncResult(callback, state);
			}
			Transaction current = Transaction.Current;
			int calculateBatchSize = this.CalculateBatchSize(batchItems);
			if (current != null)
			{
				string localIdentifier = current.TransactionInformation.LocalIdentifier;
				BatchManager<TItem>.BatchedObjectsAsyncResult batchedObjectsAsyncResult = new BatchManager<TItem>.BatchedObjectsAsyncResult(trackingContext, batchItems, calculateBatchSize, timeout, BatchManager<TItem>.transactionObjectCompleteCallback, state);
				lock (this.syncTransactionLock)
				{
					if (this.transactionalBatchResults.ContainsKey(localIdentifier))
					{
						this.transactionalBatchResults[localIdentifier].Enqueue(batchedObjectsAsyncResult);
					}
					else
					{
						if (this.transactionCompletedEventHandler != null)
						{
							current.TransactionCompleted += this.transactionCompletedEventHandler;
						}
						current.EnlistVolatile(new BatchManager<TItem>.BatchNotification(trackingContext, this, localIdentifier), EnlistmentOptions.EnlistDuringPrepareRequired);
						Queue<BatchManager<TItem>.BatchedObjectsAsyncResult> batchedObjectsAsyncResults = new Queue<BatchManager<TItem>.BatchedObjectsAsyncResult>();
						batchedObjectsAsyncResults.Enqueue(batchedObjectsAsyncResult);
						this.transactionalBatchResults.Add(localIdentifier, batchedObjectsAsyncResults);
					}
				}
				return new CompletedAsyncResult(callback, state);
			}
			if ((long)(this.batchOverheadSize + calculateBatchSize) >= this.maximumBatchSize)
			{
				return this.BatchedBegin(trackingContext, batchItems, null, timeout, callback, state);
			}
			BatchManager<TItem>.BatchedObjectsAsyncResult batchedObjectsAsyncResult1 = new BatchManager<TItem>.BatchedObjectsAsyncResult(trackingContext, batchItems, calculateBatchSize, timeout, callback, state);
			BatchManager<TItem>.PerformFlushAsyncResult performFlushAsyncResult = null;
			lock (this.syncLock)
			{
				this.batchedResults.Enqueue(batchedObjectsAsyncResult1);
				BatchManager<TItem> batchManager = this;
				batchManager.pendingBatchSize = batchManager.pendingBatchSize + calculateBatchSize;
				if ((long)this.pendingBatchSize >= this.flushThreshold)
				{
					if (!this.isTimerSet || this.flushTimer.Cancel())
					{
						performFlushAsyncResult = this.BeginFlush(trackingContext, TimeSpan.MaxValue, BatchManager<TItem>.flushCompleteCallback, this);
					}
				}
				else if (!this.isTimerSet)
				{
					this.SetFlushTimer(false);
				}
			}
			if (performFlushAsyncResult != null)
			{
				performFlushAsyncResult.StartOperation();
				if (performFlushAsyncResult.CompletedSynchronously)
				{
					this.EndFlush(performFlushAsyncResult);
				}
			}
			return batchedObjectsAsyncResult1;
		}

		private BatchManager<TItem>.PerformFlushAsyncResult BeginFlush(TrackingContext trackingContext, TimeSpan closeTimeout, AsyncCallback callback, object state)
		{
			this.isTimerSet = false;
			if (this.pendingBatchSize == this.batchOverheadSize)
			{
				return null;
			}
			List<BatchManager<TItem>.BatchedObjectsAsyncResult> batchedObjectsAsyncResults = new List<BatchManager<TItem>.BatchedObjectsAsyncResult>();
			TimeSpan remainingTime = closeTimeout;
			int num = 0;
			while (this.batchedResults.Count > 0)
			{
				BatchManager<TItem>.BatchedObjectsAsyncResult batchedObjectsAsyncResult = this.batchedResults.Peek();
				int batchSize = batchedObjectsAsyncResult.BatchSize;
				if (batchedObjectsAsyncResult.RemainingTime > TimeSpan.Zero)
				{
					if ((long)(num + batchSize) > this.maximumBatchSize)
					{
						break;
					}
					if (batchedObjectsAsyncResult.RemainingTime < remainingTime)
					{
						remainingTime = batchedObjectsAsyncResult.RemainingTime;
					}
					batchedObjectsAsyncResults.Add(batchedObjectsAsyncResult);
					num = num + batchSize;
					this.batchedResults.Dequeue();
				}
				else
				{
					ActionItem.Schedule((object s) => batchedObjectsAsyncResult.CompleteBatch(new TimeoutException()), null);
					BatchManager<TItem> batchManager = this;
					batchManager.pendingBatchSize = batchManager.pendingBatchSize - batchSize;
					this.batchedResults.Dequeue();
				}
			}
			BatchManager<TItem> batchManager1 = this;
			batchManager1.pendingBatchSize = batchManager1.pendingBatchSize - num;
			if (this.pendingBatchSize > this.batchOverheadSize)
			{
				this.SetFlushTimer((long)this.pendingBatchSize >= this.flushThreshold);
			}
			if (batchedObjectsAsyncResults.Count <= 0)
			{
				return null;
			}
			return new BatchManager<TItem>.PerformFlushAsyncResult(trackingContext, this, batchedObjectsAsyncResults, null, remainingTime, callback, state);
		}

		public void EndBatchedOperation(IAsyncResult result)
		{
			if (result is BatchManager<TItem>.BatchedObjectsAsyncResult)
			{
				BatchManager<TItem>.BatchedObjectsAsyncResult.End(result);
				return;
			}
			if (result is CompletedAsyncResult)
			{
				CompletedAsyncResult.End(result);
				return;
			}
			this.BatchedEnd(result, Transaction.Current == null);
		}

		private void EndFlush(IAsyncResult result)
		{
			try
			{
				BatchManager<TItem>.PerformFlushAsyncResult.End(result);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				BatchManager<TItem>.PerformFlushAsyncResult performFlushAsyncResult = (BatchManager<TItem>.PerformFlushAsyncResult)result;
				MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteBatchManagerException(performFlushAsyncResult.TrackingContext.Activity, performFlushAsyncResult.TrackingContext.TrackingId, performFlushAsyncResult.TrackingContext.SystemTracker, "BatchManager.EndFlush", exception.ToStringSlim()));
			}
		}

		protected override void OnAbort()
		{
			if (this.flushTimer != null)
			{
				this.flushTimer.Cancel();
			}
			lock (this.syncLock)
			{
				foreach (BatchManager<TItem>.BatchedObjectsAsyncResult batchedResult in this.batchedResults)
				{
					BatchManager<TItem>.BatchedObjectsAsyncResult batchedObjectsAsyncResult = batchedResult;
					ActionItem.Schedule((object s) => batchedObjectsAsyncResult.CompleteBatch(new CommunicationObjectAbortedException(SRClient.BatchManagerAborted)), null);
				}
				this.batchedResults.Clear();
				this.pendingBatchSize = 0;
			}
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid());
			BatchManager<TItem>.PerformFlushAsyncResult performFlushAsyncResult = null;
			lock (this.syncLock)
			{
				if (this.flushTimer.Cancel())
				{
					performFlushAsyncResult = this.BeginFlush(instance, timeout, callback, state);
				}
			}
			if (performFlushAsyncResult != null)
			{
				performFlushAsyncResult.StartOperation();
				return performFlushAsyncResult;
			}
			return new CompletedAsyncResult(callback, state);
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult(callback, state);
		}

		protected override void OnClose(TimeSpan timeout)
		{
			this.OnEndClose(this.OnBeginClose(timeout, null, null));
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			if (result is CompletedAsyncResult)
			{
				CompletedAsyncResult.End(result);
				return;
			}
			if (!result.CompletedSynchronously)
			{
				this.EndFlush(result);
			}
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
		}

		private static void OnFlushComplete(object state)
		{
			IAsyncResult asyncResult = (IAsyncResult)state;
			if (!asyncResult.CompletedSynchronously)
			{
				((BatchManager<TItem>)asyncResult.AsyncState).EndFlush(asyncResult);
			}
		}

		private static void OnFlushTimerFired(object state)
		{
			BatchManager<TItem>.PerformFlushAsyncResult performFlushAsyncResult;
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid());
			BatchManager<TItem> batchManager = (BatchManager<TItem>)state;
			if (batchManager.State == CommunicationState.Opened)
			{
				lock (batchManager.syncLock)
				{
					performFlushAsyncResult = batchManager.BeginFlush(instance, TimeSpan.MaxValue, BatchManager<TItem>.flushCompleteCallback, batchManager);
				}
				if (performFlushAsyncResult != null)
				{
					performFlushAsyncResult.StartOperation();
					if (performFlushAsyncResult.CompletedSynchronously)
					{
						batchManager.EndFlush(performFlushAsyncResult);
					}
				}
			}
		}

		protected override void OnOpen(TimeSpan timeout)
		{
			this.OnEndOpen(this.OnBeginOpen(timeout, null, null));
		}

		private static void OnTransactionObjectCompleteCallback(object state)
		{
			try
			{
				BatchManager<TItem>.BatchedObjectsAsyncResult.End((IAsyncResult)state);
			}
			catch (Exception exception)
			{
				if (Fx.IsFatal(exception))
				{
					throw;
				}
			}
		}

		private void SetFlushTimer(bool fireImmediately)
		{
			if (this.FlushInterval != TimeSpan.MaxValue)
			{
				this.isTimerSet = true;
				this.flushTimer.Set((fireImmediately ? TimeSpan.Zero : this.FlushInterval));
			}
		}

		private sealed class BatchedObjectsAsyncResult : AsyncResult
		{
			private Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper;

			public IEnumerable<TItem> BatchedObjects
			{
				get;
				private set;
			}

			public int BatchSize
			{
				get;
				private set;
			}

			public TimeSpan RemainingTime
			{
				get
				{
					return this.timeoutHelper.RemainingTime();
				}
			}

			public TrackingContext TrackingContext
			{
				get;
				private set;
			}

			public BatchedObjectsAsyncResult(TrackingContext trackingContext, IEnumerable<TItem> batchedObjects, int batchSize, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				this.TrackingContext = trackingContext;
				this.BatchedObjects = batchedObjects;
				this.BatchSize = batchSize;
				this.timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
			}

			public void CompleteBatch(Exception delayedException)
			{
				base.Complete(false, delayedException);
			}

			public static new void End(IAsyncResult result)
			{
				AsyncResult.End<BatchManager<TItem>.BatchedObjectsAsyncResult>(result);
			}
		}

		private sealed class BatchNotification : IEnlistmentNotification
		{
			private readonly static AsyncCallback onFlushCompleted;

			private readonly TrackingContext trackingContext;

			private readonly BatchManager<TItem> batchManager;

			private readonly string transactionIdentifier;

			private PreparingEnlistment preparingEnlistment;

			private bool prepareCalled;

			static BatchNotification()
			{
				BatchManager<TItem>.BatchNotification.onFlushCompleted = new AsyncCallback(BatchManager<TItem>.BatchNotification.OnFlushCompleted);
			}

			public BatchNotification(TrackingContext trackingContext, BatchManager<TItem> batchManager, string transactionIdentifier)
			{
				this.trackingContext = trackingContext;
				this.batchManager = batchManager;
				this.transactionIdentifier = transactionIdentifier;
			}

			private static void OnFlushCompleted(IAsyncResult result)
			{
				BatchManager<TItem>.BatchNotification asyncState = (BatchManager<TItem>.BatchNotification)result.AsyncState;
				bool flag = false;
				try
				{
					BatchManager<TItem>.PerformFlushAsyncResult.End(result);
					flag = true;
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteBatchManagerException(asyncState.trackingContext.Activity, asyncState.trackingContext.TrackingId, asyncState.trackingContext.SystemTracker, "BatchManager<T>.BatchNotification.OnFlushCompleted", exception.ToStringSlim()));
					TransactionResultManager.Instance.SetTransactionResult(asyncState.transactionIdentifier, exception, asyncState.trackingContext);
					try
					{
						asyncState.preparingEnlistment.ForceRollback(exception);
					}
					catch (InvalidOperationException invalidOperationException1)
					{
						InvalidOperationException invalidOperationException = invalidOperationException1;
						MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteBatchManagerException(asyncState.trackingContext.Activity, asyncState.trackingContext.TrackingId, asyncState.trackingContext.SystemTracker, "BatchManager<T>.BatchNotification.OnFlushCompleted.ForceRollback", invalidOperationException.ToStringSlim()));
					}
				}
				lock (asyncState.batchManager.syncTransactionLock)
				{
					asyncState.batchManager.transactionalBatchResults.Remove(asyncState.transactionIdentifier);
				}
				if (asyncState.batchManager.OnTransactionStateChanged != null)
				{
					asyncState.batchManager.OnTransactionStateChanged(asyncState.trackingContext, asyncState.transactionIdentifier, (flag ? TransactionCommitStatus.Committed : TransactionCommitStatus.Aborted));
				}
				if (flag)
				{
					asyncState.preparingEnlistment.Prepared();
				}
			}

			void System.Transactions.IEnlistmentNotification.Commit(Enlistment enlistment)
			{
				enlistment.Done();
			}

			void System.Transactions.IEnlistmentNotification.InDoubt(Enlistment enlistment)
			{
				MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteBatchManagerTransactionInDoubt(this.trackingContext.Activity, this.trackingContext.TrackingId, this.trackingContext.SystemTracker, this.transactionIdentifier, false));
				if (this.batchManager.OnTransactionStateChanged != null)
				{
					this.batchManager.OnTransactionStateChanged(this.trackingContext, this.transactionIdentifier, 3);
				}
				enlistment.Done();
			}

			void System.Transactions.IEnlistmentNotification.Prepare(PreparingEnlistment enlistment)
			{
				List<BatchManager<TItem>.BatchedObjectsAsyncResult> list;
				lock (this.batchManager.syncTransactionLock)
				{
					list = this.batchManager.transactionalBatchResults[this.transactionIdentifier].ToList<BatchManager<TItem>.BatchedObjectsAsyncResult>();
				}
				TimeSpan maxValue = TimeSpan.MaxValue;
				foreach (BatchManager<TItem>.BatchedObjectsAsyncResult batchedObjectsAsyncResult in list)
				{
					if (batchedObjectsAsyncResult.RemainingTime >= maxValue)
					{
						continue;
					}
					maxValue = batchedObjectsAsyncResult.RemainingTime;
				}
				this.preparingEnlistment = enlistment;
				this.prepareCalled = true;
				try
				{
					BatchManager<TItem>.PerformFlushAsyncResult performFlushAsyncResult = new BatchManager<TItem>.PerformFlushAsyncResult(this.trackingContext, this.batchManager, list, this.transactionIdentifier, maxValue, BatchManager<TItem>.BatchNotification.onFlushCompleted, this);
					performFlushAsyncResult.StartOperation();
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteBatchManagerException(this.trackingContext.Activity, this.trackingContext.TrackingId, this.trackingContext.SystemTracker, string.Format(CultureInfo.InvariantCulture, "BatchManager<T>.BatchNotification.Prepare Rollback. Transaction ID: {0}", new object[] { this.transactionIdentifier }), exception.ToStringSlim()));
					TransactionResultManager.Instance.SetTransactionResult(this.transactionIdentifier, exception, this.trackingContext);
					try
					{
						this.preparingEnlistment.ForceRollback(exception);
					}
					catch (InvalidOperationException invalidOperationException1)
					{
						InvalidOperationException invalidOperationException = invalidOperationException1;
						MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteBatchManagerException(this.trackingContext.Activity, this.trackingContext.TrackingId, this.trackingContext.SystemTracker, "BatchManager<T>.BatchNotification.Prepare.ForceRollback", invalidOperationException.ToStringSlim()));
					}
				}
			}

			void System.Transactions.IEnlistmentNotification.Rollback(Enlistment enlistment)
			{
				lock (this.batchManager.syncTransactionLock)
				{
					this.batchManager.transactionalBatchResults.Remove(this.transactionIdentifier);
				}
				if (this.batchManager.OnTransactionStateChanged != null && this.prepareCalled)
				{
					MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteBatchManagerTransactionInDoubt(this.trackingContext.Activity, this.trackingContext.TrackingId, this.trackingContext.SystemTracker, this.transactionIdentifier, true));
					this.batchManager.OnTransactionStateChanged(this.trackingContext, this.transactionIdentifier, 3);
				}
				enlistment.Done();
			}
		}

		private sealed class PerformFlushAsyncResult : AsyncResult
		{
			private static AsyncCallback onFlushCompletionCallback;

			private static AsyncCallback singleOperationCompleteCallback;

			private readonly BatchManager<TItem>.StartBatchOperationDelegate startBatchOperation;

			private readonly TrackingContext trackingContext;

			private readonly IList<BatchManager<TItem>.BatchedObjectsAsyncResult> batchedResults;

			private readonly TimeSpan timeout;

			private readonly bool isInTransaction;

			private readonly string transactionId;

			private int batchedResultsCount;

			private Exception completionException;

			public BatchManager<TItem> BatchManager
			{
				get;
				private set;
			}

			public TrackingContext TrackingContext
			{
				get
				{
					return this.trackingContext;
				}
			}

			static PerformFlushAsyncResult()
			{
				BatchManager<TItem>.PerformFlushAsyncResult.onFlushCompletionCallback = new AsyncCallback(BatchManager<TItem>.PerformFlushAsyncResult.OnFlushCompletedCallback);
				BatchManager<TItem>.PerformFlushAsyncResult.singleOperationCompleteCallback = new AsyncCallback(BatchManager<TItem>.PerformFlushAsyncResult.OnSingleOperationCompletedCallback);
			}

			public PerformFlushAsyncResult(TrackingContext trackingContext, BatchManager<TItem> batchManager, List<BatchManager<TItem>.BatchedObjectsAsyncResult> batchedResults, string transactionId, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				this.trackingContext = trackingContext;
				this.BatchManager = batchManager;
				this.startBatchOperation = this.BatchManager.StartBatchOperation;
				this.batchedResults = batchedResults;
				this.timeout = timeout;
				this.batchedResultsCount = 0;
				this.transactionId = transactionId;
				this.isInTransaction = transactionId != null;
			}

			private void CompleteSingleOperation(BatchManager<TItem>.BatchedObjectsAsyncResult singleOperation, Exception delayedException)
			{
				if (delayedException != null)
				{
					this.completionException = delayedException;
					MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteBatchManagerException(this.TrackingContext.Activity, this.TrackingContext.TrackingId, this.TrackingContext.SystemTracker, "BatchManager.PerformFlushAsyncResult.CompleteSingleOperation", delayedException.ToStringSlim()));
				}
				IOThreadScheduler.ScheduleCallbackNoFlow((object s) => singleOperation.CompleteBatch(delayedException), null);
				if (Interlocked.Decrement(ref this.batchedResultsCount) == 0)
				{
					base.Complete(false, this.completionException);
				}
			}

			private bool ContinueOperation(Exception delayedException, IList<BatchManager<TItem>.BatchedObjectsAsyncResult> batchedObjectsAsyncResults)
			{
				if (delayedException != null)
				{
					MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteBatchManagerException(this.TrackingContext.Activity, this.TrackingContext.TrackingId, this.TrackingContext.SystemTracker, "BatchManager.PerformFlushAsyncResult.ContinueOperation", delayedException.ToStringSlim()));
				}
				if (this.isInTransaction || delayedException == null || delayedException is CommunicationObjectFaultedException)
				{
					ExceptionInfo exceptionInfo = new ExceptionInfo(delayedException);
					foreach (BatchManager<TItem>.BatchedObjectsAsyncResult batchedObjectsAsyncResult in batchedObjectsAsyncResults)
					{
						IOThreadScheduler.ScheduleCallbackNoFlow((object s) => ((BatchManager<TItem>.BatchedObjectsAsyncResult)s).CompleteBatch(exceptionInfo.CreateException()), batchedObjectsAsyncResult);
					}
					return false;
				}
				int count = batchedObjectsAsyncResults.Count;
				bool flag = count > 1;
				Interlocked.Add(ref this.batchedResultsCount, count);
				foreach (BatchManager<TItem>.BatchedObjectsAsyncResult batchedObjectsAsyncResult1 in batchedObjectsAsyncResults)
				{
					if (!this.ShouldRetry(delayedException, batchedObjectsAsyncResult1, flag))
					{
						this.CompleteSingleOperation(batchedObjectsAsyncResult1, delayedException);
					}
					else
					{
						try
						{
							IAsyncResult batchedBegin = this.BatchManager.BatchedBegin(this.TrackingContext, batchedObjectsAsyncResult1.BatchedObjects, this.transactionId, this.timeout, BatchManager<TItem>.PerformFlushAsyncResult.singleOperationCompleteCallback, new Tuple<BatchManager<TItem>.PerformFlushAsyncResult, BatchManager<TItem>.BatchedObjectsAsyncResult>(this, batchedObjectsAsyncResult1));
							if (batchedBegin.CompletedSynchronously)
							{
								this.OnSingleOperationCompleted(batchedBegin);
							}
						}
						catch (Exception exception1)
						{
							Exception exception = exception1;
							if (Fx.IsFatal(exception))
							{
								throw;
							}
							this.CompleteSingleOperation(batchedObjectsAsyncResult1, exception);
						}
					}
				}
				return true;
			}

			public static new void End(IAsyncResult result)
			{
				AsyncResult.End<BatchManager<TItem>.PerformFlushAsyncResult>(result);
			}

			private static ICollection<IList<BatchManager<TItem>.BatchedObjectsAsyncResult>> GroupBy(IList<BatchManager<TItem>.BatchedObjectsAsyncResult> batchedObjectsAsyncResults, GroupByKeySelectorDelegate<TItem> selector)
			{
				IList<BatchManager<TItem>.BatchedObjectsAsyncResult> batchedObjectsAsyncResults1;
				Dictionary<IComparable, IList<BatchManager<TItem>.BatchedObjectsAsyncResult>> comparables = new Dictionary<IComparable, IList<BatchManager<TItem>.BatchedObjectsAsyncResult>>();
				foreach (BatchManager<TItem>.BatchedObjectsAsyncResult batchedObjectsAsyncResult in batchedObjectsAsyncResults)
				{
					object obj = selector(batchedObjectsAsyncResult.BatchedObjects);
					if (obj == null)
					{
						obj = BatchManager<TItem>.nullKey;
					}
					IComparable comparable = (IComparable)obj;
					if (!comparables.TryGetValue(comparable, out batchedObjectsAsyncResults1))
					{
						batchedObjectsAsyncResults1 = new List<BatchManager<TItem>.BatchedObjectsAsyncResult>();
						comparables.Add(comparable, batchedObjectsAsyncResults1);
					}
					batchedObjectsAsyncResults1.Add(batchedObjectsAsyncResult);
				}
				return comparables.Values;
			}

			private void OnFlushCompleted(IAsyncResult result, IList<BatchManager<TItem>.BatchedObjectsAsyncResult> batchedObjectsAsyncResults)
			{
				Exception exception = null;
				try
				{
					this.BatchManager.BatchedEnd(result, this.isInTransaction);
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
				this.TryContinueOperation(result.CompletedSynchronously, exception, batchedObjectsAsyncResults);
			}

			private static void OnFlushCompletedCallback(IAsyncResult result)
			{
				if (!result.CompletedSynchronously)
				{
					Tuple<BatchManager<TItem>.PerformFlushAsyncResult, IList<BatchManager<TItem>.BatchedObjectsAsyncResult>> asyncState = (Tuple<BatchManager<TItem>.PerformFlushAsyncResult, IList<BatchManager<TItem>.BatchedObjectsAsyncResult>>)result.AsyncState;
					asyncState.Item1.OnFlushCompleted(result, asyncState.Item2);
				}
			}

			private void OnSingleOperationCompleted(IAsyncResult result)
			{
				Exception exception = null;
				BatchManager<TItem>.BatchedObjectsAsyncResult item2 = ((Tuple<BatchManager<TItem>.PerformFlushAsyncResult, BatchManager<TItem>.BatchedObjectsAsyncResult>)result.AsyncState).Item2;
				try
				{
					this.BatchManager.BatchedEnd(result, true);
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
				this.CompleteSingleOperation(item2, exception);
			}

			private static void OnSingleOperationCompletedCallback(IAsyncResult result)
			{
				if (!result.CompletedSynchronously)
				{
					((Tuple<BatchManager<TItem>.PerformFlushAsyncResult, BatchManager<TItem>.BatchedObjectsAsyncResult>)result.AsyncState).Item1.OnSingleOperationCompleted(result);
				}
			}

			private bool ShouldRetry(Exception delayedException, BatchManager<TItem>.BatchedObjectsAsyncResult singleOperation, bool isMultiCommandBatch)
			{
				bool onRetry = this.BatchManager.OnRetry == null;
				if (!onRetry)
				{
					try
					{
						onRetry = this.BatchManager.OnRetry(singleOperation.BatchedObjects, delayedException, isMultiCommandBatch);
					}
					catch (Exception exception1)
					{
						Exception exception = exception1;
						if (Fx.IsFatal(exception))
						{
							throw;
						}
						MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteHandledExceptionWarning(exception.ToStringSlim()));
						onRetry = false;
					}
				}
				return onRetry;
			}

			public static void StartBatchOperation(BatchManager<TItem>.PerformFlushAsyncResult thisPtr, IList<BatchManager<TItem>.BatchedObjectsAsyncResult> batchedObjectsAsyncResults)
			{
				List<TItem> tItems = new List<TItem>();
				foreach (BatchManager<TItem>.BatchedObjectsAsyncResult batchedObjectsAsyncResult in batchedObjectsAsyncResults)
				{
					TrackingContext trackingContext = batchedObjectsAsyncResult.TrackingContext;
					MessagingClientEtwProvider.TraceClient(() => {
					});
					tItems.AddRange(batchedObjectsAsyncResult.BatchedObjects);
				}
				try
				{
					IAsyncResult batchedBegin = thisPtr.BatchManager.BatchedBegin(thisPtr.TrackingContext, tItems, thisPtr.transactionId, thisPtr.timeout, BatchManager<TItem>.PerformFlushAsyncResult.onFlushCompletionCallback, new Tuple<BatchManager<TItem>.PerformFlushAsyncResult, IList<BatchManager<TItem>.BatchedObjectsAsyncResult>>(thisPtr, batchedObjectsAsyncResults));
					if (batchedBegin.CompletedSynchronously)
					{
						thisPtr.OnFlushCompleted(batchedBegin, batchedObjectsAsyncResults);
					}
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					thisPtr.TryContinueOperation(true, exception, batchedObjectsAsyncResults);
				}
			}

			public static void StartGroupByBatchOperation(BatchManager<TItem>.PerformFlushAsyncResult thisPtr, IList<BatchManager<TItem>.BatchedObjectsAsyncResult> batchedObjectsAsyncResults)
			{
				ICollection<IList<BatchManager<TItem>.BatchedObjectsAsyncResult>> lists = BatchManager<TItem>.PerformFlushAsyncResult.GroupBy(batchedObjectsAsyncResults, thisPtr.BatchManager.GroupByKeySelector);
				Interlocked.Add(ref thisPtr.batchedResultsCount, lists.Count);
				foreach (IList<BatchManager<TItem>.BatchedObjectsAsyncResult> batchedObjectsAsyncResults1 in lists)
				{
					BatchManager<TItem>.PerformFlushAsyncResult.StartBatchOperation(thisPtr, batchedObjectsAsyncResults1);
				}
			}

			public void StartOperation()
			{
				Interlocked.Increment(ref this.batchedResultsCount);
				this.startBatchOperation(this, this.batchedResults);
			}

			private void TryContinueOperation(bool completedSynchronously, Exception delayedException, IList<BatchManager<TItem>.BatchedObjectsAsyncResult> batchedObjectsAsyncResults)
			{
				this.ContinueOperation(delayedException, batchedObjectsAsyncResults);
				if (Interlocked.Decrement(ref this.batchedResultsCount) == 0)
				{
					base.Complete(completedSynchronously, delayedException);
				}
			}
		}

		private delegate void StartBatchOperationDelegate(BatchManager<TItem>.PerformFlushAsyncResult thisPtr, IList<BatchManager<TItem>.BatchedObjectsAsyncResult> batchedObjectsAsyncResults);
	}
}