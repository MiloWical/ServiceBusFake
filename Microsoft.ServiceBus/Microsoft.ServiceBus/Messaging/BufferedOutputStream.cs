using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using System;
using System.IO;

namespace Microsoft.ServiceBus.Messaging
{
	internal class BufferedOutputStream : Stream
	{
		private InternalBufferManager bufferManager;

		private byte[][] chunks;

		private int chunkCount;

		private byte[] currentChunk;

		private int currentChunkSize;

		private int maxSize;

		private int maxSizeQuota;

		private int totalSize;

		private bool callerReturnsBuffer;

		private bool bufferReturned;

		private bool initialized;

		public override bool CanRead
		{
			get
			{
				return false;
			}
		}

		public override bool CanSeek
		{
			get
			{
				return false;
			}
		}

		public override bool CanWrite
		{
			get
			{
				return true;
			}
		}

		public override long Length
		{
			get
			{
				return (long)this.totalSize;
			}
		}

		public override long Position
		{
			get
			{
				throw Fx.Exception.AsError(new NotSupportedException(SRClient.SeekNotSupported), null);
			}
			set
			{
				throw Fx.Exception.AsError(new NotSupportedException(SRClient.SeekNotSupported), null);
			}
		}

		public BufferedOutputStream(int initialSize, int maxSize, InternalBufferManager bufferManager) : this()
		{
			this.Reinitialize(initialSize, maxSize, bufferManager);
		}

		private BufferedOutputStream()
		{
			this.chunks = new byte[4][];
		}

		private void AllocNextChunk(int minimumChunkSize)
		{
			int num;
			num = ((int)this.currentChunk.Length <= 1073741823 ? (int)this.currentChunk.Length * 2 : 2147483647);
			if (minimumChunkSize > num)
			{
				num = minimumChunkSize;
			}
			byte[] numArray = this.bufferManager.TakeBuffer(num);
			if (this.chunkCount == (int)this.chunks.Length)
			{
				byte[][] numArray1 = new byte[(int)this.chunks.Length * 2][];
				Array.Copy(this.chunks, numArray1, (int)this.chunks.Length);
				this.chunks = numArray1;
			}
			byte[][] numArray2 = this.chunks;
			BufferedOutputStream bufferedOutputStream = this;
			int num1 = bufferedOutputStream.chunkCount;
			int num2 = num1;
			bufferedOutputStream.chunkCount = num1 + 1;
			numArray2[num2] = numArray;
			this.currentChunk = numArray;
			this.currentChunkSize = 0;
		}

