using Microsoft.ServiceBus.Messaging.Amqp;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
	internal sealed class UIntEncoding : EncodingBase
	{
		public UIntEncoding() : base(112)
		{
		}

		public static uint? Decode(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
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
			Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode[] formatCodeArray = new Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode[] { 112, 82, 67 };
			EncodingBase.VerifyFormatCode(formatCode, offset, formatCodeArray);
			if (formatCode == 67)
			{
				return new uint?(0);
			}
			return new uint?((formatCode == 82 ? (uint)AmqpBitConverter.ReadUByte(buffer) : AmqpBitConverter.ReadUInt(buffer)));
		}

		public override object DecodeObject(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
		{
			return UIntEncoding.Decode(buffer, formatCode);
		}

		public static void Encode(uint? value, ByteBuffer buffer)
		{
			if (!value.HasValue)
			{
				AmqpEncoding.EncodeNull(buffer);
				return;
			}
			uint? nullable = value;
			if ((nullable.GetValueOrDefault() != 0 ? false : nullable.HasValue))
			{
				AmqpBitConverter.WriteUByte(buffer, 67);
				return;
			}
			if (value.Value > 255)
			{
				AmqpBitConverter.WriteUByte(buffer, 112);
				AmqpBitConverter.WriteUInt(buffer, value.Value);
				return;
			}
			AmqpBitConverter.WriteUByte(buffer, 82);
			AmqpBitConverter.WriteUByte(buffer, (byte)value.Value);
		}

		public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
		{
			if (arrayEncoding)
			{
				AmqpBitConverter.WriteUInt(buffer, (uint)value);
				return;
			}
			UIntEncoding.Encode(new uint?((uint)value), buffer);
		}

		public static int GetEncodeSize(uint? value)
		{
			if (!value.HasValue)
			{
				return 1;
			}
			if (value.Value == 0)
			{
				return 1;
			}
			if (value.Value > 255)
			{
				return 5;
			}
			return 2;
		}

		public override int GetObjectEncodeSize(object value, bool arrayEncoding)
		{
			if (arrayEncoding)
			{
				return 4;
			}
			return UIntEncoding.GetEncodeSize(new uint?((uint)value));
		}
	}
}