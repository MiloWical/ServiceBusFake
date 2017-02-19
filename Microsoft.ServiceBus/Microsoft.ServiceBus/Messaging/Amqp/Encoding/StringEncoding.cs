using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging.Amqp;
using System;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
	internal sealed class StringEncoding : EncodingBase
	{
		public StringEncoding() : base(177)
		{
		}

		public static string Decode(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
		{
			int num;
			System.Text.Encoding uTF8;
			if (formatCode == 0)
			{
				Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode1 = AmqpEncoding.ReadFormatCode(buffer);
				formatCode = formatCode1;
				if (formatCode1 == 64)
				{
					return null;
				}
			}
			if (formatCode != 161)
			{
				if (formatCode != 177)
				{
					throw AmqpEncoding.GetEncodingException(SRAmqp.AmqpInvalidFormatCode(formatCode, buffer.Offset));
				}
				num = (int)AmqpBitConverter.ReadUInt(buffer);
				uTF8 = System.Text.Encoding.UTF8;
			}
			else
			{
				num = AmqpBitConverter.ReadUByte(buffer);
				uTF8 = System.Text.Encoding.UTF8;
			}
			string str = uTF8.GetString(buffer.Buffer, buffer.Offset, num);
			buffer.Complete(num);
			return str;
		}

		public override object DecodeObject(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
		{
			return StringEncoding.Decode(buffer, formatCode);
		}

		public static void Encode(string value, ByteBuffer buffer)
		{
			if (value == null)
			{
				AmqpEncoding.EncodeNull(buffer);
				return;
			}
			byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value);
			int encodeWidthBySize = AmqpEncoding.GetEncodeWidthBySize((int)bytes.Length);
			AmqpBitConverter.WriteUByte(buffer, (byte)((encodeWidthBySize == 1 ? 161 : 177)));
			StringEncoding.Encode(bytes, encodeWidthBySize, buffer);
		}

		private static void Encode(byte[] encodedData, int width, ByteBuffer buffer)
		{
			if (width != 1)
			{
				AmqpBitConverter.WriteUInt(buffer, (uint)encodedData.Length);
			}
			else
			{
				AmqpBitConverter.WriteUByte(buffer, (byte)((int)encodedData.Length));
			}
			AmqpBitConverter.WriteBytes(buffer, encodedData, 0, (int)encodedData.Length);
		}

		public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
		{
			if (!arrayEncoding)
			{
				StringEncoding.Encode((string)value, buffer);
				return;
			}
			StringEncoding.Encode(System.Text.Encoding.UTF8.GetBytes((string)value), 4, buffer);
		}

		public static int GetEncodeSize(string value)
		{
			if (value == null)
			{
				return 1;
			}
			int byteCount = System.Text.Encoding.UTF8.GetByteCount(value);
			return 1 + AmqpEncoding.GetEncodeWidthBySize(byteCount) + byteCount;
		}

		public override int GetObjectEncodeSize(object value, bool arrayEncoding)
		{
			if (!arrayEncoding)
			{
				return StringEncoding.GetEncodeSize((string)value);
			}
			return 4 + System.Text.Encoding.UTF8.GetByteCount((string)value);
		}
	}
}