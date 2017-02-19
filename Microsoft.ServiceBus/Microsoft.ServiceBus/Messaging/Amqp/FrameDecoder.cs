using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal sealed class FrameDecoder
	{
		private int maxFrameSize;

		private ByteBuffer currentFrameBuffer;

		public FrameDecoder(int maxFrameSize)
		{
			this.maxFrameSize = maxFrameSize;
		}

		public void ExtractFrameBuffers(ByteBuffer buffer, SerializedWorker<ByteBuffer> bufferHandler)
		{
			if (this.currentFrameBuffer != null)
			{
				int num = Math.Min(this.currentFrameBuffer.Size, buffer.Length);
				AmqpBitConverter.WriteBytes(this.currentFrameBuffer, buffer.Buffer, buffer.Offset, num);
				buffer.Complete(num);
				if (this.currentFrameBuffer.Size == 0)
				{
					ByteBuffer byteBuffer = this.currentFrameBuffer;
					this.currentFrameBuffer = null;
					bufferHandler.DoWork(byteBuffer);
				}
			}
			while (buffer.Length >= AmqpCodec.MinimumFrameDecodeSize)
			{
				int frameSize = AmqpCodec.GetFrameSize(buffer);
				if (frameSize < AmqpCodec.MinimumFrameDecodeSize || frameSize > this.maxFrameSize)
				{
					throw new AmqpException(AmqpError.FramingError, SRClient.InvalidFrameSize(frameSize, this.maxFrameSize));
				}
				int num1 = Math.Min(frameSize, buffer.Length);
				this.currentFrameBuffer = new ByteBuffer(frameSize, false);
				AmqpBitConverter.WriteBytes(this.currentFrameBuffer, buffer.Buffer, buffer.Offset, num1);
				buffer.Complete(num1);
				if (frameSize != num1)
				{
					break;
				}
				ByteBuffer byteBuffer1 = this.currentFrameBuffer;
				this.currentFrameBuffer = null;
				bufferHandler.DoWork(byteBuffer1);
			}
		}

		public ProtocolHeader ExtractProtocolHeader(ByteBuffer buffer)
		{
			if (buffer.Length < 8)
			{
				return null;
			}
			ProtocolHeader protocolHeader = new ProtocolHeader();
			protocolHeader.Decode(buffer);
			return protocolHeader;
		}
	}
}