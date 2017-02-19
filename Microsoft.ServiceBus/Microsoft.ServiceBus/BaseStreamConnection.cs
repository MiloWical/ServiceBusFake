using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;

namespace Microsoft.ServiceBus
{
	internal class BaseStreamConnection : Microsoft.ServiceBus.Channels.IConnection
	{
		private int asyncReadBufferSize;

		private byte[] readBuffer;

		private TimeSpan sendTimeout;

		private TimeSpan receiveTimeout;

		private bool aborted;

		private int asyncReadSize;

		private AsyncCallback onRead;

		private WaitCallback asyncReadCallback;

		private volatile Exception asyncReadException;

		private object asyncReadState;

		private bool asyncReadPending;

		private bool asyncWritePending;

		private IOThreadTimer receiveTimer;

		private static Action<object> onReceiveTimeout;

		private IOThreadTimer sendTimer;

		private static Action<object> onSendTimeout;

		private string timeoutErrorString;

		private IPEndPoint remoteEndpoint;

		public EventTraceActivity Activity
		{
			get
			{
				return JustDecompileGenerated_get_Activity();
			}
			set
			{
				JustDecompileGenerated_set_Activity(value);
			}
		}

		private EventTraceActivity JustDecompileGenerated_Activity_k__BackingField;

		public EventTraceActivity JustDecompileGenerated_get_Activity()
		{
			return this.JustDecompileGenerated_Activity_k__BackingField;
		}

		private void JustDecompileGenerated_set_Activity(EventTraceActivity value)
		{
			this.JustDecompileGenerated_Activity_k__BackingField = value;
		}

		public byte[] AsyncReadBuffer
		{
			get
			{
				if (this.readBuffer == null)
				{
					lock (this.ThisLock)
					{
						this.ThrowIfClosed();
						if (this.readBuffer == null)
						{
							this.readBuffer = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.Utility.AllocateByteArray(this.asyncReadBufferSize);
						}
					}
				}
				return this.readBuffer;
			}
		}

		public int AsyncReadBufferSize
		{
			get
			{
				return JustDecompileGenerated_get_AsyncReadBufferSize();
			}
			set
			{
				JustDecompileGenerated_set_AsyncReadBufferSize(value);
			}
		}

		public int JustDecompileGenerated_get_AsyncReadBufferSize()
		{
			return this.asyncReadBufferSize;
		}

		protected void JustDecompileGenerated_set_AsyncReadBufferSize(int value)
		{
			this.asyncReadBufferSize = value;
		}

		protected BaseStreamConnection.State CloseState
		{
			get;
			set;
		}

		public TraceEventType ExceptionEventType
		{
			get;
			set;
		}

		protected bool IsShutdown
		{
			get;
			set;
		}

		private TimeSpan ReceiveTimeout
		{
			get
			{
				if (!this.Stream.CanTimeout)
				{
					return this.receiveTimeout;
				}
				return Microsoft.ServiceBus.Common.TimeoutHelper.FromMilliseconds(this.Stream.ReadTimeout);
			}
			set
			{
				if (this.Stream.CanTimeout)
				{
					this.Stream.ReadTimeout = Microsoft.ServiceBus.Common.TimeoutHelper.ToMilliseconds(value);
				}
				this.receiveTimeout = value;
			}
		}

		private IOThreadTimer ReceiveTimer
		{
			get
			{
				if (this.receiveTimer == null)
				{
					if (BaseStreamConnection.onReceiveTimeout == null)
					{
						BaseStreamConnection.onReceiveTimeout = new Action<object>(BaseStreamConnection.OnReceiveTimeout);
					}
					this.receiveTimer = new IOThreadTimer(BaseStreamConnection.onReceiveTimeout, this, false);
				}
				return this.receiveTimer;
			}
		}

		public IPEndPoint RemoteIPEndPoint
		{
			get
			{
				return JustDecompileGenerated_get_RemoteIPEndPoint();
			}
			set
			{
				JustDecompileGenerated_set_RemoteIPEndPoint(value);
			}
		}

		public IPEndPoint JustDecompileGenerated_get_RemoteIPEndPoint()
		{
			return this.remoteEndpoint;
		}

		public void JustDecompileGenerated_set_RemoteIPEndPoint(IPEndPoint value)
		{
			this.remoteEndpoint = value;
		}

