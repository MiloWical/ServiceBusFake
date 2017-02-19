using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
	internal sealed class TcpTransport : TransportBase
	{
		private readonly static SegmentBufferPool SmallBufferPool;

		private readonly static EventHandler<SocketAsyncEventArgs> onWriteComplete;

		private readonly static EventHandler<SocketAsyncEventArgs> onReadComplete;

		private readonly Socket socket;

		private readonly EndPoint localEndPoint;

		private readonly EndPoint remoteEndPoint;

		private readonly TcpTransport.WriteAsyncEventArgs sendEventArgs;

		private readonly TcpTransport.ReadAsyncEventArgs receiveEventArgs;

		public override EndPoint LocalEndPoint
		{
			get
			{
				return this.localEndPoint;
			}
		}

		public override EndPoint RemoteEndPoint
		{
			get
			{
				return this.remoteEndPoint;
			}
		}

		static TcpTransport()
		{
			TcpTransport.SmallBufferPool = new SegmentBufferPool(8, 100000);
			TcpTransport.onWriteComplete = new EventHandler<SocketAsyncEventArgs>(TcpTransport.OnWriteComplete);
			TcpTransport.onReadComplete = new EventHandler<SocketAsyncEventArgs>(TcpTransport.OnReadComplete);
		}

		public TcpTransport(Socket socket, TcpTransportSettings transportSettings) : base("tcp")
		{
			this.socket = socket;
			this.socket.NoDelay = true;
			this.socket.SendBufferSize = 0;
			this.socket.ReceiveBufferSize = 0;
			this.localEndPoint = this.socket.LocalEndPoint;
			this.remoteEndPoint = this.socket.RemoteEndPoint;
			this.sendEventArgs = new TcpTransport.WriteAsyncEventArgs()
			{
				Transport = this
			};
			this.sendEventArgs.Completed += TcpTransport.onWriteComplete;
			this.receiveEventArgs = new TcpTransport.ReadAsyncEventArgs();
			this.receiveEventArgs.Completed += TcpTransport.onReadComplete;
			this.receiveEventArgs.Transport = this;
		}

		protected override void AbortInternal()
		{
			this.sendEventArgs.Dispose();
			this.receiveEventArgs.Dispose();
			this.socket.Close(0);
		}

		protected override bool CloseInternal()
		{
			this.sendEventArgs.Dispose();
			this.receiveEventArgs.Dispose();
			this.socket.Shutdown(SocketShutdown.Both);
			this.socket.Close();
			return true;
		}

		private void HandleReadComplete(TransportAsyncCallbackArgs args, bool fromCache, bool completedSynchronously)
		{
			if (this.receiveEventArgs.SocketError != SocketError.Success)
			{
				args.Exception = new SocketException((int)this.receiveEventArgs.SocketError);
			}
			else
			{
				try
				{
					int num = (fromCache ? this.receiveEventArgs.ReadBuffer.Length : this.receiveEventArgs.BytesTransferred);
					int count = num;
					if (num > 0)
					{
						if (!this.receiveEventArgs.IsSegment)
						{
							ByteBuffer readBuffer = this.receiveEventArgs.ReadBuffer;
							if (readBuffer != null)
							{
								if (!fromCache)
								{
									readBuffer.Append(num);
								}
								if (num > args.Count)
								{
									Buffer.BlockCopy(readBuffer.Buffer, readBuffer.Offset, args.Buffer, args.Offset, args.Count);
									count = args.Count;
									readBuffer.Complete(args.Count);
								}
								else
								{
									Buffer.BlockCopy(readBuffer.Buffer, readBuffer.Offset, args.Buffer, args.Offset, num);
									readBuffer.Reset();
								}
							}
							else
							{
								count = 0;
							}
						}
						else
						{
							Buffer.BlockCopy(this.receiveEventArgs.Buffer, this.receiveEventArgs.Offset, args.Buffer, args.Offset, num);
							ArraySegment<byte> nums = new ArraySegment<byte>(this.receiveEventArgs.Buffer, this.receiveEventArgs.Offset, this.receiveEventArgs.Count);
							TcpTransport.SmallBufferPool.ReturnBuffer(nums);
						}
					}
					args.BytesTransfered = count;
					args.Exception = null;
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					args.Exception = exception;
				}
			}
			args.CompletedSynchronously = completedSynchronously;
			try
			{
				this.receiveEventArgs.Reset();
			}
			catch (ObjectDisposedException objectDisposedException)
			{
				args.Exception = objectDisposedException;
			}
			if (!completedSynchronously)
			{
				args.CompletedCallback(args);
			}
		}

		private void HandleWriteComplete(TransportAsyncCallbackArgs args, bool syncCompleted)
		{
			if (this.sendEventArgs.SocketError != SocketError.Success)
			{
				args.Exception = new SocketException((int)this.sendEventArgs.SocketError);
			}
			else
			{
				args.BytesTransfered = this.sendEventArgs.BytesTransferred;
				args.Exception = null;
			}
			args.CompletedSynchronously = syncCompleted;
			try
			{
				this.sendEventArgs.Reset();
			}
			catch (ObjectDisposedException objectDisposedException)
			{
				args.Exception = objectDisposedException;
			}
			if (!syncCompleted)
			{
				args.CompletedCallback(args);
			}
		}

		private static void OnReadComplete(object sender, SocketAsyncEventArgs socketArgs)
		{
			TcpTransport.ReadAsyncEventArgs readAsyncEventArg = (TcpTransport.ReadAsyncEventArgs)socketArgs;
			readAsyncEventArg.Transport.HandleReadComplete(readAsyncEventArg.Args, false, false);
		}

		private static void OnWriteComplete(object sender, SocketAsyncEventArgs socketArgs)
		{
			TcpTransport.WriteAsyncEventArgs writeAsyncEventArg = (TcpTransport.WriteAsyncEventArgs)socketArgs;
			writeAsyncEventArg.Transport.HandleWriteComplete(writeAsyncEventArg.Args, false);
		}

		public sealed override bool ReadAsync(TransportAsyncCallbackArgs args)
		{
			bool flag;
			this.receiveEventArgs.AddBytes(args.Count);
			if (this.receiveEventArgs.ReadBuffer == null || this.receiveEventArgs.ReadBuffer.Length <= 0)
			{
				if (args.Count > TcpTransport.SmallBufferPool.SegmentSize)
				{
					this.receiveEventArgs.PrepareRead(args.Count);
					ByteBuffer readBuffer = this.receiveEventArgs.ReadBuffer;
					this.receiveEventArgs.SetBuffer(readBuffer.Buffer, readBuffer.Offset, readBuffer.Size);
				}
				else
				{
					ArraySegment<byte> nums = TcpTransport.SmallBufferPool.TakeBuffer(args.Count);
					this.receiveEventArgs.SetBuffer(nums.Array, nums.Offset, nums.Count);
					this.receiveEventArgs.IsSegment = true;
				}
				this.receiveEventArgs.Args = args;
				if (this.socket.ReceiveAsync(this.receiveEventArgs))
				{
					flag = true;
				}
				else
				{
					this.HandleReadComplete(args, false, true);
					flag = false;
				}
			}
			else
			{
				this.HandleReadComplete(args, true, true);
				flag = false;
			}
			return flag;
		}

		public sealed override bool WriteAsync(TransportAsyncCallbackArgs args)
		{
			this.sendEventArgs.PrepareWrite(args.Count);
			if (args.Buffer == null)
			{
				ArraySegment<byte>[] nums = new ArraySegment<byte>[args.ByteBufferList.Count];
				for (int i = 0; i < (int)nums.Length; i++)
				{
					nums[i] = new ArraySegment<byte>(args.ByteBufferList[i].Buffer, args.ByteBufferList[i].Offset, args.ByteBufferList[i].Length);
				}
				this.sendEventArgs.BufferList = nums;
			}
			else
			{
				this.sendEventArgs.SetBuffer(args.Buffer, args.Offset, args.Count);
			}
			this.sendEventArgs.Args = args;
			if (this.socket.SendAsync(this.sendEventArgs))
			{
				return true;
			}
			this.HandleWriteComplete(args, true);
			return false;
		}

		private struct BufferSizeTracker
		{
			private static long durationTicks;

			private static int[] thresholds;

			private static int[] bufferSizes;

			private Timestamp firstOperation;

			private int transferedBytes;

			private int level;

			static BufferSizeTracker()
			{
				TcpTransport.BufferSizeTracker.durationTicks = TimeSpan.FromSeconds(5).Ticks;
				TcpTransport.BufferSizeTracker.thresholds = new int[] { 0, 65536, 524288, 2097152 };
				TcpTransport.BufferSizeTracker.bufferSizes = new int[] { 0, 4096, 16384, 65536 };
			}

			public void AddBytes(int bytes)
			{
				if (this.transferedBytes == 0)
				{
					this.firstOperation = Timestamp.Now;
				}
				TcpTransport.BufferSizeTracker bufferSizeTracker = this;
				bufferSizeTracker.transferedBytes = bufferSizeTracker.transferedBytes + bytes;
			}

			public bool TryUpdateBufferSize(out int bufferSize)
			{
				bufferSize = 0;
				int num = 0;
				bool flag = false;
				if (this.firstOperation.ElapsedTicks >= TcpTransport.BufferSizeTracker.durationTicks)
				{
					int length = (int)TcpTransport.BufferSizeTracker.thresholds.Length - 1;
					while (length >= 0)
					{
						if (this.transferedBytes < TcpTransport.BufferSizeTracker.thresholds[length])
						{
							length--;
						}
						else
						{
							num = length;
							break;
						}
					}
					this.transferedBytes = 0;
					if (num != this.level)
					{
						this.level = num;
						bufferSize = TcpTransport.BufferSizeTracker.bufferSizes[num];
						flag = true;
					}
				}
				return flag;
			}
		}

		private sealed class ReadAsyncEventArgs : SocketAsyncEventArgs
		{
			private TcpTransport.BufferSizeTracker readTracker;

			private int bufferSize;

			public TransportAsyncCallbackArgs Args
			{
				get;
				set;
			}

			public bool IsSegment
			{
				get;
				set;
			}

			public ByteBuffer ReadBuffer
			{
				get;
				set;
			}

			public TcpTransport Transport
			{
				get;
				set;
			}

			public ReadAsyncEventArgs()
			{
			}

			public void AddBytes(int bytes)
			{
				this.readTracker.AddBytes(bytes);
			}

			public new void Dispose()
			{
				if (this.ReadBuffer != null)
				{
					this.ReadBuffer.Dispose();
					this.ReadBuffer = null;
				}
				base.Dispose();
			}

			public void PrepareRead(int count)
			{
				int num;
				if (this.readTracker.TryUpdateBufferSize(out num))
				{
					MessagingClientEtwProvider.TraceClient<TcpTransport, string, int, int>((TcpTransport a, string b, int c, int d) => MessagingClientEtwProvider.Provider.EventWriteAmqpDynamicBufferSizeChange(a, b, c, d), this.Transport, "read", this.bufferSize, num);
					this.bufferSize = num;
					this.Transport.socket.ReceiveBufferSize = this.bufferSize;
					if (this.ReadBuffer != null)
					{
						this.ReadBuffer.Dispose();
						this.ReadBuffer = null;
					}
				}
				if (this.ReadBuffer == null)
				{
					this.ReadBuffer = new ByteBuffer((this.bufferSize > 0 ? this.bufferSize : Math.Min(count, 65536)), false, true);
				}
			}

			public void Reset()
			{
				this.IsSegment = false;
				this.Args = null;
				base.SetBuffer(null, 0, 0);
				if (this.bufferSize == 0 && this.ReadBuffer != null)
				{
					this.ReadBuffer.Dispose();
					this.ReadBuffer = null;
				}
			}
		}

		private sealed class WriteAsyncEventArgs : SocketAsyncEventArgs
		{
			private TcpTransport.BufferSizeTracker writeTracker;

			private int bufferSize;

			public TransportAsyncCallbackArgs Args
			{
				get;
				set;
			}

			public TcpTransport Transport
			{
				get;
				set;
			}

			public WriteAsyncEventArgs()
			{
			}

			public void PrepareWrite(int writeSize)
			{
				int num;
				this.writeTracker.AddBytes(writeSize);
				if (this.writeTracker.TryUpdateBufferSize(out num))
				{
					MessagingClientEtwProvider.TraceClient<TcpTransport, string, int, int>((TcpTransport a, string b, int c, int d) => MessagingClientEtwProvider.Provider.EventWriteAmqpDynamicBufferSizeChange(a, b, c, d), this.Transport, "write", this.bufferSize, num);
					this.bufferSize = num;
					this.Transport.socket.SendBufferSize = this.bufferSize;
				}
			}

			public void Reset()
			{
				this.Args = null;
				base.SetBuffer(null, 0, 0);
			}
		}
	}
}