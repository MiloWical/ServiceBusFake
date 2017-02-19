using Microsoft.ServiceBus.Common;
using System;
using System.Runtime.CompilerServices;
using System.Transactions;

namespace Microsoft.ServiceBus.Messaging
{
	internal sealed class OpenOnceManager : SingletonManager<MessageClientEntity>
	{
		private readonly MessageClientEntity clientEntity;

		private readonly TimeSpan openTimeout;

		public bool ShouldOpen
		{
			get
			{
				return !this.clientEntity.IsOpened;
			}
		}

		public OpenOnceManager(MessageClientEntity clientEntity) : base(new object())
		{
			this.clientEntity = clientEntity;
			this.openTimeout = clientEntity.OperationTimeout;
		}

		public IAsyncResult Begin(AsyncCallback callback, object state, Func<AsyncCallback, object, IAsyncResult> beginOperation, Action<IAsyncResult> endOperation)
		{
			return new OpenOnceManager.OpenOnceManagerAsyncResult<bool>(this, this.openTimeout, callback, state, beginOperation, (IAsyncResult r, bool& output) => {
				endOperation(r);
				output = true;
				return true;
			});
		}

		public IAsyncResult Begin<T>(AsyncCallback callback, object state, Func<AsyncCallback, object, IAsyncResult> beginOperation, OpenOnceManager.EndOperation<T> endOperation)
		{
			return new OpenOnceManager.OpenOnceManagerAsyncResult<T>(this, this.openTimeout, callback, state, beginOperation, endOperation);
		}

		public IAsyncResult Begin<T>(AsyncCallback callback, object state, Func<AsyncCallback, object, IAsyncResult> beginOperation, Func<IAsyncResult, T> endOperation)
		{
			return new OpenOnceManager.OpenOnceManagerAsyncResult<T>(this, this.openTimeout, callback, state, beginOperation, (IAsyncResult r, T& output) => {
				output = endOperation(r);
				return true;
			});
		}

		private IAsyncResult BeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return base.BeginGetInstance(timeout, callback, state);
		}

		public static void End(IAsyncResult result)
		{
			bool flag;
			OpenOnceManager.OpenOnceManagerAsyncResult<bool>.End(result, out flag);
		}

		public static T End<T>(IAsyncResult result)
		{
			T t;
			OpenOnceManager.OpenOnceManagerAsyncResult<T>.End(result, out t);
			return t;
		}

		public static bool End<T>(IAsyncResult result, out T output)
		{
			return OpenOnceManager.OpenOnceManagerAsyncResult<T>.End(result, out output);
		}

		private void EndOpen(IAsyncResult asyncResult)
		{
			base.EndGetInstance(asyncResult);
		}

		protected override IAsyncResult OnBeginCreateInstance(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.clientEntity.BeginOpen(timeout, callback, state);
		}

		protected override MessageClientEntity OnEndCreateInstance(IAsyncResult asyncResult)
		{
			this.clientEntity.EndOpen(asyncResult);
			return this.clientEntity;
		}

		public void Open()
		{
			OpenOnceManager.End(this.Begin(null, null, (AsyncCallback c, object s) => new CompletedAsyncResult(c, s), (IAsyncResult r) => {
			}));
		}

		public static bool ShouldEnd<T>(IAsyncResult result)
		{
			return result is OpenOnceManager.OpenOnceManagerAsyncResult<T>;
		}

		public static bool ShouldEnd(IAsyncResult result)
		{
			return result is OpenOnceManager.OpenOnceManagerAsyncResult<bool>;
		}

		internal delegate bool EndOperation<T>(IAsyncResult result, out T output);

		private sealed class OpenOnceManagerAsyncResult<T> : AsyncResult
		{
			private readonly static AsyncResult.AsyncCompletion openComplete;

			private readonly static AsyncResult.AsyncCompletion operationComplete;

			private readonly OpenOnceManager openOnceManager;

			private readonly Func<AsyncCallback, object, IAsyncResult> beginOperation;

			private readonly OpenOnceManager.EndOperation<T> endOperation;

			private readonly Transaction transaction;

			private T output;

			private bool operationResult;

			static OpenOnceManagerAsyncResult()
			{
				OpenOnceManager.OpenOnceManagerAsyncResult<T>.openComplete = new AsyncResult.AsyncCompletion(OpenOnceManager.OpenOnceManagerAsyncResult<T>.OpenComplete);
				OpenOnceManager.OpenOnceManagerAsyncResult<T>.operationComplete = new AsyncResult.AsyncCompletion(OpenOnceManager.OpenOnceManagerAsyncResult<T>.OperationComplete);
			}

			public OpenOnceManagerAsyncResult(OpenOnceManager openOnceManager, TimeSpan openTimeout, AsyncCallback callback, object state, Func<AsyncCallback, object, IAsyncResult> beginOperation, OpenOnceManager.EndOperation<T> endOperation) : base(callback, state)
			{
				this.transaction = Transaction.Current;
				this.openOnceManager = openOnceManager;
				this.beginOperation = beginOperation;
				this.endOperation = endOperation;
				if (openOnceManager.ShouldOpen)
				{
					if (base.SyncContinue(this.openOnceManager.BeginOpen(openTimeout, base.PrepareAsyncCompletion(OpenOnceManager.OpenOnceManagerAsyncResult<T>.openComplete), this)))
					{
						base.Complete(true);
						return;
					}
				}
				else if (this.BeginOperation())
				{
					base.Complete(true);
				}
			}

			private bool BeginOperation()
			{
				IAsyncResult asyncResult;
				using (IDisposable disposable = base.PrepareTransactionalCall(this.transaction))
				{
					asyncResult = this.beginOperation(base.PrepareAsyncCompletion(OpenOnceManager.OpenOnceManagerAsyncResult<T>.operationComplete), this);
				}
				return base.SyncContinue(asyncResult);
			}

			public static bool End(IAsyncResult result, out T output)
			{
				OpenOnceManager.OpenOnceManagerAsyncResult<T> openOnceManagerAsyncResult = AsyncResult.End<OpenOnceManager.OpenOnceManagerAsyncResult<T>>(result);
				output = openOnceManagerAsyncResult.output;
				return openOnceManagerAsyncResult.operationResult;
			}

			private static bool OpenComplete(IAsyncResult result)
			{
				OpenOnceManager.OpenOnceManagerAsyncResult<T> asyncState = (OpenOnceManager.OpenOnceManagerAsyncResult<T>)result.AsyncState;
				asyncState.openOnceManager.EndOpen(result);
				return asyncState.BeginOperation();
			}

			private static bool OperationComplete(IAsyncResult result)
			{
				OpenOnceManager.OpenOnceManagerAsyncResult<T> asyncState = (OpenOnceManager.OpenOnceManagerAsyncResult<T>)result.AsyncState;
				asyncState.operationResult = asyncState.endOperation(result, out asyncState.output);
				return true;
			}
		}
	}
}