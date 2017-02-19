using Microsoft.ServiceBus.Diagnostics;
using System;
using System.Threading;

namespace Microsoft.ServiceBus.Channels
{
	internal class PreReadConnection : DelegatingConnection
	{
		private int asyncBytesRead;

		private byte[] preReadData;

		private int preReadOffset;

		private int preReadCount;

		public PreReadConnection(IConnection innerConnection, byte[] initialData) : this(innerConnection, initialData, 0, (int)initialData.Length)
		{
		}

		public PreReadConnection(IConnection innerConnection, byte[] initialData, int initialOffset, int initialSize) : base(innerConnection)
		{
			this.preReadData = initialData;
			this.preReadOffset = initialOffset;
			this.preReadCount = initialSize;
		}

		public void AddPreReadData(byte[] initialData, int initialOffset, int initialSize)
		{
			if (this.preReadCount <= 0)
			{
				this.preReadData = initialData;
				this.preReadOffset = initialOffset;
				this.preReadCount = initialSize;
				return;
			}
			byte[] numArray = this.preReadData;
			this.preReadData = DiagnosticUtility.Utility.AllocateByteArray(initialSize + this.preReadCount);
			Buffer.BlockCopy(numArray, this.preReadOffset, this.preReadData, 0, this.preReadCount);
			Buffer.BlockCopy(initialData, initialOffset, this.preReadData, this.preReadCount, initialSize);
			this.preReadOffset = 0;
			PreReadConnection preReadConnection = this;
			preReadConnection.preReadCount = preReadConnection.preReadCount + initialSize;
		}

		public override AsyncReadResult BeginRead(int offset, int size, TimeSpan timeout, WaitCallback callback, object state)
		{
			ConnectionUtilities.ValidateBufferBounds(this.AsyncReadBufferSize, offset, size);
			if (this.preReadCount <= 0)
			{
				return base.BeginRead(offset, size, timeout, callback, state);
			}
			int num = Math.Min(size, this.preReadCount);
			Buffer.BlockCopy(this.preReadData, this.preReadOffset, this.AsyncReadBuffer, offset, num);
			PreReadConnection preReadConnection = this;
			preReadConnection.preReadOffset = preReadConnection.preReadOffset + num;
			PreReadConnection preReadConnection1 = this;
			preReadConnection1.preReadCount = preReadConnection1.preReadCount - num;
			this.asyncBytesRead = num;
			return AsyncReadResult.Completed;
		}

		public override int EndRead()
		{
			if (this.asyncBytesRead <= 0)
			{
				return base.EndRead();
			}
			int num = this.asyncBytesRead;
			this.asyncBytesRead = 0;
			return num;
		}

		public override int Read(byte[] buffer, int offset, int size, TimeSpan timeout)
		{
			ConnectionUtilities.ValidateBufferBounds(buffer, offset, size);
			if (this.preReadCount <= 0)
			{
				return base.Read(buffer, offset, size, timeout);
			}
			int num = Math.Min(size, this.preReadCount);
			Buffer.BlockCopy(this.preReadData, this.preReadOffset, buffer, offset, num);
			PreReadConnection preReadConnection = this;
			preReadConnection.preReadOffset = preReadConnection.preReadOffset + num;
			PreReadConnection preReadConnection1 = this;
			preReadConnection1.preReadCount = preReadConnection1.preReadCount - num;
			return num;
		}
	}
}