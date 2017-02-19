using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Threading;

namespace Microsoft.ServiceBus.Messaging
{
	internal sealed class CloseCollectionAsyncResult : AsyncResult
	{
		private readonly static AsyncCallback onObjectClosed;

		private readonly IList<ICommunicationObject> communicationObjects;

		private Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper;

		private int count;

		private Exception DelayedException
		{
			get;
			set;
		}

		static CloseCollectionAsyncResult()
		{
			Microsoft.ServiceBus.Messaging.CloseCollectionAsyncResult.onObjectClosed = new AsyncCallback(Microsoft.ServiceBus.Messaging.CloseCollectionAsyncResult.OnObjectClosed);
		}

		public CloseCollectionAsyncResult(IEnumerable<ICommunicationObject> communicationObjects, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
		{
			this.timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
			this.communicationObjects = new List<ICommunicationObject>(communicationObjects);
			this.count = this.communicationObjects.Count;
			if (this.count <= 0)
			{
				base.Complete(true);
			}
			else
			{
				foreach (ICommunicationObject communicationObject in this.communicationObjects)
				{
					try
					{
						IAsyncResult asyncResult = communicationObject.BeginClose(this.timeoutHelper.RemainingTime(), Microsoft.ServiceBus.Messaging.CloseCollectionAsyncResult.onObjectClosed, new Microsoft.ServiceBus.Messaging.CloseCollectionAsyncResult.CallbackState(this, communicationObject));
						if (asyncResult.CompletedSynchronously)
						{
							this.CompleteClose(communicationObject, asyncResult);
						}
					}
					catch (Exception exception1)
					{
						Exception exception = exception1;
						if (Fx.IsFatal(exception))
						{
							throw;
						}
						MessagingClientEtwProvider.Provider.EventWriteExceptionAsInformation(exception.ToString());
						this.Decrement(true, exception);
						communicationObject.Abort();
					}
				}
			}
		}

		private void CompleteClose(ICommunicationObject communicationObject, IAsyncResult result)
		{
			Exception exception = null;
			try
			{
				communicationObject.EndClose(result);
			}
			catch (Exception exception2)
			{
				Exception exception1 = exception2;
				if (Fx.IsFatal(exception1))
				{
					throw;
				}
				MessagingClientEtwProvider.Provider.EventWriteExceptionAsInformation(exception1.ToStringSlim());
				communicationObject.Abort();
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
			if (Interlocked.Decrement(ref this.count) == 0)
			{
				base.Complete(completeSyncronously, this.DelayedException);
			}
		}

		public static new void End(IAsyncResult result)
		{
			AsyncResult.End<Microsoft.ServiceBus.Messaging.CloseCollectionAsyncResult>(result);
		}

		private static void OnObjectClosed(IAsyncResult result)
		{
			if (!result.CompletedSynchronously)
			{
				Microsoft.ServiceBus.Messaging.CloseCollectionAsyncResult.CallbackState asyncState = (Microsoft.ServiceBus.Messaging.CloseCollectionAsyncResult.CallbackState)result.AsyncState;
				asyncState.AsyncResult.CompleteClose(asyncState.AsyncData, result);
			}
		}

		private sealed class CallbackState
		{
			public ICommunicationObject AsyncData
			{
				get;
				private set;
			}

			public Microsoft.ServiceBus.Messaging.CloseCollectionAsyncResult AsyncResult
			{
				get;
				private set;
			}

			public CallbackState(Microsoft.ServiceBus.Messaging.CloseCollectionAsyncResult asyncResult, ICommunicationObject data)
			{
				this.AsyncResult = asyncResult;
				this.AsyncData = data;
			}
		}
	}
}