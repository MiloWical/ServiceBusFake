using Microsoft.ServiceBus.Messaging.Amqp;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
	internal sealed class ShortEncoding : EncodingBase
	{
		public ShortEncoding() : base(97)
		{
		}

		public static short? Decode(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
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
			return new short?(AmqpBitConverter.ReadShort(buffer));
		}

		public override object DecodeObject(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
		{
			return ShortEncoding.Decode(buffer, formatCode);
		}

		public static void Encode(short? value, ByteBuffer buffer)
		{
			if (!value.HasValue)
			{
				AmqpEncoding.EncodeNull(buffer);
				return;
			}
			AmqpBitConverter.WriteUByte(buffer, 97);
			AmqpBitConverter.WriteShort(buffer, value.Value);
		}

		public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
		{
			if (arrayEncoding)
			{
				AmqpBitConverter.WriteShort(buffer, (short)value);
				return;
			}
			ShortEncoding.Encode(new short?((short)value), buffer);
		}

		public static int GetEncodeSize(short? value)
		{
			if (!value.HasValue)
			{
				return 1;
			}
			return 3;
		}

		public override int GetObjectEncodeSize(object value, bool arrayEncoding)
		{
			if (arrayEncoding)
			{
				return 2;
			}
			return ShortEncoding.GetEncodeSize(new short?((short)value));
		}
	}
}