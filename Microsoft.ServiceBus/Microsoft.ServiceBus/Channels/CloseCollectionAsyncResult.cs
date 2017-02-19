using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading;

namespace Microsoft.ServiceBus.Channels
{
	internal class CloseCollectionAsyncResult : AsyncResult
	{
		private readonly static AsyncCallback nestedCallback;

		private Exception exception;

		private int count;

		static CloseCollectionAsyncResult()
		{
			Microsoft.ServiceBus.Channels.CloseCollectionAsyncResult.nestedCallback = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(Microsoft.ServiceBus.Channels.CloseCollectionAsyncResult.Callback));
		}

		public CloseCollectionAsyncResult(TimeSpan timeout, AsyncCallback otherCallback, object state, params ICommunicationObject[] collections) : this(timeout, otherCallback, state, (IList<ICommunicationObject>)collections)
		{
		}

		public CloseCollectionAsyncResult(TimeSpan timeout, AsyncCallback otherCallback, object state, IList<ICommunicationObject> collection) : base(otherCallback, state)
		{
			IAsyncResult asyncResult;
			Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
			this.count = collection.Count;
			if (this.count == 0)
			{
				base.Complete(true);
				return;
			}
			foreach (ICommunicationObject communicationObject in collection)
			{
				if (communicationObject != null)
				{
					Microsoft.ServiceBus.Channels.CloseCollectionAsyncResult.CallbackState callbackState = new Microsoft.ServiceBus.Channels.CloseCollectionAsyncResult.CallbackState(this, communicationObject);
					try
					{
						asyncResult = communicationObject.BeginClose(timeoutHelper.RemainingTime(), Microsoft.ServiceBus.Channels.CloseCollectionAsyncResult.nestedCallback, callbackState);
					}
					catch (Exception exception1)
					{
						Exception exception = exception1;
						if (Fx.IsFatal(exception))
						{
							throw;
						}
						this.Decrement(true, exception);
						communicationObject.Abort();
						continue;
					}
					if (!asyncResult.CompletedSynchronously)
					{
						continue;
					}
					this.CompleteClose(communicationObject, asyncResult);
				}
				else
				{
					this.Decrement(true);
				}
			}
		}

		private static void Callback(IAsyncResult result)
		{
			if (result.CompletedSynchronously)
			{
				return;
			}
			Microsoft.ServiceBus.Channels.CloseCollectionAsyncResult.CallbackState asyncState = (Microsoft.ServiceBus.Channels.CloseCollectionAsyncResult.CallbackState)result.AsyncState;
			asyncState.Result.CompleteClose(asyncState.Instance, result);
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
				exception = exception1;
				communicationObject.Abort();
			}
			this.Decrement(result.CompletedSynchronously, exception);
		}

		private void Decrement(bool completedSynchronously)
		{
			if (Interlocked.Decrement(ref this.count) == 0)
			{
				if (this.exception != null)
				{
					base.Complete(completedSynchronously, this.exception);
					return;
				}
				base.Complete(completedSynchronously);
			}
		}

		private void Decrement(bool completedSynchronously, Exception exception)
		{
			if (this.exception == null && exception != null)
			{
				this.exception = exception;
			}
			this.Decrement(completedSynchronously);
		}

		public static new void End(IAsyncResult result)
		{
			AsyncResult.End<Microsoft.ServiceBus.Channels.CloseCollectionAsyncResult>(result);
		}

		public static void RunSynchronously(TimeSpan timeout, params ICommunicationObject[] collection)
		{
			Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
			ICommunicationObject[] communicationObjectArray = collection;
			for (int i = 0; i < (int)communicationObjectArray.Length; i++)
			{
				ICommunicationObject communicationObject = communicationObjectArray[i];
				if (communicationObject != null)
				{
					communicationObject.Close(timeoutHelper.RemainingTime());
				}
			}
		}

		private sealed class CallbackState
		{
			private readonly ICommunicationObject instance;

			private readonly Microsoft.ServiceBus.Channels.CloseCollectionAsyncResult result;

			public ICommunicationObject Instance
			{
				get
				{
					return this.instance;
				}
			}

			public Microsoft.ServiceBus.Channels.CloseCollectionAsyncResult Result
			{
				get
				{
					return this.result;
				}
			}

			public CallbackState(Microsoft.ServiceBus.Channels.CloseCollectionAsyncResult result, ICommunicationObject instance)
			{
				this.result = result;
				this.instance = instance;
			}
		}
	}
}