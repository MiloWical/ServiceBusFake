using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Threading;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal sealed class ByteBuffer : IDisposable, ICloneable
	{
		private readonly static InternalBufferManager BufferManager;

		private static InternalBufferManager TransportBufferManager;

		private static object syncRoot;

		private byte[] buffer;

		private int start;

		private int read;

		private int write;

		private int end;

		private bool autoGrow;

		private int references;

		private InternalBufferManager bufferManager;

		public byte[] Buffer
		{
			get
			{
				return this.buffer;
			}
		}

		public int Capacity
		{
			get
			{
				return this.end - this.start;
			}
		}

		public int Length
		{
			get
			{
				return this.write - this.read;
			}
		}

		public int Offset
		{
			get
			{
				return this.read;
			}
		}

		public int Size
		{
			get
			{
				return this.end - this.write;
			}
		}

		public int WritePos
		{
			get
			{
				return this.write;
			}
		}

		static ByteBuffer()
		{
			ByteBuffer.BufferManager = InternalBufferManager.Create((long)52428800, 2147483647, false);
			ByteBuffer.syncRoot = new object();
		}

		public ByteBuffer(byte[] buffer) : this(buffer, 0, 0, (int)buffer.Length, false, null)
		{
		}

		public ByteBuffer(byte[] buffer, bool autoGrow) : this(buffer, 0, 0, (int)buffer.Length, autoGrow, null)
		{
		}

		public ByteBuffer(ArraySegment<byte> array) : this(array.Array, array.Offset, array.Count, array.Count, false, null)
		{
		}

		public ByteBuffer(int size, bool autoGrow) : this(size, autoGrow, false)
		{
		}

		public ByteBuffer(int size, bool autoGrow, bool isTransportBuffer) : this(ByteBuffer.AllocateBufferFromPool(size, isTransportBuffer), autoGrow, size)
		{
		}

		public ByteBuffer(byte[] buffer, int offset, int count) : this(buffer, offset, count, count, false, null)
		{
		}

		private ByteBuffer(ByteBuffer.ManagedBuffer bufferReference, bool autoGrow, int size) : this(bufferReference.Buffer, 0, 0, size, autoGrow, bufferReference.BufferManager)
		{
		}

		private ByteBuffer(byte[] buffer, int offset, int count, int size, bool autoGrow, InternalBufferManager bufferManager)
		{
			this.buffer = buffer;
			this.start = offset;
			this.read = offset;
			this.write = offset + count;
			this.end = offset + size;
			this.autoGrow = autoGrow;
			this.bufferManager = bufferManager;
			this.references = 1;
		}

		private void AddReference()
		{
			if (Interlocked.Increment(ref this.references) == 1)
			{
				Interlocked.Decrement(ref this.references);
				throw FxTrace.Exception.AsError(new InvalidOperationException(SRAmqp.AmqpBufferAlreadyReclaimed), null);
			}
		}

		public void AdjustPosition(int offset, int length)
		{
			this.read = offset;
			this.write = this.read + length;
		}

		private static ByteBuffer.ManagedBuffer AllocateBuffer(int size, InternalBufferManager bufferManager)
		{
			if (bufferManager != null)
			{
				byte[] numArray = bufferManager.TakeBuffer(size);
				if (numArray != null)
				{
					return new ByteBuffer.ManagedBuffer(numArray, bufferManager);
				}
			}
			return new ByteBuffer.ManagedBuffer(ByteBuffer.BufferManager.TakeBuffer(size), ByteBuffer.BufferManager);
		}

		private static ByteBuffer.ManagedBuffer AllocateBufferFromPool(int size, bool isTransportBuffer)
		{
			return ByteBuffer.AllocateBuffer(size, (isTransportBuffer ? ByteBuffer.TransportBufferManager : ByteBuffer.BufferManager));
		}

		public void Append(int size)
		{
			ByteBuffer byteBuffer = this;
			byteBuffer.write = byteBuffer.write + size;
		}

		public object Clone()
		{
			this.AddReference();
			return this;
		}

		public void Complete(int size)
		{
			ByteBuffer byteBuffer = this;
			byteBuffer.read = byteBuffer.read + size;
		}

		public void Dispose()
		{
			this.RemoveReference();
		}

		public static void InitBufferManagers()
		{
			if (ByteBuffer.TransportBufferManager == null)
			{
				lock (ByteBuffer.syncRoot)
				{
					if (ByteBuffer.TransportBufferManager == null)
					{
						ByteBuffer.TransportBufferManager = InternalBufferManager.Create((long)50331648, 65536, true);
					}
				}
			}
		}

		private void RemoveReference()
		{
			if (this.references > 0 && Interlocked.Decrement(ref this.references) == 0)
			{
				byte[] numArray = this.buffer;
				this.buffer = null;
				if (this.bufferManager != null)
				{
					this.bufferManager.ReturnBuffer(numArray);
				}
			}
		}

		public void Reset()
		{
			this.read = this.start;
			this.write = this.start;
		}

		public void Seek(int seekPosition)
		{
			this.read = this.start + seekPosition;
		}

		public void Validate(bool write, int dataSize)
		{
			ByteBuffer.ManagedBuffer managedBuffer;
			bool length = false;
			if (!write)
			{
				length = this.Length >= dataSize;
			}
			else
			{
				if (this.Size < dataSize && this.autoGrow)
				{
					if (this.references != 1)
					{
						throw new InvalidOperationException("Cannot grow the current buffer because it has more than one references");
					}
					int num = Math.Max(this.Capacity * 2, this.Capacity + dataSize);
					managedBuffer = (this.bufferManager == null ? new ByteBuffer.ManagedBuffer(new byte[num], null) : ByteBuffer.AllocateBuffer(num, this.bufferManager));
					System.Buffer.BlockCopy(this.buffer, this.start, managedBuffer.Buffer, 0, this.Capacity);
					int num1 = this.read - this.start;
					int num2 = this.write - this.start;
					this.start = 0;
					this.read = num1;
					this.write = num2;
					this.end = num;
					if (this.bufferManager != null)
					{
						this.bufferManager.ReturnBuffer(this.buffer);
					}
					this.buffer = managedBuffer.Buffer;
					this.bufferManager = managedBuffer.BufferManager;
				}
				length = this.Size >= dataSize;
			}
			if (!length)
			{
				throw new AmqpException(AmqpError.DecodeError, SRAmqp.AmqpInsufficientBufferSize(dataSize, (write ? this.Size : this.Length)));
			}
		}

		private struct ManagedBuffer
		{
			public InternalBufferManager BufferManager;

			public byte[] Buffer;

			public ManagedBuffer(byte[] buffer, InternalBufferManager bufferManager)
			{
				this.Buffer = buffer;
				this.BufferManager = bufferManager;
			}
		}
	}
}