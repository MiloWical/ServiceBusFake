using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging.Amqp;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
	internal static class AmqpBitConverter
	{
		public static uint PeekUInt(ByteBuffer buffer)
		{
			buffer.Validate(false, 4);
			uint num = AmqpBitConverter.ReadUInt(buffer.Buffer, buffer.Offset, buffer.Length);
			return num;
		}

		public static sbyte ReadByte(ByteBuffer buffer)
		{
			buffer.Validate(false, 1);
			sbyte num = (sbyte)buffer.Buffer[buffer.Offset];
			buffer.Complete(1);
			return num;
		}

		public static void ReadBytes(ByteBuffer buffer, byte[] data, int offset, int count)
		{
			buffer.Validate(false, count);
			Buffer.BlockCopy(buffer.Buffer, buffer.Offset, data, offset, count);
			buffer.Complete(count);
		}

		public static unsafe double ReadDouble(ByteBuffer buffer)
		{
			double num = 0;
			buffer.Validate(false, 8);
			fixed (byte* numPointer = &buffer.Buffer[buffer.Offset])
			{
				byte* numPointer1 = (byte*)(&num);
				*numPointer1 = *(numPointer + 7);
				*(numPointer1 + 1) = *(numPointer + 6);
				*(numPointer1 + 2) = *(numPointer + 5);
				*(numPointer1 + 3) = *(numPointer + 4);
				*(numPointer1 + 4) = *(numPointer + 3);
				*(numPointer1 + 5) = *(numPointer + 2);
				*(numPointer1 + 6) = *(numPointer + 1);
				*(numPointer1 + 7) = *numPointer;
			}
			buffer.Complete(8);
			return num;
		}

		public static unsafe float ReadFloat(ByteBuffer buffer)
		{
			float single = 0f;
			buffer.Validate(false, 4);
			fixed (byte* numPointer = &buffer.Buffer[buffer.Offset])
			{
				byte* numPointer1 = (byte*)(&single);
				*numPointer1 = *(numPointer + 3);
				*(numPointer1 + 1) = *(numPointer + 2);
				*(numPointer1 + 2) = *(numPointer + 1);
				*(numPointer1 + 3) = *numPointer;
			}
			buffer.Complete(4);
			return single;
		}

		public static unsafe int ReadInt(ByteBuffer buffer)
		{
			int num = 0;
			buffer.Validate(false, 4);
			fixed (byte* numPointer = &buffer.Buffer[buffer.Offset])
			{
				byte* numPointer1 = (byte*)(&num);
				*numPointer1 = *(numPointer + 3);
				*(numPointer1 + 1) = *(numPointer + 2);
				*(numPointer1 + 2) = *(numPointer + 1);
				*(numPointer1 + 3) = *numPointer;
			}
			buffer.Complete(4);
			return num;
		}

		public static unsafe long ReadLong(ByteBuffer buffer)
		{
			long num = 0L;
			buffer.Validate(false, 8);
			fixed (byte* numPointer = &buffer.Buffer[buffer.Offset])
			{
				byte* numPointer1 = (byte*)(&num);
				*numPointer1 = *(numPointer + 7);
				*(numPointer1 + 1) = *(numPointer + 6);
				*(numPointer1 + 2) = *(numPointer + 5);
				*(numPointer1 + 3) = *(numPointer + 4);
				*(numPointer1 + 4) = *(numPointer + 3);
				*(numPointer1 + 5) = *(numPointer + 2);
				*(numPointer1 + 6) = *(numPointer + 1);
				*(numPointer1 + 7) = *numPointer;
			}
			buffer.Complete(8);
			return num;
		}

		public static unsafe short ReadShort(ByteBuffer buffer)
		{
			short num = 0;
			buffer.Validate(false, 2);
			fixed (byte* numPointer = &buffer.Buffer[buffer.Offset])
			{
				byte* numPointer1 = (byte*)(&num);
				*numPointer1 = *(numPointer + 1);
				*(numPointer1 + 1) = *numPointer;
			}
			buffer.Complete(2);
			return num;
		}

		public static byte ReadUByte(ByteBuffer buffer)
		{
			buffer.Validate(false, 1);
			byte num = buffer.Buffer[buffer.Offset];
			buffer.Complete(1);
			return num;
		}

		public static uint ReadUInt(ByteBuffer buffer)
		{
			buffer.Validate(false, 4);
			uint num = AmqpBitConverter.ReadUInt(buffer.Buffer, buffer.Offset, buffer.Length);
			buffer.Complete(4);
			return num;
		}

		public static unsafe uint ReadUInt(byte[] buffer, int offset, int count)
		{
			uint num = 0;
			AmqpBitConverter.Validate(count, 4);
			fixed (byte* numPointer = &buffer[offset])
			{
				byte* numPointer1 = (byte*)(&num);
				*numPointer1 = *(numPointer + 3);
				*(numPointer1 + 1) = *(numPointer + 2);
				*(numPointer1 + 2) = *(numPointer + 1);
				*(numPointer1 + 3) = *numPointer;
			}
			return num;
		}

		public static ulong ReadULong(ByteBuffer buffer)
		{
			buffer.Validate(false, 8);
			ulong num = AmqpBitConverter.ReadULong(buffer.Buffer, buffer.Offset, buffer.Length);
			buffer.Complete(8);
			return num;
		}

		public static unsafe ulong ReadULong(byte[] buffer, int offset, int count)
		{
			ulong num = 0L;
			AmqpBitConverter.Validate(count, 8);
			fixed (byte* numPointer = &buffer[offset])
			{
				byte* numPointer1 = (byte*)(&num);
				*numPointer1 = *(numPointer + 7);
				*(numPointer1 + 1) = *(numPointer + 6);
				*(numPointer1 + 2) = *(numPointer + 5);
				*(numPointer1 + 3) = *(numPointer + 4);
				*(numPointer1 + 4) = *(numPointer + 3);
				*(numPointer1 + 5) = *(numPointer + 2);
				*(numPointer1 + 6) = *(numPointer + 1);
				*(numPointer1 + 7) = *numPointer;
			}
			return num;
		}

		public static unsafe ushort ReadUShort(ByteBuffer buffer)
		{
			ushort num = 0;
			buffer.Validate(false, 2);
			fixed (byte* numPointer = &buffer.Buffer[buffer.Offset])
			{
				byte* numPointer1 = (byte*)(&num);
				*numPointer1 = *(numPointer + 1);
				*(numPointer1 + 1) = *numPointer;
			}
			buffer.Complete(2);
			return num;
		}

		public static unsafe Guid ReadUuid(ByteBuffer buffer)
		{
			Guid guid = new Guid();
			buffer.Validate(false, 16);
			fixed (byte* numPointer = &buffer.Buffer[buffer.Offset])
			{
				byte* numPointer1 = (byte*)(&guid);
				*numPointer1 = *(numPointer + 3);
				*(numPointer1 + 1) = *(numPointer + 2);
				*(numPointer1 + 2) = *(numPointer + 1);
				*(numPointer1 + 3) = *numPointer;
				*(numPointer1 + 4) = *(numPointer + 5);
				*(numPointer1 + 5) = *(numPointer + 4);
				*(numPointer1 + 6) = *(numPointer + 7);
				*(numPointer1 + 7) = *(numPointer + 6);
				*(numPointer1 + 8) = (byte)(*(numPointer + 8));
			}
			buffer.Complete(16);
			return guid;
		}

		private static void Validate(int bufferSize, int dataSize)
		{
			if (bufferSize < dataSize)
			{
				throw new AmqpException(AmqpError.DecodeError, SRAmqp.AmqpInsufficientBufferSize(dataSize, bufferSize));
			}
		}

		public static void WriteByte(ByteBuffer buffer, sbyte data)
		{
			buffer.Validate(true, 1);
			buffer.Buffer[buffer.WritePos] = (byte)data;
			buffer.Append(1);
		}

		public static void WriteBytes(ByteBuffer buffer, byte[] data, int offset, int count)
		{
			buffer.Validate(true, count);
			Buffer.BlockCopy(data, offset, buffer.Buffer, buffer.WritePos, count);
			buffer.Append(count);
		}

		public static unsafe void WriteDouble(ByteBuffer buffer, double data)
		{
			buffer.Validate(true, 8);
			fixed (byte* numPointer = &buffer.Buffer[buffer.WritePos])
			{
				byte* numPointer1 = (byte*)(&data);
				*numPointer = *(numPointer1 + 7);
				*(numPointer + 1) = *(numPointer1 + 6);
				*(numPointer + 2) = *(numPointer1 + 5);
				*(numPointer + 3) = *(numPointer1 + 4);
				*(numPointer + 4) = *(numPointer1 + 3);
				*(numPointer + 5) = *(numPointer1 + 2);
				*(numPointer + 6) = *(numPointer1 + 1);
				*(numPointer + 7) = *numPointer1;
			}
			buffer.Append(8);
		}

		public static unsafe void WriteFloat(ByteBuffer buffer, float data)
		{
			buffer.Validate(true, 4);
			fixed (byte* numPointer = &buffer.Buffer[buffer.WritePos])
			{
				byte* numPointer1 = (byte*)(&data);
				*numPointer = *(numPointer1 + 3);
				*(numPointer + 1) = *(numPointer1 + 2);
				*(numPointer + 2) = *(numPointer1 + 1);
				*(numPointer + 3) = *numPointer1;
			}
			buffer.Append(4);
		}

		public static unsafe void WriteInt(ByteBuffer buffer, int data)
		{
			buffer.Validate(true, 4);
			fixed (byte* numPointer = &buffer.Buffer[buffer.WritePos])
			{
				byte* numPointer1 = (byte*)(&data);
				*numPointer = *(numPointer1 + 3);
				*(numPointer + 1) = *(numPointer1 + 2);
				*(numPointer + 2) = *(numPointer1 + 1);
				*(numPointer + 3) = *numPointer1;
			}
			buffer.Append(4);
		}

		public static unsafe void WriteLong(ByteBuffer buffer, long data)
		{
			buffer.Validate(true, 8);
			fixed (byte* numPointer = &buffer.Buffer[buffer.WritePos])
			{
				byte* numPointer1 = (byte*)(&data);
				*numPointer = *(numPointer1 + 7);
				*(numPointer + 1) = *(numPointer1 + 6);
				*(numPointer + 2) = *(numPointer1 + 5);
				*(numPointer + 3) = *(numPointer1 + 4);
				*(numPointer + 4) = *(numPointer1 + 3);
				*(numPointer + 5) = *(numPointer1 + 2);
				*(numPointer + 6) = *(numPointer1 + 1);
				*(numPointer + 7) = *numPointer1;
			}
			buffer.Append(8);
		}

		public static unsafe void WriteShort(ByteBuffer buffer, short data)
		{
			buffer.Validate(true, 2);
			fixed (byte* numPointer = &buffer.Buffer[buffer.WritePos])
			{
				byte* numPointer1 = (byte*)(&data);
				*numPointer = *(numPointer1 + 1);
				*(numPointer + 1) = *numPointer1;
			}
			buffer.Append(2);
		}

		public static void WriteUByte(ByteBuffer buffer, byte data)
		{
			buffer.Validate(true, 1);
			buffer.Buffer[buffer.WritePos] = data;
			buffer.Append(1);
		}

		public static void WriteUByte(byte[] buffer, int offset, byte data)
		{
			AmqpBitConverter.Validate((int)buffer.Length - offset, 1);
			buffer[offset] = data;
		}

		public static unsafe void WriteUInt(ByteBuffer buffer, uint data)
		{
			buffer.Validate(true, 4);
			fixed (byte* numPointer = &buffer.Buffer[buffer.WritePos])
			{
				byte* numPointer1 = (byte*)(&data);
				*numPointer = *(numPointer1 + 3);
				*(numPointer + 1) = *(numPointer1 + 2);
				*(numPointer + 2) = *(numPointer1 + 1);
				*(numPointer + 3) = *numPointer1;
			}
			buffer.Append(4);
		}

		public static unsafe void WriteUInt(byte[] buffer, int offset, uint data)
		{
			AmqpBitConverter.Validate((int)buffer.Length - offset, 4);
			fixed (byte* numPointer = &buffer[offset])
			{
				byte* numPointer1 = (byte*)(&data);
				*numPointer = *(numPointer1 + 3);
				*(numPointer + 1) = *(numPointer1 + 2);
				*(numPointer + 2) = *(numPointer1 + 1);
				*(numPointer + 3) = *numPointer1;
			}
		}

		public static unsafe void WriteULong(ByteBuffer buffer, ulong data)
		{
			buffer.Validate(true, 8);
			fixed (byte* numPointer = &buffer.Buffer[buffer.WritePos])
			{
				byte* numPointer1 = (byte*)(&data);
				*numPointer = *(numPointer1 + 7);
				*(numPointer + 1) = *(numPointer1 + 6);
				*(numPointer + 2) = *(numPointer1 + 5);
				*(numPointer + 3) = *(numPointer1 + 4);
				*(numPointer + 4) = *(numPointer1 + 3);
				*(numPointer + 5) = *(numPointer1 + 2);
				*(numPointer + 6) = *(numPointer1 + 1);
				*(numPointer + 7) = *numPointer1;
			}
			buffer.Append(8);
		}

		public static unsafe void WriteUShort(ByteBuffer buffer, ushort data)
		{
			buffer.Validate(true, 2);
			fixed (byte* numPointer = &buffer.Buffer[buffer.WritePos])
			{
				byte* numPointer1 = (byte*)(&data);
				*numPointer = *(numPointer1 + 1);
				*(numPointer + 1) = *numPointer1;
			}
			buffer.Append(2);
		}

		public static unsafe void WriteUShort(byte[] buffer, int offset, ushort data)
		{
			AmqpBitConverter.Validate((int)buffer.Length - offset, 2);
			fixed (byte* numPointer = &buffer[offset])
			{
				byte* numPointer1 = (byte*)(&data);
				*numPointer = *(numPointer1 + 1);
				*(numPointer + 1) = *numPointer1;
			}
		}

		public static unsafe void WriteUuid(ByteBuffer buffer, Guid data)
		{
			buffer.Validate(true, 16);
			fixed (byte* numPointer = &buffer.Buffer[buffer.WritePos])
			{
				byte* numPointer1 = (byte*)(&data);
				*numPointer = *(numPointer1 + 3);
				*(numPointer + 1) = *(numPointer1 + 2);
				*(numPointer + 2) = *(numPointer1 + 1);
				*(numPointer + 3) = *numPointer1;
				*(numPointer + 4) = *(numPointer1 + 5);
				*(numPointer + 5) = *(numPointer1 + 4);
				*(numPointer + 6) = *(numPointer1 + 7);
				*(numPointer + 7) = *(numPointer1 + 6);
				*(numPointer + 8) = (byte)(*(numPointer1 + 8));
			}
			buffer.Append(16);
		}
	}
}