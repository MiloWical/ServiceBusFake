using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Tracing;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus
{
	internal class CompositeDuplexStream : Stream
	{
		protected Stream inputStream;

		protected Stream outputStream;

		private int lastReadTimeout = 2147483647;

		private bool disposed;

		protected EventTraceActivity Activity
		{
			get;
			private set;
		}

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
				Stream stream = this.inputStream;
				Stream stream1 = this.outputStream;
				if (stream == null || stream1 == null)
				{
					return false;
				}
				if (!stream.CanTimeout)
				{
					return false;
				}
				return stream1.CanTimeout;
			}
		}

		public override bool CanWrite
		{
			get
			{
				return true;
			}
		}

		public override long Length
		{
			get
			{
				throw Fx.Exception.AsError(new NotImplementedException(string.Concat(base.GetType().Name, ".get_Length")), this.Activity);
			}
		}

		public override long Position
		{
			get
			{
				throw Fx.Exception.AsError(new NotImplementedException(string.Concat(base.GetType().Name, ".get_Position")), this.Activity);
			}
			set
			{
				throw Fx.Exception.AsError(new NotImplementedException(string.Concat(base.GetType().Name, ".set_Position")), this.Activity);
			}
		}

		public override int ReadTimeout
		{
			get
			{
				Stream stream = this.inputStream;
				if (stream == null)
				{
					throw Fx.Exception.AsWarning(new ObjectDisposedException(base.GetType().Name), this.Activity);
				}
				return stream.ReadTimeout;
			}
			set
			{
				Stream stream = this.inputStream;
				if (stream == null)
				{
					throw Fx.Exception.AsWarning(new ObjectDisposedException(base.GetType().Name), this.Activity);
				}
				stream.ReadTimeout = value;
			}
		}

		public override int WriteTimeout
		{
			get
			{
				Stream stream = this.outputStream;
				if (stream == null)
				{
					throw Fx.Exception.AsWarning(new ObjectDisposedException(base.GetType().Name), this.Activity);
				}
				return stream.WriteTimeout;
			}
			set
			{
				Stream stream = this.outputStream;
				if (stream == null)
				{
					throw Fx.Exception.AsWarning(new ObjectDisposedException(base.GetType().Name), this.Activity);
				}
				stream.WriteTimeout = value;
			}
		}

		protected CompositeDuplexStream(EventTraceActivity activity) : this(null, null, activity)
		{
		}

		public CompositeDuplexStream(Stream inputStream, Stream outputStream, EventTraceActivity activity)
		{
			this.inputStream = inputStream;
			this.outputStream = outputStream;
			this.Activity = activity;
		}

		public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			Stream stream = this.inputStream;
			if (stream == null)
			{
				throw Fx.Exception.AsWarning(new ObjectDisposedException(base.GetType().Name), this.Activity);
			}
			if (stream.CanTimeout)
			{
				this.lastReadTimeout = stream.ReadTimeout;
				stream.ReadTimeout = 2147483647;
			}
			return stream.BeginRead(buffer, offset, count, callback, state);
		}

		public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			Stream stream = this.outputStream;
			if (stream == null)
			{
				throw Fx.Exception.AsWarning(new ObjectDisposedException(base.GetType().Name), this.Activity);
			}
			return stream.BeginWrite(buffer, offset, count, callback, state);
		}

		protected override void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				this.disposed = true;
				try
				{
					if (disposing)
					{
						this.DisposeStream(ref this.inputStream);
						this.DisposeStream(ref this.outputStream);
					}
				}
				finally
				{
					base.Dispose(disposing);
				}
			}
		}

		private void DisposeStream(ref Stream stream)
		{
			Stream stream1 = stream;
			stream = null;
			if (stream1 != null)
			{
				try
				{
					stream1.Close();
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					Fx.Exception.TraceHandled(exception, "CompositeDuplexStream.DisposeStream", this.Activity);
				}
			}
		}

		public override int EndRead(IAsyncResult asyncResult)
		{
			Stream stream = this.inputStream;
			if (stream == null)
			{
				throw Fx.Exception.AsWarning(new ObjectDisposedException(base.GetType().Name), this.Activity);
			}
			if (stream.CanTimeout)
			{
				stream.ReadTimeout = this.lastReadTimeout;
			}
			return stream.EndRead(asyncResult);
		}

		public override void EndWrite(IAsyncResult asyncResult)
		{
			Stream stream = this.outputStream;
			if (stream == null)
			{
				throw Fx.Exception.AsWarning(new ObjectDisposedException(base.GetType().Name), this.Activity);
			}
			stream.EndWrite(asyncResult);
		}

		public override void Flush()
		{
			try
			{
				Stream stream = this.outputStream;
				if (stream != null)
				{
					stream.Flush();
				}
			}
			catch (NotSupportedException notSupportedException)
			{
			}
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			Stream stream = this.inputStream;
			if (stream == null)
			{
				throw Fx.Exception.AsWarning(new ObjectDisposedException(base.GetType().Name), this.Activity);
			}
			return stream.Read(buffer, offset, count);
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw Fx.Exception.AsError(new NotImplementedException(string.Concat(base.GetType().Name, ".Seek")), this.Activity);
		}

		public override void SetLength(long value)
		{
			throw Fx.Exception.AsError(new NotImplementedException(string.Concat(base.GetType().Name, ".SetLength")), this.Activity);
		}

		public virtual void Shutdown()
		{
			Stream stream = this.outputStream;
			if (stream != null)
			{
				stream.Close();
			}
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			Stream stream = this.outputStream;
			if (stream == null)
			{
				throw Fx.Exception.AsWarning(new ObjectDisposedException(base.GetType().Name), this.Activity);
			}
			stream.Write(buffer, offset, count);
		}
	}
}