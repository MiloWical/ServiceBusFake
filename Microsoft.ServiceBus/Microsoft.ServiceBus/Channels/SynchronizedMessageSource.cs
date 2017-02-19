using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.ServiceModel.Channels;
using System.Threading;

namespace Microsoft.ServiceBus.Channels
{
	internal class SynchronizedMessageSource
	{
		private Microsoft.ServiceBus.Channels.IMessageSource source;

		private ThreadNeutralSemaphore sourceLock;

		public SynchronizedMessageSource(Microsoft.ServiceBus.Channels.IMessageSource source)
		{
			this.source = source;
			this.sourceLock = new ThreadNeutralSemaphore(1);
		}

		public IAsyncResult BeginReceive(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new Microsoft.ServiceBus.Channels.SynchronizedMessageSource.ReceiveAsyncResult(this, timeout, callback, state);
		}

		public IAsyncResult BeginWaitForMessage(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new Microsoft.ServiceBus.Channels.SynchronizedMessageSource.WaitForMessageAsyncResult(this, timeout, callback, state);
		}

		public Message EndReceive(IAsyncResult result)
		{
			return Microsoft.ServiceBus.Channels.SynchronizedMessageSource.SynchronizedAsyncResult<Message>.End(result);
		}

		public bool EndWaitForMessage(IAsyncResult result)
		{
			return Microsoft.ServiceBus.Channels.SynchronizedMessageSource.SynchronizedAsyncResult<bool>.End(result);
		}

		public Message Receive(TimeSpan timeout)
		{
			Message message;
			Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
			if (!this.sourceLock.TryEnter(timeoutHelper.RemainingTime()))
			{
				ExceptionUtility exceptionUtility = DiagnosticUtility.ExceptionUtility;
				string receiveTimedOut2 = Resources.ReceiveTimedOut2;
				object[] objArray = new object[] { timeout };
				throw exceptionUtility.ThrowHelperError(new TimeoutException(Microsoft.ServiceBus.SR.GetString(receiveTimedOut2, objArray), ThreadNeutralSemaphore.CreateEnterTimedOutException(timeout)));
			}
			try
			{
				message = this.source.Receive(timeoutHelper.RemainingTime());
			}
			finally
			{
				this.sourceLock.Exit();
			}
			return message;
		}

		public bool WaitForMessage(TimeSpan timeout)
		{
			bool flag;
			Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
			if (!this.sourceLock.TryEnter(timeoutHelper.RemainingTime()))
			{
				ExceptionUtility exceptionUtility = DiagnosticUtility.ExceptionUtility;
				string waitForMessageTimedOut = Resources.WaitForMessageTimedOut;
				object[] objArray = new object[] { timeout };
				throw exceptionUtility.ThrowHelperError(new TimeoutException(Microsoft.ServiceBus.SR.GetString(waitForMessageTimedOut, objArray), ThreadNeutralSemaphore.CreateEnterTimedOutException(timeout)));
			}
			try
			{
				flag = this.source.WaitForMessage(timeoutHelper.RemainingTime());
			}
			finally
			{
				this.sourceLock.Exit();
			}
			return flag;
		}

		private class ReceiveAsyncResult : Microsoft.ServiceBus.Channels.SynchronizedMessageSource.SynchronizedAsyncResult<Message>
		{
			private static WaitCallback onReceiveComplete;

			static ReceiveAsyncResult()
			{
				Microsoft.ServiceBus.Channels.SynchronizedMessageSource.ReceiveAsyncResult.onReceiveComplete = new WaitCallback(Microsoft.ServiceBus.Channels.SynchronizedMessageSource.ReceiveAsyncResult.OnReceiveComplete);
			}

			public ReceiveAsyncResult(Microsoft.ServiceBus.Channels.SynchronizedMessageSource syncSource, TimeSpan timeout, AsyncCallback callback, object state) : base(syncSource, timeout, callback, state)
			{
			}

			private static void OnReceiveComplete(object state)
			{
				Microsoft.ServiceBus.Channels.SynchronizedMessageSource.ReceiveAsyncResult receiveAsyncResult = (Microsoft.ServiceBus.Channels.SynchronizedMessageSource.ReceiveAsyncResult)state;
				Exception exception = null;
				try
				{
					receiveAsyncResult.SetReturnValue(receiveAsyncResult.Source.EndReceive());
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
				receiveAsyncResult.CompleteWithUnlock(false, exception);
			}

			protected override bool PerformOperation(TimeSpan timeout)
			{
				if (base.Source.BeginReceive(timeout, Microsoft.ServiceBus.Channels.SynchronizedMessageSource.ReceiveAsyncResult.onReceiveComplete, this) != Microsoft.ServiceBus.Channels.AsyncReceiveResult.Completed)
				{
					return false;
				}
				base.SetReturnValue(base.Source.EndReceive());
				return true;
			}
		}

