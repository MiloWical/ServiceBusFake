using Microsoft.ServiceBus.Messaging.Amqp;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
	internal sealed class BinaryEncoding : EncodingBase
	{
		public BinaryEncoding() : base(176)
		{
		}

		public static ArraySegment<byte> Decode(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
		{
			return BinaryEncoding.Decode(buffer, formatCode, true);
		}

		public static ArraySegment<byte> Decode(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode, bool copy)
		{
			int num;
			ArraySegment<byte> nums;
			if (formatCode == 0)
			{
				Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode1 = AmqpEncoding.ReadFormatCode(buffer);
				formatCode = formatCode1;
				if (formatCode1 == 64)
				{
					return AmqpConstants.NullBinary;
				}
			}
			AmqpEncoding.ReadCount(buffer, formatCode, 160, 176, out num);
			if (num == 0)
			{
				return AmqpConstants.EmptyBinary;
			}
			if (!copy)
			{
				nums = new ArraySegment<byte>(buffer.Buffer, buffer.Offset, num);
			}
			else
			{
				byte[] numArray = new byte[num];
				Buffer.BlockCopy(buffer.Buffer, buffer.Offset, numArray, 0, num);
				nums = new ArraySegment<byte>(numArray, 0, num);
			}
			buffer.Complete(num);
			return nums;
		}

		public override object DecodeObject(ByteBuffer buffer, Microsoft.ServiceBus.Messaging.Amqp.Encoding.FormatCode formatCode)
		{
			return BinaryEncoding.Decode(buffer, formatCode);
		}

		public static void Encode(ArraySegment<byte> value, ByteBuffer buffer)
		{
			if (value.Array == null)
			{
				AmqpEncoding.EncodeNull(buffer);
				return;
			}
			if (AmqpEncoding.GetEncodeWidthBySize(value.Count) != 1)
			{
				AmqpBitConverter.WriteUByte(buffer, 176);
				AmqpBitConverter.WriteUInt(buffer, (uint)value.Count);
			}
			else
			{
				AmqpBitConverter.WriteUByte(buffer, 160);
				AmqpBitConverter.WriteUByte(buffer, (byte)value.Count);
			}
			AmqpBitConverter.WriteBytes(buffer, value.Array, value.Offset, value.Count);
		}

		public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
		{
			if (!arrayEncoding)
			{
				BinaryEncoding.Encode((ArraySegment<byte>)value, buffer);
				return;
			}
			ArraySegment<byte> nums = (ArraySegment<byte>)value;
			AmqpBitConverter.WriteUInt(buffer, (uint)nums.Count);
			AmqpBitConverter.WriteBytes(buffer, nums.Array, nums.Offset, nums.Count);
		}

		public static int GetEncodeSize(ArraySegment<byte> value)
		{
			if (value.Array == null)
			{
				return 1;
			}
			return 1 + AmqpEncoding.GetEncodeWidthBySize(value.Count) + value.Count;
		}

		public override int GetObjectEncodeSize(object value, bool arrayEncoding)
		{
			if (!arrayEncoding)
			{
				return BinaryEncoding.GetEncodeSize((ArraySegment<byte>)value);
			}
			return 4 + ((ArraySegment<byte>)value).Count;
		}
	}
}