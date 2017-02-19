using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Properties;
using System;
using System.Transactions;

namespace Microsoft.ServiceBus
{
	public abstract class RetryPolicy
	{
		internal readonly static TimeSpan ServerBusyBaseSleepTime;

		private object serverBusyLock = new object();

		private volatile bool serverBusy;

		private volatile string serverBusyExceptionMessage;

		private volatile IOThreadTimer serverBusyResetTimer;

		public static RetryPolicy Default
		{
			get
			{
				return new RetryExponential(Constants.DefaultRetryMinBackoff, Constants.DefaultRetryMaxBackoff, Constants.DefaultMaxDeliveryCount);
			}
		}

		internal bool IsServerBusy
		{
			get
			{
				return this.serverBusy;
			}
		}

		public static RetryPolicy NoRetry
		{
			get
			{
				return new Microsoft.ServiceBus.NoRetry();
			}
		}

		internal string ServerBusyExceptionMessage
		{
			get
			{
				return this.serverBusyExceptionMessage;
			}
		}

		static RetryPolicy()
		{
			RetryPolicy.ServerBusyBaseSleepTime = TimeSpan.FromSeconds(10);
		}

		internal RetryPolicy()
		{
			this.serverBusyResetTimer = new IOThreadTimer(new Action<object>(RetryPolicy.OnTimerCallback), this, true);
		}

		public abstract RetryPolicy Clone();

		protected abstract bool IsRetryableException(Exception lastException);

		protected abstract bool OnShouldRetry(TimeSpan remainingTime, int currentRetryCount, out TimeSpan retryInterval);

		private static void OnTimerCallback(object state)
		{
			RetryPolicy retryPolicy = (RetryPolicy)state;
			if (retryPolicy.IsServerBusy)
			{
				retryPolicy.ResetServerBusy();
			}
		}

		internal void ResetServerBusy()
		{
			if (this.serverBusy)
			{
				lock (this.serverBusyLock)
				{
					if (this.serverBusy)
					{
						this.serverBusy = false;
						this.serverBusyExceptionMessage = Microsoft.ServiceBus.SR.GetString(Resources.ServerBusy, new object[0]);
						this.serverBusyResetTimer.Cancel();
					}
				}
			}
		}

		internal void SetServerBusy(string exceptionMessage)
		{
			if (!this.serverBusy)
			{
				lock (this.serverBusyLock)
				{
					if (!this.serverBusy)
					{
						this.serverBusy = true;
						if (!string.IsNullOrWhiteSpace(exceptionMessage))
						{
							this.serverBusyExceptionMessage = exceptionMessage;
						}
						else
						{
							this.serverBusyExceptionMessage = Microsoft.ServiceBus.SR.GetString(Resources.ServerBusy, new object[0]);
						}
						this.serverBusyResetTimer.Set(RetryPolicy.ServerBusyBaseSleepTime);
					}
				}
			}
		}

		internal bool ShouldRetry(TimeSpan remainingTime, int currentRetryCount, Exception lastException, out TimeSpan retryInterval)
		{
			if (lastException is ServerBusyException)
			{
				this.SetServerBusy(lastException.Message);
			}
			if (Transaction.Current == null)
			{
				if (lastException == null || remainingTime == TimeSpan.Zero)
				{
					retryInterval = TimeSpan.Zero;
					return false;
				}
				if (this.IsRetryableException(lastException))
				{
					return this.OnShouldRetry(remainingTime, currentRetryCount, out retryInterval);
				}
			}
			retryInterval = TimeSpan.Zero;
			return false;
		}
	}
}