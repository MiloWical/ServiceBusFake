using Microsoft.ServiceBus.Messaging.Amqp;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
	internal sealed class UuidEncoding : EncodingBase
	{
		public UuidEncoding() : base(152)
		{
		}

		public static Guid? Decode(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
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
			return new Guid?(AmqpBitConverter.ReadUuid(buffer));
		}

		public override object DecodeObject(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
		{
			return UuidEncoding.Decode(buffer, formatCode);
		}

		public static void Encode(Guid? value, ByteBuffer buffer)
		{
			if (!value.HasValue)
			{
				AmqpEncoding.EncodeNull(buffer);
				return;
			}
			AmqpBitConverter.WriteUByte(buffer, 152);
			AmqpBitConverter.WriteUuid(buffer, value.Value);
		}

		public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
		{
			if (arrayEncoding)
			{
				AmqpBitConverter.WriteUuid(buffer, (Guid)value);
				return;
			}
			UuidEncoding.Encode(new Guid?((Guid)value), buffer);
		}

		public static int GetEncodeSize(Guid? value)
		{
			if (!value.HasValue)
			{
				return 1;
			}
			return 17;
		}

		public override int GetObjectEncodeSize(object value, bool arrayEncoding)
		{
			if (arrayEncoding)
			{
				return 16;
			}
			return UuidEncoding.GetEncodeSize(new Guid?((Guid)value));
		}
	}
}