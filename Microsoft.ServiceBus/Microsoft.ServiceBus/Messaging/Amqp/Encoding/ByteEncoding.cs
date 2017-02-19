using Microsoft.ServiceBus.Messaging.Amqp;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
	internal sealed class ByteEncoding : EncodingBase
	{
		public ByteEncoding() : base(81)
		{
		}

		public static sbyte? Decode(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
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
			return new sbyte?(AmqpBitConverter.ReadByte(buffer));
		}

		public override object DecodeObject(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
		{
			return ByteEncoding.Decode(buffer, formatCode);
		}

		public static void Encode(sbyte? value, ByteBuffer buffer)
		{
			if (!value.HasValue)
			{
				AmqpEncoding.EncodeNull(buffer);
				return;
			}
			AmqpBitConverter.WriteUByte(buffer, 81);
			AmqpBitConverter.WriteByte(buffer, value.Value);
		}

		public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
		{
			if (arrayEncoding)
			{
				AmqpBitConverter.WriteByte(buffer, (sbyte)value);
				return;
			}
			ByteEncoding.Encode(new sbyte?((sbyte)value), buffer);
		}

		public static int GetEncodeSize(sbyte? value)
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
			return ByteEncoding.GetEncodeSize(new sbyte?((sbyte)value));
		}
	}
}