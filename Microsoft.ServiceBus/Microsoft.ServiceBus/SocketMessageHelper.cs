using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Properties;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.ServiceModel.Channels;
using System.Threading;

namespace Microsoft.ServiceBus
{
	internal class SocketMessageHelper
	{
		private System.ServiceModel.Channels.MessageEncoder MessageEncoder
		{
			get;
			set;
		}

		public System.ServiceModel.Channels.MessageVersion MessageVersion
		{
			get
			{
				return this.MessageEncoder.MessageVersion;
			}
		}

		private BufferManager ReceiveBufferManager
		{
			get;
			set;
		}

		private BufferManager SendBufferManager
		{
			get;
			set;
		}

		public SocketMessageHelper(System.ServiceModel.Channels.MessageEncoder messageEncoder)
		{
			this.SendBufferManager = BufferManager.CreateBufferManager((long)67108864, 65536);
			this.ReceiveBufferManager = BufferManager.CreateBufferManager((long)0, 65536);
			this.MessageEncoder = messageEncoder;
		}

		public SocketMessageHelper() : this(ClientMessageUtility.DefaultBinaryMessageEncoderFactory.Encoder)
		{
		}

		public IAsyncResult BeginReceiveBytes(Microsoft.ServiceBus.Channels.IConnection connection, int size, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return (new SocketMessageHelper.ReceiveBytesAsyncResult(connection, size, timeout, callback, state)).Start();
		}

		public IAsyncResult BeginReceiveMessage(Microsoft.ServiceBus.Channels.IConnection connection, TimeSpan timeout, AsyncCallback callback, object state)
		{
			SocketMessageHelper.ReceiveMessageAsyncResult receiveMessageAsyncResult = new SocketMessageHelper.ReceiveMessageAsyncResult(this, connection, timeout, callback, state);
			receiveMessageAsyncResult.Start();
			return receiveMessageAsyncResult;
		}

		public IAsyncResult BeginSendMessage(Microsoft.ServiceBus.Channels.IConnection connection, Message message, TimeSpan timeout, AsyncCallback callback, object state)
		{
			ArraySegment<byte> nums = this.MessageEncoder.WriteMessage(message, 65536, this.SendBufferManager);
			return (new SocketMessageHelper.SendMessageAsyncResult(connection, nums, timeout, callback, state)).Start();
		}

		public ArraySegment<byte> EndReceiveBytes(IAsyncResult result)
		{
			return AsyncResult<SocketMessageHelper.ReceiveBytesAsyncResult>.End(result).Bytes;
		}

		public Message EndReceiveMessage(IAsyncResult result)
		{
			return AsyncResult<SocketMessageHelper.ReceiveMessageAsyncResult>.End(result).Message;
		}

		public void EndSendMessage(IAsyncResult result)
		{
			AsyncResult<SocketMessageHelper.SendMessageAsyncResult>.End(result);
		}

		public Message ReceiveMessage(Microsoft.ServiceBus.Channels.IConnection connection, TimeSpan timeout)
		{
			SocketMessageHelper.ReceiveMessageAsyncResult receiveMessageAsyncResult = new SocketMessageHelper.ReceiveMessageAsyncResult(this, connection, timeout, null, null);
			receiveMessageAsyncResult.RunSynchronously();
			return receiveMessageAsyncResult.Message;
		}

		public void SendMessage(Microsoft.ServiceBus.Channels.IConnection connection, Message message)
		{
			this.SendMessage(connection, message, TimeSpan.MaxValue);
		}

		public void SendMessage(Microsoft.ServiceBus.Channels.IConnection connection, Message message, TimeSpan timeout)
		{
			ArraySegment<byte> nums = this.MessageEncoder.WriteMessage(message, 65536, this.SendBufferManager);
			SocketMessageHelper.SendMessageAsyncResult sendMessageAsyncResult = new SocketMessageHelper.SendMessageAsyncResult(connection, nums, timeout, null, null);
			sendMessageAsyncResult.RunSynchronously();
		}

		private sealed class ReadBytesAsyncResult : AsyncResult<SocketMessageHelper.ReadBytesAsyncResult>
		{
			private readonly static WaitCallback readCallback;

			private readonly Microsoft.ServiceBus.Channels.IConnection connection;

			private readonly int requestedCount;

			private readonly bool throwIfNotAllRead;

			private int totalBytesRead;

			private Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper;

			protected internal override EventTraceActivity Activity
			{
				get
				{
					return this.connection.Activity;
				}
			}

