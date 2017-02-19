using System;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal sealed class SegmentBufferPool
	{
		private readonly int segmentSize;

		private readonly byte[] heap;

		private readonly int[] offsets;

		private readonly object lockObject = new object();

		private int top;

		public int SegmentSize
		{
			get
			{
				return this.segmentSize;
			}
		}

		public SegmentBufferPool(int segmentSize, int count)
		{
			this.segmentSize = segmentSize;
			this.heap = new byte[segmentSize * count];
			this.offsets = new int[count];
			this.top = count - 1;
			int num = count - 1;
			int num1 = 0;
			while (num >= 0)
			{
				this.offsets[num] = num1;
				num--;
				num1 = num1 + segmentSize;
			}
		}

		public void ReturnBuffer(ArraySegment<byte> buffer)
		{
			if (buffer.Array == this.heap)
			{
				lock (this.lockObject)
				{
					if (this.top < (int)this.offsets.Length - 1)
					{
						SegmentBufferPool segmentBufferPool = this;
						segmentBufferPool.top = segmentBufferPool.top + 1;
						this.offsets[this.top] = buffer.Offset;
					}
				}
			}
		}

		public ArraySegment<byte> TakeBuffer(int bufferSize)
		{
			int num;
			if (bufferSize > this.segmentSize)
			{
				throw new ArgumentOutOfRangeException("bufferSize");
			}
			lock (this.lockObject)
			{
				if (this.top < 0)
				{
					num = -1;
				}
				else
				{
					num = this.offsets[this.top];
					SegmentBufferPool segmentBufferPool = this;
					segmentBufferPool.top = segmentBufferPool.top - 1;
				}
			}
			if (num < 0)
			{
				return new ArraySegment<byte>(new byte[bufferSize]);
			}
			return new ArraySegment<byte>(this.heap, num, bufferSize);
		}
	}
}