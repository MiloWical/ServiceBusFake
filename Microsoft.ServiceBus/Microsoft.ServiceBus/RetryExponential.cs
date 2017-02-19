using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Properties;
using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus
{
	public sealed class RetryExponential : RetryPolicy
	{
		public TimeSpan DeltaBackoff
		{
			get;
			private set;
		}

		public TimeSpan MaximumBackoff
		{
			get;
			private set;
		}

		public int MaxRetryCount
		{
			get;
			private set;
		}

		public TimeSpan MinimalBackoff
		{
			get;
			private set;
		}

		public TimeSpan TerminationTimeBuffer
		{
			get;
			private set;
		}

		public RetryExponential(TimeSpan minBackoff, TimeSpan maxBackoff, int maxRetryCount) : this(minBackoff, maxBackoff, Constants.DefaultRetryDeltaBackoff, Constants.DefaultRetryTerminationBuffer, maxRetryCount, true)
		{
			if (this.DeltaBackoff >= (maxBackoff - minBackoff))
			{
				TimeSpan timeSpan = maxBackoff - minBackoff;
				this.DeltaBackoff = new TimeSpan(timeSpan.Ticks / (long)2);
			}
		}

		[Obsolete("This constructor is obsolete. Please use the other constructor instead.")]
		public RetryExponential(TimeSpan minBackoff, TimeSpan maxBackoff, TimeSpan deltaBackoff, TimeSpan terminationTimeBuffer, int maxRetryCount) : this(minBackoff, maxBackoff, deltaBackoff, terminationTimeBuffer, maxRetryCount, true)
		{
		}

		internal RetryExponential(TimeSpan minBackoff, TimeSpan maxBackoff, TimeSpan deltaBackoff, TimeSpan terminationTimeBuffer, int maxRetryCount, bool throwOnClientChecks)
		{
			TimeoutHelper.ThrowIfNegativeArgument(minBackoff, "minBackoff");
			TimeoutHelper.ThrowIfNonPositiveArgument(maxBackoff, "maxBackoff");
			TimeoutHelper.ThrowIfNonPositiveArgument(deltaBackoff, "deltaBackoff");
			TimeoutHelper.ThrowIfNonPositiveArgument(terminationTimeBuffer, "terminationTimeBuffer");
			if (maxRetryCount <= 0)
			{
				throw new ArgumentOutOfRangeException("maxRetryCount", Resources.ValueMustBePositive);
			}
			if (minBackoff >= maxBackoff && throwOnClientChecks)
			{
				throw new ArgumentException(SRClient.RetryPolicyInvalidBackoffPeriod(minBackoff, maxBackoff));
			}
			this.MinimalBackoff = minBackoff;
			this.MaximumBackoff = maxBackoff;
			this.DeltaBackoff = deltaBackoff;
			this.TerminationTimeBuffer = terminationTimeBuffer;
			this.MaxRetryCount = maxRetryCount;
		}

		public override RetryPolicy Clone()
		{
			RetryExponential retryExponential = new RetryExponential(this.MinimalBackoff, this.MaximumBackoff, this.DeltaBackoff, this.TerminationTimeBuffer, this.MaxRetryCount, false);
			if (base.IsServerBusy)
			{
				retryExponential.SetServerBusy(base.ServerBusyExceptionMessage);
			}
			return retryExponential;
		}

		private bool IsRetryableException(MessagingException lastException)
		{
			if (lastException == null)
			{
				throw FxTrace.Exception.ArgumentNull("lastException");
			}
			return lastException.IsTransient;
		}

		protected override bool IsRetryableException(Exception lastException)
		{
			if (lastException == null)
			{
				throw FxTrace.Exception.ArgumentNull("lastException");
			}
			MessagingException messagingException = lastException as MessagingException;
			if (messagingException == null)
			{
				return false;
			}
			return this.IsRetryableException(messagingException);
		}

		protected override bool OnShouldRetry(TimeSpan remainingTime, int currentRetryCount, out TimeSpan retryInterval)
		{
			if (currentRetryCount > this.MaxRetryCount)
			{
				retryInterval = TimeSpan.Zero;
				return false;
			}
			int totalMilliseconds = (int)(this.DeltaBackoff.TotalMilliseconds * 0.8);
			TimeSpan deltaBackoff = this.DeltaBackoff;
			int num = ConcurrentRandom.Next(totalMilliseconds, (int)(deltaBackoff.TotalMilliseconds * 1.2));
			double num1 = (Math.Pow(2, (double)currentRetryCount) - 1) * (double)num;
			TimeSpan minimalBackoff = this.MinimalBackoff;
			TimeSpan maximumBackoff = this.MaximumBackoff;
			double num2 = Math.Min(minimalBackoff.TotalMilliseconds + num1, maximumBackoff.TotalMilliseconds);
			retryInterval = TimeSpan.FromMilliseconds(num2);
			if (base.IsServerBusy)
			{
				retryInterval = retryInterval + RetryPolicy.ServerBusyBaseSleepTime;
			}
			if (retryInterval < (remainingTime - this.TerminationTimeBuffer))
			{
				return true;
			}
			retryInterval = TimeSpan.Zero;
			return false;
		}
	}
}