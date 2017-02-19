using System;

namespace Microsoft.ServiceBus
{
	public sealed class NoRetry : RetryPolicy
	{
		public NoRetry()
		{
		}

		public override RetryPolicy Clone()
		{
			return new Microsoft.ServiceBus.NoRetry();
		}

		protected override bool IsRetryableException(Exception lastException)
		{
			return false;
		}

		protected override bool OnShouldRetry(TimeSpan remainingTime, int currentRetryCount, out TimeSpan retryInterval)
		{
			retryInterval = TimeSpan.Zero;
			return false;
		}
	}
}