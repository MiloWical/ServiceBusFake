using Microsoft.ServiceBus.Messaging.Amqp;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
	internal sealed class UShortEncoding : EncodingBase
	{
		public UShortEncoding() : base(96)
		{
		}

		public static ushort? Decode(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
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
			return new ushort?(AmqpBitConverter.ReadUShort(buffer));
		}

		public override object DecodeObject(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
		{
			return UShortEncoding.Decode(buffer, formatCode);
		}

		public static void Encode(ushort? value, ByteBuffer buffer)
		{
			if (!value.HasValue)
			{
				AmqpEncoding.EncodeNull(buffer);
				return;
			}
			AmqpBitConverter.WriteUByte(buffer, 96);
			AmqpBitConverter.WriteUShort(buffer, value.Value);
		}

		public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
		{
			if (arrayEncoding)
			{
				AmqpBitConverter.WriteUShort(buffer, (ushort)value);
				return;
			}
			UShortEncoding.Encode(new ushort?((ushort)value), buffer);
		}

		public static int GetEncodeSize(ushort? value)
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
			return UShortEncoding.GetEncodeSize(new ushort?((ushort)value));
		}
	}
}