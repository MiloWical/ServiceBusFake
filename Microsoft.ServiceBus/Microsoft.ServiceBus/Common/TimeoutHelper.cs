using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.ServiceBus.Common
{
	[DebuggerStepThrough]
	internal struct TimeoutHelper
	{
		private DateTime deadline;

		private bool deadlineSet;

		private TimeSpan originalTimeout;

		public readonly static TimeSpan MaxWait;

		public TimeSpan OriginalTimeout
		{
			get
			{
				return this.originalTimeout;
			}
		}

		static TimeoutHelper()
		{
			Microsoft.ServiceBus.Common.TimeoutHelper.MaxWait = TimeSpan.FromMilliseconds(2147483647);
		}

		public TimeoutHelper(TimeSpan timeout) : this(timeout, false)
		{
		}

		public TimeoutHelper(TimeSpan timeout, bool startTimeout)
		{
			this.originalTimeout = timeout;
			this.deadline = DateTime.MaxValue;
			this.deadlineSet = timeout == TimeSpan.MaxValue;
			if (startTimeout && !this.deadlineSet)
			{
				this.SetDeadline();
			}
		}

		public static TimeSpan Add(TimeSpan timeout1, TimeSpan timeout2)
		{
			return Ticks.ToTimeSpan(Ticks.Add(Ticks.FromTimeSpan(timeout1), Ticks.FromTimeSpan(timeout2)));
		}

		public static DateTime Add(DateTime time, TimeSpan timeout)
		{
			if (timeout >= TimeSpan.Zero && (DateTime.MaxValue - time) <= timeout)
			{
				return DateTime.MaxValue;
			}
			if (timeout <= TimeSpan.Zero && (DateTime.MinValue - time) >= timeout)
			{
				return DateTime.MinValue;
			}
			return time + timeout;
		}

		public static TimeSpan Divide(TimeSpan timeout, int factor)
		{
			if (timeout == TimeSpan.MaxValue)
			{
				return TimeSpan.MaxValue;
			}
			return Ticks.ToTimeSpan(Ticks.FromTimeSpan(timeout) / (long)factor + (long)1);
		}

		public TimeSpan ElapsedTime()
		{
			return this.originalTimeout - this.RemainingTime();
		}

		public static TimeSpan FromMilliseconds(int milliseconds)
		{
			if (milliseconds == -1)
			{
				return TimeSpan.MaxValue;
			}
			return TimeSpan.FromMilliseconds((double)milliseconds);
		}

		public static bool IsTooLarge(TimeSpan timeout)
		{
			if (timeout <= Microsoft.ServiceBus.Common.TimeoutHelper.MaxWait)
			{
				return false;
			}
			return timeout != TimeSpan.MaxValue;
		}

		public static TimeSpan Min(TimeSpan val1, TimeSpan val2)
		{
			if (val1 > val2)
			{
				return val2;
			}
			return val1;
		}

		public static DateTime Min(DateTime val1, DateTime val2)
		{
			if (val1 > val2)
			{
				return val2;
			}
			return val1;
		}

		public TimeSpan RemainingTime()
		{
			if (!this.deadlineSet)
			{
				this.SetDeadline();
				return this.originalTimeout;
			}
			if (this.deadline == DateTime.MaxValue)
			{
				return TimeSpan.MaxValue;
			}
			TimeSpan utcNow = this.deadline - DateTime.UtcNow;
			if (utcNow <= TimeSpan.Zero)
			{
				return TimeSpan.Zero;
			}
			return utcNow;
		}

		private void SetDeadline()
		{
			this.deadline = DateTime.UtcNow + this.originalTimeout;
			this.deadlineSet = true;
		}

		public static DateTime Subtract(DateTime time, TimeSpan timeout)
		{
			return Microsoft.ServiceBus.Common.TimeoutHelper.Add(time, TimeSpan.Zero - timeout);
		}

		public static void ThrowIfNegativeArgument(TimeSpan timeout)
		{
			Microsoft.ServiceBus.Common.TimeoutHelper.ThrowIfNegativeArgument(timeout, "timeout");
		}

		public static void ThrowIfNegativeArgument(TimeSpan timeout, string argumentName)
		{
			if (timeout < TimeSpan.Zero)
			{
				throw Fx.Exception.ArgumentOutOfRange(argumentName, timeout, SRCore.TimeoutMustBeNonNegative(argumentName, timeout));
			}
		}

		public static void ThrowIfNonPositiveArgument(TimeSpan timeout)
		{
			Microsoft.ServiceBus.Common.TimeoutHelper.ThrowIfNonPositiveArgument(timeout, "timeout");
		}

		public static void ThrowIfNonPositiveArgument(TimeSpan timeout, string argumentName)
		{
			if (timeout <= TimeSpan.Zero)
			{
				throw Fx.Exception.ArgumentOutOfRange(argumentName, timeout, SRCore.TimeoutMustBePositive(argumentName, timeout));
			}
		}

		public static int ToMilliseconds(TimeSpan timeout)
		{
			if (timeout == TimeSpan.MaxValue)
			{
				return -1;
			}
			long num = Ticks.FromTimeSpan(timeout);
			if (num / (long)10000 > (long)2147483647)
			{
				return 2147483647;
			}
			return Ticks.ToMilliseconds(num);
		}

		public static bool WaitOne(WaitHandle waitHandle, TimeSpan timeout)
		{
			Microsoft.ServiceBus.Common.TimeoutHelper.ThrowIfNegativeArgument(timeout);
			if (timeout == TimeSpan.MaxValue)
			{
				waitHandle.WaitOne();
				return true;
			}
			return waitHandle.WaitOne(timeout, false);
		}
	}
}