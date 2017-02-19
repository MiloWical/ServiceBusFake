using Microsoft.ServiceBus.Messaging.Amqp;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
	internal sealed class ListEncoding : EncodingBase
	{
		public ListEncoding() : base(208)
		{
		}

		public static IList Decode(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
		{
			int num;
			int num1;
			if (formatCode == 0)
			{
				Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode1 = AmqpEncoding.ReadFormatCode(buffer);
				formatCode = formatCode1;
				if (formatCode1 == 64)
				{
					return null;
				}
			}
			IList objs = new List<object>();
			if (formatCode == 69)
			{
				return objs;
			}
			AmqpEncoding.ReadSizeAndCount(buffer, formatCode, 192, 208, out num, out num1);
			while (num1 > 0)
			{
				objs.Add(AmqpEncoding.DecodeObject(buffer));
				num1--;
			}
			return objs;
		}

		public override object DecodeObject(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
		{
			return ListEncoding.Decode(buffer, formatCode);
		}

		public static void Encode(IList value, ByteBuffer buffer)
		{
			if (value == null)
			{
				AmqpEncoding.EncodeNull(buffer);
				return;
			}
			if (value.Count == 0)
			{
				AmqpBitConverter.WriteUByte(buffer, 69);
				return;
			}
			int valueSize = ListEncoding.GetValueSize(value);
			int encodeWidthByCountAndSize = AmqpEncoding.GetEncodeWidthByCountAndSize(value.Count, valueSize);
			AmqpBitConverter.WriteUByte(buffer, (byte)((encodeWidthByCountAndSize == 1 ? 192 : 208)));
			ListEncoding.Encode(value, encodeWidthByCountAndSize, encodeWidthByCountAndSize + valueSize, buffer);
		}

		private static void Encode(IList value, int width, int size, ByteBuffer buffer)
		{
			if (width != 1)
			{
				AmqpBitConverter.WriteUInt(buffer, (uint)size);
				AmqpBitConverter.WriteUInt(buffer, (uint)value.Count);
			}
			else
			{
				AmqpBitConverter.WriteUByte(buffer, (byte)size);
				AmqpBitConverter.WriteUByte(buffer, (byte)value.Count);
			}
			if (value.Count > 0)
			{
				foreach (object obj in value)
				{
					AmqpEncoding.EncodeObject(obj, buffer);
				}
			}
		}

		public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
		{
			if (!arrayEncoding)
			{
				ListEncoding.Encode((IList)value, buffer);
				return;
			}
			IList lists = (IList)value;
			int valueSize = 4 + ListEncoding.GetValueSize(lists);
			ListEncoding.Encode(lists, 4, valueSize, buffer);
		}

		public static int GetEncodeSize(IList value)
		{
			if (value == null)
			{
				return 1;
			}
			if (value.Count == 0)
			{
				return 1;
			}
			int valueSize = ListEncoding.GetValueSize(value);
			int encodeWidthByCountAndSize = AmqpEncoding.GetEncodeWidthByCountAndSize(value.Count, valueSize);
			return 1 + encodeWidthByCountAndSize * 2 + valueSize;
		}

		public override int GetObjectEncodeSize(object value, bool arrayEncoding)
		{
			if (!arrayEncoding)
			{
				return ListEncoding.GetEncodeSize((IList)value);
			}
			return 8 + ListEncoding.GetValueSize((IList)value);
		}

		public static int GetValueSize(IList value)
		{
			int objectEncodeSize = 0;
			if (value.Count > 0)
			{
				foreach (object obj in value)
				{
					objectEncodeSize = objectEncodeSize + AmqpEncoding.GetObjectEncodeSize(obj);
				}
			}
			return objectEncodeSize;
		}
	}
}