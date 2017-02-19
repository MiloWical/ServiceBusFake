using Microsoft.ServiceBus;
using System;
using System.Globalization;
using System.Threading;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal struct SequenceNumber : IComparable<SequenceNumber>, IEquatable<SequenceNumber>
	{
		private int sequenceNumber;

		public uint Value
		{
			get
			{
				return (uint)this.sequenceNumber;
			}
		}

		public SequenceNumber(uint value)
		{
			this.sequenceNumber = (int)value;
		}

		public static int Compare(int x, int y)
		{
			int num = x - y;
			if (num == -2147483648)
			{
				throw new InvalidOperationException(SRAmqp.AmqpInvalidSequenceNumberComparison(x, y));
			}
			return num;
		}

		public int CompareTo(SequenceNumber value)
		{
			return SequenceNumber.Compare(this.sequenceNumber, value.sequenceNumber);
		}

		public bool Equals(SequenceNumber obj)
		{
			return this.sequenceNumber == obj.sequenceNumber;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is SequenceNumber))
			{
				return false;
			}
			return this.Equals((SequenceNumber)obj);
		}

		public override int GetHashCode()
		{
			return this.sequenceNumber.GetHashCode();
		}

		public static SequenceNumber Increment(ref int sn)
		{
			return Interlocked.Increment(ref sn);
		}

		public uint Increment()
		{
			SequenceNumber sequenceNumber = this;
			int num = sequenceNumber.sequenceNumber + 1;
			int num1 = num;
			sequenceNumber.sequenceNumber = num;
			return (uint)num1;
		}

		public static SequenceNumber operator +(SequenceNumber value1, int delta)
		{
			return value1.sequenceNumber + delta;
		}

		public static bool operator ==(SequenceNumber value1, SequenceNumber value2)
		{
			return value1.sequenceNumber == value2.sequenceNumber;
		}

		public static bool operator >(SequenceNumber value1, SequenceNumber value2)
		{
			return value1.CompareTo(value2) > 0;
		}

		public static bool operator >=(SequenceNumber value1, SequenceNumber value2)
		{
			return value1.CompareTo(value2) >= 0;
		}

		public static implicit operator SequenceNumber(uint value)
		{
			return new SequenceNumber(value);
		}

		public static bool operator !=(SequenceNumber value1, SequenceNumber value2)
		{
			return value1.sequenceNumber != value2.sequenceNumber;
		}

		public static bool operator <(SequenceNumber value1, SequenceNumber value2)
		{
			return value1.CompareTo(value2) < 0;
		}

		public static bool operator <=(SequenceNumber value1, SequenceNumber value2)
		{
			return value1.CompareTo(value2) <= 0;
		}

		public static SequenceNumber operator -(SequenceNumber value1, int delta)
		{
			return value1.sequenceNumber - delta;
		}

		public static int operator -(SequenceNumber value1, SequenceNumber value2)
		{
			return value1.sequenceNumber - value2.sequenceNumber;
		}

		public override string ToString()
		{
			return this.sequenceNumber.ToString(CultureInfo.InvariantCulture);
		}
	}
}