		private abstract class SynchronizedAsyncResult<T> : AsyncResult
		{
			private readonly static Action<object> onEnterComplete;

			private T returnValue;

			private bool exitLock;

			private Microsoft.ServiceBus.Channels.SynchronizedMessageSource syncSource;

			private Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper;

			protected Microsoft.ServiceBus.Channels.IMessageSource Source
			{
				get
				{
					return this.syncSource.source;
				}
			}

			static SynchronizedAsyncResult()
			{
				Microsoft.ServiceBus.Channels.SynchronizedMessageSource.SynchronizedAsyncResult<T>.onEnterComplete = new Action<object>(Microsoft.ServiceBus.Channels.SynchronizedMessageSource.SynchronizedAsyncResult<T>.OnEnterComplete);
			}

			public SynchronizedAsyncResult(Microsoft.ServiceBus.Channels.SynchronizedMessageSource syncSource, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				bool flag;
				this.syncSource = syncSource;
				this.timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
				if (!syncSource.sourceLock.Enter(Microsoft.ServiceBus.Channels.SynchronizedMessageSource.SynchronizedAsyncResult<T>.onEnterComplete, this))
				{
					return;
				}
				this.exitLock = true;
				bool flag1 = false;
				try
				{
					flag = this.PerformOperation(this.timeoutHelper.RemainingTime());
					flag1 = true;
				}
				finally
				{
					if (!flag1)
					{
						this.ExitLock();
					}
				}
				if (flag)
				{
					this.CompleteWithUnlock(true);
				}
			}

			protected void CompleteWithUnlock(bool synchronous)
			{
				this.CompleteWithUnlock(synchronous, null);
			}

			protected void CompleteWithUnlock(bool synchronous, Exception exception)
			{
				this.ExitLock();
				base.Complete(synchronous, exception);
			}

			public static new T End(IAsyncResult result)
			{
				return AsyncResult.End<Microsoft.ServiceBus.Channels.SynchronizedMessageSource.SynchronizedAsyncResult<T>>(result).returnValue;
			}

			private void ExitLock()
			{
				if (this.exitLock)
				{
					this.syncSource.sourceLock.Exit();
					this.exitLock = false;
				}
			}

			private static void OnEnterComplete(object state)
			{
				bool flag;
				Microsoft.ServiceBus.Channels.SynchronizedMessageSource.SynchronizedAsyncResult<T> synchronizedAsyncResult = (Microsoft.ServiceBus.Channels.SynchronizedMessageSource.SynchronizedAsyncResult<T>)state;
				Exception exception = null;
				try
				{
					synchronizedAsyncResult.exitLock = true;
					flag = synchronizedAsyncResult.PerformOperation(synchronizedAsyncResult.timeoutHelper.RemainingTime());
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
					synchronizedAsyncResult.CompleteWithUnlock(false, exception);
				}
			}

			protected abstract bool PerformOperation(TimeSpan timeout);

			protected void SetReturnValue(T returnValue)
			{
				this.returnValue = returnValue;
			}
		}

		private class WaitForMessageAsyncResult : Microsoft.ServiceBus.Channels.SynchronizedMessageSource.SynchronizedAsyncResult<bool>
		{
			private static WaitCallback onWaitForMessageComplete;

			static WaitForMessageAsyncResult()
			{
				Microsoft.ServiceBus.Channels.SynchronizedMessageSource.WaitForMessageAsyncResult.onWaitForMessageComplete = new WaitCallback(Microsoft.ServiceBus.Channels.SynchronizedMessageSource.WaitForMessageAsyncResult.OnWaitForMessageComplete);
			}

			public WaitForMessageAsyncResult(Microsoft.ServiceBus.Channels.SynchronizedMessageSource syncSource, TimeSpan timeout, AsyncCallback callback, object state) : base(syncSource, timeout, callback, state)
			{
			}

			private static void OnWaitForMessageComplete(object state)
			{
				Microsoft.ServiceBus.Channels.SynchronizedMessageSource.WaitForMessageAsyncResult waitForMessageAsyncResult = (Microsoft.ServiceBus.Channels.SynchronizedMessageSource.WaitForMessageAsyncResult)state;
				Exception exception = null;
				try
				{
					waitForMessageAsyncResult.SetReturnValue(waitForMessageAsyncResult.Source.EndWaitForMessage());
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
				waitForMessageAsyncResult.CompleteWithUnlock(false, exception);
			}

			protected override bool PerformOperation(TimeSpan timeout)
			{
				if (base.Source.BeginWaitForMessage(timeout, Microsoft.ServiceBus.Channels.SynchronizedMessageSource.WaitForMessageAsyncResult.onWaitForMessageComplete, this) != Microsoft.ServiceBus.Channels.AsyncReceiveResult.Completed)
				{
					return false;
				}
				base.SetReturnValue(base.Source.EndWaitForMessage());
				return true;
			}
		}
	}
}