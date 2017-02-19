using Microsoft.ServiceBus.Tracing;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Transactions;

namespace Microsoft.ServiceBus.Common
{
	[DebuggerStepThrough]
	internal abstract class AsyncResult : IAsyncResult
	{
		public const string DisablePrepareForRethrow = "DisablePrepareForRethrow";

		private static AsyncCallback asyncCompletionWrapperCallback;

		private AsyncCallback callback;

		private bool completedSynchronously;

		private bool endCalled;

		private Exception exception;

		private bool isCompleted;

		private AsyncResult.AsyncCompletion nextAsyncCompletion;

		private IAsyncResult deferredTransactionalResult;

		private AsyncResult.TransactionSignalScope transactionContext;

		private object state;

		private ManualResetEvent manualResetEvent;

		private object thisLock;

		protected internal virtual EventTraceActivity Activity
		{
			get
			{
				return null;
			}
		}

		public object AsyncState
		{
			get
			{
				return this.state;
			}
		}

		public WaitHandle AsyncWaitHandle
		{
			get
			{
				if (this.manualResetEvent != null)
				{
					return this.manualResetEvent;
				}
				lock (this.ThisLock)
				{
					if (this.manualResetEvent == null)
					{
						this.manualResetEvent = new ManualResetEvent(this.isCompleted);
					}
				}
				return this.manualResetEvent;
			}
		}

		public bool CompletedSynchronously
		{
			get
			{
				return this.completedSynchronously;
			}
		}

		public bool HasCallback
		{
			get
			{
				return this.callback != null;
			}
		}

		public bool IsCompleted
		{
			get
			{
				return this.isCompleted;
			}
		}

		protected Action<AsyncResult, Exception> OnCompleting
		{
			get;
			set;
		}

		protected object ThisLock
		{
			get
			{
				return this.thisLock;
			}
		}

		protected virtual System.Diagnostics.TraceEventType TraceEventType
		{
			get
			{
				return System.Diagnostics.TraceEventType.Verbose;
			}
		}

		protected Action<AsyncCallback, IAsyncResult> VirtualCallback
		{
			get;
			set;
		}

		protected AsyncResult(AsyncCallback callback, object state)
		{
			this.callback = callback;
			this.state = state;
			this.thisLock = new object();
		}

