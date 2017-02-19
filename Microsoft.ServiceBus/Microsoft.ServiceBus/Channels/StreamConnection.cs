using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;

namespace Microsoft.ServiceBus.Channels
{
	internal class StreamConnection : Microsoft.ServiceBus.Channels.IConnection
	{
		private byte[] asyncReadBuffer;

		private int bytesRead;

		private Microsoft.ServiceBus.Channels.ConnectionStream innerStream;

		private AsyncCallback onRead;

		private IAsyncResult readResult;

		private WaitCallback readCallback;

		private System.IO.Stream stream;

		private TimeSpan lastReadTimeout;

		private TimeSpan lastWriteTimeout;

		public EventTraceActivity Activity
		{
			get
			{
				return this.innerStream.Connection.Activity;
			}
		}

		public byte[] AsyncReadBuffer
		{
			get
			{
				if (this.asyncReadBuffer == null)
				{
					lock (this.ThisLock)
					{
						if (this.asyncReadBuffer == null)
						{
							this.asyncReadBuffer = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.Utility.AllocateByteArray(this.innerStream.Connection.AsyncReadBufferSize);
						}
					}
				}
				return this.asyncReadBuffer;
			}
		}

		public int AsyncReadBufferSize
		{
			get
			{
				return this.innerStream.Connection.AsyncReadBufferSize;
			}
		}

		public TraceEventType ExceptionEventType
		{
			get
			{
				return this.innerStream.ExceptionEventType;
			}
			set
			{
				this.innerStream.ExceptionEventType = value;
			}
		}

		public IPEndPoint RemoteIPEndPoint
		{
			get
			{
				return this.innerStream.Connection.RemoteIPEndPoint;
			}
		}

		public System.IO.Stream Stream
		{
			get
			{
				return this.stream;
			}
		}

		public object ThisLock
		{
			get
			{
				return this;
			}
		}

		public StreamConnection(System.IO.Stream stream, Microsoft.ServiceBus.Channels.ConnectionStream innerStream)
		{
			this.stream = stream;
			this.innerStream = innerStream;
			this.onRead = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(this.OnRead));
		}

		public void Abort()
		{
			this.innerStream.Abort();
		}

		public AsyncReadResult BeginRead(int offset, int size, TimeSpan timeout, WaitCallback callback, object state)
		{
			AsyncReadResult asyncReadResult;
			Microsoft.ServiceBus.Channels.ConnectionUtilities.ValidateBufferBounds(this.AsyncReadBufferSize, offset, size);
			this.readCallback = callback;
			try
			{
				this.SetReadTimeout(timeout);
				IAsyncResult asyncResult = this.stream.BeginRead(this.AsyncReadBuffer, offset, size, this.onRead, state);
				if (asyncResult.CompletedSynchronously)
				{
					this.bytesRead = this.stream.EndRead(asyncResult);
					return AsyncReadResult.Completed;
				}
				else
				{
					asyncReadResult = AsyncReadResult.Queued;
				}
			}
			catch (IOException oException1)
			{
				IOException oException = oException1;
				throw Fx.Exception.AsError(Microsoft.ServiceBus.Channels.StreamConnection.ConvertIOException(oException), this.Activity);
			}
			return asyncReadResult;
		}

		public IAsyncResult BeginWrite(byte[] buffer, int offset, int size, bool immediate, TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult asyncResult;
			try
			{
				this.innerStream.Immediate = immediate;
				this.SetWriteTimeout(timeout);
				asyncResult = this.stream.BeginWrite(buffer, offset, size, callback, state);
			}
			catch (IOException oException1)
			{
				IOException oException = oException1;
				throw Fx.Exception.AsError(Microsoft.ServiceBus.Channels.StreamConnection.ConvertIOException(oException), this.Activity);
			}
			return asyncResult;
		}

		public void Close(TimeSpan timeout)
		{
			this.innerStream.CloseTimeout = timeout;
			try
			{
				this.stream.Close();
			}
			catch (IOException oException1)
			{
				IOException oException = oException1;
				throw Fx.Exception.AsError(Microsoft.ServiceBus.Channels.StreamConnection.ConvertIOException(oException), this.Activity);
			}
		}

