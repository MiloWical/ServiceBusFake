using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class Frame : IDisposable
	{
		public const int HeaderSize = 8;

		private const byte DefaultDataOffset = 2;

		public ushort Channel
		{
			get;
			set;
		}

		public Performative Command
		{
			get;
			set;
		}

		public byte DataOffset
		{
			get;
			private set;
		}

		public ArraySegment<byte> Payload
		{
			get;
			set;
		}

		public ByteBuffer RawByteBuffer
		{
			get;
			private set;
		}

		public int Size
		{
			get;
			private set;
		}

		public FrameType Type
		{
			get;
			private set;
		}

		public Frame() : this(FrameType.Amqp)
		{
		}

		public Frame(FrameType type)
		{
			this.Type = type;
			this.DataOffset = 2;
		}

		public void Decode(ByteBuffer buffer)
		{
			this.RawByteBuffer = buffer;
			int offset = buffer.Offset;
			int length = buffer.Length;
			this.DecodeHeader(buffer);
			this.DecodeCommand(buffer);
			this.DecodePayload(buffer);
			buffer.AdjustPosition(offset, length);
		}

		private void DecodeCommand(ByteBuffer buffer)
		{
			if (buffer.Length > 0)
			{
				this.Command = (Performative)AmqpCodec.DecodeAmqpDescribed(buffer);
			}
		}

		private void DecodeHeader(ByteBuffer buffer)
		{
			this.Size = (int)AmqpBitConverter.ReadUInt(buffer);
			this.DataOffset = AmqpBitConverter.ReadUByte(buffer);
			this.Type = (FrameType)AmqpBitConverter.ReadUByte(buffer);
			this.Channel = AmqpBitConverter.ReadUShort(buffer);
			buffer.Complete(this.DataOffset * 4 - 8);
		}

		private void DecodePayload(ByteBuffer buffer)
		{
			if (buffer.Length > 0)
			{
				this.Payload = new ArraySegment<byte>(buffer.Buffer, buffer.Offset, buffer.Length);
			}
		}

		public void Dispose()
		{
			if (this.RawByteBuffer != null)
			{
				this.RawByteBuffer.Dispose();
			}
		}

		public static ByteBuffer EncodeCommand(FrameType type, ushort channel, Performative command, int payloadSize)
		{
			int serializableEncodeSize = 8;
			if (command != null)
			{
				serializableEncodeSize = serializableEncodeSize + AmqpCodec.GetSerializableEncodeSize(command);
			}
			serializableEncodeSize = serializableEncodeSize + payloadSize;
			ByteBuffer byteBuffer = new ByteBuffer(serializableEncodeSize, false, false);
			AmqpBitConverter.WriteUInt(byteBuffer, (uint)serializableEncodeSize);
			AmqpBitConverter.WriteUByte(byteBuffer, 2);
			AmqpBitConverter.WriteUByte(byteBuffer, (byte)type);
			AmqpBitConverter.WriteUShort(byteBuffer, channel);
			if (command != null)
			{
				AmqpCodec.EncodeSerializable(command, byteBuffer);
			}
			return byteBuffer;
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] size = new object[] { this.Size, this.DataOffset, (byte)this.Type, this.Channel };
			stringBuilder.AppendFormat(invariantCulture, "FRM({0:X4}|{1}|{2}|{3:X2}", size);
			if (this.Command != null)
			{
				CultureInfo cultureInfo = CultureInfo.InvariantCulture;
				object[] command = new object[] { this.Command };
				stringBuilder.AppendFormat(cultureInfo, "  {0}", command);
			}
			if (this.Payload.Count > 0)
			{
				CultureInfo invariantCulture1 = CultureInfo.InvariantCulture;
				object[] count = new object[] { this.Payload.Count };
				stringBuilder.AppendFormat(invariantCulture1, ",{0}", count);
			}
			stringBuilder.Append(')');
			return stringBuilder.ToString();
		}
	}
}