		private static void AsyncCompletionWrapperCallback(IAsyncResult result)
		{
			if (result == null)
			{
				throw Fx.Exception.AsError(new InvalidOperationException(SRCore.InvalidNullAsyncResult), null);
			}
			if (result.CompletedSynchronously)
			{
				return;
			}
			AsyncResult asyncState = (AsyncResult)result.AsyncState;
			if (asyncState.transactionContext != null && !asyncState.transactionContext.Signal(result))
			{
				return;
			}
			AsyncResult.AsyncCompletion nextCompletion = asyncState.GetNextCompletion();
			if (nextCompletion == null)
			{
				AsyncResult.ThrowInvalidAsyncResult(result);
			}
			bool flag = false;
			Exception exception = null;
			try
			{
				flag = nextCompletion(result);
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

		protected bool CheckSyncContinue(IAsyncResult result)
		{
			AsyncResult.AsyncCompletion asyncCompletion;
			return this.TryContinueHelper(result, out asyncCompletion);
		}

		protected void Complete(bool didCompleteSynchronously)
		{
			this.Complete(didCompleteSynchronously, null);
		}

		protected void Complete(bool didCompleteSynchronously, Exception e)
		{
			if (!this.TryComplete(didCompleteSynchronously, e))
			{
				throw Fx.Exception.AsError(new InvalidOperationException(SRCore.AsyncResultCompletedTwice(this.GetType())), null);
			}
		}

		protected static TAsyncResult End<TAsyncResult>(IAsyncResult result)
		where TAsyncResult : AsyncResult
		{
			if (result == null)
			{
				throw Fx.Exception.ArgumentNull("result");
			}
			TAsyncResult manualResetEvent = (TAsyncResult)(result as TAsyncResult);
			if (manualResetEvent == null)
			{
				throw Fx.Exception.Argument("result", SRCore.InvalidAsyncResult);
			}
			if (manualResetEvent.endCalled)
			{
				throw Fx.Exception.AsError(new InvalidOperationException(SRCore.AsyncResultAlreadyEnded), null);
			}
			manualResetEvent.endCalled = true;
			if (!manualResetEvent.isCompleted)
			{
				lock (manualResetEvent.ThisLock)
				{
					if (!manualResetEvent.isCompleted && manualResetEvent.manualResetEvent == null)
					{
						manualResetEvent.manualResetEvent = new ManualResetEvent(manualResetEvent.isCompleted);
					}
				}
			}
			if (manualResetEvent.manualResetEvent != null)
			{
				manualResetEvent.manualResetEvent.WaitOne();
				manualResetEvent.manualResetEvent.Close();
			}
			if (manualResetEvent.exception != null)
			{
				Fx.Exception.TraceException<Exception>(manualResetEvent.exception, manualResetEvent.TraceEventType, manualResetEvent.Activity);
				ExceptionDispatcher.Throw(manualResetEvent.exception);
			}
			return manualResetEvent;
		}

		private AsyncResult.AsyncCompletion GetNextCompletion()
		{
			AsyncResult.AsyncCompletion asyncCompletion = this.nextAsyncCompletion;
			this.transactionContext = null;
			this.nextAsyncCompletion = null;
			return asyncCompletion;
		}

		protected AsyncCallback PrepareAsyncCompletion(AsyncResult.AsyncCompletion callback)
		{
			if (this.transactionContext != null)
			{
				if (!this.transactionContext.IsPotentiallyAbandoned)
				{
					this.transactionContext.Prepared();
				}
				else
				{
					this.transactionContext = null;
				}
			}
			this.nextAsyncCompletion = callback;
			if (AsyncResult.asyncCompletionWrapperCallback == null)
			{
				AsyncResult.asyncCompletionWrapperCallback = new AsyncCallback(AsyncResult.AsyncCompletionWrapperCallback);
			}
			return AsyncResult.asyncCompletionWrapperCallback;
		}

		protected IDisposable PrepareTransactionalCall(Transaction transaction)
		{
			object transactionSignalScope;
			if (this.transactionContext != null && !this.transactionContext.IsPotentiallyAbandoned)
			{
				AsyncResult.ThrowInvalidAsyncResult("PrepareTransactionalCall should only be called as the object of non-nested using statements. If the Begin succeeds, Check/SyncContinue must be called before another PrepareTransactionalCall.");
			}
			if (transaction == null)
			{
				transactionSignalScope = null;
			}
			else
			{
				transactionSignalScope = new AsyncResult.TransactionSignalScope(this, transaction);
			}
			AsyncResult.TransactionSignalScope transactionSignalScope1 = (AsyncResult.TransactionSignalScope)transactionSignalScope;
			this.transactionContext = (AsyncResult.TransactionSignalScope)transactionSignalScope;
			return transactionSignalScope1;
		}

		protected bool SyncContinue(IAsyncResult result)
		{
			AsyncResult.AsyncCompletion asyncCompletion;
			if (!this.TryContinueHelper(result, out asyncCompletion))
			{
				return false;
			}
			return asyncCompletion(result);
		}

		protected static void ThrowInvalidAsyncResult(IAsyncResult result)
		{
			throw Fx.Exception.AsError(new InvalidOperationException(SRCore.InvalidAsyncResultImplementation(result.GetType())), null);
		}

		protected static void ThrowInvalidAsyncResult(string debugText)
		{
			string invalidAsyncResultImplementationGeneric = SRCore.InvalidAsyncResultImplementationGeneric;
			throw Fx.Exception.AsError(new InvalidOperationException(invalidAsyncResultImplementationGeneric), null);
		}

		protected bool TryComplete(bool didCompleteSynchronously, Exception exception)
		{
			bool flag;
			lock (this.ThisLock)
			{
				if (!this.isCompleted)
				{
					this.exception = exception;
					this.isCompleted = true;
					goto Label0;
				}
				else
				{
					flag = false;
				}
			}
			return flag;
		Label0:
			this.completedSynchronously = didCompleteSynchronously;
			if (this.OnCompleting != null)
			{
				try
				{
					this.OnCompleting(this, this.exception);
				}
				catch (Exception exception2)
				{
					Exception exception1 = exception2;
					if (Fx.IsFatal(exception1))
					{
						throw;
					}
					this.exception = exception1;
				}
			}
			if (!didCompleteSynchronously)
			{
				lock (this.ThisLock)
				{
					if (this.manualResetEvent != null)
					{
						this.manualResetEvent.Set();
					}
				}
			}
			if (this.callback != null)
			{
				try
				{
					if (this.VirtualCallback == null)
					{
						this.callback(this);
					}
					else
					{
						this.VirtualCallback(this.callback, this);
					}
				}
				catch (Exception exception4)
				{
					Exception exception3 = exception4;
					if (!Fx.IsFatal(exception3))
					{
						throw Fx.Exception.AsError(new CallbackException(SRCore.AsyncCallbackThrewException, exception3), null);
					}
					throw;
				}
			}
			return true;
		}

		protected bool TryComplete(bool didcompleteSynchronously)
		{
			return this.TryComplete(didcompleteSynchronously, null);
		}

		private bool TryContinueHelper(IAsyncResult result, out AsyncResult.AsyncCompletion callback)
		{
			if (result == null)
			{
				throw Fx.Exception.AsError(new InvalidOperationException(SRCore.InvalidNullAsyncResult), null);
			}
			callback = null;
			if (!result.CompletedSynchronously)
			{
				if (!object.ReferenceEquals(result, this.deferredTransactionalResult))
				{
					return false;
				}
				if (this.transactionContext == null || !this.transactionContext.IsSignalled)
				{
					AsyncResult.ThrowInvalidAsyncResult(result);
				}
				this.deferredTransactionalResult = null;
			}
			else if (this.transactionContext != null)
			{
				if (this.transactionContext.State != AsyncResult.TransactionSignalState.Completed)
				{
					AsyncResult.ThrowInvalidAsyncResult("Check/SyncContinue cannot be called from within the PrepareTransactionalCall using block.");
				}
				else if (this.transactionContext.IsSignalled)
				{
					AsyncResult.ThrowInvalidAsyncResult(result);
				}
			}
			callback = this.GetNextCompletion();
			if (callback == null)
			{
				AsyncResult.ThrowInvalidAsyncResult("Only call Check/SyncContinue once per async operation (once per PrepareAsyncCompletion).");
			}
			return true;
		}

		protected delegate bool AsyncCompletion(IAsyncResult result);

		[Serializable]
		private class TransactionSignalScope : SignalGate<IAsyncResult>, IDisposable
		{
			[NonSerialized]
			private TransactionScope transactionScope;

			[NonSerialized]
			private AsyncResult parent;

			private bool disposed;

			public bool IsPotentiallyAbandoned
			{
				get
				{
					if (this.State == AsyncResult.TransactionSignalState.Abandoned)
					{
						return true;
					}
					if (this.State != AsyncResult.TransactionSignalState.Completed)
					{
						return false;
					}
					return !base.IsSignalled;
				}
			}

			public AsyncResult.TransactionSignalState State
			{
				get;
				private set;
			}

			public TransactionSignalScope(AsyncResult result, Transaction transaction)
			{
				this.parent = result;
				this.transactionScope = Fx.CreateTransactionScope(transaction);
			}

			protected virtual void Dispose(bool disposing)
			{
				IAsyncResult asyncResult;
				if (disposing && !this.disposed)
				{
					this.disposed = true;
					if (this.State == AsyncResult.TransactionSignalState.Ready)
					{
						this.State = AsyncResult.TransactionSignalState.Abandoned;
					}
					else if (this.State != AsyncResult.TransactionSignalState.Prepared)
					{
						AsyncResult.ThrowInvalidAsyncResult("PrepareTransactionalCall should only be called in a using. Dispose called multiple times.");
					}
					else
					{
						this.State = AsyncResult.TransactionSignalState.Completed;
					}
					try
					{
						Fx.CompleteTransactionScope(ref this.transactionScope);
					}
					catch (Exception exception)
					{
						if (!Fx.IsFatal(exception))
						{
							throw Fx.Exception.AsError(new InvalidOperationException(SRCore.AsyncTransactionException), null);
						}
						throw;
					}
					if (this.State == AsyncResult.TransactionSignalState.Completed && base.Unlock(out asyncResult))
					{
						if (this.parent.deferredTransactionalResult != null)
						{
							AsyncResult.ThrowInvalidAsyncResult(this.parent.deferredTransactionalResult);
						}
						this.parent.deferredTransactionalResult = asyncResult;
					}
				}
			}

			public void Prepared()
			{
				if (this.State != AsyncResult.TransactionSignalState.Ready)
				{
					AsyncResult.ThrowInvalidAsyncResult("PrepareAsyncCompletion should only be called once per PrepareTransactionalCall.");
				}
				this.State = AsyncResult.TransactionSignalState.Prepared;
			}

			void System.IDisposable.Dispose()
			{
				this.Dispose(true);
				GC.SuppressFinalize(this);
			}
		}

		private enum TransactionSignalState
		{
			Ready,
			Prepared,
			Completed,
			Abandoned
		}
	}
}