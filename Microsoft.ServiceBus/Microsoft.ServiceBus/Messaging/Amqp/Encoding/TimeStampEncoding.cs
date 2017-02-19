using Microsoft.ServiceBus.Messaging.Amqp;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
	internal sealed class TimeStampEncoding : EncodingBase
	{
		private readonly static long MaxMilliseconds;

		static TimeStampEncoding()
		{
			TimeSpan universalTime = DateTime.MaxValue.ToUniversalTime() - AmqpConstants.StartOfEpoch;
			TimeStampEncoding.MaxMilliseconds = (long)universalTime.TotalMilliseconds;
		}

		public TimeStampEncoding() : base(131)
		{
		}

		public static DateTime? Decode(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
		{
			if (formatCode == 0)
			{
				Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode1 = AmqpEncoding.ReadFormatCode(buffer);
				formatCode = formatCode1;
				if (formatCode1 == 64)
				{
					return null;
				}
			}
			return new DateTime?(TimeStampEncoding.ToDateTime(AmqpBitConverter.ReadLong(buffer)));
		}

		public override object DecodeObject(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
		{
			return TimeStampEncoding.Decode(buffer, formatCode);
		}

		public static void Encode(DateTime? value, ByteBuffer buffer)
		{
			if (!value.HasValue)
			{
				AmqpEncoding.EncodeNull(buffer);
				return;
			}
			AmqpBitConverter.WriteUByte(buffer, 131);
			AmqpBitConverter.WriteLong(buffer, TimeStampEncoding.GetMilliseconds(value.Value));
		}

		public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
		{
			if (arrayEncoding)
			{
				AmqpBitConverter.WriteLong(buffer, TimeStampEncoding.GetMilliseconds((DateTime)value));
				return;
			}
			TimeStampEncoding.Encode(new DateTime?((DateTime)value), buffer);
		}

		public static int GetEncodeSize(DateTime? value)
		{
			if (!value.HasValue)
			{
				return 1;
			}
			return 9;
		}

		public static long GetMilliseconds(DateTime value)
		{
			return (long)(value.ToUniversalTime() - AmqpConstants.StartOfEpoch).TotalMilliseconds;
		}

		public override int GetObjectEncodeSize(object value, bool arrayEncoding)
		{
			if (arrayEncoding)
			{
				return 8;
			}
			return TimeStampEncoding.GetEncodeSize(new DateTime?((DateTime)value));
		}

		public static DateTime ToDateTime(long milliseconds)
		{
			milliseconds = (milliseconds < (long)0 ? (long)0 : milliseconds);
			if (milliseconds >= TimeStampEncoding.MaxMilliseconds)
			{
				return DateTime.MaxValue;
			}
			return AmqpConstants.StartOfEpoch.AddMilliseconds((double)milliseconds);
		}
	}
}