using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal sealed class BufferListStream : Stream, ICloneable
	{
		private IList<ArraySegment<byte>> bufferList;

		private int readArray;

		private int readOffset;

		private long length;

		private long position;

		private bool disposed;

		public override bool CanRead
		{
			get
			{
				return true;
			}
		}

		public override bool CanSeek
		{
			get
			{
				return true;
			}
		}

		public override bool CanWrite
		{
			get
			{
				return false;
			}
		}

		public override long Length
		{
			get
			{
				this.ThrowIfDisposed();
				return this.length;
			}
		}

		public override long Position
		{
			get
			{
				this.ThrowIfDisposed();
				return this.position;
			}
			set
			{
				this.ThrowIfDisposed();
				this.SetPosition(value);
			}
		}

		public BufferListStream(IList<ArraySegment<byte>> bufferList)
		{
			this.bufferList = bufferList;
			for (int i = 0; i < this.bufferList.Count; i++)
			{
				BufferListStream count = this;
				long num = count.length;
				ArraySegment<byte> item = this.bufferList[i];
				count.length = num + (long)item.Count;
			}
		}

		private void Advance(int count, int segmentCount)
		{
			if (count > segmentCount)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			BufferListStream bufferListStream = this;
			bufferListStream.position = bufferListStream.position + (long)count;
			BufferListStream bufferListStream1 = this;
			bufferListStream1.readOffset = bufferListStream1.readOffset + count;
			if (this.readOffset == segmentCount)
			{
				BufferListStream bufferListStream2 = this;
				bufferListStream2.readArray = bufferListStream2.readArray + 1;
				this.readOffset = 0;
			}
		}

		public object Clone()
		{
			this.ThrowIfDisposed();
			return new BufferListStream(this.bufferList);
		}

		public static BufferListStream Create(Stream stream, int segmentSize)
		{
			return BufferListStream.Create(stream, segmentSize, false);
		}

		public static BufferListStream Create(Stream stream, int segmentSize, bool forceCopyStream)
		{
			BufferListStream bufferListStream;
			int num;
			if (stream == null)
			{
				throw FxTrace.Exception.ArgumentNull("stream");
			}
			if (!(stream is BufferListStream) || forceCopyStream)
			{
				stream.Position = (long)0;
				bufferListStream = new BufferListStream(BufferListStream.ReadStream(stream, segmentSize, out num));
			}
			else
			{
				bufferListStream = (BufferListStream)((BufferListStream)stream).Clone();
			}
			return bufferListStream;
		}

		protected override void Dispose(bool disposing)
		{
			try
			{
				if (!this.disposed && disposing)
				{
					this.bufferList = null;
					this.disposed = true;
				}
			}
			finally
			{
				base.Dispose(disposing);
			}
		}

		public override void Flush()
		{
			throw new InvalidOperationException();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			this.ThrowIfDisposed();
			if (this.readArray == this.bufferList.Count)
			{
				return 0;
			}
			int num = 0;
			while (count > 0 && this.readArray < this.bufferList.Count)
			{
				ArraySegment<byte> item = this.bufferList[this.readArray];
				int num1 = item.Count - this.readOffset;
				int num2 = Math.Min(num1, count);
				Buffer.BlockCopy(item.Array, item.Offset + this.readOffset, buffer, offset, num2);
				this.Advance(num2, item.Count);
				count = count - num2;
				offset = offset + num2;
				num = num + num2;
			}
			return num;
		}

		public ArraySegment<byte>[] ReadBuffers(int count, bool advance, out bool more)
		{
			this.ThrowIfDisposed();
			more = false;
			if (this.readArray == this.bufferList.Count)
			{
				return null;
			}
			List<ArraySegment<byte>> arraySegments = new List<ArraySegment<byte>>();
			int num = this.readArray;
			int num1 = this.readOffset;
			long num2 = this.position;
			while (count > 0 && this.readArray < this.bufferList.Count)
			{
				ArraySegment<byte> item = this.bufferList[this.readArray];
				int num3 = item.Count - this.readOffset;
				int num4 = Math.Min(num3, count);
				arraySegments.Add(new ArraySegment<byte>(item.Array, item.Offset + this.readOffset, num4));
				this.Advance(num4, item.Count);
				count = count - num4;
			}
			more = this.readArray < this.bufferList.Count;
			if (!advance)
			{
				this.readArray = num;
				this.readOffset = num1;
				this.position = num2;
			}
			return arraySegments.ToArray();
		}

		public override int ReadByte()
		{
			this.ThrowIfDisposed();
			if (this.readArray == this.bufferList.Count)
			{
				return -1;
			}
			ArraySegment<byte> item = this.bufferList[this.readArray];
			int array = item.Array[item.Offset + this.readOffset];
			this.Advance(1, item.Count);
			return array;
		}

		public ArraySegment<byte> ReadBytes(int count)
		{
			this.ThrowIfDisposed();
			if (this.readArray == this.bufferList.Count)
			{
				return new ArraySegment<byte>();
			}
			ArraySegment<byte> item = this.bufferList[this.readArray];
			if (item.Count - this.readOffset >= count)
			{
				int num = item.Count;
				item = new ArraySegment<byte>(item.Array, item.Offset + this.readOffset, count);
				this.Advance(count, num);
				return item;
			}
			count = Math.Min(count, (int)(this.length - this.position));
			byte[] numArray = new byte[count];
			this.Read(numArray, 0, count);
			item = new ArraySegment<byte>(numArray);
			return item;
		}

		public static ArraySegment<byte>[] ReadStream(Stream stream, int segmentSize, out int length)
		{
			if (stream == null)
			{
				throw FxTrace.Exception.ArgumentNull("stream");
			}
			length = 0;
			List<ArraySegment<byte>> arraySegments = new List<ArraySegment<byte>>();
			while (true)
			{
				byte[] numArray = new byte[segmentSize];
				int num = stream.Read(numArray, 0, (int)numArray.Length);
				if (num == 0)
				{
					break;
				}
				arraySegments.Add(new ArraySegment<byte>(numArray, 0, num));
				length = length + num;
			}
			return arraySegments.ToArray();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			this.ThrowIfDisposed();
			long num = (long)0;
			if (origin == SeekOrigin.Begin)
			{
				num = offset;
			}
			else if (origin == SeekOrigin.Current)
			{
				num = num + this.position + offset;
			}
			else if (origin == SeekOrigin.End)
			{
				num = this.length + offset;
			}
			this.SetPosition(num);
			return num;
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		private void SetPosition(long pos)
		{
			int i;
			if (pos < (long)0)
			{
				throw new ArgumentOutOfRangeException("position");
			}
			this.position = pos;
			for (i = 0; i < this.bufferList.Count && pos > (long)0 && pos >= (long)this.bufferList[i].Count; i++)
			{
				ArraySegment<byte> item = this.bufferList[i];
				pos = pos - (long)item.Count;
			}
			this.readArray = i;
			this.readOffset = (int)pos;
		}

		private void ThrowIfDisposed()
		{
			if (this.disposed)
			{
				throw new ObjectDisposedException(base.GetType().Name);
			}
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}
	}
}