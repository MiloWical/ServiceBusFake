using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging.Amqp;
using System;
using System.Collections;

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
	internal sealed class ArrayEncoding : EncodingBase
	{
		public ArrayEncoding() : base(240)
		{
		}

		public static T[] Decode<T>(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
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
			AmqpEncoding.ReadSizeAndCount(buffer, formatCode, 224, 240, out num, out num1);
			formatCode = AmqpEncoding.ReadFormatCode(buffer);
			return ArrayEncoding.Decode<T>(buffer, num, num1, formatCode);
		}

		private static T[] Decode<T>(ByteBuffer buffer, int size, int count, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
		{
			T[] tArray = new T[count];
			EncodingBase encoding = AmqpEncoding.GetEncoding(formatCode);
			object obj = null;
			if (formatCode == 0)
			{
				obj = AmqpEncoding.DecodeObject(buffer);
				formatCode = AmqpEncoding.ReadFormatCode(buffer);
			}
			for (int i = 0; i < count; i++)
			{
				object describedType = encoding.DecodeObject(buffer, formatCode);
				if (obj != null)
				{
					describedType = new DescribedType(obj, describedType);
				}
				tArray[i] = (T)describedType;
			}
			return tArray;
		}

		public override object DecodeObject(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
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
			int num = 0;
			int num1 = 0;
			AmqpEncoding.ReadSizeAndCount(buffer, formatCode, 224, 240, out num, out num1);
			formatCode = AmqpEncoding.ReadFormatCode(buffer);
			Array arrays = null;
			byte num2 = formatCode;
			if (num2 > 152)
			{
				if (num2 > 193)
				{
					switch (num2)
					{
						case 208:
						{
							arrays = ArrayEncoding.Decode<IList>(buffer, num, num1, formatCode);
							return arrays;
						}
						case 209:
						{
							break;
						}
						default:
						{
							if (num2 == 224 || num2 == 240)
							{
								arrays = ArrayEncoding.Decode<Array>(buffer, num, num1, formatCode);
								return arrays;
							}
							else
							{
								throw new NotSupportedException(SRClient.NotSupportFrameCode(formatCode));
							}
						}
					}
				}
				else
				{
					switch (num2)
					{
						case 160:
						{
							arrays = ArrayEncoding.Decode<ArraySegment<byte>>(buffer, num, num1, formatCode);
							return arrays;
						}
						case 161:
						{
							arrays = ArrayEncoding.Decode<string>(buffer, num, num1, formatCode);
							return arrays;
						}
						case 162:
						{
							throw new NotSupportedException(SRClient.NotSupportFrameCode(formatCode));
						}
						case 163:
						{
							arrays = ArrayEncoding.Decode<AmqpSymbol>(buffer, num, num1, formatCode);
							return arrays;
						}
						default:
						{
							switch (num2)
							{
								case 176:
								{
									arrays = ArrayEncoding.Decode<ArraySegment<byte>>(buffer, num, num1, formatCode);
									return arrays;
								}
								case 177:
								{
									arrays = ArrayEncoding.Decode<string>(buffer, num, num1, formatCode);
									return arrays;
								}
								case 178:
								{
									throw new NotSupportedException(SRClient.NotSupportFrameCode(formatCode));
								}
								case 179:
								{
									arrays = ArrayEncoding.Decode<AmqpSymbol>(buffer, num, num1, formatCode);
									return arrays;
								}
								default:
								{
									switch (num2)
									{
										case 192:
										{
											arrays = ArrayEncoding.Decode<IList>(buffer, num, num1, formatCode);
											return arrays;
										}
										case 193:
										{
											break;
										}
										default:
										{
											throw new NotSupportedException(SRClient.NotSupportFrameCode(formatCode));
										}
									}
									break;
								}
							}
							break;
						}
					}
				}
				arrays = ArrayEncoding.Decode<AmqpMap>(buffer, num, num1, formatCode);
			}
			else
			{
				if (num2 > 97)
				{
					switch (num2)
					{
						case 112:
						{
							arrays = ArrayEncoding.Decode<uint>(buffer, num, num1, formatCode);
							return arrays;
						}
						case 113:
						{
							arrays = ArrayEncoding.Decode<int>(buffer, num, num1, formatCode);
							return arrays;
						}
						case 114:
						{
							arrays = ArrayEncoding.Decode<float>(buffer, num, num1, formatCode);
							return arrays;
						}
						case 115:
						{
							arrays = ArrayEncoding.Decode<char>(buffer, num, num1, formatCode);
							return arrays;
						}
						default:
						{
							switch (num2)
							{
								case 128:
								{
									arrays = ArrayEncoding.Decode<ulong>(buffer, num, num1, formatCode);
									return arrays;
								}
								case 129:
								{
									break;
								}
								case 130:
								{
									arrays = ArrayEncoding.Decode<double>(buffer, num, num1, formatCode);
									return arrays;
								}
								case 131:
								{
									arrays = ArrayEncoding.Decode<DateTime>(buffer, num, num1, formatCode);
									return arrays;
								}
								default:
								{
									if (num2 == 152)
									{
										arrays = ArrayEncoding.Decode<Guid>(buffer, num, num1, formatCode);
										return arrays;
									}
									else
									{
										throw new NotSupportedException(SRClient.NotSupportFrameCode(formatCode));
									}
								}
							}
							break;
						}
					}
				}
				else
				{
					switch (num2)
					{
						case 80:
						{
							arrays = ArrayEncoding.Decode<byte>(buffer, num, num1, formatCode);
							return arrays;
						}
						case 81:
						{
							arrays = ArrayEncoding.Decode<sbyte>(buffer, num, num1, formatCode);
							return arrays;
						}
						case 82:
						{
							arrays = ArrayEncoding.Decode<uint>(buffer, num, num1, formatCode);
							return arrays;
						}
						case 83:
						{
							arrays = ArrayEncoding.Decode<ulong>(buffer, num, num1, formatCode);
							return arrays;
						}
						case 84:
						{
							arrays = ArrayEncoding.Decode<int>(buffer, num, num1, formatCode);
							return arrays;
						}
						case 85:
						{
							break;
						}
						case 86:
						{
							arrays = ArrayEncoding.Decode<bool>(buffer, num, num1, formatCode);
							return arrays;
						}
						default:
						{
							switch (num2)
							{
								case 96:
								{
									arrays = ArrayEncoding.Decode<ushort>(buffer, num, num1, formatCode);
									return arrays;
								}
								case 97:
								{
									arrays = ArrayEncoding.Decode<short>(buffer, num, num1, formatCode);
									return arrays;
								}
								default:
								{
									throw new NotSupportedException(SRClient.NotSupportFrameCode(formatCode));
								}
							}
							break;
						}
					}
				}
				arrays = ArrayEncoding.Decode<long>(buffer, num, num1, formatCode);
			}
			return arrays;
		}

		public static void Encode<T>(T[] value, ByteBuffer buffer)
		{
			int num;
			if (value == null)
			{
				AmqpEncoding.EncodeNull(buffer);
				return;
			}
			int encodeSize = ArrayEncoding.GetEncodeSize(value, false, out num);
			AmqpBitConverter.WriteUByte(buffer, (byte)((num == 1 ? 224 : 240)));
			ArrayEncoding.Encode(value, num, encodeSize, buffer);
		}

		private static void Encode(Array value, int width, int encodeSize, ByteBuffer buffer)
		{
			encodeSize = encodeSize - (1 + width);
			if (width != 1)
			{
				AmqpBitConverter.WriteUInt(buffer, (uint)encodeSize);
				AmqpBitConverter.WriteUInt(buffer, (uint)value.Length);
			}
			else
			{
				AmqpBitConverter.WriteUByte(buffer, (byte)encodeSize);
				AmqpBitConverter.WriteUByte(buffer, (byte)value.Length);
			}
			if (value.Length > 0)
			{
				object obj = value.GetValue(0);
				EncodingBase encoding = AmqpEncoding.GetEncoding(obj);
				AmqpBitConverter.WriteUByte(buffer, encoding.FormatCode);
				if (encoding.FormatCode == 0)
				{
					DescribedType describedType = (DescribedType)obj;
					AmqpEncoding.EncodeObject(describedType.Descriptor, buffer);
					AmqpBitConverter.WriteUByte(buffer, AmqpEncoding.GetEncoding(describedType.Value).FormatCode);
				}
				foreach (object obj1 in value)
				{
					encoding.EncodeObject(obj1, true, buffer);
				}
			}
		}

		public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
		{
			int num;
			Array arrays = (Array)value;
			int encodeSize = ArrayEncoding.GetEncodeSize(arrays, arrayEncoding, out num);
			AmqpBitConverter.WriteUByte(buffer, (byte)((num == 1 ? 224 : 240)));
			ArrayEncoding.Encode(arrays, num, encodeSize, buffer);
		}

		public static int GetEncodeSize<T>(T[] value)
		{
			if (value == null)
			{
				return 1;
			}
			return ArrayEncoding.GetEncodeSize(value, false);
		}

		private static int GetEncodeSize(Array array, bool arrayEncoding)
		{
			int num;
			return ArrayEncoding.GetEncodeSize(array, arrayEncoding, out num);
		}

		private static int GetEncodeSize(Array array, bool arrayEncoding, out int width)
		{
			int valueSize = 1 + ArrayEncoding.GetValueSize(array, null);
			width = (arrayEncoding ? 4 : AmqpEncoding.GetEncodeWidthByCountAndSize(array.Length, valueSize));
			valueSize = valueSize + 1 + width + width;
			return valueSize;
		}

		public override int GetObjectEncodeSize(object value, bool arrayEncoding)
		{
			return ArrayEncoding.GetEncodeSize((Array)value, arrayEncoding);
		}

		private static int GetValueSize(Array value, Type type)
		{
			if (value.Length == 0)
			{
				return 0;
			}
			if (type == null)
			{
				type = value.GetValue(0).GetType();
			}
			EncodingBase encoding = AmqpEncoding.GetEncoding(type);
			int objectEncodeSize = 0;
			foreach (object obj in value)
			{
				bool flag = true;
				if (encoding.FormatCode == 0 && objectEncodeSize == 0)
				{
					flag = false;
				}
				objectEncodeSize = objectEncodeSize + encoding.GetObjectEncodeSize(obj, flag);
			}
			return objectEncodeSize;
		}
	}
}