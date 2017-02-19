using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class Data : AmqpDescribed
	{
		public readonly static string Name;

		public readonly static ulong Code;

		static Data()
		{
			Data.Name = "amqp:data:binary";
			Data.Code = (ulong)117;
		}

		public Data() : base(Data.Name, Data.Code)
		{
		}

		public override void DecodeValue(ByteBuffer buffer)
		{
			base.Value = BinaryEncoding.Decode(buffer, 0);
		}

		public override void EncodeValue(ByteBuffer buffer)
		{
			BinaryEncoding.Encode((ArraySegment<byte>)base.Value, buffer);
		}

		public static ArraySegment<byte> GetEncodedPrefix(int valueLength)
		{
			int num;
			byte[] code = new byte[] { 0, 83, (byte)Data.Code, 0, 0, 0, 0, 0 };
			byte[] numArray = code;
			if (valueLength > 255)
			{
				numArray[3] = 176;
				AmqpBitConverter.WriteUInt(numArray, 4, (uint)valueLength);
				num = 8;
			}
			else
			{
				numArray[3] = 160;
				numArray[4] = (byte)valueLength;
				num = 5;
			}
			return new ArraySegment<byte>(numArray, 0, num);
		}

		public override int GetValueEncodeSize()
		{
			return BinaryEncoding.GetEncodeSize((ArraySegment<byte>)base.Value);
		}

		public override string ToString()
		{
			return "data()";
		}
	}
}