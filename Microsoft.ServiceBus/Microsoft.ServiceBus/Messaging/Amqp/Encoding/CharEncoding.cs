using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging.Amqp;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
	internal sealed class CharEncoding : EncodingBase
	{
		public CharEncoding() : base(115)
		{
		}

		public static char? Decode(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
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
			string str = char.ConvertFromUtf32(AmqpBitConverter.ReadInt(buffer));
			if (str.Length > 1)
			{
				throw new ArgumentOutOfRangeException(SRClient.ErroConvertingToChar);
			}
			return new char?(str[0]);
		}

		public override object DecodeObject(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
		{
			return CharEncoding.Decode(buffer, formatCode);
		}

		public static void Encode(char? value, ByteBuffer buffer)
		{
			if (!value.HasValue)
			{
				AmqpEncoding.EncodeNull(buffer);
				return;
			}
			AmqpBitConverter.WriteUByte(buffer, 115);
			AmqpBitConverter.WriteInt(buffer, char.ConvertToUtf32(new string(value.Value, 1), 0));
		}

		public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
		{
			if (!arrayEncoding)
			{
				CharEncoding.Encode(new char?((char)value), buffer);
				return;
			}
			AmqpBitConverter.WriteInt(buffer, char.ConvertToUtf32(new string((char)value, 1), 0));
		}

		public static int GetEncodeSize(char? value)
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
			return CharEncoding.GetEncodeSize(new char?((char)value));
		}
	}
}