		public override IAsyncResult BeginRead(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
		{
			throw Fx.Exception.AsError(new NotSupportedException(SRClient.ReadNotSupported), null);
		}

		public override IAsyncResult BeginWrite(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
		{
			this.Write(buffer, offset, size);
			return new CompletedAsyncResult(callback, state);
		}

		public void Clear()
		{
			if (!this.callerReturnsBuffer)
			{
				for (int i = 0; i < this.chunkCount; i++)
				{
					this.bufferManager.ReturnBuffer(this.chunks[i]);
					this.chunks[i] = null;
				}
			}
			this.callerReturnsBuffer = false;
			this.initialized = false;
			this.bufferReturned = false;
			this.chunkCount = 0;
			this.currentChunk = null;
		}

		public override void Close()
		{
		}

		protected virtual Exception CreateQuotaExceededException(int maxSizeQuota)
		{
			return new InvalidOperationException(SRClient.BufferedOutputStreamQuotaExceeded(maxSizeQuota));
		}

		protected override void Dispose(bool disposing)
		{
			this.Clear();
			base.Dispose(disposing);
		}

		public override int EndRead(IAsyncResult result)
		{
			throw Fx.Exception.AsError(new NotSupportedException(SRClient.ReadNotSupported), null);
		}

		public override void EndWrite(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
		}

		public override void Flush()
		{
		}

		public override int Read(byte[] buffer, int offset, int size)
		{
			throw Fx.Exception.AsError(new NotSupportedException(SRClient.ReadNotSupported), null);
		}

		public override int ReadByte()
		{
			throw Fx.Exception.AsError(new NotSupportedException(SRClient.ReadNotSupported), null);
		}

		public void Reinitialize(int initialSize, int maxSizeQuota, InternalBufferManager bufferManager)
		{
			this.Reinitialize(initialSize, maxSizeQuota, maxSizeQuota, bufferManager);
		}

		public void Reinitialize(int initialSize, int maxSizeQuota, int effectiveMaxSize, InternalBufferManager bufferManager)
		{
			this.maxSizeQuota = maxSizeQuota;
			this.maxSize = effectiveMaxSize;
			this.bufferManager = bufferManager;
			this.currentChunk = bufferManager.TakeBuffer(initialSize);
			this.currentChunkSize = 0;
			this.totalSize = 0;
			this.chunkCount = 1;
			this.chunks[0] = this.currentChunk;
			this.initialized = true;
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw Fx.Exception.AsError(new NotSupportedException(SRClient.SeekNotSupported), null);
		}

		public override void SetLength(long value)
		{
			throw Fx.Exception.AsError(new NotSupportedException(SRClient.SeekNotSupported), null);
		}

		public void Skip(int size)
		{
			this.WriteCore(null, 0, size);
		}

		public byte[] ToArray(out int bufferSize)
		{
			byte[] numArray;
			if (this.chunkCount != 1)
			{
				numArray = this.bufferManager.TakeBuffer(this.totalSize);
				int length = 0;
				int num = this.chunkCount - 1;
				for (int i = 0; i < num; i++)
				{
					byte[] numArray1 = this.chunks[i];
					Buffer.BlockCopy(numArray1, 0, numArray, length, (int)numArray1.Length);
					length = length + (int)numArray1.Length;
				}
				Buffer.BlockCopy(this.currentChunk, 0, numArray, length, this.currentChunkSize);
				bufferSize = this.totalSize;
			}
			else
			{
				numArray = this.currentChunk;
				bufferSize = this.currentChunkSize;
				this.callerReturnsBuffer = true;
			}
			this.bufferReturned = true;
			return numArray;
		}

		public MemoryStream ToMemoryStream()
		{
			int num;
			return new MemoryStream(this.ToArray(out num), 0, num);
		}

		public override void Write(byte[] buffer, int offset, int size)
		{
			this.WriteCore(buffer, offset, size);
		}

		public override void WriteByte(byte value)
		{
			if (this.totalSize == this.maxSize)
			{
				throw Fx.Exception.AsError(this.CreateQuotaExceededException(this.maxSize), null);
			}
			if (this.currentChunkSize == (int)this.currentChunk.Length)
			{
				this.AllocNextChunk(1);
			}
			byte[] numArray = this.currentChunk;
			BufferedOutputStream bufferedOutputStream = this;
			int num = bufferedOutputStream.currentChunkSize;
			int num1 = num;
			bufferedOutputStream.currentChunkSize = num + 1;
			numArray[num1] = value;
		}

		private void WriteCore(byte[] buffer, int offset, int size)
		{
			if (size < 0)
			{
				throw Fx.Exception.ArgumentOutOfRange("size", size, SRClient.ValueMustBeNonNegative);
			}
			if (2147483647 - size < this.totalSize)
			{
				throw Fx.Exception.AsError(this.CreateQuotaExceededException(this.maxSizeQuota), null);
			}
			int num = this.totalSize + size;
			if (num > this.maxSize)
			{
				throw Fx.Exception.AsError(this.CreateQuotaExceededException(this.maxSizeQuota), null);
			}
			int length = (int)this.currentChunk.Length - this.currentChunkSize;
			if (size > length)
			{
				if (length > 0)
				{
					if (buffer != null)
					{
						Buffer.BlockCopy(buffer, offset, this.currentChunk, this.currentChunkSize, length);
					}
					this.currentChunkSize = (int)this.currentChunk.Length;
					offset = offset + length;
					size = size - length;
				}
				this.AllocNextChunk(size);
			}
			if (buffer != null)
			{
				Buffer.BlockCopy(buffer, offset, this.currentChunk, this.currentChunkSize, size);
			}
			this.totalSize = num;
			BufferedOutputStream bufferedOutputStream = this;
			bufferedOutputStream.currentChunkSize = bufferedOutputStream.currentChunkSize + size;
		}
	}
}