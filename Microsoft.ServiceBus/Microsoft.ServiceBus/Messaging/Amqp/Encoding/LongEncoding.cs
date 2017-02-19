using Microsoft.ServiceBus.Messaging.Amqp;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
	internal sealed class LongEncoding : EncodingBase
	{
		public LongEncoding() : base(129)
		{
		}

		public static long? Decode(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
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
			int offset = buffer.Offset;
			Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode[] formatCodeArray = new Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode[] { 129, 85 };
			EncodingBase.VerifyFormatCode(formatCode, offset, formatCodeArray);
			return new long?((formatCode == 85 ? (long)AmqpBitConverter.ReadByte(buffer) : AmqpBitConverter.ReadLong(buffer)));
		}

		public override object DecodeObject(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
		{
			return LongEncoding.Decode(buffer, formatCode);
		}

		public static void Encode(long? value, ByteBuffer buffer)
		{
			if (!value.HasValue)
			{
				AmqpEncoding.EncodeNull(buffer);
				return;
			}
			long? nullable = value;
			if ((nullable.GetValueOrDefault() >= (long)-128 ? true : !nullable.HasValue))
			{
				long? nullable1 = value;
				if ((nullable1.GetValueOrDefault() <= (long)127 ? true : !nullable1.HasValue))
				{
					AmqpBitConverter.WriteUByte(buffer, 85);
					AmqpBitConverter.WriteByte(buffer, (sbyte)value.Value);
					return;
				}
			}
			AmqpBitConverter.WriteUByte(buffer, 129);
			AmqpBitConverter.WriteLong(buffer, value.Value);
		}

		public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
		{
			if (arrayEncoding)
			{
				AmqpBitConverter.WriteLong(buffer, (long)value);
				return;
			}
			LongEncoding.Encode(new long?((long)value), buffer);
		}

		public static int GetEncodeSize(long? value)
		{
			if (!value.HasValue)
			{
				return 1;
			}
			long? nullable = value;
			if ((nullable.GetValueOrDefault() >= (long)-128 ? true : !nullable.HasValue))
			{
				long? nullable1 = value;
				if ((nullable1.GetValueOrDefault() <= (long)127 ? true : !nullable1.HasValue))
				{
					return 2;
				}
			}
			return 9;
		}

		public override int GetObjectEncodeSize(object value, bool arrayEncoding)
		{
			if (arrayEncoding)
			{
				return 8;
			}
			return LongEncoding.GetEncodeSize(new long?((long)value));
		}
	}
}