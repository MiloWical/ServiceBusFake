using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging.Amqp;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
	internal sealed class DescribedEncoding : EncodingBase
	{
		public DescribedEncoding() : base(0)
		{
		}

		public static DescribedType Decode(ByteBuffer buffer)
		{
			Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode = AmqpEncoding.ReadFormatCode(buffer);
			Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode1 = formatCode;
			if (formatCode == 64)
			{
				return null;
			}
			return DescribedEncoding.Decode(buffer, formatCode1);
		}

		private static DescribedType Decode(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
		{
			if (formatCode != 0)
			{
				throw AmqpEncoding.GetEncodingException(SRAmqp.AmqpInvalidFormatCode(formatCode, buffer.Offset));
			}
			object obj = AmqpEncoding.DecodeObject(buffer);
			return new DescribedType(obj, AmqpEncoding.DecodeObject(buffer));
		}

		public override object DecodeObject(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
		{
			if (formatCode == 0)
			{
				return DescribedEncoding.Decode(buffer, formatCode);
			}
			return AmqpEncoding.DecodeObject(buffer, formatCode);
		}

		public static void Encode(DescribedType value, ByteBuffer buffer)
		{
			if (value.Value == null)
			{
				AmqpEncoding.EncodeNull(buffer);
				return;
			}
			AmqpBitConverter.WriteUByte(buffer, 0);
			AmqpEncoding.EncodeObject(value.Descriptor, buffer);
			AmqpEncoding.EncodeObject(value.Value, buffer);
		}

		public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
		{
			if (!arrayEncoding)
			{
				DescribedEncoding.Encode((DescribedType)value, buffer);
				return;
			}
			object obj = ((DescribedType)value).Value;
			AmqpEncoding.GetEncoding(obj).EncodeObject(obj, true, buffer);
		}

		public static int GetEncodeSize(DescribedType value)
		{
			if (value == null)
			{
				return 1;
			}
			return 1 + AmqpEncoding.GetObjectEncodeSize(value.Descriptor) + AmqpEncoding.GetObjectEncodeSize(value.Value);
		}

		public override int GetObjectEncodeSize(object value, bool arrayEncoding)
		{
			if (!arrayEncoding)
			{
				return DescribedEncoding.GetEncodeSize((DescribedType)value);
			}
			object obj = ((DescribedType)value).Value;
			return AmqpEncoding.GetEncoding(obj).GetObjectEncodeSize(obj, true);
		}
	}
}