using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging.Amqp;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
	internal static class AmqpEncoding
	{
		private static Dictionary<Type, EncodingBase> encodingsByType;

		private static Dictionary<FormatCode, EncodingBase> encodingsByCode;

		private static BooleanEncoding booleanEncoding;

		private static UByteEncoding ubyteEncoding;

		private static UShortEncoding ushortEncoding;

		private static UIntEncoding uintEncoding;

		private static ULongEncoding ulongEncoding;

		private static ByteEncoding byteEncoding;

		private static ShortEncoding shortEncoding;

		private static IntEncoding intEncoding;

		private static LongEncoding longEncoding;

		private static FloatEncoding floatEncoding;

		private static DoubleEncoding doubleEncoding;

		private static DecimalEncoding decimal128Encoding;

		private static CharEncoding charEncoding;

		private static TimeStampEncoding timeStampEncoding;

		private static UuidEncoding uuidEncoding;

		private static BinaryEncoding binaryEncoding;

		private static SymbolEncoding symbolEncoding;

		private static StringEncoding stringEncoding;

		private static ListEncoding listEncoding;

		private static MapEncoding mapEncoding;

		private static ArrayEncoding arrayEncoding;

		private static DescribedEncoding describedTypeEncoding;

		static AmqpEncoding()
		{
			AmqpEncoding.booleanEncoding = new BooleanEncoding();
			AmqpEncoding.ubyteEncoding = new UByteEncoding();
			AmqpEncoding.ushortEncoding = new UShortEncoding();
			AmqpEncoding.uintEncoding = new UIntEncoding();
			AmqpEncoding.ulongEncoding = new ULongEncoding();
			AmqpEncoding.byteEncoding = new ByteEncoding();
			AmqpEncoding.shortEncoding = new ShortEncoding();
			AmqpEncoding.intEncoding = new IntEncoding();
			AmqpEncoding.longEncoding = new LongEncoding();
			AmqpEncoding.floatEncoding = new FloatEncoding();
			AmqpEncoding.doubleEncoding = new DoubleEncoding();
			AmqpEncoding.decimal128Encoding = new DecimalEncoding();
			AmqpEncoding.charEncoding = new CharEncoding();
			AmqpEncoding.timeStampEncoding = new TimeStampEncoding();
			AmqpEncoding.uuidEncoding = new UuidEncoding();
			AmqpEncoding.binaryEncoding = new BinaryEncoding();
			AmqpEncoding.symbolEncoding = new SymbolEncoding();
			AmqpEncoding.stringEncoding = new StringEncoding();
			AmqpEncoding.listEncoding = new ListEncoding();
			AmqpEncoding.mapEncoding = new MapEncoding();
			AmqpEncoding.arrayEncoding = new ArrayEncoding();
			AmqpEncoding.describedTypeEncoding = new DescribedEncoding();
			Dictionary<Type, EncodingBase> types = new Dictionary<Type, EncodingBase>()
			{
				{ typeof(bool), AmqpEncoding.booleanEncoding },
				{ typeof(byte), AmqpEncoding.ubyteEncoding },
				{ typeof(ushort), AmqpEncoding.ushortEncoding },
				{ typeof(uint), AmqpEncoding.uintEncoding },
				{ typeof(ulong), AmqpEncoding.ulongEncoding },
				{ typeof(sbyte), AmqpEncoding.byteEncoding },
				{ typeof(short), AmqpEncoding.shortEncoding },
				{ typeof(int), AmqpEncoding.intEncoding },
				{ typeof(long), AmqpEncoding.longEncoding },
				{ typeof(float), AmqpEncoding.floatEncoding },
				{ typeof(double), AmqpEncoding.doubleEncoding },
				{ typeof(decimal), AmqpEncoding.decimal128Encoding },
				{ typeof(char), AmqpEncoding.charEncoding },
				{ typeof(DateTime), AmqpEncoding.timeStampEncoding },
				{ typeof(Guid), AmqpEncoding.uuidEncoding },
				{ typeof(ArraySegment<byte>), AmqpEncoding.binaryEncoding },
				{ typeof(AmqpSymbol), AmqpEncoding.symbolEncoding },
				{ typeof(string), AmqpEncoding.stringEncoding },
				{ typeof(AmqpMap), AmqpEncoding.mapEncoding }
			};
			AmqpEncoding.encodingsByType = types;
			Dictionary<FormatCode, EncodingBase> formatCodes = new Dictionary<FormatCode, EncodingBase>()
			{
				{ 66, AmqpEncoding.booleanEncoding },
				{ 65, AmqpEncoding.booleanEncoding },
				{ 86, AmqpEncoding.booleanEncoding },
				{ 80, AmqpEncoding.ubyteEncoding },
				{ 96, AmqpEncoding.ushortEncoding },
				{ 112, AmqpEncoding.uintEncoding },
				{ 82, AmqpEncoding.uintEncoding },
				{ 67, AmqpEncoding.uintEncoding },
				{ 128, AmqpEncoding.ulongEncoding },
				{ 83, AmqpEncoding.ulongEncoding },
				{ 68, AmqpEncoding.ulongEncoding },
				{ 81, AmqpEncoding.byteEncoding },
				{ 97, AmqpEncoding.shortEncoding },
				{ 113, AmqpEncoding.intEncoding },
				{ 84, AmqpEncoding.intEncoding },
				{ 129, AmqpEncoding.longEncoding },
				{ 85, AmqpEncoding.longEncoding },
				{ 114, AmqpEncoding.floatEncoding },
				{ 130, AmqpEncoding.doubleEncoding },
				{ 148, AmqpEncoding.decimal128Encoding },
				{ 115, AmqpEncoding.charEncoding },
				{ 131, AmqpEncoding.timeStampEncoding },
				{ 152, AmqpEncoding.uuidEncoding },
				{ 160, AmqpEncoding.binaryEncoding },
				{ 176, AmqpEncoding.binaryEncoding },
				{ 163, AmqpEncoding.symbolEncoding },
				{ 179, AmqpEncoding.symbolEncoding },
				{ 161, AmqpEncoding.stringEncoding },
				{ 177, AmqpEncoding.stringEncoding },
				{ 69, AmqpEncoding.listEncoding },
				{ 192, AmqpEncoding.listEncoding },
				{ 208, AmqpEncoding.listEncoding },
				{ 193, AmqpEncoding.mapEncoding },
				{ 209, AmqpEncoding.mapEncoding },
				{ 224, AmqpEncoding.arrayEncoding },
				{ 240, AmqpEncoding.arrayEncoding },
				{ 0, AmqpEncoding.describedTypeEncoding }
			};
			AmqpEncoding.encodingsByCode = formatCodes;
		}

		public static object DecodeObject(ByteBuffer buffer)
		{
			FormatCode formatCode = AmqpEncoding.ReadFormatCode(buffer);
			if (formatCode == 64)
			{
				return null;
			}
			return AmqpEncoding.DecodeObject(buffer, formatCode);
		}

		public static object DecodeObject(ByteBuffer buffer, FormatCode formatCode)
		{
			EncodingBase encodingBase;
			if (!AmqpEncoding.encodingsByCode.TryGetValue(formatCode, out encodingBase))
			{
				throw AmqpEncoding.GetEncodingException(SRAmqp.AmqpInvalidFormatCode(formatCode, buffer.Offset));
			}
			return encodingBase.DecodeObject(buffer, formatCode);
		}

		public static void EncodeNull(ByteBuffer buffer)
		{
			AmqpBitConverter.WriteUByte(buffer, 64);
		}

		public static void EncodeObject(object value, ByteBuffer buffer)
		{
			if (value == null)
			{
				AmqpEncoding.EncodeNull(buffer);
				return;
			}
			IAmqpSerializable amqpSerializable = value as IAmqpSerializable;
			if (amqpSerializable != null)
			{
				amqpSerializable.Encode(buffer);
				return;
			}
			AmqpEncoding.GetEncoding(value).EncodeObject(value, false, buffer);
		}

		public static int GetEncodeWidthByCountAndSize(int count, int valueSize)
		{
			if (count < 255 && valueSize < 255)
			{
				return 1;
			}
			return 4;
		}

		public static int GetEncodeWidthBySize(int size)
		{
			if (size > 255)
			{
				return 4;
			}
			return 1;
		}

		public static EncodingBase GetEncoding(object value)
		{
			EncodingBase encodingBase = null;
			Type type = value.GetType();
			if (AmqpEncoding.encodingsByType.TryGetValue(type, out encodingBase))
			{
				return encodingBase;
			}
			if (type.IsArray)
			{
				return AmqpEncoding.arrayEncoding;
			}
			if (value is IList)
			{
				return AmqpEncoding.listEncoding;
			}
			if (!(value is DescribedType))
			{
				throw AmqpEncoding.GetEncodingException(SRAmqp.AmqpInvalidType(type.ToString()));
			}
			return AmqpEncoding.describedTypeEncoding;
		}

		public static EncodingBase GetEncoding(Type type)
		{
			EncodingBase encodingBase = null;
			if (AmqpEncoding.encodingsByType.TryGetValue(type, out encodingBase))
			{
				return encodingBase;
			}
			if (type.IsArray)
			{
				return AmqpEncoding.arrayEncoding;
			}
			if (typeof(IList).IsAssignableFrom(type))
			{
				return AmqpEncoding.listEncoding;
			}
			if (!typeof(DescribedType).IsAssignableFrom(type))
			{
				throw AmqpEncoding.GetEncodingException(SRAmqp.AmqpInvalidType(type.ToString()));
			}
			return AmqpEncoding.describedTypeEncoding;
		}

		public static EncodingBase GetEncoding(FormatCode formatCode)
		{
			EncodingBase encodingBase;
			if (AmqpEncoding.encodingsByCode.TryGetValue(formatCode, out encodingBase))
			{
				return encodingBase;
			}
			return null;
		}

		public static AmqpException GetEncodingException(string message)
		{
			return new AmqpException(AmqpError.InvalidField, message);
		}

		public static int GetObjectEncodeSize(object value)
		{
			if (value == null)
			{
				return 1;
			}
			IAmqpSerializable amqpSerializable = value as IAmqpSerializable;
			if (amqpSerializable != null)
			{
				return amqpSerializable.EncodeSize;
			}
			return AmqpEncoding.GetEncoding(value).GetObjectEncodeSize(value, false);
		}

		public static void ReadCount(ByteBuffer buffer, FormatCode formatCode, FormatCode formatCode8, FormatCode formatCode32, out int count)
		{
			if (formatCode == formatCode8)
			{
				count = AmqpBitConverter.ReadUByte(buffer);
				return;
			}
			if (formatCode != formatCode32)
			{
				throw AmqpEncoding.GetEncodingException(SRAmqp.AmqpInvalidFormatCode(formatCode, buffer.Offset));
			}
			count = (int)AmqpBitConverter.ReadUInt(buffer);
		}

		public static FormatCode ReadFormatCode(ByteBuffer buffer)
		{
			byte num = AmqpBitConverter.ReadUByte(buffer);
			byte num1 = 0;
			if (FormatCode.HasExtType(num))
			{
				num1 = AmqpBitConverter.ReadUByte(buffer);
			}
			return new FormatCode(num, num1);
		}

		public static void ReadSizeAndCount(ByteBuffer buffer, FormatCode formatCode, FormatCode formatCode8, FormatCode formatCode32, out int size, out int count)
		{
			if (formatCode == formatCode8)
			{
				size = AmqpBitConverter.ReadUByte(buffer);
				count = AmqpBitConverter.ReadUByte(buffer);
				return;
			}
			if (formatCode != formatCode32)
			{
				throw AmqpEncoding.GetEncodingException(SRAmqp.AmqpInvalidFormatCode(formatCode, buffer.Offset));
			}
			size = (int)AmqpBitConverter.ReadUInt(buffer);
			count = (int)AmqpBitConverter.ReadUInt(buffer);
		}
	}
}