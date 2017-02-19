using Microsoft.ServiceBus.Messaging.Amqp;
using System;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
	internal sealed class SymbolEncoding : EncodingBase
	{
		public SymbolEncoding() : base(179)
		{
		}

		public static AmqpSymbol Decode(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
		{
			int num;
			if (formatCode == 0)
			{
				Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode1 = AmqpEncoding.ReadFormatCode(buffer);
				formatCode = formatCode1;
				if (formatCode1 == 64)
				{
					return new AmqpSymbol();
				}
			}
			AmqpEncoding.ReadCount(buffer, formatCode, 163, 179, out num);
			string str = System.Text.Encoding.ASCII.GetString(buffer.Buffer, buffer.Offset, num);
			buffer.Complete(num);
			return new AmqpSymbol(str);
		}

		public override object DecodeObject(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
		{
			return SymbolEncoding.Decode(buffer, formatCode);
		}

		public static void Encode(AmqpSymbol value, ByteBuffer buffer)
		{
			if (value.Value == null)
			{
				AmqpEncoding.EncodeNull(buffer);
				return;
			}
			byte[] bytes = System.Text.Encoding.ASCII.GetBytes(value.Value);
			int encodeWidthBySize = AmqpEncoding.GetEncodeWidthBySize((int)bytes.Length);
			AmqpBitConverter.WriteUByte(buffer, (byte)((encodeWidthBySize == 1 ? 163 : 179)));
			SymbolEncoding.Encode(bytes, encodeWidthBySize, buffer);
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
				SymbolEncoding.Encode((AmqpSymbol)value, buffer);
				return;
			}
			System.Text.Encoding aSCII = System.Text.Encoding.ASCII;
			AmqpSymbol amqpSymbol = (AmqpSymbol)value;
			SymbolEncoding.Encode(aSCII.GetBytes(amqpSymbol.Value), 4, buffer);
		}

		public static int GetEncodeSize(AmqpSymbol value)
		{
			if (value.Value == null)
			{
				return 1;
			}
			return 1 + AmqpEncoding.GetEncodeWidthBySize(value.ValueSize) + value.ValueSize;
		}

		public override int GetObjectEncodeSize(object value, bool arrayEncoding)
		{
			if (!arrayEncoding)
			{
				return SymbolEncoding.GetEncodeSize((AmqpSymbol)value);
			}
			return 4 + System.Text.Encoding.ASCII.GetByteCount(((AmqpSymbol)value).Value);
		}

		public static int GetValueSize(AmqpSymbol value)
		{
			if (value.Value == null)
			{
				return 0;
			}
			return System.Text.Encoding.ASCII.GetByteCount(value.Value);
		}
	}
}