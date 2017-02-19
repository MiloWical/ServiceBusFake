using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Properties;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Diagnostics;
using System.IO;
using System.ServiceModel;
using System.Threading;

namespace Microsoft.ServiceBus.Channels
{
	internal class ConnectionStream : Stream
	{
		private readonly IConnection connection;

		private TimeSpan closeTimeout;

		private int readTimeout;

		private int writeTimeout;

		private bool immediate;

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
				return false;
			}
		}

		public override bool CanTimeout
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
				return true;
			}
		}

		public TimeSpan CloseTimeout
		{
			get
			{
				return this.closeTimeout;
			}
			set
			{
				this.closeTimeout = value;
			}
		}

		public IConnection Connection
		{
			get
			{
				return this.connection;
			}
		}

		public TraceEventType ExceptionEventType
		{
			get
			{
				return this.connection.ExceptionEventType;
			}
			set
			{
				this.connection.ExceptionEventType = value;
			}
		}

		public bool Immediate
		{
			get
			{
				return this.immediate;
			}
			set
			{
				this.immediate = value;
			}
		}

		public override long Length
		{
			get
			{
				throw Fx.Exception.AsError(new NotSupportedException(Microsoft.ServiceBus.SR.GetString(Resources.SeekNotSupported, new object[0])), this.Connection.Activity);
			}
		}

		public override long Position
		{
			get
			{
				throw Fx.Exception.AsError(new NotSupportedException(Microsoft.ServiceBus.SR.GetString(Resources.SeekNotSupported, new object[0])), this.Connection.Activity);
			}
			set
			{
				throw Fx.Exception.AsError(new NotSupportedException(Microsoft.ServiceBus.SR.GetString(Resources.SeekNotSupported, new object[0])), this.Connection.Activity);
			}
		}

		public override int ReadTimeout
		{
			get
			{
				return this.readTimeout;
			}
			set
			{
				if (value < -1)
				{
					ExceptionTrace exception = Fx.Exception;
					object obj = value;
					string valueMustBeInRange = Resources.ValueMustBeInRange;
					object[] objArray = new object[] { -1, 2147483647 };
					throw exception.AsError(new ArgumentOutOfRangeException("value", obj, Microsoft.ServiceBus.SR.GetString(valueMustBeInRange, objArray)), this.connection.Activity);
				}
				this.readTimeout = value;
			}
		}

		public override int WriteTimeout
		{
			get
			{
				return this.writeTimeout;
			}
			set
			{
				if (value < -1)
				{
					ExceptionTrace exception = Fx.Exception;
					object obj = value;
					string valueMustBeInRange = Resources.ValueMustBeInRange;
					object[] objArray = new object[] { -1, 2147483647 };
					throw exception.AsError(new ArgumentOutOfRangeException("value", obj, Microsoft.ServiceBus.SR.GetString(valueMustBeInRange, objArray)), this.connection.Activity);
				}
				this.writeTimeout = value;
			}
		}

		public ConnectionStream(IConnection connection, IDefaultCommunicationTimeouts defaultTimeouts) : this(connection, defaultTimeouts.CloseTimeout, defaultTimeouts.ReceiveTimeout, defaultTimeouts.SendTimeout)
		{
		}

		public ConnectionStream(IConnection connection) : this(connection, Microsoft.ServiceBus.Channels.ServiceDefaults.CloseTimeout, Microsoft.ServiceBus.Channels.ServiceDefaults.ReceiveTimeout, Microsoft.ServiceBus.Channels.ServiceDefaults.SendTimeout)
		{
		}

		public ConnectionStream(IConnection connection, TimeSpan closeTimeout, TimeSpan readTimeout, TimeSpan writeTimeout)
		{
			if (connection == null)
			{
				throw Fx.Exception.ArgumentNull("connection");
			}
			this.connection = connection;
			this.closeTimeout = closeTimeout;
			this.ReadTimeout = Microsoft.ServiceBus.Common.TimeoutHelper.ToMilliseconds(readTimeout);
			this.WriteTimeout = Microsoft.ServiceBus.Common.TimeoutHelper.ToMilliseconds(writeTimeout);
			this.immediate = true;
		}

		public void Abort()
		{
			this.connection.Abort();
		}

		public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			return new ConnectionStream.ReadAsyncResult(this.connection, buffer, offset, count, Microsoft.ServiceBus.Common.TimeoutHelper.FromMilliseconds(this.ReadTimeout), callback, state);
		}

		public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			return this.connection.BeginWrite(buffer, offset, count, this.Immediate, Microsoft.ServiceBus.Common.TimeoutHelper.FromMilliseconds(this.WriteTimeout), callback, state);
		}

		public override void Close()
		{
			this.connection.Close(this.CloseTimeout);
		}

		public override int EndRead(IAsyncResult asyncResult)
		{
			return ConnectionStream.ReadAsyncResult.End(asyncResult);
		}

		public override void EndWrite(IAsyncResult asyncResult)
		{
			this.connection.EndWrite(asyncResult);
		}

		public override void Flush()
		{
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return this.Read(buffer, offset, count, Microsoft.ServiceBus.Common.TimeoutHelper.FromMilliseconds(this.ReadTimeout));
		}

		protected int Read(byte[] buffer, int offset, int count, TimeSpan timeout)
		{
			return this.connection.Read(buffer, offset, count, timeout);
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw Fx.Exception.AsError(new NotSupportedException(Microsoft.ServiceBus.SR.GetString(Resources.SeekNotSupported, new object[0])), this.Connection.Activity);
		}

		public override void SetLength(long value)
		{
			throw Fx.Exception.AsError(new NotSupportedException(Microsoft.ServiceBus.SR.GetString(Resources.SeekNotSupported, new object[0])), this.Connection.Activity);
		}

		public void Shutdown(TimeSpan timeout)
		{
			this.connection.Shutdown(timeout);
		}

		public bool Validate(Uri uri)
		{
			return this.connection.Validate(uri);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			this.connection.Write(buffer, offset, count, this.Immediate, Microsoft.ServiceBus.Common.TimeoutHelper.FromMilliseconds(this.WriteTimeout));
		}

		private class ReadAsyncResult : AsyncResult
		{
			private readonly static WaitCallback onAsyncReadComplete;

			private int bytesRead;

			private byte[] buffer;

			private int offset;

			private IConnection connection;

			protected internal override EventTraceActivity Activity
			{
				get
				{
					return this.connection.Activity;
				}
			}

			protected override TraceEventType TraceEventType
			{
				get
				{
					return this.connection.ExceptionEventType;
				}
			}

			static ReadAsyncResult()
			{
				ConnectionStream.ReadAsyncResult.onAsyncReadComplete = new WaitCallback(ConnectionStream.ReadAsyncResult.OnAsyncReadComplete);
			}

			public ReadAsyncResult(IConnection connection, byte[] buffer, int offset, int count, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				this.buffer = buffer;
				this.offset = offset;
				this.connection = connection;
				if (this.connection.BeginRead(0, Math.Min(count, this.connection.AsyncReadBufferSize), timeout, ConnectionStream.ReadAsyncResult.onAsyncReadComplete, this) == AsyncReadResult.Completed)
				{
					this.HandleRead();
					base.Complete(true);
				}
			}

			public static new int End(IAsyncResult result)
			{
				return AsyncResult.End<ConnectionStream.ReadAsyncResult>(result).bytesRead;
			}

			private void HandleRead()
			{
				this.bytesRead = this.connection.EndRead();
				byte[] asyncReadBuffer = this.connection.AsyncReadBuffer;
				if (asyncReadBuffer != null)
				{
					Buffer.BlockCopy(asyncReadBuffer, 0, this.buffer, this.offset, this.bytesRead);
				}
			}

			private static void OnAsyncReadComplete(object state)
			{
				ConnectionStream.ReadAsyncResult readAsyncResult = (ConnectionStream.ReadAsyncResult)state;
				Exception exception = null;
				try
				{
					readAsyncResult.HandleRead();
				}
				catch (Exception exception2)
				{
					Exception exception1 = exception2;
					if (Fx.IsFatal(exception1))
					{
						throw;
					}
					exception = exception1;
				}
				readAsyncResult.Complete(false, exception);
			}
		}
	}
}