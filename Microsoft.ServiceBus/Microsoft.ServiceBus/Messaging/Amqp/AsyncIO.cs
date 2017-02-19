using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using Microsoft.ServiceBus.Messaging.Amqp.Transport;
using System;
using System.Collections.Generic;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal sealed class AsyncIO : AmqpObject
	{
		private readonly IIoHandler ioHandler;

		private readonly TransportBase transport;

		private readonly AsyncIO.AsyncWriter writer;

		private readonly AsyncIO.AsyncReader reader;

		public TransportBase Transport
		{
			get
			{
				return this.transport;
			}
		}

		public AsyncIO(IIoHandler parent, int maxFrameSize, TransportBase transport, bool isInitiator) : base("async-io")
		{
			AsyncIO.AsyncWriter asyncFrameWriter;
			this.ioHandler = parent;
			this.transport = transport;
			if (this.transport.RequiresCompleteFrames)
			{
				asyncFrameWriter = new AsyncIO.AsyncFrameWriter(this.transport, parent);
			}
			else
			{
				asyncFrameWriter = new AsyncIO.AsyncWriter(this.transport, parent);
			}
			this.writer = asyncFrameWriter;
			this.reader = new AsyncIO.AsyncReader(this, maxFrameSize, isInitiator);
		}

		protected override void AbortInternal()
		{
			this.transport.Abort();
		}

		protected override bool CloseInternal()
		{
			this.writer.IssueClose();
			return true;
		}

		protected override void OnClose(TimeSpan timeout)
		{
			this.writer.IssueClose();
		}

		protected override void OnOpen(TimeSpan timeout)
		{
			this.OpenInternal();
			base.State = AmqpObjectState.Opened;
		}

		protected override bool OpenInternal()
		{
			this.reader.StartReading();
			return true;
		}

		public void WriteBuffer(ByteBuffer buffer)
		{
			this.writer.WriteBuffer(buffer);
		}

		public void WriteBuffer(IList<ByteBuffer> buffers)
		{
			this.writer.WriteBuffer(buffers);
		}

		public class AsyncBufferReader
		{
			private static Action<TransportAsyncCallbackArgs> onReadComplete;

			private readonly TransportBase transport;

			static AsyncBufferReader()
			{
				AsyncIO.AsyncBufferReader.onReadComplete = new Action<TransportAsyncCallbackArgs>(AsyncIO.AsyncBufferReader.OnReadComplete);
			}

			public AsyncBufferReader(TransportBase transport)
			{
				this.transport = transport;
			}

			private bool HandleReadComplete(TransportAsyncCallbackArgs args)
			{
				bool flag = true;
				Exception exception = null;
				if (args.Exception != null)
				{
					exception = args.Exception;
				}
				else if (args.BytesTransfered == 0)
				{
					exception = new ObjectDisposedException(this.transport.ToString());
				}
				else if (args.BytesTransfered < args.Count)
				{
					args.SetBuffer(args.Buffer, args.Offset + args.BytesTransfered, args.Count - args.BytesTransfered);
					flag = false;
				}
				if (flag)
				{
					TransportAsyncCallbackArgs userToken2 = (TransportAsyncCallbackArgs)args.UserToken2;
					userToken2.Exception = exception;
					userToken2.BytesTransfered = userToken2.Count;
					userToken2.CompletedCallback(userToken2);
				}
				return flag;
			}

			private static void OnReadComplete(TransportAsyncCallbackArgs args)
			{
				AsyncIO.AsyncBufferReader userToken = (AsyncIO.AsyncBufferReader)args.UserToken;
				if (!userToken.HandleReadComplete(args) && !args.CompletedSynchronously)
				{
					userToken.Read(args);
				}
			}

			private void Read(TransportAsyncCallbackArgs args)
			{
				try
				{
					while (!this.transport.ReadAsync(args) && !this.HandleReadComplete(args))
					{
					}
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					args.Exception = exception;
					this.HandleReadComplete(args);
				}
			}

			public void ReadBuffer(TransportAsyncCallbackArgs args)
			{
				TransportAsyncCallbackArgs transportAsyncCallbackArg = new TransportAsyncCallbackArgs();
				transportAsyncCallbackArg.SetBuffer(args.Buffer, args.Offset, args.Count);
				transportAsyncCallbackArg.UserToken = this;
				transportAsyncCallbackArg.UserToken2 = args;
				transportAsyncCallbackArg.CompletedCallback = AsyncIO.AsyncBufferReader.onReadComplete;
				this.Read(transportAsyncCallbackArg);
			}
		}

		public class AsyncBufferWriter
		{
			private readonly TransportBase transport;

			private static Action<TransportAsyncCallbackArgs> onWriteComplete;

			static AsyncBufferWriter()
			{
				AsyncIO.AsyncBufferWriter.onWriteComplete = new Action<TransportAsyncCallbackArgs>(AsyncIO.AsyncBufferWriter.OnWriteComplete);
			}

			public AsyncBufferWriter(TransportBase transport)
			{
				this.transport = transport;
			}

			private bool HandleWriteComplete(TransportAsyncCallbackArgs args)
			{
				bool flag = true;
				Exception exception = null;
				if (args.Exception != null)
				{
					exception = args.Exception;
				}
				else if (args.BytesTransfered == 0)
				{
					exception = new ObjectDisposedException(this.transport.ToString());
				}
				else if (args.BytesTransfered < args.Count)
				{
					args.SetBuffer(args.Buffer, args.Offset + args.BytesTransfered, args.Count - args.BytesTransfered);
					flag = false;
				}
				TransportAsyncCallbackArgs userToken2 = (TransportAsyncCallbackArgs)args.UserToken2;
				if (flag && userToken2.CompletedCallback != null)
				{
					userToken2.Exception = exception;
					userToken2.BytesTransfered = userToken2.Count;
					userToken2.CompletedCallback(userToken2);
				}
				return flag;
			}

			private static void OnWriteComplete(TransportAsyncCallbackArgs args)
			{
				AsyncIO.AsyncBufferWriter userToken = (AsyncIO.AsyncBufferWriter)args.UserToken;
				if (!userToken.HandleWriteComplete(args) && !args.CompletedSynchronously)
				{
					userToken.Write(args);
				}
			}

			private void Write(TransportAsyncCallbackArgs args)
			{
				try
				{
					while (!this.transport.WriteAsync(args) && !this.HandleWriteComplete(args))
					{
					}
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					args.Exception = exception;
					this.HandleWriteComplete(args);
				}
			}

			public void WriteBuffer(TransportAsyncCallbackArgs args)
			{
				TransportAsyncCallbackArgs transportAsyncCallbackArg = new TransportAsyncCallbackArgs();
				transportAsyncCallbackArg.SetBuffer(args.Buffer, args.Offset, args.Count);
				transportAsyncCallbackArg.CompletedCallback = AsyncIO.AsyncBufferWriter.onWriteComplete;
				transportAsyncCallbackArg.UserToken = this;
				transportAsyncCallbackArg.UserToken2 = args;
				this.Write(transportAsyncCallbackArg);
			}
		}

		public class AsyncFrameWriter : AsyncIO.AsyncWriter
		{
			public AsyncFrameWriter(TransportBase transport, IIoHandler parent) : base(transport, parent)
			{
			}

			public override void WriteBuffer(IList<ByteBuffer> buffers)
			{
				int length = 0;
				foreach (ByteBuffer buffer in buffers)
				{
					length = length + buffer.Length;
				}
				ByteBuffer byteBuffer = new ByteBuffer(length, false, false);
				foreach (ByteBuffer buffer1 in buffers)
				{
					Buffer.BlockCopy(buffer1.Buffer, buffer1.Offset, byteBuffer.Buffer, byteBuffer.Length, buffer1.Length);
					byteBuffer.Append(buffer1.Length);
					buffer1.Dispose();
				}
				base.WriteBuffer(byteBuffer);
			}
		}

		private sealed class AsyncReader
		{
			private readonly static SegmentBufferPool FrameSizeSegmentPool;

			private readonly static Action<TransportAsyncCallbackArgs> onReadBufferComplete;

			private readonly AsyncIO asyncIo;

			private readonly int maxFrameSize;

			private readonly bool readProtocolHeader;

			private readonly TransportAsyncCallbackArgs readAsyncEventArgs;

			private AsyncIO.AsyncReader.ReadState readState;

			private ArraySegment<byte> frameSizeBuffer;

			private int remainingBytes;

			static AsyncReader()
			{
				AsyncIO.AsyncReader.FrameSizeSegmentPool = new SegmentBufferPool(4, 100000);
				AsyncIO.AsyncReader.onReadBufferComplete = new Action<TransportAsyncCallbackArgs>(AsyncIO.AsyncReader.OnReadBufferComplete);
			}

			public AsyncReader(AsyncIO parent, int maxFrameSize, bool readProtocolHeader)
			{
				this.asyncIo = parent;
				this.maxFrameSize = maxFrameSize;
				this.readProtocolHeader = readProtocolHeader;
				this.frameSizeBuffer = AsyncIO.AsyncReader.FrameSizeSegmentPool.TakeBuffer(4);
				this.readAsyncEventArgs = new TransportAsyncCallbackArgs()
				{
					CompletedCallback = AsyncIO.AsyncReader.onReadBufferComplete,
					UserToken = this
				};
			}

			private void Cleanup()
			{
				ArraySegment<byte> nums;
				lock (this.readAsyncEventArgs)
				{
					nums = this.frameSizeBuffer;
					this.frameSizeBuffer = new ArraySegment<byte>();
				}
				if (nums.Array != null)
				{
					AsyncIO.AsyncReader.FrameSizeSegmentPool.ReturnBuffer(nums);
				}
			}

			private void HandleFrameBodyReadComplete(TransportAsyncCallbackArgs args)
			{
				ByteBuffer userToken2 = (ByteBuffer)args.UserToken2;
				userToken2.Append(userToken2.Size);
				this.asyncIo.ioHandler.OnReceiveBuffer(userToken2);
				this.SetReadFrameSize();
			}

			private void HandleFrameSizeReadComplete(TransportAsyncCallbackArgs args)
			{
				int num = (int)AmqpBitConverter.ReadUInt(this.frameSizeBuffer.Array, this.frameSizeBuffer.Offset, 4);
				if (num <= 0 || num > this.maxFrameSize)
				{
					throw new AmqpException(AmqpError.FramingError, SRClient.InvalidFrameSize(num, this.maxFrameSize));
				}
				this.SetReadFrameBody(num);
			}

			private void HandleProtocolHeaderReadComplete(TransportAsyncCallbackArgs args)
			{
				this.asyncIo.ioHandler.OnReceiveBuffer(new ByteBuffer(args.Buffer, 0, args.Count));
				this.SetReadFrameSize();
			}

			private bool HandleReadBufferComplete(TransportAsyncCallbackArgs args)
			{
				bool flag;
				if (args.Exception != null)
				{
					this.asyncIo.ioHandler.OnIoFault(args.Exception);
					flag = false;
				}
				else if (args.BytesTransfered != 0)
				{
					flag = true;
					AsyncIO.AsyncReader bytesTransfered = this;
					bytesTransfered.remainingBytes = bytesTransfered.remainingBytes - args.BytesTransfered;
					if (this.remainingBytes <= 0)
					{
						switch (this.readState)
						{
							case AsyncIO.AsyncReader.ReadState.ProtocolHeader:
							{
								this.HandleProtocolHeaderReadComplete(args);
								if (!flag)
								{
									this.Cleanup();
								}
								return flag;
							}
							case AsyncIO.AsyncReader.ReadState.FrameSize:
							{
								this.HandleFrameSizeReadComplete(args);
								if (!flag)
								{
									this.Cleanup();
								}
								return flag;
							}
							case AsyncIO.AsyncReader.ReadState.FrameBody:
							{
								this.HandleFrameBodyReadComplete(args);
								if (!flag)
								{
									this.Cleanup();
								}
								return flag;
							}
						}
						throw new AmqpException(AmqpError.IllegalState);
					}
					else
					{
						args.SetBuffer(args.Buffer, args.Offset + args.BytesTransfered, args.Count - args.BytesTransfered);
					}
				}
				else
				{
					this.asyncIo.ioHandler.OnIoFault(new AmqpException(AmqpError.ConnectionForced));
					flag = false;
				}
				if (!flag)
				{
					this.Cleanup();
				}
				return flag;
			}

			private static void OnReadBufferComplete(TransportAsyncCallbackArgs args)
			{
				bool flag;
				AsyncIO.AsyncReader userToken = (AsyncIO.AsyncReader)args.UserToken;
				try
				{
					flag = userToken.HandleReadBufferComplete(args);
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					userToken.asyncIo.ioHandler.OnIoFault(exception);
					userToken.Cleanup();
					flag = false;
				}
				if (flag)
				{
					userToken.ReadBuffer();
				}
			}

			private void ReadBuffer()
			{
				try
				{
					if (this.asyncIo.State != AmqpObjectState.End)
					{
						while (this.asyncIo.State != AmqpObjectState.End && !this.asyncIo.transport.ReadAsync(this.readAsyncEventArgs) && this.HandleReadBufferComplete(this.readAsyncEventArgs))
						{
						}
					}
					else
					{
						this.Cleanup();
						ByteBuffer userToken2 = (ByteBuffer)this.readAsyncEventArgs.UserToken2;
						if (userToken2 != null)
						{
							userToken2.Dispose();
						}
					}
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					this.asyncIo.ioHandler.OnIoFault(exception);
					this.Cleanup();
				}
			}

			private void SetReadFrameBody(int frameSize)
			{
				ByteBuffer byteBuffer = new ByteBuffer(frameSize, false, false);
				AmqpBitConverter.WriteUInt(byteBuffer, (uint)frameSize);
				this.readState = AsyncIO.AsyncReader.ReadState.FrameBody;
				this.remainingBytes = byteBuffer.Size;
				this.readAsyncEventArgs.SetBuffer(byteBuffer.Buffer, byteBuffer.Length, this.remainingBytes);
				this.readAsyncEventArgs.UserToken2 = byteBuffer;
			}

			private void SetReadFrameSize()
			{
				this.readState = AsyncIO.AsyncReader.ReadState.FrameSize;
				this.remainingBytes = 4;
				this.readAsyncEventArgs.SetBuffer(this.frameSizeBuffer.Array, this.frameSizeBuffer.Offset, this.frameSizeBuffer.Count);
				this.readAsyncEventArgs.UserToken2 = null;
			}

			private void SetReadProtocolHeader()
			{
				this.readState = AsyncIO.AsyncReader.ReadState.ProtocolHeader;
				byte[] numArray = new byte[8];
				this.remainingBytes = (int)numArray.Length;
				this.readAsyncEventArgs.SetBuffer(numArray, 0, (int)numArray.Length);
			}

			public void StartReading()
			{
				if (!this.readProtocolHeader)
				{
					this.SetReadFrameSize();
				}
				else
				{
					this.SetReadProtocolHeader();
				}
				this.ReadBuffer();
			}

			private enum ReadState : byte
			{
				ProtocolHeader,
				FrameSize,
				FrameBody
			}
		}

		public class AsyncWriter : IWorkDelegate<IList<ByteBuffer>>
		{
			private readonly static Action<TransportAsyncCallbackArgs> writeCompleteCallback;

			private readonly TransportBase transport;

			private readonly TransportAsyncCallbackArgs writeAsyncEventArgs;

			private readonly SerializedBatchableWorker<ByteBuffer> writeWorker;

			private readonly IIoHandler parent;

			static AsyncWriter()
			{
				AsyncIO.AsyncWriter.writeCompleteCallback = new Action<TransportAsyncCallbackArgs>(AsyncIO.AsyncWriter.WriteCompleteCallback);
			}

			public AsyncWriter(TransportBase transport, IIoHandler parent)
			{
				this.transport = transport;
				this.parent = parent;
				this.writeWorker = new SerializedBatchableWorker<ByteBuffer>(this);
				this.writeAsyncEventArgs = new TransportAsyncCallbackArgs()
				{
					CompletedCallback = AsyncIO.AsyncWriter.writeCompleteCallback
				};
			}

			private bool HandleWriteBufferComplete(TransportAsyncCallbackArgs args)
			{
				bool flag;
				if (args.Exception == null)
				{
					flag = true;
				}
				else
				{
					flag = false;
					this.parent.OnIoFault(args.Exception);
				}
				args.Reset();
				return flag;
			}

			public void IssueClose()
			{
				this.writeWorker.IssueClose();
			}

			bool Microsoft.ServiceBus.Messaging.Amqp.IWorkDelegate<System.Collections.Generic.IList<Microsoft.ServiceBus.Messaging.Amqp.ByteBuffer>>.Invoke(IList<ByteBuffer> bufferList)
			{
				bool flag;
				if (bufferList == null)
				{
					this.transport.SafeClose();
					return true;
				}
				try
				{
					this.writeAsyncEventArgs.SetBuffer(bufferList);
					this.writeAsyncEventArgs.UserToken = this;
					flag = (!this.transport.WriteAsync(this.writeAsyncEventArgs) ? this.HandleWriteBufferComplete(this.writeAsyncEventArgs) : false);
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					this.writeAsyncEventArgs.Exception = exception;
					this.writeAsyncEventArgs.UserToken = this;
					flag = this.HandleWriteBufferComplete(this.writeAsyncEventArgs);
				}
				return flag;
			}

			public void WriteBuffer(ByteBuffer buffer)
			{
				this.writeWorker.DoWork(buffer);
			}

			public virtual void WriteBuffer(IList<ByteBuffer> buffers)
			{
				this.writeWorker.DoWork(buffers);
			}

			private static void WriteCompleteCallback(TransportAsyncCallbackArgs args)
			{
				AsyncIO.AsyncWriter userToken = (AsyncIO.AsyncWriter)args.UserToken;
				if (!userToken.HandleWriteBufferComplete(args))
				{
					return;
				}
				userToken.writeWorker.ContinueWork();
			}
		}

		public sealed class FrameBufferReader
		{
			private readonly static Action<TransportAsyncCallbackArgs> onSizeComplete;

			private readonly static Action<TransportAsyncCallbackArgs> onFrameComplete;

			private readonly TransportBase transport;

			private readonly byte[] sizeBuffer;

			private IIoHandler parent;

			static FrameBufferReader()
			{
				AsyncIO.FrameBufferReader.onSizeComplete = new Action<TransportAsyncCallbackArgs>(AsyncIO.FrameBufferReader.OnReadSizeComplete);
				AsyncIO.FrameBufferReader.onFrameComplete = new Action<TransportAsyncCallbackArgs>(AsyncIO.FrameBufferReader.OnReadFrameComplete);
			}

			public FrameBufferReader(IIoHandler parent, TransportBase transport)
			{
				this.parent = parent;
				this.transport = transport;
				this.sizeBuffer = new byte[4];
			}

			private bool HandleReadComplete(TransportAsyncCallbackArgs args)
			{
				unsafe
				{
					bool flag = true;
					Exception exception = null;
					if (args.Exception != null)
					{
						exception = args.Exception;
					}
					else if (args.BytesTransfered == 0)
					{
						exception = new ObjectDisposedException(this.transport.ToString());
					}
					else if (args.BytesTransfered < args.Count)
					{
						args.SetBuffer(args.Buffer, args.Offset + args.BytesTransfered, args.Count - args.BytesTransfered);
						flag = false;
					}
					if (flag)
					{
						if (exception != null || object.ReferenceEquals(args.CompletedCallback, AsyncIO.FrameBufferReader.onFrameComplete))
						{
							ByteBuffer byteBuffer = null;
							if (exception != null)
							{
								this.parent.OnIoFault(exception);
							}
							else
							{
								byteBuffer = new ByteBuffer(args.Buffer, 0, (int)args.Buffer.Length);
								this.parent.OnReceiveBuffer(byteBuffer);
							}
						}
						else
						{
							uint num = AmqpBitConverter.ReadUInt(this.sizeBuffer, 0, (int)this.sizeBuffer.Length);
							byte[] numArray = new byte[num];
							Buffer.BlockCopy(this.sizeBuffer, 0, numArray, 0, (int)this.sizeBuffer.Length);
							args.SetBuffer(numArray, (int)this.sizeBuffer.Length, (int)(num - (int)this.sizeBuffer.Length));
							args.CompletedCallback = AsyncIO.FrameBufferReader.onFrameComplete;
							flag = false;
						}
					}
					return flag;
				}
			}

			private static void OnReadFrameComplete(TransportAsyncCallbackArgs args)
			{
				AsyncIO.FrameBufferReader userToken = (AsyncIO.FrameBufferReader)args.UserToken;
				if (!userToken.HandleReadComplete(args))
				{
					userToken.ReadCore(args);
				}
			}

			private static void OnReadSizeComplete(TransportAsyncCallbackArgs args)
			{
				AsyncIO.FrameBufferReader userToken = (AsyncIO.FrameBufferReader)args.UserToken;
				if (!userToken.HandleReadComplete(args))
				{
					userToken.ReadCore(args);
				}
			}

			private void ReadCore(TransportAsyncCallbackArgs args)
			{
				try
				{
					while (!this.transport.ReadAsync(args) && !this.HandleReadComplete(args))
					{
					}
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					args.Exception = exception;
					this.HandleReadComplete(args);
				}
			}

			public void ReadFrame()
			{
				TransportAsyncCallbackArgs transportAsyncCallbackArg = new TransportAsyncCallbackArgs()
				{
					UserToken = this
				};
				transportAsyncCallbackArg.SetBuffer(this.sizeBuffer, 0, (int)this.sizeBuffer.Length);
				transportAsyncCallbackArg.CompletedCallback = AsyncIO.FrameBufferReader.onSizeComplete;
				this.ReadCore(transportAsyncCallbackArg);
			}
		}
	}
}