			public ArraySegment<byte> Bytes
			{
				get;
				private set;
			}

			static ReadBytesAsyncResult()
			{
				SocketMessageHelper.ReadBytesAsyncResult.readCallback = new WaitCallback(SocketMessageHelper.ReadBytesAsyncResult.ReadCallback);
			}

			public ReadBytesAsyncResult(Microsoft.ServiceBus.Channels.IConnection connection, int requestedCount, bool throwIfNotAllRead, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				this.connection = connection;
				this.requestedCount = requestedCount;
				this.throwIfNotAllRead = throwIfNotAllRead;
				this.timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
				if (this.BeginRead() == AsyncReadResult.Completed)
				{
					this.ReadComplete(true);
				}
			}

			private AsyncReadResult BeginRead()
			{
				return this.connection.BeginRead(this.totalBytesRead, this.requestedCount - this.totalBytesRead, this.timeoutHelper.RemainingTime(), SocketMessageHelper.ReadBytesAsyncResult.readCallback, this);
			}

			private static void ReadCallback(object state)
			{
				((SocketMessageHelper.ReadBytesAsyncResult)state).ReadComplete(false);
			}

			private void ReadComplete(bool calledSynchronously)
			{
				try
				{
					int num = this.connection.EndRead();
					if (num != 0)
					{
						SocketMessageHelper.ReadBytesAsyncResult readBytesAsyncResult = this;
						readBytesAsyncResult.totalBytesRead = readBytesAsyncResult.totalBytesRead + num;
						if (this.totalBytesRead == this.requestedCount)
						{
							this.Bytes = new ArraySegment<byte>(this.connection.AsyncReadBuffer, 0, this.totalBytesRead);
							base.Complete(calledSynchronously);
						}
						else
						{
							AsyncReadResult asyncReadResult = AsyncReadResult.Completed;
							while (this.totalBytesRead != this.requestedCount)
							{
								if (asyncReadResult == AsyncReadResult.Completed)
								{
									asyncReadResult = this.BeginRead();
									if (asyncReadResult != AsyncReadResult.Completed)
									{
										continue;
									}
									num = this.connection.EndRead();
									if (num != 0)
									{
										SocketMessageHelper.ReadBytesAsyncResult readBytesAsyncResult1 = this;
										readBytesAsyncResult1.totalBytesRead = readBytesAsyncResult1.totalBytesRead + num;
									}
									else
									{
										if (this.throwIfNotAllRead)
										{
											TimeSpan originalTimeout = this.timeoutHelper.OriginalTimeout - this.timeoutHelper.RemainingTime();
											throw new InvalidDataException(string.Concat(Resources.PrematureEOF, " ", originalTimeout.ToString()));
										}
										this.Bytes = new ArraySegment<byte>(this.connection.AsyncReadBuffer, 0, this.totalBytesRead);
										base.Complete(calledSynchronously);
										return;
									}
								}
								else
								{
									break;
								}
							}
						}
					}
					else
					{
						if (this.throwIfNotAllRead)
						{
							TimeSpan timeSpan = this.timeoutHelper.OriginalTimeout - this.timeoutHelper.RemainingTime();
							throw new InvalidDataException(string.Concat(Resources.PrematureEOF, " ", timeSpan.ToString()));
						}
						this.Bytes = new ArraySegment<byte>(this.connection.AsyncReadBuffer, 0, this.totalBytesRead);
						base.Complete(calledSynchronously);
					}
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					base.Complete(calledSynchronously, exception);
				}
			}
		}

		private class ReceiveBytesAsyncResult : IteratorAsyncResult<SocketMessageHelper.ReceiveBytesAsyncResult>
		{
			private readonly Microsoft.ServiceBus.Channels.IConnection connection;

			private readonly int size;

			public ArraySegment<byte> Bytes
			{
				get;
				private set;
			}

			public ReceiveBytesAsyncResult(Microsoft.ServiceBus.Channels.IConnection connection, int size, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.connection = connection;
				this.size = size;
			}

			protected override IEnumerator<IteratorAsyncResult<SocketMessageHelper.ReceiveBytesAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				SocketMessageHelper.ReceiveBytesAsyncResult receiveBytesAsyncResult = this;
				IteratorAsyncResult<SocketMessageHelper.ReceiveBytesAsyncResult>.BeginCall readBytesAsyncResult = (SocketMessageHelper.ReceiveBytesAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => new SocketMessageHelper.ReadBytesAsyncResult(thisPtr.connection, thisPtr.size, false, t, c, s);
				yield return receiveBytesAsyncResult.CallAsync(readBytesAsyncResult, (SocketMessageHelper.ReceiveBytesAsyncResult thisPtr, IAsyncResult r) => thisPtr.Bytes = AsyncResult<SocketMessageHelper.ReadBytesAsyncResult>.End(r).Bytes, IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
			}
		}

