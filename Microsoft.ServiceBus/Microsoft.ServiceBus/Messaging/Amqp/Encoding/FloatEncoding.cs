using Microsoft.ServiceBus.Messaging.Amqp;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
	internal sealed class FloatEncoding : EncodingBase
	{
		public FloatEncoding() : base(114)
		{
		}

		public static float? Decode(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
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
			return new float?(AmqpBitConverter.ReadFloat(buffer));
		}

		public override object DecodeObject(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
		{
			return FloatEncoding.Decode(buffer, formatCode);
		}

		public static void Encode(float? value, ByteBuffer buffer)
		{
			if (!value.HasValue)
			{
				AmqpEncoding.EncodeNull(buffer);
				return;
			}
			AmqpBitConverter.WriteUByte(buffer, 114);
			AmqpBitConverter.WriteFloat(buffer, value.Value);
		}

		public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
		{
			if (arrayEncoding)
			{
				AmqpBitConverter.WriteFloat(buffer, (float)value);
				return;
			}
			FloatEncoding.Encode(new float?((float)value), buffer);
		}

		public static int GetEncodeSize(float? value)
		{
			if (!value.HasValue)
			{
				return 1;
			}
			return 5;
		}

		public override int GetObjectEncodeSize(object value, bool arrayEncoding)
		{
			if (arrayEncoding)
			{
				return 4;
			}
			return FloatEncoding.GetEncodeSize(new float?((float)value));
		}
	}
}