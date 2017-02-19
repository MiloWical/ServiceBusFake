using Microsoft.ServiceBus.Messaging.Amqp;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
	internal sealed class DoubleEncoding : EncodingBase
	{
		public DoubleEncoding() : base(130)
		{
		}

		public static double? Decode(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
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
			return new double?(AmqpBitConverter.ReadDouble(buffer));
		}

		public override object DecodeObject(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
		{
			return DoubleEncoding.Decode(buffer, formatCode);
		}

		public static void Encode(double? value, ByteBuffer buffer)
		{
			if (!value.HasValue)
			{
				AmqpEncoding.EncodeNull(buffer);
				return;
			}
			AmqpBitConverter.WriteUByte(buffer, 130);
			AmqpBitConverter.WriteDouble(buffer, value.Value);
		}

		public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
		{
			if (arrayEncoding)
			{
				AmqpBitConverter.WriteDouble(buffer, (double)value);
				return;
			}
			DoubleEncoding.Encode(new double?((double)value), buffer);
		}

		public static int GetEncodeSize(double? value)
		{
			if (!value.HasValue)
			{
				return 1;
			}
			return 9;
		}

		public override int GetObjectEncodeSize(object value, bool arrayEncoding)
		{
			if (arrayEncoding)
			{
				return 8;
			}
			return DoubleEncoding.GetEncodeSize(new double?((double)value));
		}
	}
}