		private class ReceiveMessageAsyncResult : IteratorAsyncResult<SocketMessageHelper.ReceiveMessageAsyncResult>
		{
			private readonly SocketMessageHelper messageHelper;

			protected internal override EventTraceActivity Activity
			{
				get
				{
					return this.Connection.Activity;
				}
			}

			private Microsoft.ServiceBus.Channels.IConnection Connection
			{
				get;
				set;
			}

			public Message Message
			{
				get;
				private set;
			}

			public ReceiveMessageAsyncResult(SocketMessageHelper messageHelper, Microsoft.ServiceBus.Channels.IConnection connection, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.messageHelper = messageHelper;
				this.Connection = connection;
			}

			protected override IEnumerator<IteratorAsyncResult<SocketMessageHelper.ReceiveMessageAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				ArraySegment<byte> bytes = new ArraySegment<byte>();
				SocketMessageHelper.ReceiveMessageAsyncResult receiveMessageAsyncResult = this;
				yield return receiveMessageAsyncResult.CallAsync((SocketMessageHelper.ReceiveMessageAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => new SocketMessageHelper.ReadBytesAsyncResult(thisPtr.Connection, 4, true, t, c, s), (SocketMessageHelper.ReceiveMessageAsyncResult thisPtr, IAsyncResult r) => bytes = AsyncResult<SocketMessageHelper.ReadBytesAsyncResult>.End(r).Bytes, IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				int num = BitConverter.ToInt32(bytes.Array, 0);
				yield return base.CallAsync((SocketMessageHelper.ReceiveMessageAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => new SocketMessageHelper.ReadBytesAsyncResult(thisPtr.Connection, num, true, t, c, s), (SocketMessageHelper.ReceiveMessageAsyncResult thisPtr, IAsyncResult r) => bytes = AsyncResult<SocketMessageHelper.ReadBytesAsyncResult>.End(r).Bytes, IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				this.Message = this.messageHelper.MessageEncoder.ReadMessage(bytes, this.messageHelper.ReceiveBufferManager);
			}
		}

		private class SendMessageAsyncResult : IteratorAsyncResult<SocketMessageHelper.SendMessageAsyncResult>
		{
			private readonly Microsoft.ServiceBus.Channels.IConnection connection;

			private readonly ArraySegment<byte> messageBytes;

			private byte[] countBytes;

			public SendMessageAsyncResult(Microsoft.ServiceBus.Channels.IConnection connection, ArraySegment<byte> messageBytes, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.connection = connection;
				this.messageBytes = messageBytes;
			}

			protected override IEnumerator<IteratorAsyncResult<SocketMessageHelper.SendMessageAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				this.countBytes = BitConverter.GetBytes(this.messageBytes.Count);
				SocketMessageHelper.SendMessageAsyncResult sendMessageAsyncResult = this;
				IteratorAsyncResult<SocketMessageHelper.SendMessageAsyncResult>.BeginCall beginCall = (SocketMessageHelper.SendMessageAsyncResult thisRef, TimeSpan t, AsyncCallback c, object s) => thisRef.connection.BeginWrite(thisRef.countBytes, 0, (int)thisRef.countBytes.Length, true, t, c, s);
				yield return sendMessageAsyncResult.CallAsync(beginCall, (SocketMessageHelper.SendMessageAsyncResult thisRef, IAsyncResult r) => thisRef.connection.EndWrite(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				SocketMessageHelper.SendMessageAsyncResult sendMessageAsyncResult1 = this;
				IteratorAsyncResult<SocketMessageHelper.SendMessageAsyncResult>.BeginCall beginCall1 = (SocketMessageHelper.SendMessageAsyncResult thisRef, TimeSpan t, AsyncCallback c, object s) => thisRef.connection.BeginWrite(thisRef.messageBytes.Array, thisRef.messageBytes.Offset, thisRef.messageBytes.Count, true, t, c, s);
				yield return sendMessageAsyncResult1.CallAsync(beginCall1, (SocketMessageHelper.SendMessageAsyncResult thisRef, IAsyncResult r) => thisRef.connection.EndWrite(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
			}
		}
	}
}