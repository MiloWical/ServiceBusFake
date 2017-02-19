using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using Microsoft.ServiceBus.Messaging.Amqp.Sasl;
using Microsoft.ServiceBus.Messaging.Amqp.Transaction;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal static class AmqpCodec
	{
		private static Dictionary<string, Func<AmqpDescribed>> knownTypesByName;

		private static Dictionary<ulong, Func<AmqpDescribed>> knownTypesByCode;

		public static int MinimumFrameDecodeSize
		{
			get
			{
				return 8;
			}
		}

		static AmqpCodec()
		{
			Dictionary<string, Func<AmqpDescribed>> strs = new Dictionary<string, Func<AmqpDescribed>>()
			{
				{ Open.Name, new Func<AmqpDescribed>(() => new Open()) },
				{ Close.Name, new Func<AmqpDescribed>(() => new Close()) },
				{ Begin.Name, new Func<AmqpDescribed>(() => new Begin()) },
				{ End.Name, new Func<AmqpDescribed>(() => new End()) },
				{ Attach.Name, new Func<AmqpDescribed>(() => new Attach()) },
				{ Detach.Name, new Func<AmqpDescribed>(() => new Detach()) },
				{ Transfer.Name, new Func<AmqpDescribed>(() => new Transfer()) },
				{ Disposition.Name, new Func<AmqpDescribed>(() => new Disposition()) },
				{ Flow.Name, new Func<AmqpDescribed>(() => new Flow()) },
				{ Coordinator.Name, new Func<AmqpDescribed>(() => new Coordinator()) },
				{ Declare.Name, new Func<AmqpDescribed>(() => new Declare()) },
				{ Declared.Name, new Func<AmqpDescribed>(() => new Declared()) },
				{ Discharge.Name, new Func<AmqpDescribed>(() => new Discharge()) },
				{ TransactionalState.Name, new Func<AmqpDescribed>(() => new TransactionalState()) },
				{ SaslMechanisms.Name, new Func<AmqpDescribed>(() => new SaslMechanisms()) },
				{ SaslInit.Name, new Func<AmqpDescribed>(() => new SaslInit()) },
				{ SaslChallenge.Name, new Func<AmqpDescribed>(() => new SaslChallenge()) },
				{ SaslResponse.Name, new Func<AmqpDescribed>(() => new SaslResponse()) },
				{ SaslOutcome.Name, new Func<AmqpDescribed>(() => new SaslOutcome()) },
				{ Error.Name, new Func<AmqpDescribed>(() => new Error()) },
				{ Source.Name, new Func<AmqpDescribed>(() => new Source()) },
				{ Target.Name, new Func<AmqpDescribed>(() => new Target()) },
				{ Received.Name, new Func<AmqpDescribed>(() => new Received()) },
				{ Accepted.Name, new Func<AmqpDescribed>(() => new Accepted()) },
				{ Released.Name, new Func<AmqpDescribed>(() => new Released()) },
				{ Rejected.Name, new Func<AmqpDescribed>(() => new Rejected()) },
				{ Modified.Name, new Func<AmqpDescribed>(() => new Modified()) },
				{ DeleteOnClose.Name, new Func<AmqpDescribed>(() => new DeleteOnClose()) },
				{ DeleteOnNoLinks.Name, new Func<AmqpDescribed>(() => new DeleteOnNoLinks()) },
				{ DeleteOnNoMessages.Name, new Func<AmqpDescribed>(() => new DeleteOnNoMessages()) },
				{ DeleteOnNoLinksOrMessages.Name, new Func<AmqpDescribed>(() => new DeleteOnNoLinksOrMessages()) }
			};
			AmqpCodec.knownTypesByName = strs;
			Dictionary<ulong, Func<AmqpDescribed>> nums = new Dictionary<ulong, Func<AmqpDescribed>>()
			{
				{ Open.Code, new Func<AmqpDescribed>(() => new Open()) },
				{ Close.Code, new Func<AmqpDescribed>(() => new Close()) },
				{ Begin.Code, new Func<AmqpDescribed>(() => new Begin()) },
				{ End.Code, new Func<AmqpDescribed>(() => new End()) },
				{ Attach.Code, new Func<AmqpDescribed>(() => new Attach()) },
				{ Detach.Code, new Func<AmqpDescribed>(() => new Detach()) },
				{ Transfer.Code, new Func<AmqpDescribed>(() => new Transfer()) },
				{ Disposition.Code, new Func<AmqpDescribed>(() => new Disposition()) },
				{ Flow.Code, new Func<AmqpDescribed>(() => new Flow()) },
				{ Coordinator.Code, new Func<AmqpDescribed>(() => new Coordinator()) },
				{ Declare.Code, new Func<AmqpDescribed>(() => new Declare()) },
				{ Discharge.Code, new Func<AmqpDescribed>(() => new Discharge()) },
				{ Declared.Code, new Func<AmqpDescribed>(() => new Declared()) },
				{ TransactionalState.Code, new Func<AmqpDescribed>(() => new TransactionalState()) },
				{ SaslMechanisms.Code, new Func<AmqpDescribed>(() => new SaslMechanisms()) },
				{ SaslInit.Code, new Func<AmqpDescribed>(() => new SaslInit()) },
				{ SaslChallenge.Code, new Func<AmqpDescribed>(() => new SaslChallenge()) },
				{ SaslResponse.Code, new Func<AmqpDescribed>(() => new SaslResponse()) },
				{ SaslOutcome.Code, new Func<AmqpDescribed>(() => new SaslOutcome()) },
				{ Error.Code, new Func<AmqpDescribed>(() => new Error()) },
				{ Source.Code, new Func<AmqpDescribed>(() => new Source()) },
				{ Target.Code, new Func<AmqpDescribed>(() => new Target()) },
				{ Received.Code, new Func<AmqpDescribed>(() => new Received()) },
				{ Accepted.Code, new Func<AmqpDescribed>(() => new Accepted()) },
				{ Released.Code, new Func<AmqpDescribed>(() => new Released()) },
				{ Rejected.Code, new Func<AmqpDescribed>(() => new Rejected()) },
				{ Modified.Code, new Func<AmqpDescribed>(() => new Modified()) },
				{ DeleteOnClose.Code, new Func<AmqpDescribed>(() => new DeleteOnClose()) },
				{ DeleteOnNoLinks.Code, new Func<AmqpDescribed>(() => new DeleteOnNoLinks()) },
				{ DeleteOnNoMessages.Code, new Func<AmqpDescribed>(() => new DeleteOnNoMessages()) },
				{ DeleteOnNoLinksOrMessages.Code, new Func<AmqpDescribed>(() => new DeleteOnNoLinksOrMessages()) }
			};
			AmqpCodec.knownTypesByCode = nums;
		}

		public static AmqpDescribed CreateAmqpDescribed(ByteBuffer buffer)
		{
			return AmqpCodec.CreateAmqpDescribed(buffer, AmqpCodec.knownTypesByName, AmqpCodec.knownTypesByCode);
		}

		public static AmqpDescribed CreateAmqpDescribed(ByteBuffer buffer, Dictionary<string, Func<AmqpDescribed>> byName, Dictionary<ulong, Func<AmqpDescribed>> byCode)
		{
			FormatCode formatCode = AmqpEncoding.ReadFormatCode(buffer);
			if (formatCode == 64)
			{
				return null;
			}
			EncodingBase.VerifyFormatCode(formatCode, 0, buffer.Offset);
			Func<AmqpDescribed> func = null;
			formatCode = AmqpEncoding.ReadFormatCode(buffer);
			if (formatCode == 163 || formatCode == 179)
			{
				AmqpSymbol amqpSymbol = SymbolEncoding.Decode(buffer, formatCode);
				byName.TryGetValue(amqpSymbol.Value, out func);
			}
			else if (formatCode == 68 || formatCode == 128 || formatCode == 83)
			{
				ulong? nullable = ULongEncoding.Decode(buffer, formatCode);
				byCode.TryGetValue(nullable.Value, out func);
			}
			if (func == null)
			{
				throw AmqpEncoding.GetEncodingException(SRAmqp.AmqpInvalidFormatCode(formatCode, buffer.Offset));
			}
			return func();
		}

		public static AmqpDescribed DecodeAmqpDescribed(ByteBuffer buffer)
		{
			return AmqpCodec.DecodeAmqpDescribed(buffer, AmqpCodec.knownTypesByName, AmqpCodec.knownTypesByCode);
		}

		public static AmqpDescribed DecodeAmqpDescribed(ByteBuffer buffer, Dictionary<string, Func<AmqpDescribed>> byName, Dictionary<ulong, Func<AmqpDescribed>> byCode)
		{
			AmqpDescribed amqpDescribed = AmqpCodec.CreateAmqpDescribed(buffer, byName, byCode);
			if (amqpDescribed != null)
			{
				amqpDescribed.DecodeValue(buffer);
			}
			return amqpDescribed;
		}

		public static T[] DecodeArray<T>(ByteBuffer buffer)
		{
			return ArrayEncoding.Decode<T>(buffer, 0);
		}

		public static ArraySegment<byte> DecodeBinary(ByteBuffer buffer)
		{
			return BinaryEncoding.Decode(buffer, 0);
		}

		public static bool? DecodeBoolean(ByteBuffer buffer)
		{
			return BooleanEncoding.Decode(buffer, 0);
		}

		public static sbyte? DecodeByte(ByteBuffer buffer)
		{
			return ByteEncoding.Decode(buffer, 0);
		}

		public static char? DecodeChar(ByteBuffer buffer)
		{
			return CharEncoding.Decode(buffer, 0);
		}

		public static double? DecodeDouble(ByteBuffer buffer)
		{
			return DoubleEncoding.Decode(buffer, 0);
		}

		public static float? DecodeFloat(ByteBuffer buffer)
		{
			return FloatEncoding.Decode(buffer, 0);
		}

		public static int? DecodeInt(ByteBuffer buffer)
		{
			return IntEncoding.Decode(buffer, 0);
		}

		public static T DecodeKnownType<T>(ByteBuffer buffer)
		where T : class, IAmqpSerializable, new()
		{
			if (AmqpEncoding.ReadFormatCode(buffer) == 64)
			{
				return default(T);
			}
			T t = Activator.CreateInstance<T>();
			t.Decode(buffer);
			return t;
		}

		public static IList DecodeList(ByteBuffer buffer)
		{
			return ListEncoding.Decode(buffer, 0);
		}

		public static long? DecodeLong(ByteBuffer buffer)
		{
			return LongEncoding.Decode(buffer, 0);
		}

		public static AmqpMap DecodeMap(ByteBuffer buffer)
		{
			return MapEncoding.Decode(buffer, 0);
		}

		public static T DecodeMap<T>(ByteBuffer buffer)
		where T : RestrictedMap, new()
		{
			AmqpMap amqpMaps = MapEncoding.Decode(buffer, 0);
			T t = default(T);
			if (amqpMaps != null)
			{
				t = Activator.CreateInstance<T>();
				t.SetMap(amqpMaps);
			}
			return t;
		}

		public static Multiple<T> DecodeMultiple<T>(ByteBuffer buffer)
		{
			return Multiple<T>.Decode(buffer);
		}

		public static object DecodeObject(ByteBuffer buffer)
		{
			FormatCode formatCode = AmqpEncoding.ReadFormatCode(buffer);
			if (formatCode == 64)
			{
				return null;
			}
			if (formatCode != 0)
			{
				return AmqpEncoding.DecodeObject(buffer, formatCode);
			}
			object obj = AmqpCodec.DecodeObject(buffer);
			Func<AmqpDescribed> func = null;
			if (obj is AmqpSymbol)
			{
				Dictionary<string, Func<AmqpDescribed>> strs = AmqpCodec.knownTypesByName;
				AmqpSymbol amqpSymbol = (AmqpSymbol)obj;
				strs.TryGetValue(amqpSymbol.Value, out func);
			}
			else if (obj is ulong)
			{
				AmqpCodec.knownTypesByCode.TryGetValue((ulong)obj, out func);
			}
			if (func == null)
			{
				return new DescribedType(obj, AmqpCodec.DecodeObject(buffer));
			}
			AmqpDescribed amqpDescribed = func();
			amqpDescribed.DecodeValue(buffer);
			return amqpDescribed;
		}

		public static short? DecodeShort(ByteBuffer buffer)
		{
			return ShortEncoding.Decode(buffer, 0);
		}

		public static string DecodeString(ByteBuffer buffer)
		{
			return StringEncoding.Decode(buffer, 0);
		}

		public static AmqpSymbol DecodeSymbol(ByteBuffer buffer)
		{
			return SymbolEncoding.Decode(buffer, 0);
		}

		public static DateTime? DecodeTimeStamp(ByteBuffer buffer)
		{
			return TimeStampEncoding.Decode(buffer, 0);
		}

		public static byte? DecodeUByte(ByteBuffer buffer)
		{
			return UByteEncoding.Decode(buffer, 0);
		}

		public static uint? DecodeUInt(ByteBuffer buffer)
		{
			return UIntEncoding.Decode(buffer, 0);
		}

		public static ulong? DecodeULong(ByteBuffer buffer)
		{
			return ULongEncoding.Decode(buffer, 0);
		}

		public static ushort? DecodeUShort(ByteBuffer buffer)
		{
			return UShortEncoding.Decode(buffer, 0);
		}

		public static Guid? DecodeUuid(ByteBuffer buffer)
		{
			return UuidEncoding.Decode(buffer, 0);
		}

		public static void EncodeArray<T>(T[] data, ByteBuffer buffer)
		{
			ArrayEncoding.Encode<T>(data, buffer);
		}

		public static void EncodeBinary(ArraySegment<byte> data, ByteBuffer buffer)
		{
			BinaryEncoding.Encode(data, buffer);
		}

		public static void EncodeBoolean(bool? data, ByteBuffer buffer)
		{
			BooleanEncoding.Encode(data, buffer);
		}

		public static void EncodeByte(sbyte? data, ByteBuffer buffer)
		{
			ByteEncoding.Encode(data, buffer);
		}

		public static void EncodeChar(char? data, ByteBuffer buffer)
		{
			CharEncoding.Encode(data, buffer);
		}

		public static void EncodeDouble(double? data, ByteBuffer buffer)
		{
			DoubleEncoding.Encode(data, buffer);
		}

		public static void EncodeFloat(float? data, ByteBuffer buffer)
		{
			FloatEncoding.Encode(data, buffer);
		}

		public static void EncodeInt(int? data, ByteBuffer buffer)
		{
			IntEncoding.Encode(data, buffer);
		}

		public static void EncodeList(IList data, ByteBuffer buffer)
		{
			ListEncoding.Encode(data, buffer);
		}

		public static void EncodeLong(long? data, ByteBuffer buffer)
		{
			LongEncoding.Encode(data, buffer);
		}

		public static void EncodeMap(AmqpMap data, ByteBuffer buffer)
		{
			MapEncoding.Encode(data, buffer);
		}

		public static void EncodeMultiple<T>(Multiple<T> data, ByteBuffer buffer)
		{
			Multiple<T>.Encode(data, buffer);
		}

		public static void EncodeObject(object data, ByteBuffer buffer)
		{
			AmqpEncoding.EncodeObject(data, buffer);
		}

		public static void EncodeSerializable(IAmqpSerializable data, ByteBuffer buffer)
		{
			if (data == null)
			{
				AmqpEncoding.EncodeNull(buffer);
				return;
			}
			data.Encode(buffer);
		}

		public static void EncodeShort(short? data, ByteBuffer buffer)
		{
			ShortEncoding.Encode(data, buffer);
		}

		public static void EncodeString(string data, ByteBuffer buffer)
		{
			StringEncoding.Encode(data, buffer);
		}

		public static void EncodeSymbol(AmqpSymbol data, ByteBuffer buffer)
		{
			SymbolEncoding.Encode(data, buffer);
		}

		public static void EncodeTimeStamp(DateTime? data, ByteBuffer buffer)
		{
			TimeStampEncoding.Encode(data, buffer);
		}

		public static void EncodeUByte(byte? data, ByteBuffer buffer)
		{
			UByteEncoding.Encode(data, buffer);
		}

		public static void EncodeUInt(uint? data, ByteBuffer buffer)
		{
			UIntEncoding.Encode(data, buffer);
		}

		public static void EncodeULong(ulong? data, ByteBuffer buffer)
		{
			ULongEncoding.Encode(data, buffer);
		}

		public static void EncodeUShort(ushort? data, ByteBuffer buffer)
		{
			UShortEncoding.Encode(data, buffer);
		}

		public static void EncodeUuid(Guid? data, ByteBuffer buffer)
		{
			UuidEncoding.Encode(data, buffer);
		}

		public static int GetArrayEncodeSize<T>(T[] value)
		{
			return ArrayEncoding.GetEncodeSize<T>(value);
		}

		public static int GetBinaryEncodeSize(ArraySegment<byte> value)
		{
			return BinaryEncoding.GetEncodeSize(value);
		}

		public static int GetBooleanEncodeSize(bool? value)
		{
			return BooleanEncoding.GetEncodeSize(value);
		}

		public static int GetByteEncodeSize(sbyte? value)
		{
			return ByteEncoding.GetEncodeSize(value);
		}

		public static int GetCharEncodeSize(char? value)
		{
			return CharEncoding.GetEncodeSize(value);
		}

		public static int GetDoubleEncodeSize(double? value)
		{
			return DoubleEncoding.GetEncodeSize(value);
		}

		public static int GetFloatEncodeSize(float? value)
		{
			return FloatEncoding.GetEncodeSize(value);
		}

		public static int GetFrameSize(ByteBuffer buffer)
		{
			return (int)AmqpBitConverter.PeekUInt(buffer);
		}

		public static int GetIntEncodeSize(int? value)
		{
			return IntEncoding.GetEncodeSize(value);
		}

		public static int GetListEncodeSize(IList value)
		{
			return ListEncoding.GetEncodeSize(value);
		}

		public static int GetLongEncodeSize(long? value)
		{
			return LongEncoding.GetEncodeSize(value);
		}

		public static int GetMapEncodeSize(AmqpMap value)
		{
			return MapEncoding.GetEncodeSize(value);
		}

		public static int GetMultipleEncodeSize<T>(Multiple<T> value)
		{
			return Multiple<T>.GetEncodeSize(value);
		}

		public static int GetObjectEncodeSize(object value)
		{
			return AmqpEncoding.GetObjectEncodeSize(value);
		}

		public static int GetSerializableEncodeSize(IAmqpSerializable value)
		{
			if (value == null)
			{
				return 1;
			}
			return value.EncodeSize;
		}

		public static int GetShortEncodeSize(short? value)
		{
			return ShortEncoding.GetEncodeSize(value);
		}

		public static int GetStringEncodeSize(string value)
		{
			return StringEncoding.GetEncodeSize(value);
		}

		public static int GetSymbolEncodeSize(AmqpSymbol value)
		{
			return SymbolEncoding.GetEncodeSize(value);
		}

		public static int GetTimeStampEncodeSize(DateTime? value)
		{
			return TimeStampEncoding.GetEncodeSize(value);
		}

		public static int GetUByteEncodeSize(byte? value)
		{
			return UByteEncoding.GetEncodeSize(value);
		}

		public static int GetUIntEncodeSize(uint? value)
		{
			return UIntEncoding.GetEncodeSize(value);
		}

		public static int GetULongEncodeSize(ulong? value)
		{
			return ULongEncoding.GetEncodeSize(value);
		}

		public static int GetUShortEncodeSize(ushort? value)
		{
			return UShortEncoding.GetEncodeSize(value);
		}

		public static int GetUuidEncodeSize(Guid? value)
		{
			return UuidEncoding.GetEncodeSize(value);
		}

		public static void RegisterKnownTypes(string name, ulong code, Func<AmqpDescribed> ctor)
		{
			lock (AmqpCodec.knownTypesByCode)
			{
				if (!AmqpCodec.knownTypesByName.ContainsKey(name))
				{
					AmqpCodec.knownTypesByName.Add(name, ctor);
					AmqpCodec.knownTypesByCode.Add(code, ctor);
				}
			}
		}
	}
}