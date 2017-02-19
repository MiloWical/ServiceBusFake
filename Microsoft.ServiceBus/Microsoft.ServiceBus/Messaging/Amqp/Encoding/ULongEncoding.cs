using Microsoft.ServiceBus.Messaging.Amqp;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
	internal sealed class ULongEncoding : EncodingBase
	{
		public ULongEncoding() : base(128)
		{
		}

		public static ulong? Decode(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
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
			Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode[] formatCodeArray = new Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode[] { 128, 83, 68 };
			EncodingBase.VerifyFormatCode(formatCode, offset, formatCodeArray);
			if (formatCode == 68)
			{
				return new ulong?((ulong)0);
			}
			return new ulong?((formatCode == 83 ? (ulong)AmqpBitConverter.ReadUByte(buffer) : AmqpBitConverter.ReadULong(buffer)));
		}

		public override object DecodeObject(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
		{
			return ULongEncoding.Decode(buffer, formatCode);
		}

		public static void Encode(ulong? value, ByteBuffer buffer)
		{
			if (!value.HasValue)
			{
				AmqpEncoding.EncodeNull(buffer);
				return;
			}
			ulong? nullable = value;
			if ((nullable.GetValueOrDefault() != (long)0 ? false : nullable.HasValue))
			{
				AmqpBitConverter.WriteUByte(buffer, 68);
				return;
			}
			ulong? nullable1 = value;
			if ((nullable1.GetValueOrDefault() > (long)255 ? true : !nullable1.HasValue))
			{
				AmqpBitConverter.WriteUByte(buffer, 128);
				AmqpBitConverter.WriteULong(buffer, value.Value);
				return;
			}
			AmqpBitConverter.WriteUByte(buffer, 83);
			AmqpBitConverter.WriteUByte(buffer, (byte)value.Value);
		}

		public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
		{
			if (arrayEncoding)
			{
				AmqpBitConverter.WriteULong(buffer, (ulong)value);
				return;
			}
			ULongEncoding.Encode(new ulong?((ulong)value), buffer);
		}

		public static int GetEncodeSize(ulong? value)
		{
			if (!value.HasValue)
			{
				return 1;
			}
			if (value.Value == (long)0)
			{
				return 1;
			}
			if (value.Value > (long)255)
			{
				return 9;
			}
			return 2;
		}

		public override int GetObjectEncodeSize(object value, bool arrayEncoding)
		{
			if (arrayEncoding)
			{
				return 8;
			}
			return ULongEncoding.GetEncodeSize(new ulong?((ulong)value));
		}
	}
}