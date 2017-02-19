using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.IO;
using System.ServiceModel;

namespace Microsoft.ServiceBus.Channels
{
	internal class MaxMessageSizeStream : DelegatingStream
	{
		private long maxMessageSize;

		private long totalBytesRead;

		private long bytesWritten;

		public MaxMessageSizeStream(Stream stream, long maxMessageSize) : base(stream)
		{
			this.maxMessageSize = maxMessageSize;
		}

		public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			count = this.PrepareRead(count);
			return base.BeginRead(buffer, offset, count, callback, state);
		}

		public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			this.PrepareWrite(count);
			return base.BeginWrite(buffer, offset, count, callback, state);
		}

		internal static Exception CreateMaxReceivedMessageSizeExceededException(long maxMessageSize)
		{
			string maxReceivedMessageSizeExceeded = Resources.MaxReceivedMessageSizeExceeded;
			object[] objArray = new object[] { maxMessageSize };
			string str = Microsoft.ServiceBus.SR.GetString(maxReceivedMessageSizeExceeded, objArray);
			return new CommunicationException(str, new QuotaExceededException(str));
		}

		internal static Exception CreateMaxSentMessageSizeExceededException(long maxMessageSize)
		{
			string maxSentMessageSizeExceeded = Resources.MaxSentMessageSizeExceeded;
			object[] objArray = new object[] { maxMessageSize };
			string str = Microsoft.ServiceBus.SR.GetString(maxSentMessageSizeExceeded, objArray);
			return new CommunicationException(str, new QuotaExceededException(str));
		}

		public override int EndRead(IAsyncResult result)
		{
			return this.FinishRead(base.EndRead(result));
		}

		private int FinishRead(int bytesRead)
		{
			MaxMessageSizeStream maxMessageSizeStream = this;
			maxMessageSizeStream.totalBytesRead = maxMessageSizeStream.totalBytesRead + (long)bytesRead;
			return bytesRead;
		}

		private int PrepareRead(int bytesToRead)
		{
			if (this.totalBytesRead >= this.maxMessageSize)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(MaxMessageSizeStream.CreateMaxReceivedMessageSizeExceededException(this.maxMessageSize));
			}
			if (this.maxMessageSize - this.totalBytesRead > (long)2147483647)
			{
				return bytesToRead;
			}
			return Math.Min(bytesToRead, (int)(this.maxMessageSize - this.totalBytesRead));
		}

		private void PrepareWrite(int bytesToWrite)
		{
			if (this.bytesWritten + (long)bytesToWrite > this.maxMessageSize)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(MaxMessageSizeStream.CreateMaxSentMessageSizeExceededException(this.maxMessageSize));
			}
			MaxMessageSizeStream maxMessageSizeStream = this;
			maxMessageSizeStream.bytesWritten = maxMessageSizeStream.bytesWritten + (long)bytesToWrite;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			count = this.PrepareRead(count);
			return this.FinishRead(base.Read(buffer, offset, count));
		}

		public override int ReadByte()
		{
			this.PrepareRead(1);
			int num = base.ReadByte();
			if (num != -1)
			{
				this.FinishRead(1);
			}
			return num;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			this.PrepareWrite(count);
			base.Write(buffer, offset, count);
		}

		public override void WriteByte(byte value)
		{
			this.PrepareWrite(1);
			base.WriteByte(value);
		}
	}
}