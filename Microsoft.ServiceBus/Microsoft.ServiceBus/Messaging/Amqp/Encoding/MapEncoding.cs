using Microsoft.ServiceBus.Messaging.Amqp;
using System;
using System.Collections.Generic;

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
	internal sealed class MapEncoding : EncodingBase
	{
		public MapEncoding() : base(209)
		{
		}

		public static AmqpMap Decode(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
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
			AmqpEncoding.ReadSizeAndCount(buffer, formatCode, 193, 209, out num, out num1);
			AmqpMap amqpMaps = new AmqpMap();
			MapEncoding.ReadMapValue(buffer, amqpMaps, num, num1);
			return amqpMaps;
		}

		public override object DecodeObject(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
		{
			return MapEncoding.Decode(buffer, formatCode);
		}

		public static void Encode(AmqpMap value, ByteBuffer buffer)
		{
			if (value == null)
			{
				AmqpEncoding.EncodeNull(buffer);
				return;
			}
			int encodeWidth = MapEncoding.GetEncodeWidth(value);
			AmqpBitConverter.WriteUByte(buffer, (byte)((encodeWidth == 1 ? 193 : 209)));
			int valueSize = encodeWidth + value.ValueSize;
			MapEncoding.Encode(value, encodeWidth, valueSize, buffer);
		}

		private static void Encode(AmqpMap value, int width, int size, ByteBuffer buffer)
		{
			if (width != 1)
			{
				AmqpBitConverter.WriteUInt(buffer, (uint)size);
				AmqpBitConverter.WriteUInt(buffer, (uint)(value.Count * 2));
			}
			else
			{
				AmqpBitConverter.WriteUByte(buffer, (byte)size);
				AmqpBitConverter.WriteUByte(buffer, (byte)(value.Count * 2));
			}
			if (value.Count > 0)
			{
				foreach (KeyValuePair<MapKey, object> keyValuePair in (IEnumerable<KeyValuePair<MapKey, object>>)value)
				{
					AmqpEncoding.EncodeObject(keyValuePair.Key.Key, buffer);
					AmqpEncoding.EncodeObject(keyValuePair.Value, buffer);
				}
			}
		}

		public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
		{
			if (!arrayEncoding)
			{
				MapEncoding.Encode((AmqpMap)value, buffer);
				return;
			}
			AmqpMap amqpMaps = (AmqpMap)value;
			int valueSize = 4 + amqpMaps.ValueSize;
			MapEncoding.Encode(amqpMaps, 4, valueSize, buffer);
		}

		public static int GetEncodeSize(AmqpMap value)
		{
			if (value == null)
			{
				return 1;
			}
			return 1 + MapEncoding.GetEncodeWidth(value) * 2 + value.ValueSize;
		}

		private static int GetEncodeWidth(AmqpMap value)
		{
			return AmqpEncoding.GetEncodeWidthByCountAndSize(value.Count * 2, value.ValueSize);
		}

		public override int GetObjectEncodeSize(object value, bool arrayEncoding)
		{
			if (!arrayEncoding)
			{
				return MapEncoding.GetEncodeSize((AmqpMap)value);
			}
			return 8 + MapEncoding.GetValueSize((AmqpMap)value);
		}

		public static int GetValueSize(AmqpMap value)
		{
			int objectEncodeSize = 0;
			if (value.Count > 0)
			{
				foreach (KeyValuePair<MapKey, object> keyValuePair in (IEnumerable<KeyValuePair<MapKey, object>>)value)
				{
					MapKey key = keyValuePair.Key;
					objectEncodeSize = objectEncodeSize + AmqpEncoding.GetObjectEncodeSize(key.Key);
					objectEncodeSize = objectEncodeSize + AmqpEncoding.GetObjectEncodeSize(keyValuePair.Value);
				}
			}
			return objectEncodeSize;
		}

		public static void ReadMapValue(ByteBuffer buffer, AmqpMap map, int size, int count)
		{
			while (count > 0)
			{
				object obj = AmqpEncoding.DecodeObject(buffer);
				object obj1 = AmqpCodec.DecodeObject(buffer);
				map[new MapKey(obj)] = obj1;
				count = count - 2;
			}
		}
	}
}