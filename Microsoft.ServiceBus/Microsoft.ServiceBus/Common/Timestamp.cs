using System;
using System.Diagnostics;

namespace Microsoft.ServiceBus.Common
{
	internal struct Timestamp : IComparable<Timestamp>, IEquatable<Timestamp>
	{
		private readonly static double TickFrequency;

		private readonly long timestamp;

		public TimeSpan Elapsed
		{
			get
			{
				return new TimeSpan(this.GetElapsedDateTimeTicks());
			}
		}

		public long ElapsedTicks
		{
			get
			{
				return this.GetElapsedDateTimeTicks();
			}
		}

		public static Timestamp Now
		{
			get
			{
				return new Timestamp(Stopwatch.GetTimestamp());
			}
		}

		static Timestamp()
		{
			Timestamp.TickFrequency = 10000000 / (double)Stopwatch.Frequency;
		}

		private Timestamp(long timestamp)
		{
			this.timestamp = timestamp;
		}

		public int CompareTo(Timestamp other)
		{
			return this.timestamp.CompareTo(other.timestamp);
		}

		private static long ConvertRawTicksToTicks(long rawTicks)
		{
			if (!Stopwatch.IsHighResolution)
			{
				return rawTicks;
			}
			return (long)((double)rawTicks * Timestamp.TickFrequency);
		}

		public bool Equals(Timestamp other)
		{
			return this.timestamp == other.timestamp;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Timestamp))
			{
				return false;
			}
			return this.Equals((Timestamp)obj);
		}

		private long GetElapsedDateTimeTicks()
		{
			return Timestamp.ConvertRawTicksToTicks(this.GetRawElapsedTicks());
		}

		public override int GetHashCode()
		{
			return this.timestamp.GetHashCode();
		}

		private long GetRawElapsedTicks()
		{
			return Stopwatch.GetTimestamp() - this.timestamp;
		}

		public static Timestamp operator +(Timestamp t, TimeSpan duration)
		{
			long num = (long)((double)t.timestamp + (double)duration.Ticks / Timestamp.TickFrequency);
			return new Timestamp(num);
		}

		public static bool operator ==(Timestamp t1, Timestamp t2)
		{
			return t1.timestamp == t2.timestamp;
		}

		public static bool operator >(Timestamp t1, Timestamp t2)
		{
			return t1.timestamp > t2.timestamp;
		}

		public static bool operator >=(Timestamp t1, Timestamp t2)
		{
			return t1.timestamp >= t2.timestamp;
		}

		public static bool operator !=(Timestamp t1, Timestamp t2)
		{
			return t1.timestamp != t2.timestamp;
		}

		public static bool operator <(Timestamp t1, Timestamp t2)
		{
			return t1.timestamp < t2.timestamp;
		}

		public static bool operator <=(Timestamp t1, Timestamp t2)
		{
			return t1.timestamp <= t2.timestamp;
		}

		public static Timestamp operator -(Timestamp t, TimeSpan duration)
		{
			long num = (long)((double)t.timestamp - (double)duration.Ticks / Timestamp.TickFrequency);
			return new Timestamp(num);
		}

		public static TimeSpan operator -(Timestamp t1, Timestamp t2)
		{
			long num = t1.timestamp - t2.timestamp;
			return new TimeSpan(Timestamp.ConvertRawTicksToTicks(num));
		}
	}
}