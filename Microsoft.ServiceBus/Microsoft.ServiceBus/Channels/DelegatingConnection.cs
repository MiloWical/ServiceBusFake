using Microsoft.ServiceBus.Tracing;
using System;
using System.Diagnostics;
using System.Net;
using System.ServiceModel.Channels;
using System.Threading;

namespace Microsoft.ServiceBus.Channels
{
	internal abstract class DelegatingConnection : Microsoft.ServiceBus.Channels.IConnection
	{
		private readonly Microsoft.ServiceBus.Channels.IConnection connection;

		public virtual EventTraceActivity Activity
		{
			get
			{
				return this.connection.Activity;
			}
		}

		public virtual byte[] AsyncReadBuffer
		{
			get
			{
				return this.connection.AsyncReadBuffer;
			}
		}

		public virtual int AsyncReadBufferSize
		{
			get
			{
				return this.connection.AsyncReadBufferSize;
			}
		}

		protected Microsoft.ServiceBus.Channels.IConnection Connection
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

		public IPEndPoint RemoteIPEndPoint
		{
			get
			{
				return this.connection.RemoteIPEndPoint;
			}
		}

		protected DelegatingConnection(Microsoft.ServiceBus.Channels.IConnection connection)
		{
			this.connection = connection;
		}

		public virtual void Abort()
		{
			this.connection.Abort();
		}

		public virtual AsyncReadResult BeginRead(int offset, int size, TimeSpan timeout, WaitCallback callback, object state)
		{
			return this.connection.BeginRead(offset, size, timeout, callback, state);
		}

		public virtual IAsyncResult BeginWrite(byte[] buffer, int offset, int size, bool immediate, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.connection.BeginWrite(buffer, offset, size, immediate, timeout, callback, state);
		}

		public virtual void Close(TimeSpan timeout)
		{
			this.connection.Close(timeout);
		}

		public virtual int EndRead()
		{
			return this.connection.EndRead();
		}

		public virtual void EndWrite(IAsyncResult result)
		{
			this.connection.EndWrite(result);
		}

		T Microsoft.ServiceBus.Channels.IConnection.GetProperty<T>()
		{
			return this.connection.GetProperty<T>();
		}

		public virtual int Read(byte[] buffer, int offset, int size, TimeSpan timeout)
		{
			return this.connection.Read(buffer, offset, size, timeout);
		}

		public virtual void Shutdown(TimeSpan timeout)
		{
			this.connection.Shutdown(timeout);
		}

		public virtual bool Validate(Uri uri)
		{
			return this.connection.Validate(uri);
		}

		public virtual void Write(byte[] buffer, int offset, int size, bool immediate, TimeSpan timeout)
		{
			this.connection.Write(buffer, offset, size, immediate, timeout);
		}

		public virtual void Write(byte[] buffer, int offset, int size, bool immediate, TimeSpan timeout, BufferManager bufferManager)
		{
			this.connection.Write(buffer, offset, size, immediate, timeout, bufferManager);
		}
	}
}