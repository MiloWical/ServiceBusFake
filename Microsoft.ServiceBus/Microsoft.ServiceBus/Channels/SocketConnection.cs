using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;

namespace Microsoft.ServiceBus.Channels
{
	internal class SocketConnection : Microsoft.ServiceBus.Channels.IConnection
	{
		private const int MinSocketBufferSize = 524288;

		private static EventHandler<SocketAsyncEventArgs> onReceiveAsyncCompleted;

		private readonly Socket socket;

		private TimeSpan sendTimeout;

		private TimeSpan readFinTimeout;

		private TimeSpan receiveTimeout;

		private Microsoft.ServiceBus.Channels.SocketConnection.CloseState closeState;

		private bool isShutdown;

		private bool noDelay;

		private bool aborted;

		private TraceEventType exceptionEventType;

		private Microsoft.ServiceBus.Common.TimeoutHelper closeTimeoutHelper;

		private int asyncReadSize;

		private SocketAsyncEventArgs asyncReadEventArgs;

		private byte[] readBuffer;

		private int asyncReadBufferSize;

		private object asyncReadState;

		private WaitCallback asyncReadCallback;

		private Exception asyncReadException;

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
				return this.readBuffer;
			}
		}

		public int AsyncReadBufferSize
		{
			get
			{
				return this.asyncReadBufferSize;
			}
		}

		public TraceEventType ExceptionEventType
		{
			get
			{
				return this.exceptionEventType;
			}
			set
			{
				this.exceptionEventType = value;
			}
		}

		private IOThreadTimer ReceiveTimer
		{
			get
			{
				if (this.receiveTimer == null)
				{
					if (Microsoft.ServiceBus.Channels.SocketConnection.onReceiveTimeout == null)
					{
						Microsoft.ServiceBus.Channels.SocketConnection.onReceiveTimeout = new Action<object>(Microsoft.ServiceBus.Channels.SocketConnection.OnReceiveTimeout);
					}
					this.receiveTimer = new IOThreadTimer(Microsoft.ServiceBus.Channels.SocketConnection.onReceiveTimeout, this, false);
				}
				return this.receiveTimer;
			}
		}

		public IPEndPoint RemoteIPEndPoint
		{
			get
			{
				if (this.remoteEndpoint == null && this.closeState == Microsoft.ServiceBus.Channels.SocketConnection.CloseState.Open)
				{
					try
					{
						this.remoteEndpoint = (IPEndPoint)this.socket.RemoteEndPoint;
					}
					catch (SocketException socketException1)
					{
						SocketException socketException = socketException1;
						throw Fx.Exception.TraceException<Exception>(this.ConvertReceiveException(socketException, TimeSpan.Zero), this.ExceptionEventType, this.Activity);
					}
					catch (ObjectDisposedException objectDisposedException1)
					{
						ObjectDisposedException objectDisposedException = objectDisposedException1;
						Exception exception = this.ConvertObjectDisposedException(objectDisposedException);
						if (!object.ReferenceEquals(exception, objectDisposedException))
						{
							throw Fx.Exception.TraceException<Exception>(exception, this.ExceptionEventType, this.Activity);
						}
						throw;
					}
				}
				return this.remoteEndpoint;
			}
		}

		private string RemoteIPEndPointString
		{
			get
			{
				IPEndPoint remoteIPEndPoint = this.RemoteIPEndPoint;
				if (remoteIPEndPoint == null)
				{
					return string.Empty;
				}
				return remoteIPEndPoint.ToString();
			}
		}

		private IOThreadTimer SendTimer
		{
			get
			{
				if (this.sendTimer == null)
				{
					if (Microsoft.ServiceBus.Channels.SocketConnection.onSendTimeout == null)
					{
						Microsoft.ServiceBus.Channels.SocketConnection.onSendTimeout = new Action<object>(Microsoft.ServiceBus.Channels.SocketConnection.OnSendTimeout);
					}
					this.sendTimer = new IOThreadTimer(Microsoft.ServiceBus.Channels.SocketConnection.onSendTimeout, this, false);
				}
				return this.sendTimer;
			}
		}

		private object ThisLock
		{
			get
			{
				return this;
			}
		}

		public SocketConnection(Socket socket, int asyncReadBufferSize, EventTraceActivity activity)
		{
			if (socket == null)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("socket");
			}
			this.closeState = Microsoft.ServiceBus.Channels.SocketConnection.CloseState.Open;
			this.exceptionEventType = TraceEventType.Warning;
			this.Activity = activity;
			this.socket = socket;
			this.readBuffer = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.Utility.AllocateByteArray(asyncReadBufferSize);
			Socket socket1 = this.socket;
			Socket socket2 = this.socket;
			int num = Math.Max(524288, asyncReadBufferSize);
			int num1 = num;
			socket2.SendBufferSize = num;
			socket1.ReceiveBufferSize = num1;
			TimeSpan maxValue = TimeSpan.MaxValue;
			TimeSpan timeSpan = maxValue;
			this.receiveTimeout = maxValue;
			this.sendTimeout = timeSpan;
			this.asyncReadBufferSize = asyncReadBufferSize;
			this.TraceSocketInfo(socket, TraceCode.SocketConnectionCreate, null);
		}

		public void Abort()
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
				Fx.Exception.TraceHandled(exception, "SocketConnection.Abort", this.Activity);
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
				if (this.closeState != Microsoft.ServiceBus.Channels.SocketConnection.CloseState.Closed)
				{
					this.timeoutErrorString = timeoutErrorString;
					this.aborted = true;
					this.closeState = Microsoft.ServiceBus.Channels.SocketConnection.CloseState.Closed;
					if (!this.asyncReadPending)
					{
						this.DisposeReadEventArgs();
					}
					else
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
			this.TraceSocketInfo(this.socket, TraceCode.SocketConnectionAbort, timeoutErrorString);
			this.socket.Close(0);
		}

		private void AbortRead()
		{
			lock (this.ThisLock)
			{
				if (this.asyncReadPending)
				{
					if (this.closeState == Microsoft.ServiceBus.Channels.SocketConnection.CloseState.Closed)
					{
						this.DisposeReadEventArgs();
					}
					else
					{
						this.SetUserToken(this.asyncReadEventArgs, null);
						this.asyncReadPending = false;
						this.CancelReceiveTimer();
					}
				}
			}
		}

		public virtual AsyncReadResult BeginRead(int offset, int size, TimeSpan timeout, WaitCallback callback, object state)
		{
			Microsoft.ServiceBus.Channels.ConnectionUtilities.ValidateBufferBounds(this.AsyncReadBufferSize, offset, size);
			this.ThrowIfClosed();
			return this.BeginReadCore(offset, size, timeout, callback, state);
		}

		private AsyncReadResult BeginReadCore(int offset, int size, TimeSpan timeout, WaitCallback callback, object state)
		{
			AsyncReadResult asyncReadResult;
			bool flag = true;
			lock (this.ThisLock)
			{
				this.ThrowIfClosed();
				Fx.AssertAndThrow(!this.asyncReadPending, "SocketConnection.BeginRead called with an existing read pending.");
				this.EnsureReadEventArgs();
				this.asyncReadState = state;
				this.asyncReadCallback = callback;
				this.SetUserToken(this.asyncReadEventArgs, this);
				this.asyncReadPending = true;
				this.SetReadTimeout(timeout, false, false);
			}
			try
			{
				try
				{
					if (offset != this.asyncReadEventArgs.Offset || size != this.asyncReadEventArgs.Count)
					{
						this.asyncReadEventArgs.SetBuffer(offset, size);
					}
					if (!this.ReceiveAsync())
					{
						this.HandleReceiveAsyncCompleted();
						this.asyncReadSize = this.asyncReadEventArgs.BytesTransferred;
						flag = false;
						asyncReadResult = AsyncReadResult.Completed;
					}
					else
					{
						flag = false;
						asyncReadResult = AsyncReadResult.Queued;
					}
				}
				catch (SocketException socketException1)
				{
					SocketException socketException = socketException1;
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelper(this.ConvertReceiveException(socketException, TimeSpan.MaxValue), this.ExceptionEventType);
				}
				catch (ObjectDisposedException objectDisposedException1)
				{
					ObjectDisposedException objectDisposedException = objectDisposedException1;
					Exception exception = this.ConvertObjectDisposedException(objectDisposedException);
					if (!object.ReferenceEquals(exception, objectDisposedException))
					{
						throw Fx.Exception.TraceException<Exception>(exception, this.ExceptionEventType, this.Activity);
					}
					Fx.Exception.TraceException<Exception>(exception, this.ExceptionEventType, this.Activity);
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
			Microsoft.ServiceBus.Channels.ConnectionUtilities.ValidateBufferBounds(buffer, offset, size);
			try
			{
				lock (this.ThisLock)
				{
					this.asyncWritePending = true;
				}
				this.SetImmediate(immediate);
				this.SetWriteTimeout(timeout, false);
				IAsyncResult asyncResult1 = this.socket.BeginSend(buffer, offset, size, SocketFlags.None, callback, state);
				asyncResult = asyncResult1;
			}
			catch (SocketException socketException1)
			{
				SocketException socketException = socketException1;
				throw Fx.Exception.TraceException<Exception>(this.ConvertSendException(socketException, TimeSpan.MaxValue), this.ExceptionEventType, this.Activity);
			}
			catch (ObjectDisposedException objectDisposedException1)
			{
				ObjectDisposedException objectDisposedException = objectDisposedException1;
				Exception exception = this.ConvertObjectDisposedException(objectDisposedException);
				if (!object.ReferenceEquals(exception, objectDisposedException))
				{
					throw Fx.Exception.TraceException<Exception>(exception, this.ExceptionEventType, this.Activity);
				}
				throw;
			}
			return asyncResult;
		}

		private void CancelReceiveTimer()
		{
			IOThreadTimer oThreadTimer = this.receiveTimer;
			this.receiveTimer = null;
			if (oThreadTimer != null)
			{
				oThreadTimer.Cancel();
			}
		}

		private void CancelSendTimer()
		{
			IOThreadTimer oThreadTimer = this.sendTimer;
			this.sendTimer = null;
			if (oThreadTimer != null)
			{
				oThreadTimer.Cancel();
			}
		}

		public void Close(TimeSpan timeout)
		{
			lock (this.ThisLock)
			{
				if (this.closeState == Microsoft.ServiceBus.Channels.SocketConnection.CloseState.Closing || this.closeState == Microsoft.ServiceBus.Channels.SocketConnection.CloseState.Closed)
				{
					return;
				}
				else
				{
					this.TraceSocketInfo(this.socket, TraceCode.SocketConnectionClose, timeout.ToString());
					this.closeState = Microsoft.ServiceBus.Channels.SocketConnection.CloseState.Closing;
				}
			}
			this.closeTimeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
			this.Shutdown(this.closeTimeoutHelper.RemainingTime());
			this.CloseSync();
		}

		private void CloseSync()
		{
			byte[] numArray = new byte[1];
			this.readFinTimeout = this.closeTimeoutHelper.RemainingTime();
			try
			{
				if (this.ReadCore(numArray, 0, 1, this.readFinTimeout, true) > 0)
				{
					ExceptionTrace exception = Fx.Exception;
					string socketCloseReadReceivedData = Resources.SocketCloseReadReceivedData;
					object[] remoteIPEndPointString = new object[] { this.RemoteIPEndPointString };
					throw exception.TraceException<CommunicationException>(new CommunicationException(Microsoft.ServiceBus.SR.GetString(socketCloseReadReceivedData, remoteIPEndPointString)), this.ExceptionEventType, this.Activity);
				}
			}
			catch (TimeoutException timeoutException1)
			{
				TimeoutException timeoutException = timeoutException1;
				ExceptionTrace exceptionTrace = Fx.Exception;
				string socketCloseReadTimeout = Resources.SocketCloseReadTimeout;
				object[] objArray = new object[] { this.RemoteIPEndPointString, this.readFinTimeout };
				throw exceptionTrace.TraceException<TimeoutException>(new TimeoutException(Microsoft.ServiceBus.SR.GetString(socketCloseReadTimeout, objArray), timeoutException), this.ExceptionEventType, this.Activity);
			}
			this.ContinueClose(this.closeTimeoutHelper.RemainingTime());
		}

		private void ContinueClose(TimeSpan timeout)
		{
			if (timeout <= TimeSpan.Zero && Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceWarning)
			{
				TraceUtility.TraceEvent(TraceEventType.Warning, TraceCode.SocketConnectionAbortClose, this);
			}
			this.socket.Close(Microsoft.ServiceBus.Common.TimeoutHelper.ToMilliseconds(timeout));
			lock (this.ThisLock)
			{
				if (!this.asyncReadPending && this.closeState != Microsoft.ServiceBus.Channels.SocketConnection.CloseState.Closed && !this.asyncReadPending)
				{
					this.DisposeReadEventArgs();
				}
				this.closeState = Microsoft.ServiceBus.Channels.SocketConnection.CloseState.Closed;
			}
		}

		private Exception ConvertObjectDisposedException(ObjectDisposedException originalException)
		{
			if (this.timeoutErrorString != null)
			{
				return new TimeoutException(this.timeoutErrorString, originalException);
			}
			if (!this.aborted)
			{
				return originalException;
			}
			return new CommunicationObjectAbortedException(Microsoft.ServiceBus.SR.GetString(Resources.SocketConnectionDisposed, new object[0]), originalException);
		}

		private Exception ConvertReceiveException(SocketException socketException, TimeSpan remainingTime)
		{
			return Microsoft.ServiceBus.Channels.SocketConnection.ConvertTransferException(socketException, this.receiveTimeout, socketException, this.aborted, this.timeoutErrorString, remainingTime);
		}

		private Exception ConvertSendException(SocketException socketException, TimeSpan remainingTime)
		{
			return Microsoft.ServiceBus.Channels.SocketConnection.ConvertTransferException(socketException, this.sendTimeout, socketException, this.aborted, this.timeoutErrorString, remainingTime);
		}

		internal static Exception ConvertTransferException(SocketException socketException, TimeSpan timeout, Exception originalException)
		{
			return Microsoft.ServiceBus.Channels.SocketConnection.ConvertTransferException(socketException, timeout, originalException, false, null, TimeSpan.MaxValue);
		}

		private static Exception ConvertTransferException(SocketException socketException, TimeSpan timeout, Exception originalException, bool aborted, string timeoutErrorString, TimeSpan remainingTime)
		{
			if (socketException.ErrorCode == 6)
			{
				return new CommunicationObjectAbortedException(socketException.Message, socketException);
			}
			if (timeoutErrorString != null)
			{
				return new TimeoutException(timeoutErrorString, originalException);
			}
			if (socketException.ErrorCode == 10053 && remainingTime <= TimeSpan.Zero)
			{
				string tcpConnectionTimedOut = Resources.TcpConnectionTimedOut;
				object[] objArray = new object[] { timeout };
				return new TimeoutException(Microsoft.ServiceBus.SR.GetString(tcpConnectionTimedOut, objArray), originalException);
			}
			if (socketException.ErrorCode == 10052 || socketException.ErrorCode == 10053 || socketException.ErrorCode == 10054)
			{
				if (aborted)
				{
					return new CommunicationObjectAbortedException(Microsoft.ServiceBus.SR.GetString(Resources.TcpLocalConnectionAborted, new object[0]), originalException);
				}
				string tcpConnectionResetError = Resources.TcpConnectionResetError;
				object[] objArray1 = new object[] { timeout };
				return new CommunicationException(Microsoft.ServiceBus.SR.GetString(tcpConnectionResetError, objArray1), originalException);
			}
			if (socketException.ErrorCode == 10060)
			{
				string str = Resources.TcpConnectionTimedOut;
				object[] objArray2 = new object[] { timeout };
				return new TimeoutException(Microsoft.ServiceBus.SR.GetString(str, objArray2), originalException);
			}
			if (aborted)
			{
				string tcpTransferError = Resources.TcpTransferError;
				object[] errorCode = new object[] { socketException.ErrorCode, socketException.Message };
				return new CommunicationObjectAbortedException(Microsoft.ServiceBus.SR.GetString(tcpTransferError, errorCode), originalException);
			}
			string tcpTransferError1 = Resources.TcpTransferError;
			object[] errorCode1 = new object[] { socketException.ErrorCode, socketException.Message };
			return new CommunicationException(Microsoft.ServiceBus.SR.GetString(tcpTransferError1, errorCode1), originalException);
		}

		private void DisposeReadEventArgs()
		{
			if (this.asyncReadEventArgs != null)
			{
				this.asyncReadEventArgs.Completed -= Microsoft.ServiceBus.Channels.SocketConnection.onReceiveAsyncCompleted;
				this.asyncReadEventArgs.Dispose();
				GC.SuppressFinalize(this);
			}
			this.TryReturnReadBuffer();
		}

		public int EndRead()
		{
			if (this.asyncReadException != null)
			{
				this.AbortRead();
				throw Fx.Exception.TraceException<Exception>(this.asyncReadException, this.ExceptionEventType, this.Activity);
			}
			lock (this.ThisLock)
			{
				if (!this.asyncReadPending)
				{
					throw Fx.AssertAndThrow("SocketConnection.EndRead called with no read pending.");
				}
				this.SetUserToken(this.asyncReadEventArgs, null);
				this.asyncReadPending = false;
				if (this.closeState == Microsoft.ServiceBus.Channels.SocketConnection.CloseState.Closed)
				{
					this.DisposeReadEventArgs();
				}
			}
			return this.asyncReadSize;
		}

		public void EndWrite(IAsyncResult result)
		{
			bool flag;
			try
			{
				this.CancelSendTimer();
				lock (this.ThisLock)
				{
					this.asyncWritePending = false;
					flag = this.closeState != Microsoft.ServiceBus.Channels.SocketConnection.CloseState.Closed;
				}
				if (flag)
				{
					this.socket.EndSend(result);
				}
			}
			catch (SocketException socketException1)
			{
				SocketException socketException = socketException1;
				throw Fx.Exception.TraceException<Exception>(this.ConvertSendException(socketException, TimeSpan.MaxValue), this.ExceptionEventType, this.Activity);
			}
			catch (ObjectDisposedException objectDisposedException1)
			{
				ObjectDisposedException objectDisposedException = objectDisposedException1;
				Exception exception = this.ConvertObjectDisposedException(objectDisposedException);
				if (!object.ReferenceEquals(exception, objectDisposedException))
				{
					throw Fx.Exception.TraceException<Exception>(exception, this.ExceptionEventType, this.Activity);
				}
				throw;
			}
		}

		private void EnsureReadEventArgs()
		{
			if (this.asyncReadEventArgs == null)
			{
				if (Microsoft.ServiceBus.Channels.SocketConnection.onReceiveAsyncCompleted == null)
				{
					Microsoft.ServiceBus.Channels.SocketConnection.onReceiveAsyncCompleted = new EventHandler<SocketAsyncEventArgs>(Microsoft.ServiceBus.Channels.SocketConnection.OnReceiveAsyncCompleted);
				}
				this.asyncReadEventArgs = new SocketAsyncEventArgs();
				this.asyncReadEventArgs.SetBuffer(this.readBuffer, 0, (int)this.readBuffer.Length);
				this.asyncReadEventArgs.Completed += Microsoft.ServiceBus.Channels.SocketConnection.onReceiveAsyncCompleted;
			}
		}

		~SocketConnection()
		{
			this.DisposeReadEventArgs();
		}

		private void FinishRead()
		{
			WaitCallback waitCallback = this.asyncReadCallback;
			object obj = this.asyncReadState;
			this.asyncReadState = null;
			this.asyncReadCallback = null;
			waitCallback(obj);
		}

		private void HandleReceiveAsyncCompleted()
		{
			if (this.asyncReadEventArgs.SocketError != SocketError.Success)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new SocketException((int)this.asyncReadEventArgs.SocketError));
			}
		}

		T Microsoft.ServiceBus.Channels.IConnection.GetProperty<T>()
		{
			return default(T);
		}

		private void OnReceiveAsync(object sender, SocketAsyncEventArgs eventArgs)
		{
			this.CancelReceiveTimer();
			try
			{
				this.HandleReceiveAsyncCompleted();
				this.asyncReadSize = eventArgs.BytesTransferred;
			}
			catch (SocketException socketException)
			{
				this.asyncReadException = this.ConvertReceiveException(socketException, TimeSpan.MaxValue);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				this.asyncReadException = exception;
			}
			this.FinishRead();
		}

		private static void OnReceiveAsyncCompleted(object sender, SocketAsyncEventArgs e)
		{
			((Microsoft.ServiceBus.Channels.SocketConnection)e.UserToken).OnReceiveAsync(sender, e);
		}

		private static void OnReceiveTimeout(object state)
		{
			Microsoft.ServiceBus.Channels.SocketConnection socketConnection = (Microsoft.ServiceBus.Channels.SocketConnection)state;
			string socketAbortedReceiveTimedOut = Resources.SocketAbortedReceiveTimedOut;
			object[] objArray = new object[] { socketConnection.receiveTimeout };
			socketConnection.Abort(Microsoft.ServiceBus.SR.GetString(socketAbortedReceiveTimedOut, objArray));
		}

		private static void OnSendTimeout(object state)
		{
			Microsoft.ServiceBus.Channels.SocketConnection socketConnection = (Microsoft.ServiceBus.Channels.SocketConnection)state;
			string socketAbortedSendTimedOut = Resources.SocketAbortedSendTimedOut;
			object[] objArray = new object[] { socketConnection.sendTimeout };
			socketConnection.Abort(TraceEventType.Warning, Microsoft.ServiceBus.SR.GetString(socketAbortedSendTimedOut, objArray));
		}

		public int Read(byte[] buffer, int offset, int size, TimeSpan timeout)
		{
			Microsoft.ServiceBus.Channels.ConnectionUtilities.ValidateBufferBounds(buffer, offset, size);
			this.ThrowIfClosed();
			return this.ReadCore(buffer, offset, size, timeout, false);
		}

		private int ReadCore(byte[] buffer, int offset, int size, TimeSpan timeout, bool closing)
		{
			int num;
			Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
			try
			{
				this.SetReadTimeout(timeoutHelper.RemainingTime(), true, closing);
				num = this.socket.Receive(buffer, offset, size, SocketFlags.None);
			}
			catch (SocketException socketException1)
			{
				SocketException socketException = socketException1;
				throw Fx.Exception.TraceException<Exception>(this.ConvertReceiveException(socketException, timeoutHelper.RemainingTime()), this.ExceptionEventType, this.Activity);
			}
			catch (ObjectDisposedException objectDisposedException1)
			{
				ObjectDisposedException objectDisposedException = objectDisposedException1;
				Exception exception = this.ConvertObjectDisposedException(objectDisposedException);
				if (!object.ReferenceEquals(exception, objectDisposedException))
				{
					throw Fx.Exception.TraceException<Exception>(exception, this.ExceptionEventType, this.Activity);
				}
				Fx.Exception.TraceException<ObjectDisposedException>(objectDisposedException, this.ExceptionEventType, this.Activity);
				throw;
			}
			return num;
		}

		private bool ReceiveAsync()
		{
			if (!PartialTrustHelpers.ShouldFlowSecurityContext && !ExecutionContext.IsFlowSuppressed())
			{
				return this.ReceiveAsyncNoFlow();
			}
			return this.socket.ReceiveAsync(this.asyncReadEventArgs);
		}

		[SecurityCritical]
		private bool ReceiveAsyncNoFlow()
		{
			bool flag;
			AsyncFlowControl asyncFlowControl = ExecutionContext.SuppressFlow();
			try
			{
				flag = this.socket.ReceiveAsync(this.asyncReadEventArgs);
			}
			finally
			{
				((IDisposable)asyncFlowControl).Dispose();
			}
			return flag;
		}

		private void SetImmediate(bool immediate)
		{
			if (immediate != this.noDelay)
			{
				lock (this.ThisLock)
				{
					this.ThrowIfClosed();
					this.socket.NoDelay = immediate;
				}
				this.noDelay = immediate;
			}
		}

		private void SetReadTimeout(TimeSpan timeout, bool synchronous, bool closing)
		{
			if (!synchronous)
			{
				this.receiveTimeout = timeout;
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
					throw exception.TraceException<TimeoutException>(new TimeoutException(Microsoft.ServiceBus.SR.GetString(tcpConnectionTimedOut, objArray)), this.ExceptionEventType, this.Activity);
				}
				if (Microsoft.ServiceBus.Channels.SocketConnection.UpdateTimeout(this.receiveTimeout, timeout))
				{
					lock (this.ThisLock)
					{
						if (!closing || this.closeState != Microsoft.ServiceBus.Channels.SocketConnection.CloseState.Closing)
						{
							this.ThrowIfClosed();
						}
						this.socket.ReceiveTimeout = Microsoft.ServiceBus.Common.TimeoutHelper.ToMilliseconds(timeout);
					}
					this.receiveTimeout = timeout;
					return;
				}
			}
		}

		private void SetUserToken(SocketAsyncEventArgs args, object userToken)
		{
			if (args != null)
			{
				args.UserToken = userToken;
			}
		}

		private void SetWriteTimeout(TimeSpan timeout, bool synchronous)
		{
			if (!synchronous)
			{
				this.sendTimeout = timeout;
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
					throw exception.TraceException<TimeoutException>(new TimeoutException(Microsoft.ServiceBus.SR.GetString(tcpConnectionTimedOut, objArray)), this.ExceptionEventType, this.Activity);
				}
				if (Microsoft.ServiceBus.Channels.SocketConnection.UpdateTimeout(this.sendTimeout, timeout))
				{
					lock (this.ThisLock)
					{
						this.ThrowIfClosed();
						this.socket.SendTimeout = Microsoft.ServiceBus.Common.TimeoutHelper.ToMilliseconds(timeout);
					}
					this.sendTimeout = timeout;
					return;
				}
			}
		}

		public void Shutdown(TimeSpan timeout)
		{
			lock (this.ThisLock)
			{
				if (!this.isShutdown)
				{
					this.isShutdown = true;
				}
				else
				{
					return;
				}
			}
			try
			{
				this.socket.Shutdown(SocketShutdown.Send);
			}
			catch (SocketException socketException1)
			{
				SocketException socketException = socketException1;
				throw Fx.Exception.TraceException<Exception>(this.ConvertSendException(socketException, TimeSpan.MaxValue), this.ExceptionEventType, this.Activity);
			}
			catch (ObjectDisposedException objectDisposedException1)
			{
				ObjectDisposedException objectDisposedException = objectDisposedException1;
				Exception exception = this.ConvertObjectDisposedException(objectDisposedException);
				if (!object.ReferenceEquals(exception, objectDisposedException))
				{
					throw Fx.Exception.TraceException<Exception>(exception, this.ExceptionEventType, this.Activity);
				}
				Fx.Exception.TraceException<ObjectDisposedException>(objectDisposedException, this.ExceptionEventType, this.Activity);
				throw;
			}
		}

		private void ThrowIfClosed()
		{
			if (this.closeState == Microsoft.ServiceBus.Channels.SocketConnection.CloseState.Closing || this.closeState == Microsoft.ServiceBus.Channels.SocketConnection.CloseState.Closed)
			{
				throw Fx.Exception.TraceException<Exception>(this.ConvertObjectDisposedException(new ObjectDisposedException(this.GetType().ToString(), Microsoft.ServiceBus.SR.GetString(Resources.SocketConnectionDisposed, new object[0]))), this.ExceptionEventType, this.Activity);
			}
		}

		private void TraceSocketInfo(Socket socket, TraceCode traceCode, string timeoutString)
		{
		}

		private void TryReturnReadBuffer()
		{
			if (this.readBuffer != null && !this.aborted)
			{
				this.readBuffer = null;
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
			Microsoft.ServiceBus.Channels.ConnectionUtilities.ValidateBufferBounds(buffer, offset, size);
			Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
			try
			{
				this.SetImmediate(immediate);
				int num = size;
				while (num > 0)
				{
					this.SetWriteTimeout(timeoutHelper.RemainingTime(), true);
					size = Math.Min(num, 16777216);
					this.socket.Send(buffer, offset, size, SocketFlags.None);
					num = num - size;
					offset = offset + size;
				}
			}
			catch (SocketException socketException1)
			{
				SocketException socketException = socketException1;
				throw Fx.Exception.TraceException<Exception>(this.ConvertSendException(socketException, timeoutHelper.RemainingTime()), this.ExceptionEventType, this.Activity);
			}
			catch (ObjectDisposedException objectDisposedException1)
			{
				ObjectDisposedException objectDisposedException = objectDisposedException1;
				Exception exception = this.ConvertObjectDisposedException(objectDisposedException);
				if (!object.ReferenceEquals(exception, objectDisposedException))
				{
					throw Fx.Exception.TraceException<Exception>(exception, this.ExceptionEventType, this.Activity);
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

		private enum CloseState
		{
			Open,
			Closing,
			Closed
		}
	}
}