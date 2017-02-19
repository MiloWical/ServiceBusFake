using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Common.Parallel;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace Microsoft.ServiceBus.Messaging
{
	[DebuggerStepThrough]
	internal abstract class IteratorAsyncResult<TIteratorAsyncResult> : AsyncResult<TIteratorAsyncResult>
	where TIteratorAsyncResult : IteratorAsyncResult<TIteratorAsyncResult>
	{
		private readonly static Action<AsyncResult, Exception> onFinally;

		private static AsyncResult.AsyncCompletion stepCallbackDelegate;

		private Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper;

		private volatile bool everCompletedAsynchronously;

		private IEnumerator<IteratorAsyncResult<TIteratorAsyncResult>.AsyncStep> steps;

		private Exception lastAsyncStepException;

		protected Exception LastAsyncStepException
		{
			get
			{
				return this.lastAsyncStepException;
			}
			set
			{
				this.lastAsyncStepException = value;
			}
		}

		public TimeSpan OriginalTimeout
		{
			get
			{
				return this.timeoutHelper.OriginalTimeout;
			}
		}

		private static AsyncResult.AsyncCompletion StepCallbackDelegate
		{
			get
			{
				if (IteratorAsyncResult<TIteratorAsyncResult>.stepCallbackDelegate == null)
				{
					IteratorAsyncResult<TIteratorAsyncResult>.stepCallbackDelegate = new AsyncResult.AsyncCompletion(IteratorAsyncResult<TIteratorAsyncResult>.StepCallback);
				}
				return IteratorAsyncResult<TIteratorAsyncResult>.stepCallbackDelegate;
			}
		}

		static IteratorAsyncResult()
		{
			IteratorAsyncResult<TIteratorAsyncResult>.onFinally = new Action<AsyncResult, Exception>(IteratorAsyncResult<TIteratorAsyncResult>.Finally);
		}

		protected IteratorAsyncResult(TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
		{
			this.timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout, true);
			IteratorAsyncResult<TIteratorAsyncResult> iteratorAsyncResult = this;
			iteratorAsyncResult.OnCompleting = (Action<AsyncResult, Exception>)Delegate.Combine(iteratorAsyncResult.OnCompleting, IteratorAsyncResult<TIteratorAsyncResult>.onFinally);
		}

		protected IteratorAsyncResult<TIteratorAsyncResult>.AsyncStep CallAsync(IteratorAsyncResult<TIteratorAsyncResult>.BeginCall beginCall, IteratorAsyncResult<TIteratorAsyncResult>.EndCall endCall, IteratorAsyncResult<TIteratorAsyncResult>.Call call, IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy policy)
		{
			return new IteratorAsyncResult<TIteratorAsyncResult>.AsyncStep(null, beginCall, endCall, call, policy);
		}

		protected IteratorAsyncResult<TIteratorAsyncResult>.AsyncStep CallAsync(IteratorAsyncResult<TIteratorAsyncResult>.BeginCall beginCall, IteratorAsyncResult<TIteratorAsyncResult>.EndCall endCall, IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy policy)
		{
			return new IteratorAsyncResult<TIteratorAsyncResult>.AsyncStep(null, beginCall, endCall, null, policy);
		}

		protected IteratorAsyncResult<TIteratorAsyncResult>.AsyncStep CallAsyncSleep(TimeSpan amountToSleep)
		{
			return this.CallAsyncSleep(amountToSleep, CancellationToken.None);
		}

		protected IteratorAsyncResult<TIteratorAsyncResult>.AsyncStep CallAsyncSleep(TimeSpan amountToSleep, CancellationToken cancellationToken)
		{
			return this.CallAsync((TIteratorAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => new IteratorAsyncResult<TIteratorAsyncResult>.SleepAsyncResult(amountToSleep, cancellationToken, c, s), (TIteratorAsyncResult thisPtr, IAsyncResult r) => IteratorAsyncResult<TIteratorAsyncResult>.SleepAsyncResult.End(r), (TIteratorAsyncResult thisPtr, TimeSpan t) => Thread.Sleep(amountToSleep), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
		}

		protected IteratorAsyncResult<TIteratorAsyncResult>.AsyncStep CallParallelAsync<TWorkItem>(ICollection<TWorkItem> workItems, IteratorAsyncResult<TIteratorAsyncResult>.BeginCall<TWorkItem> beginCall, IteratorAsyncResult<TIteratorAsyncResult>.EndCall<TWorkItem> endCall, IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy policy)
		{
			return this.CallAsync((TIteratorAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => new IteratorAsyncResult<TIteratorAsyncResult>.ParallelAsyncResult<TWorkItem>(thisPtr, workItems, beginCall, endCall, t, c, s), (TIteratorAsyncResult thisPtr, IAsyncResult r) => AsyncResult<IteratorAsyncResult<TIteratorAsyncResult>.ParallelAsyncResult<TWorkItem>>.End(r), policy);
		}

		protected IteratorAsyncResult<TIteratorAsyncResult>.AsyncStep CallParallelAsync<TWorkItem>(ICollection<TWorkItem> workItems, IteratorAsyncResult<TIteratorAsyncResult>.BeginCall<TWorkItem> beginCall, IteratorAsyncResult<TIteratorAsyncResult>.EndCall<TWorkItem> endCall, TimeSpan timeout, IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy policy)
		{
			return this.CallAsync((TIteratorAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => new IteratorAsyncResult<TIteratorAsyncResult>.ParallelAsyncResult<TWorkItem>(thisPtr, workItems, beginCall, endCall, timeout, c, s), (TIteratorAsyncResult thisPtr, IAsyncResult r) => AsyncResult<IteratorAsyncResult<TIteratorAsyncResult>.ParallelAsyncResult<TWorkItem>>.End(r), policy);
		}

		protected IteratorAsyncResult<TIteratorAsyncResult>.AsyncStep CallTask(Func<TIteratorAsyncResult, TimeSpan, Task> taskFunc, IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy policy)
		{
			return this.CallAsync((TIteratorAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => {
				Task task = taskFunc(thisPtr, t);
				if (task.Status == TaskStatus.Created)
				{
					task.Start();
				}
				return task.ToAsyncResult(c, s);
			}, (TIteratorAsyncResult thisPtr, IAsyncResult r) => TaskHelpers.EndAsyncResult(r), policy);
		}

		protected IteratorAsyncResult<TIteratorAsyncResult>.AsyncStep CallTransactionalAsync(Transaction transaction, IteratorAsyncResult<TIteratorAsyncResult>.BeginCall beginCall, IteratorAsyncResult<TIteratorAsyncResult>.EndCall endCall, IteratorAsyncResult<TIteratorAsyncResult>.Call call, IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy policy)
		{
			return new IteratorAsyncResult<TIteratorAsyncResult>.AsyncStep(transaction, beginCall, endCall, call, policy);
		}

		protected IteratorAsyncResult<TIteratorAsyncResult>.AsyncStep CallTransactionalAsync(Transaction transaction, IteratorAsyncResult<TIteratorAsyncResult>.BeginCall beginCall, IteratorAsyncResult<TIteratorAsyncResult>.EndCall endCall, IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy policy)
		{
			return new IteratorAsyncResult<TIteratorAsyncResult>.AsyncStep(transaction, beginCall, endCall, null, policy);
		}

		protected void Complete(Exception operationException)
		{
			base.Complete(!this.everCompletedAsynchronously, operationException);
		}

		private void EnumerateSteps(IteratorAsyncResult<TIteratorAsyncResult>.CurrentThreadType state)
		{
			while (!base.IsCompleted && this.MoveNextStep())
			{
				this.LastAsyncStepException = null;
				IteratorAsyncResult<TIteratorAsyncResult>.AsyncStep current = this.steps.Current;
				if (current.BeginCall == null)
				{
					continue;
				}
				IAsyncResult beginCall = null;
				if (state == IteratorAsyncResult<TIteratorAsyncResult>.CurrentThreadType.Synchronous && current.HasSynchronous)
				{
					if (current.Policy != IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer)
					{
						try
						{
							using (IDisposable disposable = base.PrepareTransactionalCall(current.Transaction))
							{
								current.Call((TIteratorAsyncResult)this, this.timeoutHelper.RemainingTime());
							}
						}
						catch (Exception exception1)
						{
							Exception exception = exception1;
							if (Fx.IsFatal(exception) || !this.HandleException(exception))
							{
								throw;
							}
						}
					}
					else
					{
						using (IDisposable disposable1 = base.PrepareTransactionalCall(current.Transaction))
						{
							current.Call((TIteratorAsyncResult)this, this.timeoutHelper.RemainingTime());
						}
					}
				}
				else if (current.Policy != IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer)
				{
					try
					{
						using (IDisposable disposable2 = base.PrepareTransactionalCall(current.Transaction))
						{
							beginCall = current.BeginCall((TIteratorAsyncResult)this, this.timeoutHelper.RemainingTime(), base.PrepareAsyncCompletion(IteratorAsyncResult<TIteratorAsyncResult>.StepCallbackDelegate), this);
						}
					}
					catch (Exception exception3)
					{
						Exception exception2 = exception3;
						if (Fx.IsFatal(exception2) || !this.HandleException(exception2))
						{
							throw;
						}
					}
				}
				else
				{
					using (IDisposable disposable3 = base.PrepareTransactionalCall(current.Transaction))
					{
						beginCall = current.BeginCall((TIteratorAsyncResult)this, this.timeoutHelper.RemainingTime(), base.PrepareAsyncCompletion(IteratorAsyncResult<TIteratorAsyncResult>.StepCallbackDelegate), this);
					}
				}
				if (beginCall == null)
				{
					continue;
				}
				if (!base.CheckSyncContinue(beginCall))
				{
					return;
				}
				try
				{
					this.steps.Current.EndCall((TIteratorAsyncResult)this, beginCall);
				}
				catch (Exception exception5)
				{
					Exception exception4 = exception5;
					if (Fx.IsFatal(exception4) || !this.HandleException(exception4))
					{
						throw;
					}
				}
			}
			if (!base.IsCompleted)
			{
				base.Complete(!this.everCompletedAsynchronously);
			}
		}

		private static void Finally(AsyncResult result, Exception exception)
		{
			IteratorAsyncResult<TIteratorAsyncResult> iteratorAsyncResult = (IteratorAsyncResult<TIteratorAsyncResult>)result;
			try
			{
				IEnumerator<IteratorAsyncResult<TIteratorAsyncResult>.AsyncStep> enumerator = iteratorAsyncResult.steps;
				if (enumerator != null)
				{
					enumerator.Dispose();
				}
			}
			catch (Exception exception2)
			{
				Exception exception1 = exception2;
				if (Fx.IsFatal(exception1))
				{
					throw;
				}
				MessagingClientEtwProvider.Provider.EventWriteExceptionAsWarning(exception1.ToStringSlim());
				if (exception == null)
				{
					throw;
				}
			}
		}

		protected abstract IEnumerator<IteratorAsyncResult<TIteratorAsyncResult>.AsyncStep> GetAsyncSteps();

		private bool HandleException(Exception e)
		{
			bool flag;
			this.LastAsyncStepException = e;
			switch (this.steps.Current.Policy)
			{
				case (IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy)IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer:
				{
					flag = false;
					if (base.IsCompleted)
					{
						break;
					}
					this.Complete(e);
					flag = true;
					break;
				}
				case (IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy)IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue:
				{
					flag = true;
					break;
				}
				default:
				{
					flag = false;
					break;
				}
			}
			return flag;
		}

		private bool MoveNextStep()
		{
			return this.steps.MoveNext();
		}

		protected TimeSpan RemainingTime()
		{
			return this.timeoutHelper.RemainingTime();
		}

		public TIteratorAsyncResult RunSynchronously()
		{
			try
			{
				this.steps = this.GetAsyncSteps();
				this.EnumerateSteps(IteratorAsyncResult<TIteratorAsyncResult>.CurrentThreadType.Synchronous);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				this.Complete(exception);
			}
			return AsyncResult.End<TIteratorAsyncResult>(this);
		}

		public IAsyncResult Start()
		{
			try
			{
				this.steps = this.GetAsyncSteps();
				this.EnumerateSteps(IteratorAsyncResult<TIteratorAsyncResult>.CurrentThreadType.StartingThread);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				this.Complete(exception);
			}
			return this;
		}

		private static bool StepCallback(IAsyncResult result)
		{
			IteratorAsyncResult<TIteratorAsyncResult> asyncState = (IteratorAsyncResult<TIteratorAsyncResult>)result.AsyncState;
			bool flag = asyncState.CheckSyncContinue(result);
			if (!flag)
			{
				asyncState.everCompletedAsynchronously = true;
				try
				{
					asyncState.steps.Current.EndCall((TIteratorAsyncResult)asyncState, result);
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception) || !asyncState.HandleException(exception))
					{
						throw;
					}
				}
				asyncState.EnumerateSteps(IteratorAsyncResult<TIteratorAsyncResult>.CurrentThreadType.Callback);
			}
			return flag;
		}

		[DebuggerStepThrough]
		protected struct AsyncStep
		{
			private readonly IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy policy;

			private readonly IteratorAsyncResult<TIteratorAsyncResult>.BeginCall beginCall;

			private readonly IteratorAsyncResult<TIteratorAsyncResult>.EndCall endCall;

			private readonly IteratorAsyncResult<TIteratorAsyncResult>.Call call;

			private readonly Transaction transaction;

			public readonly static IteratorAsyncResult<TIteratorAsyncResult>.AsyncStep Empty;

			public IteratorAsyncResult<TIteratorAsyncResult>.BeginCall BeginCall
			{
				get
				{
					return this.beginCall;
				}
			}

			public IteratorAsyncResult<TIteratorAsyncResult>.Call Call
			{
				get
				{
					return this.call;
				}
			}

			public IteratorAsyncResult<TIteratorAsyncResult>.EndCall EndCall
			{
				get
				{
					return this.endCall;
				}
			}

			public bool HasSynchronous
			{
				get
				{
					return this.call != null;
				}
			}

			public IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy Policy
			{
				get
				{
					return (IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy)this.policy;
				}
			}

			public Transaction Transaction
			{
				get
				{
					return this.transaction;
				}
			}

			static AsyncStep()
			{
				IteratorAsyncResult<TIteratorAsyncResult>.AsyncStep.Empty = new IteratorAsyncResult<TIteratorAsyncResult>.AsyncStep();
			}

			public AsyncStep(Transaction transaction, IteratorAsyncResult<TIteratorAsyncResult>.BeginCall beginCall, IteratorAsyncResult<TIteratorAsyncResult>.EndCall endCall, IteratorAsyncResult<TIteratorAsyncResult>.Call call, IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy policy)
			{
				this.transaction = transaction;
				this.policy = policy;
				this.beginCall = beginCall;
				this.endCall = endCall;
				this.call = call;
			}
		}

		protected delegate IAsyncResult BeginCall(TIteratorAsyncResult thisPtr, TimeSpan timeout, AsyncCallback callback, object state);

		protected delegate IAsyncResult BeginCall<TWorkItem>(TIteratorAsyncResult thisPtr, TWorkItem workItem, TimeSpan timeout, AsyncCallback callback, object state);

		protected delegate void Call(TIteratorAsyncResult thisPtr, TimeSpan timeout);

		private enum CurrentThreadType
		{
			Synchronous,
			StartingThread,
			Callback
		}

		protected delegate void EndCall(TIteratorAsyncResult thisPtr, IAsyncResult ar);

		protected delegate void EndCall<TWorkItem>(TIteratorAsyncResult thisPtr, TWorkItem workItem, IAsyncResult ar);

		protected enum ExceptionPolicy
		{
			Transfer,
			Continue
		}

		private sealed class ParallelAsyncResult<TWorkItem> : AsyncResult<IteratorAsyncResult<TIteratorAsyncResult>.ParallelAsyncResult<TWorkItem>>
		{
			private static AsyncCallback completed;

			private readonly TIteratorAsyncResult iteratorAsyncResult;

			private readonly ICollection<TWorkItem> workItems;

			private readonly IteratorAsyncResult<TIteratorAsyncResult>.EndCall<TWorkItem> endCall;

			private long actions;

			private Exception firstException;

			static ParallelAsyncResult()
			{
				IteratorAsyncResult<TIteratorAsyncResult>.ParallelAsyncResult<TWorkItem>.completed = new AsyncCallback(IteratorAsyncResult<TIteratorAsyncResult>.ParallelAsyncResult<TWorkItem>.OnCompleted);
			}

			public ParallelAsyncResult(TIteratorAsyncResult iteratorAsyncResult, ICollection<TWorkItem> workItems, IteratorAsyncResult<TIteratorAsyncResult>.BeginCall<TWorkItem> beginCall, IteratorAsyncResult<TIteratorAsyncResult>.EndCall<TWorkItem> endCall, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				this.iteratorAsyncResult = iteratorAsyncResult;
				this.workItems = workItems;
				this.endCall = endCall;
				this.actions = (long)(this.workItems.Count + 1);
				foreach (TWorkItem workItem in workItems)
				{
					try
					{
						beginCall(iteratorAsyncResult, workItem, timeout, IteratorAsyncResult<TIteratorAsyncResult>.ParallelAsyncResult<TWorkItem>.completed, new IteratorAsyncResult<TIteratorAsyncResult>.ParallelAsyncResult<TWorkItem>.CallbackState(this, workItem));
					}
					catch (Exception exception1)
					{
						Exception exception = exception1;
						if (Fx.IsFatal(exception))
						{
							throw;
						}
						this.TryComplete(exception, true);
					}
				}
				this.TryComplete(null, true);
			}

			private static void OnCompleted(IAsyncResult ar)
			{
				IteratorAsyncResult<TIteratorAsyncResult>.ParallelAsyncResult<TWorkItem>.CallbackState asyncState = (IteratorAsyncResult<TIteratorAsyncResult>.ParallelAsyncResult<TWorkItem>.CallbackState)ar.AsyncState;
				IteratorAsyncResult<TIteratorAsyncResult>.ParallelAsyncResult<TWorkItem> asyncResult = asyncState.AsyncResult;
				try
				{
					asyncResult.endCall(asyncResult.iteratorAsyncResult, asyncState.AsyncData, ar);
					asyncResult.TryComplete(null, ar.CompletedSynchronously);
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					asyncResult.TryComplete(exception, ar.CompletedSynchronously);
				}
			}

			private void TryComplete(Exception exception, bool completedSynchronously)
			{
				if (this.firstException == null)
				{
					this.firstException = exception;
				}
				if (Interlocked.Decrement(ref this.actions) == (long)0)
				{
					base.Complete(completedSynchronously, this.firstException);
				}
			}

			private sealed class CallbackState
			{
				public TWorkItem AsyncData
				{
					get;
					private set;
				}

				public IteratorAsyncResult<TIteratorAsyncResult>.ParallelAsyncResult<TWorkItem> AsyncResult
				{
					get;
					private set;
				}

				public CallbackState(IteratorAsyncResult<TIteratorAsyncResult>.ParallelAsyncResult<TWorkItem> asyncResult, TWorkItem data)
				{
					this.AsyncResult = asyncResult;
					this.AsyncData = data;
				}
			}
		}

		private sealed class SleepAsyncResult : AsyncResult<IteratorAsyncResult<TIteratorAsyncResult>.SleepAsyncResult>
		{
			private readonly static Action<object> onTimer;

			private readonly static Action<object> StaticOnCancellation;

			private readonly IOThreadTimer timer;

			private CancellationTokenRegistration cancellationTokenRegistration;

			static SleepAsyncResult()
			{
				IteratorAsyncResult<TIteratorAsyncResult>.SleepAsyncResult.onTimer = new Action<object>(IteratorAsyncResult<TIteratorAsyncResult>.SleepAsyncResult.OnTimer);
				IteratorAsyncResult<TIteratorAsyncResult>.SleepAsyncResult.StaticOnCancellation = new Action<object>(IteratorAsyncResult<TIteratorAsyncResult>.SleepAsyncResult.OnCancellation);
			}

			public SleepAsyncResult(TimeSpan amount, CancellationToken cancellationToken, AsyncCallback callback, object state) : base(callback, state)
			{
				this.timer = new IOThreadTimer(IteratorAsyncResult<TIteratorAsyncResult>.SleepAsyncResult.onTimer, this, false);
				this.timer.Set(amount);
				try
				{
					this.cancellationTokenRegistration = cancellationToken.Register(IteratorAsyncResult<TIteratorAsyncResult>.SleepAsyncResult.StaticOnCancellation, this);
				}
				catch (ObjectDisposedException objectDisposedException)
				{
					this.HandleCancellation(false);
				}
			}

			public static new void End(IAsyncResult result)
			{
				IteratorAsyncResult<TIteratorAsyncResult>.SleepAsyncResult sleepAsyncResult = AsyncResult<IteratorAsyncResult<TIteratorAsyncResult>.SleepAsyncResult>.End(result);
				try
				{
					sleepAsyncResult.cancellationTokenRegistration.Dispose();
				}
				catch (ObjectDisposedException objectDisposedException)
				{
				}
			}

			private void HandleCancellation(bool scheduleComplete)
			{
				if (this.timer.Cancel())
				{
					if (scheduleComplete)
					{
						IOThreadScheduler.ScheduleCallbackNoFlow((object s) => ((IteratorAsyncResult<TIteratorAsyncResult>.SleepAsyncResult)s).Complete(false), this);
						return;
					}
					base.Complete(true);
				}
			}

			private static void OnCancellation(object state)
			{
				((IteratorAsyncResult<TIteratorAsyncResult>.SleepAsyncResult)state).HandleCancellation(true);
			}

			private static void OnTimer(object state)
			{
				((IteratorAsyncResult<TIteratorAsyncResult>.SleepAsyncResult)state).Complete(false);
			}
		}
	}
}