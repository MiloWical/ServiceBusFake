using Microsoft.ServiceBus.Common;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.ServiceBus.Messaging
{
	internal sealed class CloseEntityCollectionAsyncResult : AsyncResult
	{
		private readonly static AsyncCallback onObjectClosed;

		private Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper;

		private int interlockedCount;

		private Exception DelayedException
		{
			get;
			set;
		}

		static CloseEntityCollectionAsyncResult()
		{
			CloseEntityCollectionAsyncResult.onObjectClosed = new AsyncCallback(CloseEntityCollectionAsyncResult.OnObjectClosed);
		}

		public CloseEntityCollectionAsyncResult(IList<ClientEntity> clientEntities, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
		{
			this.timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
			this.interlockedCount = clientEntities.Count;
			if (this.interlockedCount <= 0)
			{
				base.Complete(true);
			}
			else
			{
				foreach (ClientEntity clientEntity in clientEntities)
				{
					try
					{
						IAsyncResult asyncResult = clientEntity.BeginClose(this.timeoutHelper.RemainingTime(), CloseEntityCollectionAsyncResult.onObjectClosed, new CloseEntityCollectionAsyncResult.CallbackState(this, clientEntity));
						if (asyncResult.CompletedSynchronously)
						{
							this.CompleteClose(clientEntity, asyncResult);
						}
					}
					catch (Exception exception1)
					{
						Exception exception = exception1;
						if (Fx.IsFatal(exception))
						{
							throw;
						}
						this.Decrement(true, exception);
						clientEntity.Abort();
					}
				}
			}
		}

		private void CompleteClose(ClientEntity entity, IAsyncResult result)
		{
			Exception exception = null;
			try
			{
				entity.EndClose(result);
			}
			catch (MessagingEntityNotFoundException messagingEntityNotFoundException)
			{
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
			this.Decrement(result.CompletedSynchronously, exception);
		}

		private void Decrement(bool completeSyncronously, Exception e)
		{
			if (e != null && this.DelayedException == null)
			{
				this.DelayedException = e;
			}
			if (Interlocked.Decrement(ref this.interlockedCount) == 0)
			{
				base.Complete(completeSyncronously, this.DelayedException);
			}
		}

		public static new void End(IAsyncResult result)
		{
			AsyncResult.End<CloseEntityCollectionAsyncResult>(result);
		}

		private static void OnObjectClosed(IAsyncResult result)
		{
			if (!result.CompletedSynchronously)
			{
				CloseEntityCollectionAsyncResult.CallbackState asyncState = (CloseEntityCollectionAsyncResult.CallbackState)result.AsyncState;
				asyncState.AsyncResult.CompleteClose(asyncState.AsyncData, result);
			}
		}

		private sealed class CallbackState
		{
			public ClientEntity AsyncData
			{
				get;
				private set;
			}

			public CloseEntityCollectionAsyncResult AsyncResult
			{
				get;
				private set;
			}

			public CallbackState(CloseEntityCollectionAsyncResult asyncResult, ClientEntity data)
			{
				this.AsyncResult = asyncResult;
				this.AsyncData = data;
			}
		}
	}
}