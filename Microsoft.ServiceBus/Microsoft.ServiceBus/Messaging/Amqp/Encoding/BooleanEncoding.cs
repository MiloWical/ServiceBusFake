using Microsoft.ServiceBus.Messaging.Amqp;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
	internal sealed class BooleanEncoding : EncodingBase
	{
		public BooleanEncoding() : base(86)
		{
		}

		public static bool? Decode(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
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
			Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode[] formatCodeArray = new Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode[] { 86, 66, 65 };
			EncodingBase.VerifyFormatCode(formatCode, offset, formatCodeArray);
			if (formatCode == 86)
			{
				return new bool?(AmqpBitConverter.ReadUByte(buffer) != 0);
			}
			return new bool?((formatCode == 65 ? true : false));
		}

		public override object DecodeObject(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
		{
			return BooleanEncoding.Decode(buffer, formatCode);
		}

		public static void Encode(bool? value, ByteBuffer buffer)
		{
			if (!value.HasValue)
			{
				AmqpEncoding.EncodeNull(buffer);
				return;
			}
			AmqpBitConverter.WriteUByte(buffer, (byte)((value.Value ? 65 : 66)));
		}

		public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
		{
			object obj;
			if (!arrayEncoding)
			{
				BooleanEncoding.Encode(new bool?((bool)value), buffer);
				return;
			}
			ByteBuffer byteBuffer = buffer;
			if ((bool)value)
			{
				obj = 1;
			}
			else
			{
				obj = null;
			}
			AmqpBitConverter.WriteUByte(byteBuffer, (byte)obj);
		}

		public static int GetEncodeSize(bool? value)
		{
			if (!value.HasValue)
			{
				return 1;
			}
			return 1;
		}

		public override int GetObjectEncodeSize(object value, bool arrayEncoding)
		{
			if (arrayEncoding)
			{
				return 1;
			}
			return BooleanEncoding.GetEncodeSize(new bool?((bool)value));
		}
	}
}