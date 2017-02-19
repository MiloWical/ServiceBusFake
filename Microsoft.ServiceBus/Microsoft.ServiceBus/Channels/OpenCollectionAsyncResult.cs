using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading;

namespace Microsoft.ServiceBus.Channels
{
	internal class OpenCollectionAsyncResult : AsyncResult
	{
		private readonly static AsyncCallback nestedCallback;

		private Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper;

		private int count;

		private Exception exception;

		static OpenCollectionAsyncResult()
		{
			Microsoft.ServiceBus.Channels.OpenCollectionAsyncResult.nestedCallback = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(Microsoft.ServiceBus.Channels.OpenCollectionAsyncResult.Callback));
		}

		public OpenCollectionAsyncResult(TimeSpan timeout, AsyncCallback otherCallback, object state, params ICommunicationObject[] collections) : this(timeout, otherCallback, state, (IList<ICommunicationObject>)collections)
		{
		}

		public OpenCollectionAsyncResult(TimeSpan timeout, AsyncCallback otherCallback, object state, IList<ICommunicationObject> collection) : base(otherCallback, state)
		{
			IAsyncResult asyncResult;
			this.timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
			this.count = collection.Count;
			if (this.count == 0)
			{
				base.Complete(true);
				return;
			}
			foreach (ICommunicationObject communicationObject in collection)
			{
				if (communicationObject == null)
				{
					this.Decrement(true);
				}
				else if (this.exception == null)
				{
					Microsoft.ServiceBus.Channels.OpenCollectionAsyncResult.CallbackState callbackState = new Microsoft.ServiceBus.Channels.OpenCollectionAsyncResult.CallbackState(this, communicationObject);
					try
					{
						asyncResult = communicationObject.BeginOpen(this.timeoutHelper.RemainingTime(), Microsoft.ServiceBus.Channels.OpenCollectionAsyncResult.nestedCallback, callbackState);
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
					this.CompleteOpen(communicationObject, asyncResult);
				}
				else
				{
					return;
				}
			}
		}

		private static void Callback(IAsyncResult result)
		{
			if (result.CompletedSynchronously)
			{
				return;
			}
			Microsoft.ServiceBus.Channels.OpenCollectionAsyncResult.CallbackState asyncState = (Microsoft.ServiceBus.Channels.OpenCollectionAsyncResult.CallbackState)result.AsyncState;
			asyncState.Result.CompleteOpen(asyncState.Instance, result);
		}

		private void CompleteOpen(ICommunicationObject communicationObject, IAsyncResult result)
		{
			Exception exception = null;
			try
			{
				communicationObject.EndOpen(result);
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
			this.exception = exception;
			this.Decrement(completedSynchronously);
		}

		public static new void End(IAsyncResult result)
		{
			AsyncResult.End<Microsoft.ServiceBus.Channels.OpenCollectionAsyncResult>(result);
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
					communicationObject.Open(timeoutHelper.RemainingTime());
				}
			}
		}

		private class CallbackState
		{
			private readonly ICommunicationObject instance;

			private readonly Microsoft.ServiceBus.Channels.OpenCollectionAsyncResult result;

			public ICommunicationObject Instance
			{
				get
				{
					return this.instance;
				}
			}

			public Microsoft.ServiceBus.Channels.OpenCollectionAsyncResult Result
			{
				get
				{
					return this.result;
				}
			}

			public CallbackState(Microsoft.ServiceBus.Channels.OpenCollectionAsyncResult result, ICommunicationObject instance)
			{
				this.result = result;
				this.instance = instance;
			}
		}
	}
}