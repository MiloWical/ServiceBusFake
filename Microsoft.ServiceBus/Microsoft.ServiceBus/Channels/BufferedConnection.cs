using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Channels
{
	internal class BufferedConnection : Microsoft.ServiceBus.Channels.DelegatingConnection
	{
		private const int maxFlushSkew = 100;

		private byte[] writeBuffer;

		private int writeBufferSize;

		private int pendingWriteSize;

		private Exception pendingWriteException;

		private IOThreadTimer flushTimer;

		private long flushTimeout;

		private TimeSpan pendingTimeout;

		private object ThisLock
		{
			get
			{
				return this;
			}
		}

		public BufferedConnection(Microsoft.ServiceBus.Channels.IConnection connection, TimeSpan flushTimeout, int writeBufferSize) : base(connection)
		{
			this.flushTimeout = Ticks.FromTimeSpan(flushTimeout);
			this.writeBufferSize = writeBufferSize;
		}

		public override IAsyncResult BeginWrite(byte[] buffer, int offset, int size, bool immediate, TimeSpan timeout, AsyncCallback callback, object state)
		{
			ThreadTrace.Trace("BC:BeginWrite");
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			this.Flush(timeoutHelper.RemainingTime());
			return base.BeginWrite(buffer, offset, size, immediate, timeoutHelper.RemainingTime(), callback, state);
		}

		private void CancelFlushTimer()
		{
			if (this.flushTimer != null)
			{
				this.flushTimer.Cancel();
				this.pendingTimeout = TimeSpan.Zero;
			}
		}

		public override void Close(TimeSpan timeout)
		{
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			this.Flush(timeoutHelper.RemainingTime());
			base.Close(timeoutHelper.RemainingTime());
		}

		public override void EndWrite(IAsyncResult result)
		{
			ThreadTrace.Trace("BC:EndWrite");
			base.EndWrite(result);
		}

		private void Flush(TimeSpan timeout)
		{
			this.ThrowPendingWriteException();
			lock (this.ThisLock)
			{
				this.FlushCore(timeout);
			}
		}

		private void FlushCore(TimeSpan timeout)
		{
			if (this.pendingWriteSize > 0)
			{
				ThreadTrace.Trace("BC:Flush");
				base.Connection.Write(this.writeBuffer, 0, this.pendingWriteSize, false, timeout);
				this.pendingWriteSize = 0;
			}
		}

		private void OnFlushTimer(object state)
		{
			ThreadTrace.Trace("BC:Flush timer");
			lock (this.ThisLock)
			{
				try
				{
					this.FlushCore(this.pendingTimeout);
					this.pendingTimeout = TimeSpan.Zero;
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					this.pendingWriteException = exception;
					this.CancelFlushTimer();
				}
			}
		}

		private void SetFlushTimer()
		{
			if (this.flushTimer == null)
			{
				int milliseconds = Ticks.ToMilliseconds(Math.Min(this.flushTimeout / (long)10, Ticks.FromMilliseconds(100)));
				this.flushTimer = new IOThreadTimer(new Action<object>(this.OnFlushTimer), null, true, milliseconds);
			}
			this.flushTimer.Set(Ticks.ToTimeSpan(this.flushTimeout));
		}

		public override void Shutdown(TimeSpan timeout)
		{
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			this.Flush(timeoutHelper.RemainingTime());
			base.Shutdown(timeoutHelper.RemainingTime());
		}

		private void ThrowPendingWriteException()
		{
			if (this.pendingWriteException != null)
			{
				lock (this.ThisLock)
				{
					if (this.pendingWriteException != null)
					{
						Exception exception = this.pendingWriteException;
						this.pendingWriteException = null;
						throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(exception);
					}
				}
			}
		}

		public override void Write(byte[] buffer, int offset, int size, bool immediate, TimeSpan timeout, BufferManager bufferManager)
		{
			if (size <= 0)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("size", (object)size, Microsoft.ServiceBus.SR.GetString(Resources.ValueMustBePositive, new object[0])));
			}
			this.ThrowPendingWriteException();
			if (immediate || this.flushTimeout == (long)0)
			{
				ThreadTrace.Trace("BC:Write now");
				this.WriteNow(buffer, offset, size, timeout, bufferManager);
			}
			else
			{
				ThreadTrace.Trace("BC:Write later");
				this.WriteLater(buffer, offset, size, timeout);
				bufferManager.ReturnBuffer(buffer);
			}
			ThreadTrace.Trace("BC:Write done");
		}

		public override void Write(byte[] buffer, int offset, int size, bool immediate, TimeSpan timeout)
		{
			if (size <= 0)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("size", (object)size, Microsoft.ServiceBus.SR.GetString(Resources.ValueMustBePositive, new object[0])));
			}
			this.ThrowPendingWriteException();
			if (immediate || this.flushTimeout == (long)0)
			{
				ThreadTrace.Trace("BC:Write now");
				this.WriteNow(buffer, offset, size, timeout);
			}
			else
			{
				ThreadTrace.Trace("BC:Write later");
				this.WriteLater(buffer, offset, size, timeout);
			}
			ThreadTrace.Trace("BC:Write done");
		}

		private void WriteLater(byte[] buffer, int offset, int size, TimeSpan timeout)
		{
			lock (this.ThisLock)
			{
				bool flag = this.pendingWriteSize == 0;
				TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
				while (size > 0)
				{
					if (size < this.writeBufferSize || this.pendingWriteSize != 0)
					{
						if (this.writeBuffer == null)
						{
							this.writeBuffer = DiagnosticUtility.Utility.AllocateByteArray(this.writeBufferSize);
						}
						int num = this.writeBufferSize - this.pendingWriteSize;
						int num1 = size;
						if (num1 > num)
						{
							num1 = num;
						}
						Buffer.BlockCopy(buffer, offset, this.writeBuffer, this.pendingWriteSize, num1);
						Microsoft.ServiceBus.Channels.BufferedConnection bufferedConnection = this;
						bufferedConnection.pendingWriteSize = bufferedConnection.pendingWriteSize + num1;
						if (this.pendingWriteSize == this.writeBufferSize)
						{
							this.FlushCore(timeoutHelper.RemainingTime());
							flag = true;
						}
						size = size - num1;
						offset = offset + num1;
					}
					else
					{
						base.Connection.Write(buffer, offset, size, false, timeoutHelper.RemainingTime());
						size = 0;
					}
				}
				if (this.pendingWriteSize <= 0)
				{
					this.CancelFlushTimer();
				}
				else if (flag)
				{
					this.SetFlushTimer();
					this.pendingTimeout = TimeoutHelper.Add(this.pendingTimeout, timeoutHelper.RemainingTime());
				}
			}
		}

		private void WriteNow(byte[] buffer, int offset, int size, TimeSpan timeout)
		{
			this.WriteNow(buffer, offset, size, timeout, null);
		}

		private void WriteNow(byte[] buffer, int offset, int size, TimeSpan timeout, BufferManager bufferManager)
		{
			lock (this.ThisLock)
			{
				if (this.pendingWriteSize > 0)
				{
					int num = this.writeBufferSize - this.pendingWriteSize;
					this.CancelFlushTimer();
					if (size > num)
					{
						TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
						this.FlushCore(timeoutHelper.RemainingTime());
						timeout = timeoutHelper.RemainingTime();
					}
					else
					{
						Buffer.BlockCopy(buffer, offset, this.writeBuffer, this.pendingWriteSize, size);
						if (bufferManager != null)
						{
							bufferManager.ReturnBuffer(buffer);
						}
						Microsoft.ServiceBus.Channels.BufferedConnection bufferedConnection = this;
						bufferedConnection.pendingWriteSize = bufferedConnection.pendingWriteSize + size;
						this.FlushCore(timeout);
						return;
					}
				}
				if (bufferManager != null)
				{
					base.Connection.Write(buffer, offset, size, true, timeout, bufferManager);
				}
				else
				{
					base.Connection.Write(buffer, offset, size, true, timeout);
				}
			}
		}
	}
}