using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace Microsoft.ServiceBus.Messaging
{
	internal static class SerializationUtilities
	{
		public const long MaxBufferPoolSize = 5242880L;

		public const int MaxBufferSize = 65536;

		private const int ShortSize = 2;

		private const int IntSize = 4;

		private const int LongSize = 8;

		private const int DateTimeSize = 8;

		private const int TimeSpanSize = 8;

		private const int GuidSize = 16;

		private const int BooleanSize = 1;

		private static Dictionary<Type, PropertyValueType> typeToIntMap;

		static SerializationUtilities()
		{
			Dictionary<Type, PropertyValueType> types = new Dictionary<Type, PropertyValueType>()
			{
				{ typeof(byte), PropertyValueType.Byte },
				{ typeof(sbyte), PropertyValueType.SByte },
				{ typeof(char), PropertyValueType.Char },
				{ typeof(short), PropertyValueType.Int16 },
				{ typeof(ushort), PropertyValueType.UInt16 },
				{ typeof(int), PropertyValueType.Int32 },
				{ typeof(uint), PropertyValueType.UInt32 },
				{ typeof(long), PropertyValueType.Int64 },
				{ typeof(ulong), PropertyValueType.UInt64 },
				{ typeof(float), PropertyValueType.Single },
				{ typeof(double), PropertyValueType.Double },
				{ typeof(decimal), PropertyValueType.Decimal },
				{ typeof(bool), PropertyValueType.Boolean },
				{ typeof(Guid), PropertyValueType.Guid },
				{ typeof(string), PropertyValueType.String },
				{ typeof(Uri), PropertyValueType.Uri },
				{ typeof(DateTime), PropertyValueType.DateTime },
				{ typeof(DateTimeOffset), PropertyValueType.DateTimeOffset },
				{ typeof(TimeSpan), PropertyValueType.TimeSpan },
				{ typeof(BufferedInputStream), PropertyValueType.Stream }
			};
			SerializationUtilities.typeToIntMap = types;
		}

		public static object ConvertByteArrayToNativeValue(int messageVersion, PropertyValueType propertyTypeId, byte[] bytes)
		{
			switch (propertyTypeId)
			{
				case PropertyValueType.Null:
				{
					return null;
				}
				case PropertyValueType.Byte:
				{
					return bytes[0];
				}
				case PropertyValueType.SByte:
				{
					return (sbyte)bytes[0];
				}
				case PropertyValueType.Char:
				{
					return BitConverter.ToChar(bytes, 0);
				}
				case PropertyValueType.Int16:
				{
					return BitConverter.ToInt16(bytes, 0);
				}
				case PropertyValueType.UInt16:
				{
					return BitConverter.ToUInt16(bytes, 0);
				}
				case PropertyValueType.Int32:
				{
					return BitConverter.ToInt32(bytes, 0);
				}
				case PropertyValueType.UInt32:
				{
					return BitConverter.ToUInt32(bytes, 0);
				}
				case PropertyValueType.Int64:
				{
					return BitConverter.ToInt64(bytes, 0);
				}
				case PropertyValueType.UInt64:
				{
					return BitConverter.ToUInt64(bytes, 0);
				}
				case PropertyValueType.Single:
				{
					return BitConverter.ToSingle(bytes, 0);
				}
				case PropertyValueType.Double:
				{
					return BitConverter.ToDouble(bytes, 0);
				}
				case PropertyValueType.Decimal:
				{
					if (messageVersion < BrokeredMessage.MessageVersion3)
					{
						return XmlConvert.ToDecimal(Encoding.UTF8.GetString(bytes));
					}
					int[] num = new int[] { BitConverter.ToInt32(bytes, 0), BitConverter.ToInt32(bytes, 4), BitConverter.ToInt32(bytes, 8), BitConverter.ToInt32(bytes, 12) };
					return new decimal(num);
				}
				case PropertyValueType.Boolean:
				{
					return BitConverter.ToBoolean(bytes, 0);
				}
				case PropertyValueType.Guid:
				{
					return new Guid(bytes);
				}
				case PropertyValueType.String:
				{
					return Encoding.UTF8.GetString(bytes);
				}
				case PropertyValueType.Uri:
				{
					return new Uri(Encoding.UTF8.GetString(bytes));
				}
				case PropertyValueType.DateTime:
				{
					return DateTime.FromBinary(BitConverter.ToInt64(bytes, 0));
				}
				case PropertyValueType.DateTimeOffset:
				{
					if (messageVersion < BrokeredMessage.MessageVersion3)
					{
						return XmlConvert.ToDateTimeOffset(Encoding.UTF8.GetString(bytes));
					}
					long num1 = BitConverter.ToInt64(bytes, 0);
					long num2 = BitConverter.ToInt64(bytes, 8);
					return new DateTimeOffset(num1, TimeSpan.FromTicks(num2));
				}
				case PropertyValueType.TimeSpan:
				{
					if (messageVersion >= BrokeredMessage.MessageVersion3)
					{
						return TimeSpan.FromTicks(BitConverter.ToInt64(bytes, 0));
					}
					double num3 = BitConverter.ToDouble(bytes, 0);
					if (num3.CompareTo(TimeSpan.MaxValue.TotalMilliseconds) == 0)
					{
						return TimeSpan.MaxValue;
					}
					return TimeSpan.FromMilliseconds(num3);
				}
				case PropertyValueType.Stream:
				{
					InternalBufferManager bufferManager = ThrottledBufferManager.GetBufferManager();
					int length = (int)bytes.Length;
					byte[] numArray = bufferManager.TakeBuffer(length);
					Buffer.BlockCopy(bytes, 0, numArray, 0, length);
					return new BufferedInputStream(numArray, length, bufferManager);
				}
			}
			throw Fx.Exception.AsError(new SerializationException(SRClient.FailedToDeserializeUnsupportedProperty(propertyTypeId.ToString())), null);
		}

		public static byte[] ConvertNativeValueToByteArray(int messageVersion, PropertyValueType propertyTypeId, object value)
		{
			switch (propertyTypeId)
			{
				case PropertyValueType.Byte:
				{
					return new byte[] { (byte)value };
				}
				case PropertyValueType.SByte:
				{
					return new byte[] { (byte)((sbyte)value) };
				}
				case PropertyValueType.Char:
				{
					return BitConverter.GetBytes((char)value);
				}
				case PropertyValueType.Int16:
				{
					return BitConverter.GetBytes((short)value);
				}
				case PropertyValueType.UInt16:
				{
					return BitConverter.GetBytes((ushort)value);
				}
				case PropertyValueType.Int32:
				{
					return BitConverter.GetBytes((int)value);
				}
				case PropertyValueType.UInt32:
				{
					return BitConverter.GetBytes((uint)value);
				}
				case PropertyValueType.Int64:
				{
					return BitConverter.GetBytes((long)value);
				}
				case PropertyValueType.UInt64:
				{
					return BitConverter.GetBytes((ulong)value);
				}
				case PropertyValueType.Single:
				{
					return BitConverter.GetBytes((float)value);
				}
				case PropertyValueType.Double:
				{
					return BitConverter.GetBytes((double)value);
				}
				case PropertyValueType.Decimal:
				{
					if (messageVersion < BrokeredMessage.MessageVersion3)
					{
						return Encoding.UTF8.GetBytes(XmlConvert.ToString((decimal)value));
					}
					int[] bits = decimal.GetBits((decimal)value);
					byte[] bytes = BitConverter.GetBytes(bits[0]);
					byte[] numArray = BitConverter.GetBytes(bits[1]);
					byte[] bytes1 = BitConverter.GetBytes(bits[2]);
					byte[] numArray1 = BitConverter.GetBytes(bits[3]);
					byte[] numArray2 = new byte[16];
					Buffer.BlockCopy(bytes, 0, numArray2, 0, 4);
					Buffer.BlockCopy(numArray, 0, numArray2, 4, 4);
					Buffer.BlockCopy(bytes1, 0, numArray2, 8, 4);
					Buffer.BlockCopy(numArray1, 0, numArray2, 12, 4);
					return numArray2;
				}
				case PropertyValueType.Boolean:
				{
					return BitConverter.GetBytes((bool)value);
				}
				case PropertyValueType.Guid:
				{
					return ((Guid)value).ToByteArray();
				}
				case PropertyValueType.String:
				{
					return Encoding.UTF8.GetBytes((string)value);
				}
				case PropertyValueType.Uri:
				{
					return Encoding.UTF8.GetBytes(value.ToString());
				}
				case PropertyValueType.DateTime:
				{
					return BitConverter.GetBytes(((DateTime)value).ToBinary());
				}
				case PropertyValueType.DateTimeOffset:
				{
					if (messageVersion < BrokeredMessage.MessageVersion3)
					{
						return Encoding.UTF8.GetBytes(XmlConvert.ToString((DateTimeOffset)value));
					}
					DateTimeOffset dateTimeOffset = (DateTimeOffset)value;
					byte[] bytes2 = BitConverter.GetBytes(dateTimeOffset.Ticks);
					byte[] bytes3 = BitConverter.GetBytes(dateTimeOffset.Offset.Ticks);
					byte[] numArray3 = new byte[16];
					Buffer.BlockCopy(bytes2, 0, numArray3, 0, 8);
					Buffer.BlockCopy(bytes3, 0, numArray3, 8, 8);
					return numArray3;
				}
				case PropertyValueType.TimeSpan:
				{
					if (messageVersion >= BrokeredMessage.MessageVersion3)
					{
						return BitConverter.GetBytes(((TimeSpan)value).Ticks);
					}
					return BitConverter.GetBytes(((TimeSpan)value).TotalMilliseconds);
				}
				case PropertyValueType.Stream:
				{
					BufferedInputStream bufferedInputStream = value as BufferedInputStream;
					if (bufferedInputStream == null)
					{
						throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new NotImplementedException(), null);
					}
					long length = bufferedInputStream.Length;
					byte[] numArray4 = new byte[checked((IntPtr)length)];
					bufferedInputStream.Position = (long)0;
					bufferedInputStream.Read(numArray4, 0, (int)length);
					return numArray4;
				}
			}
			throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new SerializationException(SRClient.FailedToSerializeUnsupportedType(value.GetType().FullName)), null);
		}

		public static int GetBooleanSize(bool value)
		{
			return 1;
		}

		public static int GetDateTimeSize(DateTime value)
		{
			return 8;
		}

		public static int GetGuidSize(Guid value)
		{
			return 16;
		}

		public static int GetIntSize(int value)
		{
			return 4;
		}

		public static int GetLongSize(long value)
		{
			return 8;
		}

		public static int GetShortSize(short value)
		{
			return 2;
		}

		public static int GetStreamSize(Stream value)
		{
			if (!value.CanSeek)
			{
				return 2147483647;
			}
			return (int)value.Length;
		}

		public static int GetStringSize(string value)
		{
			return Encoding.UTF8.GetByteCount(value);
		}

		public static int GetTimeSpanSize(TimeSpan value)
		{
			return 8;
		}

		public static PropertyValueType GetTypeId(object value)
		{
			PropertyValueType propertyValueType;
			if (value == null)
			{
				return PropertyValueType.Null;
			}
			if (SerializationUtilities.typeToIntMap.TryGetValue(value.GetType(), out propertyValueType))
			{
				return propertyValueType;
			}
			return PropertyValueType.Unknown;
		}

		public static bool IsSupportedPropertyType(Type type)
		{
			return SerializationUtilities.typeToIntMap.ContainsKey(type);
		}

		public static byte[] ReadBytes(XmlReader reader, int bytesToRead)
		{
			int num;
			byte[] numArray = new byte[bytesToRead];
			int num1 = 0;
			do
			{
				if (num1 >= bytesToRead)
				{
					break;
				}
				num = reader.ReadContentAsBase64(numArray, num1, (int)numArray.Length - num1);
				num1 = num1 + num;
			}
			while (num != 0);
			if (num1 < bytesToRead)
			{
				throw Fx.Exception.AsError(new InvalidOperationException("Insufficient data in the byte-stream"), null);
			}
			return numArray;
		}
	}
}