		private TimeSpan SendTimeout
		{
			get
			{
				if (!this.Stream.CanTimeout)
				{
					return this.sendTimeout;
				}
				return Microsoft.ServiceBus.Common.TimeoutHelper.FromMilliseconds(this.Stream.WriteTimeout);
			}
			set
			{
				if (this.Stream.CanTimeout)
				{
					this.Stream.WriteTimeout = Microsoft.ServiceBus.Common.TimeoutHelper.ToMilliseconds(value);
				}
				this.sendTimeout = value;
			}
		}

		private IOThreadTimer SendTimer
		{
			get
			{
				if (this.sendTimer == null)
				{
					if (BaseStreamConnection.onSendTimeout == null)
					{
						BaseStreamConnection.onSendTimeout = new Action<object>(BaseStreamConnection.OnSendTimeout);
					}
					this.sendTimer = new IOThreadTimer(BaseStreamConnection.onSendTimeout, this, false);
				}
				return this.sendTimer;
			}
		}

		protected System.IO.Stream Stream
		{
			get;
			private set;
		}

		protected object ThisLock
		{
			get
			{
				return this;
			}
		}

		public BaseStreamConnection(System.IO.Stream stream, EventTraceActivity activity) : this(stream, 65536, activity)
		{
		}

		public BaseStreamConnection(System.IO.Stream stream, int asyncReadBufferSize, EventTraceActivity activity)
		{
			this.asyncReadBufferSize = asyncReadBufferSize;
			this.onRead = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(this.OnRead));
			this.Activity = activity;
			this.ExceptionEventType = TraceEventType.Warning;
			this.Stream = stream;
			if (!stream.CanTimeout)
			{
				TimeSpan maxValue = TimeSpan.MaxValue;
				TimeSpan timeSpan = maxValue;
				this.ReceiveTimeout = maxValue;
				this.SendTimeout = timeSpan;
				return;
			}
			this.SendTimeout = Microsoft.ServiceBus.Common.TimeoutHelper.FromMilliseconds(stream.WriteTimeout);
			this.ReceiveTimeout = Microsoft.ServiceBus.Common.TimeoutHelper.FromMilliseconds(stream.ReadTimeout);
		}

		public virtual void Abort()
		{
			try
			{
				this.Abort(null);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				Fx.Exception.TraceHandled(exception, "BaseStreamConnection.Abort", this.Activity);
			}
		}

		private void Abort(string timeoutErrorString)
		{
			TraceEventType exceptionEventType = TraceEventType.Warning;
			if (this.ExceptionEventType == TraceEventType.Information)
			{
				exceptionEventType = this.ExceptionEventType;
			}
			this.Abort(exceptionEventType, timeoutErrorString);
		}

