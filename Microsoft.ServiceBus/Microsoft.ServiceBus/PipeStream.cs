using Microsoft.ServiceBus.Common;
using System;
using System.IO;
using System.Threading;

namespace Microsoft.ServiceBus
{
	internal class PipeStream : Stream
	{
		private InputQueue<NullableArray<byte>> dataChunks;

		private byte[] currentChunk;

		private int currentChunkPosition;

		private volatile bool isShuttingDown;

		private ManualResetEvent done;

		private int naglingDelay;

		private int readTimeout;

		private int writeTimeout;

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
				return false;
			}
		}

		public override bool CanTimeout
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
				return !this.isShuttingDown;
			}
		}

		protected InputQueue<NullableArray<byte>> DataChunksQueue
		{
			get
			{
				return this.dataChunks;
			}
		}

		public override long Length
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		public override long Position
		{
			get
			{
				throw new NotSupportedException();
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		public override int ReadTimeout
		{
			get
			{
				return this.readTimeout;
			}
			set
			{
				this.readTimeout = value;
			}
		}

		protected WaitHandle ShutdownEvent
		{
			get
			{
				return this.done;
			}
		}

		public override int WriteTimeout
		{
			get
			{
				return this.writeTimeout;
			}
			set
			{
				this.writeTimeout = value;
			}
		}

		public PipeStream() : this(TimeSpan.Zero)
		{
		}

		public PipeStream(TimeSpan naglingDelay)
		{
			this.naglingDelay = Microsoft.ServiceBus.Common.TimeoutHelper.ToMilliseconds(naglingDelay);
			this.done = new ManualResetEvent(false);
			this.dataChunks = new InputQueue<NullableArray<byte>>();
			int num = -1;
			int num1 = num;
			this.writeTimeout = num;
			this.readTimeout = num1;
		}

		protected override void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				this.disposed = true;
				try
				{
					if (disposing)
					{
						this.isShuttingDown = true;
						this.done.Set();
						this.done.Close();
						this.dataChunks.Dispose();
					}
				}
				finally
				{
					base.Dispose(disposing);
				}
			}
		}

		protected virtual void EnqueueChunk(byte[] chunk)
		{
			this.dataChunks.EnqueueAndDispatch(new NullableArray<byte>(chunk));
		}

		public override void Flush()
		{
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			NullableArray<byte> nullableArray;
			bool flag;
			int num;
			if (this.isShuttingDown)
			{
				return 0;
			}
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer");
			}
			if (offset < 0 || offset >= (int)buffer.Length)
			{
				throw new ArgumentOutOfRangeException("offset");
			}
			if (count < 0 || offset + count > (int)buffer.Length)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			if (count == 0)
			{
				return 0;
			}
			bool flag1 = true;
			int num1 = 0;
			while (true)
			{
				if (this.currentChunk != null)
				{
					flag1 = false;
				}
				else
				{
					if (!flag1)
					{
						flag = this.dataChunks.Dequeue(Microsoft.ServiceBus.Common.TimeoutHelper.FromMilliseconds(this.naglingDelay), out nullableArray);
						if (!flag || flag && (nullableArray == NullableArray<byte>.NullArray || nullableArray == null))
						{
							break;
						}
						this.currentChunk = nullableArray.Value;
					}
					else
					{
						IAsyncResult asyncResult = this.dataChunks.BeginDequeue(Microsoft.ServiceBus.Common.TimeoutHelper.FromMilliseconds(this.readTimeout), null, null);
						if (!asyncResult.CompletedSynchronously)
						{
							WaitHandle[] asyncWaitHandle = new WaitHandle[] { asyncResult.AsyncWaitHandle, this.done };
							if (WaitHandle.WaitAny(asyncWaitHandle) == 1)
							{
								return 0;
							}
						}
						NullableArray<byte> nullableArray1 = this.dataChunks.EndDequeue(asyncResult);
						if (nullableArray1 == NullableArray<byte>.NullArray || nullableArray1 == null)
						{
							this.isShuttingDown = true;
							return 0;
						}
						this.currentChunk = nullableArray1.Value;
						flag1 = false;
					}
					this.currentChunkPosition = 0;
				}
				int length = (int)this.currentChunk.Length - this.currentChunkPosition;
				if (length > count)
				{
					num = count;
					Buffer.BlockCopy(this.currentChunk, this.currentChunkPosition, buffer, offset, count);
					PipeStream pipeStream = this;
					pipeStream.currentChunkPosition = pipeStream.currentChunkPosition + count;
					return num1 + num;
				}
				num = length;
				Buffer.BlockCopy(this.currentChunk, this.currentChunkPosition, buffer, offset, num);
				this.currentChunk = null;
				this.currentChunkPosition = 0;
				num1 = num1 + num;
				offset = offset + num;
				count = count - num;
			}
			if (flag && (nullableArray == NullableArray<byte>.NullArray || nullableArray == null))
			{
				this.isShuttingDown = true;
			}
			return num1;
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		public void SetEndOfStream()
		{
			this.isShuttingDown = true;
			this.done.Set();
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			if (this.isShuttingDown)
			{
				throw new EndOfStreamException();
			}
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer");
			}
			if (offset < 0 || offset >= (int)buffer.Length)
			{
				throw new ArgumentOutOfRangeException("offset");
			}
			if (count < 0 || offset + count > (int)buffer.Length)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			if (count == 0)
			{
				return;
			}
			byte[] numArray = new byte[count];
			Buffer.BlockCopy(buffer, offset, numArray, 0, count);
			this.EnqueueChunk(numArray);
		}

		public void WriteEndOfStream()
		{
			if (!this.isShuttingDown)
			{
				this.dataChunks.EnqueueAndDispatch(NullableArray<byte>.NullArray);
			}
		}
	}
}