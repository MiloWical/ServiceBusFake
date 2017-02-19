using Microsoft.ServiceBus.Messaging.Amqp;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
	internal sealed class IntEncoding : EncodingBase
	{
		public IntEncoding() : base(113)
		{
		}

		public static int? Decode(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
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
			Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode[] formatCodeArray = new Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode[] { 113, 84 };
			EncodingBase.VerifyFormatCode(formatCode, offset, formatCodeArray);
			return new int?((formatCode == 84 ? (int)AmqpBitConverter.ReadByte(buffer) : AmqpBitConverter.ReadInt(buffer)));
		}

		public override object DecodeObject(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
		{
			return IntEncoding.Decode(buffer, formatCode);
		}

		public static void Encode(int? value, ByteBuffer buffer)
		{
			if (!value.HasValue)
			{
				AmqpEncoding.EncodeNull(buffer);
				return;
			}
			int? nullable = value;
			if ((nullable.GetValueOrDefault() >= -128 ? true : !nullable.HasValue))
			{
				int? nullable1 = value;
				if ((nullable1.GetValueOrDefault() <= 127 ? true : !nullable1.HasValue))
				{
					AmqpBitConverter.WriteUByte(buffer, 84);
					AmqpBitConverter.WriteByte(buffer, (sbyte)value.Value);
					return;
				}
			}
			AmqpBitConverter.WriteUByte(buffer, 113);
			AmqpBitConverter.WriteInt(buffer, value.Value);
		}

		public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
		{
			if (arrayEncoding)
			{
				AmqpBitConverter.WriteInt(buffer, (int)value);
				return;
			}
			IntEncoding.Encode(new int?((int)value), buffer);
		}

		public static int GetEncodeSize(int? value)
		{
			if (!value.HasValue)
			{
				return 1;
			}
			if (value.Value >= -128 && value.Value <= 127)
			{
				return 2;
			}
			return 5;
		}

		public override int GetObjectEncodeSize(object value, bool arrayEncoding)
		{
			if (arrayEncoding)
			{
				return 4;
			}
			return IntEncoding.GetEncodeSize(new int?((int)value));
		}
	}
}