		private void Abort(TraceEventType traceEventType, string timeoutErrorString)
		{
			lock (this.ThisLock)
			{
				if (this.CloseState != BaseStreamConnection.State.Closed)
				{
					this.timeoutErrorString = timeoutErrorString;
					this.aborted = true;
					this.CloseState = BaseStreamConnection.State.Closed;
					if (this.asyncReadPending)
					{
						this.CancelReceiveTimer();
					}
					if (this.asyncWritePending)
					{
						this.CancelSendTimer();
					}
				}
				else
				{
					return;
				}
			}
			if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTrace(traceEventType))
			{
				TraceUtility.TraceEvent(traceEventType, TraceCode.SocketConnectionAbort, this);
			}
			try
			{
				this.Stream.Close();
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				Fx.Exception.TraceHandled(exception, "BaseStreamConnection.Abort", this.Activity);
			}
		}

		private void AbortRead()
		{
			lock (this.ThisLock)
			{
				if (this.asyncReadPending && this.CloseState != BaseStreamConnection.State.Closed)
				{
					this.asyncReadPending = false;
					this.CancelReceiveTimer();
				}
			}
		}

		public AsyncReadResult BeginRead(int offset, int size, TimeSpan timeout, WaitCallback callback, object state)
		{
			AsyncReadResult asyncReadResult;
			Microsoft.ServiceBus.Channels.ConnectionUtilities.ValidateBufferBounds(this.AsyncReadBufferSize, offset, size);
			bool flag = true;
			lock (this.ThisLock)
			{
				this.ThrowIfClosed();
				this.asyncReadState = state;
				this.asyncReadCallback = callback;
				this.asyncReadPending = true;
				this.SetReadTimeout(timeout, false, false);
			}
			try
			{
				try
				{
					IAsyncResult asyncResult = this.Stream.BeginRead(this.AsyncReadBuffer, offset, size, this.onRead, null);
					if (asyncResult.CompletedSynchronously)
					{
						this.asyncReadSize = this.Stream.EndRead(asyncResult);
						flag = false;
						asyncReadResult = AsyncReadResult.Completed;
					}
					else
					{
						flag = false;
						asyncReadResult = AsyncReadResult.Queued;
					}
				}
				catch (ObjectDisposedException objectDisposedException1)
				{
					ObjectDisposedException objectDisposedException = objectDisposedException1;
					throw Fx.Exception.AsInformation(this.ConvertObjectDisposedException(objectDisposedException), null);
				}
				catch (IOException oException1)
				{
					IOException oException = oException1;
					throw Fx.Exception.AsInformation(BaseStreamConnection.ConvertIOException(oException), null);
				}
				catch (CommunicationException communicationException)
				{
					throw;
				}
				catch (TimeoutException timeoutException)
				{
					throw;
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (!Fx.IsFatal(exception))
					{
						throw Fx.Exception.AsError(new CommunicationException(exception.Message, exception), null);
					}
					throw;
				}
			}
			finally
			{
				if (flag)
				{
					this.AbortRead();
				}
			}
			return asyncReadResult;
		}

		public IAsyncResult BeginWrite(byte[] buffer, int offset, int size, bool immediate, TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult asyncResult;
			try
			{
				lock (this.ThisLock)
				{
					this.SetWriteTimeOut(timeout, false);
					this.asyncWritePending = true;
				}
				IAsyncResult asyncResult1 = this.Stream.BeginWrite(buffer, offset, size, callback, state);
				asyncResult = asyncResult1;
			}
			catch (ObjectDisposedException objectDisposedException1)
			{
				ObjectDisposedException objectDisposedException = objectDisposedException1;
				throw Fx.Exception.AsInformation(this.ConvertObjectDisposedException(objectDisposedException), null);
			}
			catch (IOException oException1)
			{
				IOException oException = oException1;
				throw Fx.Exception.AsInformation(BaseStreamConnection.ConvertIOException(oException), null);
			}
			catch (CommunicationException communicationException)
			{
				throw;
			}
			catch (TimeoutException timeoutException)
			{
				throw;
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Fx.Exception.AsError(new CommunicationException(exception.Message, exception), null);
				}
				throw;
			}
			return asyncResult;
		}

		private void CancelReceiveTimer()
		{
			if (this.receiveTimer != null)
			{
				this.receiveTimer.Cancel();
				this.receiveTimer = null;
			}
		}

		private void CancelSendTimer()
		{
			if (this.sendTimer != null)
			{
				this.sendTimer.Cancel();
				this.sendTimer = null;
			}
		}

		public virtual void Close(TimeSpan timeout)
		{
			lock (this.ThisLock)
			{
				if (this.CloseState == BaseStreamConnection.State.Closing || this.CloseState == BaseStreamConnection.State.Closed)
				{
					return;
				}
				else
				{
					this.CloseState = BaseStreamConnection.State.Closing;
				}
			}
			try
			{
				this.Stream.Close();
			}
			catch (ObjectDisposedException objectDisposedException1)
			{
				ObjectDisposedException objectDisposedException = objectDisposedException1;
				throw Fx.Exception.AsInformation(this.ConvertObjectDisposedException(objectDisposedException), this.Activity);
			}
			catch (IOException oException1)
			{
				IOException oException = oException1;
				throw Fx.Exception.AsInformation(BaseStreamConnection.ConvertIOException(oException), this.Activity);
			}
			catch (CommunicationException communicationException)
			{
				throw;
			}
			catch (TimeoutException timeoutException)
			{
				throw;
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Fx.Exception.AsError(new CommunicationException(exception.Message, exception), null);
				}
				throw;
			}
			lock (this.ThisLock)
			{
				this.CloseState = BaseStreamConnection.State.Closed;
			}
		}

		protected static Exception ConvertIOException(IOException ioException)
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

		protected Exception ConvertObjectDisposedException(ObjectDisposedException originalException)
		{
			if (this.timeoutErrorString != null)
			{
				return new TimeoutException(this.timeoutErrorString, originalException);
			}
			if (this.aborted)
			{
				return new CommunicationObjectAbortedException(Microsoft.ServiceBus.SR.GetString(Resources.SocketConnectionDisposed, new object[0]), originalException);
			}
			return new CommunicationException(Microsoft.ServiceBus.SR.GetString(Resources.SocketConnectionDisposed, new object[0]), originalException);
		}

		public int EndRead()
		{
			if (this.asyncReadException != null)
			{
				Fx.Exception.AsInformation(this.asyncReadException, null);
				this.AbortRead();
				throw this.asyncReadException;
			}
			lock (this.ThisLock)
			{
				if (!this.asyncReadPending)
				{
					throw Fx.AssertAndThrow("BaseStreamConnection.EndRead called with no read pending.");
				}
				this.asyncReadPending = false;
			}
			return this.asyncReadSize;
		}

		public void EndWrite(IAsyncResult result)
		{
			try
			{
				this.CancelSendTimer();
				lock (this.ThisLock)
				{
					this.asyncWritePending = false;
				}
				this.Stream.EndWrite(result);
			}
			catch (ObjectDisposedException objectDisposedException1)
			{
				ObjectDisposedException objectDisposedException = objectDisposedException1;
				throw Fx.Exception.AsInformation(this.ConvertObjectDisposedException(objectDisposedException), null);
			}
			catch (IOException oException1)
			{
				IOException oException = oException1;
				throw Fx.Exception.AsInformation(BaseStreamConnection.ConvertIOException(oException), null);
			}
			catch (CommunicationException communicationException)
			{
				throw;
			}
			catch (TimeoutException timeoutException)
			{
				throw;
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Fx.Exception.AsError(new CommunicationException(exception.Message, exception), null);
				}
				throw;
			}
		}

		private void FinishRead()
		{
			try
			{
				WaitCallback waitCallback = this.asyncReadCallback;
				object obj = this.asyncReadState;
				this.asyncReadState = null;
				this.asyncReadCallback = null;
				waitCallback(obj);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				Fx.Exception.TraceHandled(exception, "BaseStreamConnection.FinishRead", this.Activity);
				this.AbortRead();
			}
		}

		public virtual T GetProperty<T>()
		{
			return default(T);
		}

		private void OnRead(IAsyncResult result)
		{
			this.CancelReceiveTimer();
			if (result.CompletedSynchronously)
			{
				return;
			}
			try
			{
				this.asyncReadSize = this.Stream.EndRead(result);
			}
			catch (ObjectDisposedException objectDisposedException)
			{
				this.asyncReadException = this.ConvertObjectDisposedException(objectDisposedException);
			}
			catch (IOException oException)
			{
				this.asyncReadException = BaseStreamConnection.ConvertIOException(oException);
			}
			catch (CommunicationException communicationException)
			{
				throw;
			}
			catch (TimeoutException timeoutException)
			{
				throw;
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				this.asyncReadException = Fx.Exception.AsError(new CommunicationException(exception.Message, exception), null);
			}
			this.FinishRead();
		}

		private static void OnReceiveTimeout(object state)
		{
			BaseStreamConnection baseStreamConnection = (BaseStreamConnection)state;
			string socketAbortedReceiveTimedOut = Resources.SocketAbortedReceiveTimedOut;
			object[] receiveTimeout = new object[] { baseStreamConnection.ReceiveTimeout };
			baseStreamConnection.Abort(Microsoft.ServiceBus.SR.GetString(socketAbortedReceiveTimedOut, receiveTimeout));
		}

		private static void OnSendTimeout(object state)
		{
			BaseStreamConnection baseStreamConnection = (BaseStreamConnection)state;
			string socketAbortedSendTimedOut = Resources.SocketAbortedSendTimedOut;
			object[] sendTimeout = new object[] { baseStreamConnection.SendTimeout };
			baseStreamConnection.Abort(TraceEventType.Warning, Microsoft.ServiceBus.SR.GetString(socketAbortedSendTimedOut, sendTimeout));
		}

		public int Read(byte[] buffer, int offset, int size, TimeSpan timeout)
		{
			this.ThrowIfClosed();
			return this.Read(buffer, offset, size, timeout, false);
		}

		protected int Read(byte[] buffer, int offset, int size, TimeSpan timeout, bool closing)
		{
			int num = 0;
			Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
			try
			{
				this.SetReadTimeout(timeoutHelper.RemainingTime(), true, closing);
				num = this.Stream.Read(buffer, offset, size);
			}
			catch (ObjectDisposedException objectDisposedException1)
			{
				ObjectDisposedException objectDisposedException = objectDisposedException1;
				throw Fx.Exception.AsInformation(this.ConvertObjectDisposedException(objectDisposedException), null);
			}
			catch (IOException oException1)
			{
				IOException oException = oException1;
				throw Fx.Exception.AsInformation(BaseStreamConnection.ConvertIOException(oException), null);
			}
			catch (CommunicationException communicationException)
			{
				throw;
			}
			catch (TimeoutException timeoutException)
			{
				throw;
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Fx.Exception.AsError(new CommunicationException(exception.Message, exception), null);
				}
				throw;
			}
			return num;
		}

		private void SetReadTimeout(TimeSpan timeout, bool synchronous, bool closing)
		{
			if (!synchronous)
			{
				this.ReceiveTimeout = timeout;
				if (timeout == TimeSpan.MaxValue)
				{
					this.CancelReceiveTimer();
					return;
				}
				this.ReceiveTimer.Set(timeout);
			}
			else
			{
				this.CancelReceiveTimer();
				if (timeout <= TimeSpan.Zero)
				{
					ExceptionTrace exception = Fx.Exception;
					string tcpConnectionTimedOut = Resources.TcpConnectionTimedOut;
					object[] objArray = new object[] { timeout };
					throw exception.AsInformation(new TimeoutException(Microsoft.ServiceBus.SR.GetString(tcpConnectionTimedOut, objArray)), null);
				}
				if (BaseStreamConnection.UpdateTimeout(this.ReceiveTimeout, timeout))
				{
					lock (this.ThisLock)
					{
						if (!closing || this.CloseState != BaseStreamConnection.State.Closing)
						{
							this.ThrowIfClosed();
						}
					}
					this.ReceiveTimeout = timeout;
					return;
				}
			}
		}

		private void SetWriteTimeOut(TimeSpan timeout, bool synchronous)
		{
			if (!synchronous)
			{
				this.SendTimeout = timeout;
				if (timeout == TimeSpan.MaxValue)
				{
					this.CancelSendTimer();
					return;
				}
				this.SendTimer.Set(timeout);
			}
			else
			{
				this.CancelSendTimer();
				if (timeout <= TimeSpan.Zero)
				{
					ExceptionTrace exception = Fx.Exception;
					string tcpConnectionTimedOut = Resources.TcpConnectionTimedOut;
					object[] objArray = new object[] { timeout };
					throw exception.AsInformation(new TimeoutException(Microsoft.ServiceBus.SR.GetString(tcpConnectionTimedOut, objArray)), null);
				}
				if (BaseStreamConnection.UpdateTimeout(this.SendTimeout, timeout))
				{
					lock (this.ThisLock)
					{
						this.ThrowIfClosed();
					}
					this.SendTimeout = timeout;
					return;
				}
			}
		}

		public virtual void Shutdown(TimeSpan timeout)
		{
			lock (this.ThisLock)
			{
				if (!this.IsShutdown)
				{
					this.IsShutdown = true;
				}
			}
		}

		private void ThrowIfClosed()
		{
			if (this.CloseState == BaseStreamConnection.State.Closing || this.CloseState == BaseStreamConnection.State.Closed)
			{
				throw Fx.Exception.AsInformation(this.ConvertObjectDisposedException(new ObjectDisposedException(this.GetType().ToString(), Microsoft.ServiceBus.SR.GetString(Resources.SocketConnectionDisposed, new object[0]))), null);
			}
		}

		private static bool UpdateTimeout(TimeSpan oldTimeout, TimeSpan newTimeout)
		{
			if (oldTimeout == newTimeout)
			{
				return false;
			}
			long ticks = oldTimeout.Ticks / (long)10;
			long num = Math.Max(oldTimeout.Ticks, newTimeout.Ticks) - Math.Min(oldTimeout.Ticks, newTimeout.Ticks);
			return num > ticks;
		}

		public bool Validate(Uri uri)
		{
			return true;
		}

		public void Write(byte[] buffer, int offset, int size, bool immediate, TimeSpan timeout)
		{
			try
			{
				this.SetWriteTimeOut(timeout, true);
				this.Stream.Write(buffer, offset, size);
			}
			catch (ObjectDisposedException objectDisposedException1)
			{
				ObjectDisposedException objectDisposedException = objectDisposedException1;
				throw Fx.Exception.AsInformation(this.ConvertObjectDisposedException(objectDisposedException), null);
			}
			catch (IOException oException1)
			{
				IOException oException = oException1;
				throw Fx.Exception.AsInformation(BaseStreamConnection.ConvertIOException(oException), null);
			}
			catch (CommunicationException communicationException)
			{
				throw;
			}
			catch (TimeoutException timeoutException)
			{
				throw;
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Fx.Exception.AsError(new CommunicationException(exception.Message, exception), null);
				}
				throw;
			}
		}

		public void Write(byte[] buffer, int offset, int size, bool immediate, TimeSpan timeout, BufferManager bufferManager)
		{
			try
			{
				this.Write(buffer, offset, size, immediate, timeout);
			}
			finally
			{
				bufferManager.ReturnBuffer(buffer);
			}
		}

		protected enum State
		{
			Open,
			Closing,
			Closed
		}
	}
}