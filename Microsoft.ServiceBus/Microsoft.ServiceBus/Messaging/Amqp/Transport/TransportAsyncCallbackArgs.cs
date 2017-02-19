using Microsoft.ServiceBus.Messaging.Amqp;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
	internal sealed class TransportAsyncCallbackArgs : IAsyncResult
	{
		public byte[] Buffer
		{
			get;
			private set;
		}

		public Microsoft.ServiceBus.Messaging.Amqp.ByteBuffer ByteBuffer
		{
			get;
			private set;
		}

		public IList<Microsoft.ServiceBus.Messaging.Amqp.ByteBuffer> ByteBufferList
		{
			get;
			private set;
		}

		public int BytesTransfered
		{
			get;
			set;
		}

		public Action<TransportAsyncCallbackArgs> CompletedCallback
		{
			get;
			set;
		}

		public bool CompletedSynchronously
		{
			get;
			set;
		}

		public int Count
		{
			get;
			private set;
		}

		public System.Exception Exception
		{
			get;
			set;
		}

		public int Offset
		{
			get;
			private set;
		}

		object System.IAsyncResult.AsyncState
		{
			get
			{
				return this.UserToken;
			}
		}

		WaitHandle System.IAsyncResult.AsyncWaitHandle
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		bool System.IAsyncResult.CompletedSynchronously
		{
			get
			{
				return this.CompletedSynchronously;
			}
		}

		bool System.IAsyncResult.IsCompleted
		{
			get
			{
				return this.BytesTransfered == this.Count;
			}
		}

		public TransportBase Transport
		{
			get;
			set;
		}

		public object UserToken
		{
			get;
			set;
		}

		public object UserToken2
		{
			get;
			set;
		}

		public TransportAsyncCallbackArgs()
		{
		}

		public void Reset()
		{
			if (this.ByteBuffer != null)
			{
				this.ByteBuffer.Dispose();
			}
			else if (this.ByteBufferList != null)
			{
				foreach (Microsoft.ServiceBus.Messaging.Amqp.ByteBuffer byteBufferList in this.ByteBufferList)
				{
					byteBufferList.Dispose();
				}
			}
			this.SetBuffer(null, 0, 0);
			this.UserToken = null;
			this.BytesTransfered = 0;
			this.Exception = null;
		}

		public void SetBuffer(byte[] buffer, int offset, int count)
		{
			this.Buffer = buffer;
			this.Offset = offset;
			this.Count = count;
			this.ByteBuffer = null;
			this.ByteBufferList = null;
		}

		public void SetBuffer(Microsoft.ServiceBus.Messaging.Amqp.ByteBuffer byteBuffer)
		{
			this.SetBuffer(byteBuffer.Buffer, byteBuffer.Offset, byteBuffer.Length);
			this.ByteBuffer = byteBuffer;
		}

		public void SetBuffer(IList<Microsoft.ServiceBus.Messaging.Amqp.ByteBuffer> byteBufferList)
		{
			this.SetBuffer(null, 0, 0);
			this.ByteBufferList = byteBufferList;
			this.Count = 0;
			foreach (Microsoft.ServiceBus.Messaging.Amqp.ByteBuffer byteBuffer in byteBufferList)
			{
				TransportAsyncCallbackArgs count = this;
				count.Count = count.Count + byteBuffer.Length;
			}
		}
	}
}