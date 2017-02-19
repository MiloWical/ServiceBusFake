using Microsoft.ServiceBus.Common.Interop;
using System;

namespace Microsoft.ServiceBus.Common
{
	internal static class Ticks
	{
		public static long Now
		{
			get
			{
				long num;
				UnsafeNativeMethods.GetSystemTimeAsFileTime(out num);
				return num;
			}
		}

		public static long Add(long firstTicks, long secondTicks)
		{
			if (firstTicks == 9223372036854775807L || firstTicks == -9223372036854775808L)
			{
				return firstTicks;
			}
			if (secondTicks == 9223372036854775807L || secondTicks == -9223372036854775808L)
			{
				return secondTicks;
			}
			if (firstTicks >= (long)0 && 9223372036854775807L - firstTicks <= secondTicks)
			{
				return 9223372036854775806L;
			}
			if (firstTicks <= (long)0 && -9223372036854775808L - firstTicks >= secondTicks)
			{
				return -9223372036854775807L;
			}
			return checked(firstTicks + secondTicks);
		}

		public static long FromMilliseconds(int milliseconds)
		{
			return checked((long)milliseconds * (long)10000);
		}

		public static long FromTimeSpan(TimeSpan duration)
		{
			return duration.Ticks;
		}

		public static int ToMilliseconds(long ticks)
		{
			return checked((int)(ticks / (long)10000));
		}

		public static TimeSpan ToTimeSpan(long ticks)
		{
			return new TimeSpan(ticks);
		}
	}
}