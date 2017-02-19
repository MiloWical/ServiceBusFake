using Microsoft.ServiceBus.Messaging.Amqp;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
	internal sealed class UByteEncoding : EncodingBase
	{
		public UByteEncoding() : base(80)
		{
		}

		public static byte? Decode(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
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
			return new byte?(AmqpBitConverter.ReadUByte(buffer));
		}

		public override object DecodeObject(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
		{
			return UByteEncoding.Decode(buffer, formatCode);
		}

		public static void Encode(byte? value, ByteBuffer buffer)
		{
			if (!value.HasValue)
			{
				AmqpEncoding.EncodeNull(buffer);
				return;
			}
			AmqpBitConverter.WriteUByte(buffer, 80);
			AmqpBitConverter.WriteUByte(buffer, value.Value);
		}

		public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
		{
			if (arrayEncoding)
			{
				AmqpBitConverter.WriteUByte(buffer, (byte)value);
				return;
			}
			UByteEncoding.Encode(new byte?((byte)value), buffer);
		}

		public static int GetEncodeSize(byte? value)
		{
			if (!value.HasValue)
			{
				return 1;
			}
			return 2;
		}

		public override int GetObjectEncodeSize(object value, bool arrayEncoding)
		{
			if (arrayEncoding)
			{
				return 1;
			}
			return UByteEncoding.GetEncodeSize(new byte?((byte)value));
		}
	}
}