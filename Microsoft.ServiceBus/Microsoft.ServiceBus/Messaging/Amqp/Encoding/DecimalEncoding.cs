using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging.Amqp;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
	internal sealed class DecimalEncoding : EncodingBase
	{
		private const int Decimal32Bias = 101;

		private const int Decimal64Bias = 398;

		private const int Decimal128Bias = 6176;

		public DecimalEncoding() : base(148)
		{
		}

		private static decimal CreateDecimal(int low, int middle, int high, int sign, int exponent)
		{
			if (exponent <= 0)
			{
				return new decimal(low, middle, high, sign < 0, (byte)(-exponent));
			}
			decimal num = new decimal(low, middle, high, sign < 0, 0);
			for (int i = 0; i < exponent; i++)
			{
				num = num * new decimal(10);
			}
			return num;
		}

		public static decimal? Decode(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
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
			return new decimal?(DecimalEncoding.DecodeValue(buffer, formatCode));
		}

		private static decimal DecodeDecimal128(ByteBuffer buffer)
		{
			byte[] numArray = new byte[16];
			AmqpBitConverter.ReadBytes(buffer, numArray, 0, (int)numArray.Length);
			int num = 1;
			int num1 = 0;
			num = ((numArray[0] & 128) != 0 ? -1 : 1);
			if ((numArray[0] & 96) != 96)
			{
				num1 = (numArray[0] & 127) << 7 | (numArray[1] & 254) >> 1;
				numArray[0] = 0;
				numArray[1] = (byte)(numArray[1] & 1);
			}
			else if ((numArray[0] & 120) == 0)
			{
				return new decimal(0);
			}
			int num2 = (int)AmqpBitConverter.ReadUInt(numArray, 4, 4);
			int num3 = (int)AmqpBitConverter.ReadUInt(numArray, 8, 4);
			int num4 = (int)AmqpBitConverter.ReadUInt(numArray, 12, 4);
			return DecimalEncoding.CreateDecimal(num4, num3, num2, num, num1 - 6176);
		}

		private static decimal DecodeDecimal32(ByteBuffer buffer)
		{
			byte[] numArray = new byte[4];
			AmqpBitConverter.ReadBytes(buffer, numArray, 0, (int)numArray.Length);
			int num = 1;
			int num1 = 0;
			num = ((numArray[0] & 128) != 0 ? -1 : 1);
			if ((numArray[0] & 96) != 96)
			{
				num1 = (numArray[0] & 127) << 1 | (numArray[1] & 128) >> 7;
				numArray[0] = 0;
				numArray[1] = (byte)(numArray[1] & 127);
			}
			else if ((numArray[0] & 120) == 0)
			{
				num1 = (numArray[0] & 31) << 3 | (numArray[1] & 224) >> 5;
				numArray[0] = 0;
				numArray[1] = (byte)(numArray[1] & 31);
				numArray[1] = (byte)(numArray[1] | 128);
			}
			int num2 = (int)AmqpBitConverter.ReadUInt(numArray, 0, (int)numArray.Length);
			return DecimalEncoding.CreateDecimal(num2, 0, 0, num, num1 - 101);
		}

		private static decimal DecodeDecimal64(ByteBuffer buffer)
		{
			byte[] numArray = new byte[8];
			AmqpBitConverter.ReadBytes(buffer, numArray, 0, (int)numArray.Length);
			int num = 1;
			int num1 = 0;
			num = ((numArray[0] & 128) != 0 ? -1 : 1);
			if ((numArray[0] & 96) != 96)
			{
				num1 = (numArray[0] & 127) << 3 | (numArray[1] & 224) >> 5;
				numArray[0] = 0;
				numArray[1] = (byte)(numArray[1] & 31);
			}
			else if ((numArray[0] & 120) == 0)
			{
				num1 = (numArray[0] & 31) << 8 | (numArray[1] & 248) >> 3;
				numArray[0] = 0;
				numArray[1] = (byte)(numArray[1] & 7);
				numArray[1] = (byte)(numArray[1] | 32);
			}
			int num2 = (int)AmqpBitConverter.ReadUInt(numArray, 0, 4);
			int num3 = (int)AmqpBitConverter.ReadUInt(numArray, 4, 4);
			return DecimalEncoding.CreateDecimal(num3, num2, 0, num, num1 - 398);
		}

		public override object DecodeObject(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
		{
			return DecimalEncoding.Decode(buffer, formatCode);
		}

		private static decimal DecodeValue(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
		{
			decimal num = new decimal(0);
			byte num1 = formatCode;
			if (num1 == 116)
			{
				num = DecimalEncoding.DecodeDecimal32(buffer);
			}
			else if (num1 == 132)
			{
				num = DecimalEncoding.DecodeDecimal64(buffer);
			}
			else
			{
				if (num1 != 148)
				{
					throw AmqpEncoding.GetEncodingException(SRAmqp.AmqpInvalidFormatCode(formatCode, buffer.Offset));
				}
				num = DecimalEncoding.DecodeDecimal128(buffer);
			}
			return num;
		}

		public static void Encode(decimal? value, ByteBuffer buffer)
		{
			if (!value.HasValue)
			{
				AmqpEncoding.EncodeNull(buffer);
				return;
			}
			AmqpBitConverter.WriteUByte(buffer, 148);
			DecimalEncoding.EncodeValue(value.Value, buffer);
		}

		public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
		{
			if (arrayEncoding)
			{
				DecimalEncoding.EncodeValue((decimal)value, buffer);
				return;
			}
			DecimalEncoding.Encode(new decimal?((decimal)value), buffer);
		}

		private static unsafe void EncodeValue(decimal value, ByteBuffer buffer)
		{
			int[] bits = decimal.GetBits(value);
			int num = bits[0];
			int num1 = bits[1];
			int num2 = bits[2];
			int num3 = bits[3];
			byte[] numArray = new byte[16];
			byte* numPointer = (byte*)(&num3);
			int num4 = 6176 - *(numPointer + 2);
			numArray[0] = *(numPointer + 3);
			numArray[0] = (byte)(numArray[0] | (byte)(num4 >> 9));
			numArray[1] = (byte)((num4 & 127) << 1);
			numArray[2] = 0;
			numArray[3] = 0;
			numPointer = (byte*)(&num2);
			numArray[4] = *(numPointer + 3);
			numArray[5] = *(numPointer + 2);
			numArray[6] = *(numPointer + 1);
			numArray[7] = *numPointer;
			numPointer = (byte*)(&num1);
			numArray[8] = *(numPointer + 3);
			numArray[9] = *(numPointer + 2);
			numArray[10] = *(numPointer + 1);
			numArray[11] = *numPointer;
			numPointer = (byte*)(&num);
			numArray[12] = *(numPointer + 3);
			numArray[13] = *(numPointer + 2);
			numArray[14] = *(numPointer + 1);
			numArray[15] = *numPointer;
			AmqpBitConverter.WriteBytes(buffer, numArray, 0, (int)numArray.Length);
		}

		public static int GetEncodeSize(decimal? value)
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
			return 17;
		}
	}
}