		private static Exception ConvertIOException(IOException ioException)
		{
			if (ioException.InnerException is TimeoutException)
			{
				return new TimeoutException(ioException.InnerException.Message, ioException);
			}
			if (ioException.InnerException is CommunicationObjectAbortedException)
			{
				return new CommunicationObjectAbortedException(ioException.InnerException.Message, ioException);
			}
			if (ioException.InnerException is CommunicationException)
			{
				return new CommunicationException(ioException.InnerException.Message, ioException);
			}
			return new CommunicationException(Microsoft.ServiceBus.SR.GetString(Resources.StreamError, new object[0]), ioException);
		}

		public int EndRead()
		{
			if (this.readResult != null)
			{
				IAsyncResult asyncResult = this.readResult;
				this.readResult = null;
				try
				{
					this.bytesRead = this.stream.EndRead(asyncResult);
				}
				catch (IOException oException1)
				{
					IOException oException = oException1;
					throw Fx.Exception.AsError(Microsoft.ServiceBus.Channels.StreamConnection.ConvertIOException(oException), this.Activity);
				}
			}
			return this.bytesRead;
		}

		public void EndWrite(IAsyncResult result)
		{
			try
			{
				this.stream.EndWrite(result);
			}
			catch (IOException oException1)
			{
				IOException oException = oException1;
				throw Fx.Exception.AsError(Microsoft.ServiceBus.Channels.StreamConnection.ConvertIOException(oException), this.Activity);
			}
		}

		T Microsoft.ServiceBus.Channels.IConnection.GetProperty<T>()
		{
			return default(T);
		}

		private void OnRead(IAsyncResult result)
		{
			if (result.CompletedSynchronously)
			{
				return;
			}
			if (this.readResult != null)
			{
				throw Fx.AssertAndThrow("StreamConnection: OnRead called twice.");
			}
			this.readResult = result;
			this.readCallback(result.AsyncState);
		}

		public int Read(byte[] buffer, int offset, int size, TimeSpan timeout)
		{
			int num;
			try
			{
				this.SetReadTimeout(timeout);
				num = this.stream.Read(buffer, offset, size);
			}
			catch (IOException oException1)
			{
				IOException oException = oException1;
				throw Fx.Exception.AsError(Microsoft.ServiceBus.Channels.StreamConnection.ConvertIOException(oException), this.Activity);
			}
			return num;
		}

		private void SetReadTimeout(TimeSpan timeout)
		{
			if (timeout != this.lastReadTimeout)
			{
				int milliseconds = Microsoft.ServiceBus.Common.TimeoutHelper.ToMilliseconds(timeout);
				if (this.stream.CanTimeout)
				{
					this.stream.ReadTimeout = milliseconds;
				}
				this.innerStream.ReadTimeout = milliseconds;
				this.lastReadTimeout = timeout;
			}
		}

		private void SetWriteTimeout(TimeSpan timeout)
		{
			if (timeout != this.lastWriteTimeout)
			{
				int milliseconds = Microsoft.ServiceBus.Common.TimeoutHelper.ToMilliseconds(timeout);
				if (this.stream.CanTimeout)
				{
					this.stream.WriteTimeout = milliseconds;
				}
				this.innerStream.WriteTimeout = milliseconds;
				this.lastWriteTimeout = timeout;
			}
		}

		public void Shutdown(TimeSpan timeout)
		{
			this.innerStream.Shutdown(timeout);
		}

		public bool Validate(Uri uri)
		{
			return this.innerStream.Validate(uri);
		}

		public void Write(byte[] buffer, int offset, int size, bool immediate, TimeSpan timeout)
		{
			try
			{
				this.innerStream.Immediate = immediate;
				this.SetWriteTimeout(timeout);
				this.stream.Write(buffer, offset, size);
			}
			catch (IOException oException1)
			{
				IOException oException = oException1;
				throw Fx.Exception.AsError(Microsoft.ServiceBus.Channels.StreamConnection.ConvertIOException(oException), this.Activity);
			}
		}

		public void Write(byte[] buffer, int offset, int size, bool immediate, TimeSpan timeout, BufferManager bufferManager)
		{
			this.Write(buffer, offset, size, immediate, timeout);
			bufferManager.ReturnBuffer(buffer);
		}
	}
}