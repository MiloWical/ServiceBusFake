using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.ServiceBus.Messaging
{
	internal class BufferedInputStream : Stream, ICloneable
	{
		private BufferedInputStream.BufferManagerByteArray data;

		private MemoryStream innerStream;

		private bool disposed;

		public byte[] Buffer
		{
			get
			{
				this.ThrowIfDisposed();
				return this.data.Bytes;
			}
		}

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
				return this.innerStream.Length;
			}
		}

		public override long Position
		{
			get
			{
				this.ThrowIfDisposed();
				return this.innerStream.Position;
			}
			set
			{
				this.ThrowIfDisposed();
				this.innerStream.Position = value;
			}
		}

		public BufferedInputStream(byte[] bytes, int bufferSize, InternalBufferManager bufferManager)
		{
			this.data = new BufferedInputStream.BufferManagerByteArray(bytes, bufferManager);
			this.innerStream = new MemoryStream(bytes, 0, bufferSize);
		}

		private BufferedInputStream(BufferedInputStream.BufferManagerByteArray data, int bufferSize)
		{
			this.data = data;
			this.data.AddReference();
			this.innerStream = new MemoryStream(data.Bytes, 0, bufferSize);
		}

		public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			this.ThrowIfDisposed();
			return new CompletedAsyncResult<int>(this.innerStream.Read(buffer, offset, count), callback, state);
		}

		public object Clone()
		{
			this.ThrowIfDisposed();
			return new BufferedInputStream(this.data, (int)this.innerStream.Length);
		}

		protected override void Dispose(bool disposing)
		{
			try
			{
				if (!this.disposed && disposing)
				{
					if (disposing)
					{
						this.innerStream.Dispose();
					}
					this.data.RemoveReference();
					this.disposed = true;
				}
			}
			finally
			{
				base.Dispose(disposing);
			}
		}

		public override int EndRead(IAsyncResult asyncResult)
		{
			return CompletedAsyncResult<int>.End(asyncResult);
		}

		public override void Flush()
		{
			throw FxTrace.Exception.AsError(new NotSupportedException(), null);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			this.ThrowIfDisposed();
			return this.innerStream.Read(buffer, offset, count);
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			this.ThrowIfDisposed();
			return this.innerStream.Seek(offset, origin);
		}

		public override void SetLength(long value)
		{
			throw FxTrace.Exception.AsError(new NotSupportedException(), null);
		}

		private void ThrowIfDisposed()
		{
			if (this.disposed)
			{
				throw FxTrace.Exception.AsError(new ObjectDisposedException("BufferedInputStream"), null);
			}
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw FxTrace.Exception.AsError(new NotSupportedException(), null);
		}

		private sealed class BufferManagerByteArray
		{
			private volatile int references;

			private InternalBufferManager BufferManager
			{
				get;
				set;
			}

			public byte[] Bytes
			{
				get;
				private set;
			}

			public BufferManagerByteArray(byte[] bytes, InternalBufferManager bufferManager)
			{
				this.Bytes = bytes;
				this.BufferManager = bufferManager;
				this.references = 1;
			}

			public void AddReference()
			{
				if (Interlocked.Increment(ref this.references) == 1)
				{
					throw FxTrace.Exception.AsError(new InvalidOperationException(SRClient.BufferAlreadyReclaimed), null);
				}
			}

			public void RemoveReference()
			{
				if (this.references > 0 && Interlocked.Decrement(ref this.references) == 0)
				{
					this.BufferManager.ReturnBuffer(this.Bytes);
					this.Bytes = null;
				}
			}
		}
	}
}