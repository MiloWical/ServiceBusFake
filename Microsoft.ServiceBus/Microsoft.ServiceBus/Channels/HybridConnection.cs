using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;

namespace Microsoft.ServiceBus.Channels
{
	internal class HybridConnection : Microsoft.ServiceBus.Channels.IConnection, IHybridConnectionStatus, IDisposable
	{
		private Microsoft.ServiceBus.Channels.IConnection readConnection;

		private Microsoft.ServiceBus.Channels.IConnection writeConnection;

		private HybridConnection.CloseState closeState;

		private bool isShutdown;

		private bool aborted;

		private TraceEventType exceptionEventType;

		private int asyncReadResultSize;

		private byte[] readBuffer;

		private int asyncReadBufferSize;

		private object asyncReadState;

		private WaitCallback asyncReadCallback;

		private Exception asyncReadException;

		private bool asyncReadPending;

		private string timeoutErrorString;

		private int readBufferOffset;

		private int readBufferSize;

		private Microsoft.ServiceBus.Common.TimeoutHelper readTimeout;

		private HybridConnectionRole role;

		private Queue<Microsoft.ServiceBus.Channels.IConnection> readConnectionQueue;

		private Queue<Microsoft.ServiceBus.Channels.IConnection> writeConnectionQueue;

		private IDisposable directConnect;

		private HybridConnectionState connectionState;

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
				return this.asyncReadBufferSize;
			}
		}

		public HybridConnectionState ConnectionState
		{
			get
			{
				return this.connectionState;
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

		public IPEndPoint RemoteIPEndPoint
		{
			get
			{
				return this.readConnection.RemoteIPEndPoint;
			}
		}

		private object ThisLock
		{
			get
			{
				return this;
			}
		}

		public HybridConnection(HybridConnectionRole role, Microsoft.ServiceBus.Channels.IConnection connection, IDisposable directConnect, int asyncReadBufferSize)
		{
			if (connection == null)
			{
				throw Fx.Exception.ArgumentNull("connection");
			}
			this.connectionState = HybridConnectionState.Relayed;
			this.closeState = HybridConnection.CloseState.Open;
			this.exceptionEventType = TraceEventType.Error;
			Microsoft.ServiceBus.Channels.IConnection connection1 = connection;
			Microsoft.ServiceBus.Channels.IConnection connection2 = connection1;
			this.writeConnection = connection1;
			this.readConnection = connection2;
			this.asyncReadBufferSize = asyncReadBufferSize;
			this.Activity = connection.Activity;
			this.role = role;
			this.readConnectionQueue = new Queue<Microsoft.ServiceBus.Channels.IConnection>();
			this.writeConnectionQueue = new Queue<Microsoft.ServiceBus.Channels.IConnection>();
			this.directConnect = directConnect;
			if (connection.AsyncReadBufferSize != asyncReadBufferSize)
			{
				throw new InvalidOperationException(SRClient.InvalidBufferSize);
			}
		}

		public void Abort()
		{
			this.Abort(null);
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
			Microsoft.ServiceBus.Channels.IConnection[] array;
			Microsoft.ServiceBus.Channels.IConnection[] connectionArray;
			lock (this.ThisLock)
			{
				if (this.closeState != HybridConnection.CloseState.Closed)
				{
					this.timeoutErrorString = timeoutErrorString;
					this.aborted = true;
					this.closeState = HybridConnection.CloseState.Closed;
					array = this.readConnectionQueue.ToArray();
					this.readConnectionQueue.Clear();
					connectionArray = this.writeConnectionQueue.ToArray();
					this.writeConnectionQueue.Clear();
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
			if (this.readConnection != this.writeConnection)
			{
				this.writeConnection.Abort();
			}
			this.readConnection.Abort();
			HybridConnection.AbortConnections(array);
			HybridConnection.AbortConnections(connectionArray);
			if (this.directConnect != null)
			{
				this.directConnect.Dispose();
			}
			EventHandler eventHandler = this.Closed;
			if (eventHandler != null)
			{
				eventHandler(this, EventArgs.Empty);
			}
		}

		private void AbortAsyncRead()
		{
			lock (this.ThisLock)
			{
				if (this.asyncReadPending && this.closeState != HybridConnection.CloseState.Closed)
				{
					this.asyncReadPending = false;
				}
			}
		}

		private static void AbortConnections(Microsoft.ServiceBus.Channels.IConnection[] connections)
		{
			if (connections != null)
			{
				for (int i = 0; i < (int)connections.Length; i++)
				{
					connections[i].Abort();
				}
			}
		}

		private void AsyncReadCompleted(object state)
		{
			int num = 0;
			try
			{
				num = this.readConnection.EndRead();
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
			this.asyncReadResultSize = num;
			if (this.asyncReadException != null || num != 0 || this.readConnectionQueue.Count <= 0)
			{
				Buffer.BlockCopy(this.readConnection.AsyncReadBuffer, this.readBufferOffset, this.AsyncReadBuffer, this.readBufferOffset, this.asyncReadResultSize);
				this.FinishAsyncRead();
				return;
			}
			if (this.role == HybridConnectionRole.Initiator)
			{
				try
				{
					this.readConnection.Close(this.readTimeout.RemainingTime());
				}
				catch (CommunicationException communicationException)
				{
					this.readConnection.Abort();
				}
				catch (TimeoutException timeoutException)
				{
					this.readConnection.Abort();
				}
			}
			this.readConnection = this.readConnectionQueue.Dequeue();
			if (this.role == HybridConnectionRole.Listener)
			{
				this.writeConnectionQueue.Enqueue(this.readConnection);
			}
			try
			{
				this.readConnection.BeginRead(this.readBufferOffset, this.readBufferSize, this.readTimeout.RemainingTime(), new WaitCallback(this.AsyncReadCompleted), null);
			}
			catch (Exception exception3)
			{
				Exception exception2 = exception3;
				if (Fx.IsFatal(exception2))
				{
					throw;
				}
				if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceError)
				{
					Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.TraceHandledException(exception2, TraceEventType.Error);
				}
			}
		}

		public AsyncReadResult BeginRead(int offset, int size, TimeSpan timeout, WaitCallback callback, object state)
		{
			Microsoft.ServiceBus.Channels.ConnectionUtilities.ValidateBufferBounds(this.AsyncReadBufferSize, offset, size);
			lock (this.ThisLock)
			{
				this.ThrowIfClosed();
				this.asyncReadState = state;
				this.asyncReadCallback = callback;
				this.asyncReadPending = true;
			}
			bool flag = true;
			try
			{
				try
				{
					this.readBufferOffset = offset;
					this.readBufferSize = size;
					this.readTimeout = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
					this.readConnection.BeginRead(this.readBufferOffset, this.readBufferSize, this.readTimeout.RemainingTime(), new WaitCallback(this.AsyncReadCompleted), null);
					flag = false;
				}
				catch (ObjectDisposedException objectDisposedException1)
				{
					ObjectDisposedException objectDisposedException = objectDisposedException1;
					Exception exception = this.ConvertObjectDisposedException(objectDisposedException);
					if (!object.ReferenceEquals(exception, objectDisposedException))
					{
						throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelper(exception, this.ExceptionEventType);
					}
					throw;
				}
			}
			finally
			{
				if (flag)
				{
					this.AbortAsyncRead();
				}
			}
			return AsyncReadResult.Queued;
		}

		public IAsyncResult BeginWrite(byte[] buffer, int offset, int size, bool immediate, TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult asyncResult;
			Microsoft.ServiceBus.Channels.ConnectionUtilities.ValidateBufferBounds(buffer, offset, size);
			Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
			try
			{
				lock (this.ThisLock)
				{
					if (this.writeConnectionQueue.Count > 0)
					{
						this.writeConnection.Shutdown(timeoutHelper.RemainingTime());
						this.writeConnection = this.writeConnectionQueue.Dequeue();
					}
				}
				IAsyncResult asyncResult1 = this.writeConnection.BeginWrite(buffer, offset, size, immediate, timeoutHelper.RemainingTime(), callback, state);
				asyncResult = asyncResult1;
			}
			catch (ObjectDisposedException objectDisposedException1)
			{
				ObjectDisposedException objectDisposedException = objectDisposedException1;
				Exception exception = this.ConvertObjectDisposedException(objectDisposedException);
				if (!object.ReferenceEquals(exception, objectDisposedException))
				{
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelper(exception, this.ExceptionEventType);
				}
				throw;
			}
			return asyncResult;
		}

		public void Close(TimeSpan timeout)
		{
			Microsoft.ServiceBus.Channels.IConnection[] array;
			Microsoft.ServiceBus.Channels.IConnection[] connectionArray;
			lock (this.ThisLock)
			{
				if (this.closeState == HybridConnection.CloseState.Closing || this.closeState == HybridConnection.CloseState.Closed)
				{
					return;
				}
				else
				{
					this.closeState = HybridConnection.CloseState.Closing;
					array = this.readConnectionQueue.ToArray();
					this.readConnectionQueue.Clear();
					connectionArray = this.writeConnectionQueue.ToArray();
					this.writeConnectionQueue.Clear();
				}
			}
			Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
			this.Shutdown(timeoutHelper.RemainingTime());
			byte[] numArray = new byte[1];
			TimeSpan timeSpan = timeoutHelper.RemainingTime();
			try
			{
				if (this.ReadCore(numArray, 0, 1, timeSpan, true) > 0)
				{
					ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
					string socketCloseReadReceivedData = Resources.SocketCloseReadReceivedData;
					object[] remoteIPEndPoint = new object[] { this.readConnection.RemoteIPEndPoint };
					throw exceptionUtility.ThrowHelper(new CommunicationException(Microsoft.ServiceBus.SR.GetString(socketCloseReadReceivedData, remoteIPEndPoint)), this.ExceptionEventType);
				}
			}
			catch (TimeoutException timeoutException1)
			{
				TimeoutException timeoutException = timeoutException1;
				ExceptionUtility exceptionUtility1 = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
				string socketCloseReadTimeout = Resources.SocketCloseReadTimeout;
				object[] objArray = new object[] { this.readConnection.RemoteIPEndPoint, timeSpan };
				throw exceptionUtility1.ThrowHelper(new TimeoutException(Microsoft.ServiceBus.SR.GetString(socketCloseReadTimeout, objArray), timeoutException), this.ExceptionEventType);
			}
			if (timeoutHelper.RemainingTime() <= TimeSpan.Zero && Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceWarning)
			{
				TraceUtility.TraceEvent(TraceEventType.Warning, TraceCode.SocketConnectionAbortClose, this);
			}
			this.readConnection.Close(timeoutHelper.RemainingTime());
			HybridConnection.CloseConnections(array, ref timeoutHelper);
			HybridConnection.CloseConnections(connectionArray, ref timeoutHelper);
			if (this.directConnect != null)
			{
				this.directConnect.Dispose();
			}
			lock (this.ThisLock)
			{
				this.closeState = HybridConnection.CloseState.Closed;
			}
			EventHandler eventHandler = this.Closed;
			if (eventHandler != null)
			{
				eventHandler(this, EventArgs.Empty);
			}
		}

		private static void CloseConnections(Microsoft.ServiceBus.Channels.IConnection[] connections, ref Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper)
		{
			if (connections != null)
			{
				for (int i = 0; i < (int)connections.Length; i++)
				{
					connections[i].Close(timeoutHelper.RemainingTime());
				}
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

		public void Dispose()
		{
			this.Abort();
			GC.SuppressFinalize(this);
		}

		public int EndRead()
		{
			if (this.asyncReadException != null)
			{
				this.AbortAsyncRead();
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelper(this.asyncReadException, this.ExceptionEventType);
			}
			lock (this.ThisLock)
			{
				if (!this.asyncReadPending)
				{
					throw Fx.AssertAndThrow("HybridConnection.EndRead called with no read pending.");
				}
				this.asyncReadPending = false;
			}
			return this.asyncReadResultSize;
		}

		public void EndWrite(IAsyncResult result)
		{
			bool flag;
			try
			{
				lock (this.ThisLock)
				{
					flag = this.closeState != HybridConnection.CloseState.Closed;
				}
				if (flag)
				{
					this.writeConnection.EndWrite(result);
				}
			}
			catch (ObjectDisposedException objectDisposedException1)
			{
				ObjectDisposedException objectDisposedException = objectDisposedException1;
				Exception exception = this.ConvertObjectDisposedException(objectDisposedException);
				if (!object.ReferenceEquals(exception, objectDisposedException))
				{
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelper(exception, this.ExceptionEventType);
				}
				throw;
			}
		}

		public void EnqueueConnection(Microsoft.ServiceBus.Channels.IConnection connection)
		{
			lock (this.ThisLock)
			{
				if (this.closeState != HybridConnection.CloseState.Closed)
				{
					if (connection.AsyncReadBufferSize != this.asyncReadBufferSize)
					{
						throw new InvalidOperationException(SRClient.InvalidBufferSize);
					}
					this.readConnectionQueue.Enqueue(connection);
					if (this.role == HybridConnectionRole.Initiator)
					{
						this.writeConnectionQueue.Enqueue(connection);
					}
					this.connectionState = HybridConnectionState.Direct;
					this.OnConnectionStateChanged();
				}
				else
				{
					connection.Close(TimeSpan.FromSeconds(1));
				}
			}
		}

		private void FinishAsyncRead()
		{
			WaitCallback waitCallback = this.asyncReadCallback;
			object obj = this.asyncReadState;
			this.asyncReadState = null;
			this.asyncReadCallback = null;
			waitCallback(obj);
		}

		T Microsoft.ServiceBus.Channels.IConnection.GetProperty<T>()
		{
			if (typeof(T) == typeof(IHybridConnectionStatus))
			{
				return (T)this;
			}
			return default(T);
		}

		private void OnConnectionStateChanged()
		{
			if (this.ConnectionStateChanged != null)
			{
				try
				{
					this.ConnectionStateChanged(this, new HybridConnectionStateChangedArgs(this.connectionState));
				}
				catch (Exception exception)
				{
					if (Fx.IsFatal(exception))
					{
						throw;
					}
				}
			}
		}

		public int Read(byte[] buffer, int offset, int size, TimeSpan timeout)
		{
			Microsoft.ServiceBus.Channels.ConnectionUtilities.ValidateBufferBounds(buffer, offset, size);
			this.ThrowIfClosed();
			return this.ReadCore(buffer, offset, size, timeout, false);
		}

		private int ReadCore(byte[] buffer, int offset, int size, TimeSpan timeout, bool closing)
		{
			int num = 0;
			Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
			try
			{
				while (true)
				{
					try
					{
						num = this.readConnection.Read(buffer, offset, size, timeoutHelper.RemainingTime());
					}
					catch (CommunicationException communicationException)
					{
						if (this.readConnectionQueue.Count == 0)
						{
							throw;
						}
					}
					if (num != 0 || this.readConnectionQueue.Count <= 0)
					{
						break;
					}
					if (this.role == HybridConnectionRole.Initiator)
					{
						this.readConnection.Close(timeoutHelper.RemainingTime());
					}
					this.readConnection = this.readConnectionQueue.Dequeue();
					if (this.role == HybridConnectionRole.Listener)
					{
						this.writeConnectionQueue.Enqueue(this.readConnection);
					}
				}
			}
			catch (ObjectDisposedException objectDisposedException1)
			{
				ObjectDisposedException objectDisposedException = objectDisposedException1;
				Exception exception = this.ConvertObjectDisposedException(objectDisposedException);
				if (!object.ReferenceEquals(exception, objectDisposedException))
				{
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelper(exception, this.ExceptionEventType);
				}
				throw;
			}
			return num;
		}

		public void Shutdown(TimeSpan timeout)
		{
			Microsoft.ServiceBus.Channels.IConnection[] array;
			Microsoft.ServiceBus.Channels.IConnection[] connectionArray;
			Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
			lock (this.ThisLock)
			{
				if (!this.isShutdown)
				{
					this.isShutdown = true;
					array = this.readConnectionQueue.ToArray();
					this.readConnectionQueue.Clear();
					connectionArray = this.writeConnectionQueue.ToArray();
					this.writeConnectionQueue.Clear();
				}
				else
				{
					return;
				}
			}
			try
			{
				this.writeConnection.Shutdown(timeoutHelper.RemainingTime());
				HybridConnection.CloseConnections(array, ref timeoutHelper);
				HybridConnection.CloseConnections(connectionArray, ref timeoutHelper);
				if (this.directConnect != null)
				{
					this.directConnect.Dispose();
				}
			}
			catch (ObjectDisposedException objectDisposedException1)
			{
				ObjectDisposedException objectDisposedException = objectDisposedException1;
				Exception exception = this.ConvertObjectDisposedException(objectDisposedException);
				if (!object.ReferenceEquals(exception, objectDisposedException))
				{
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelper(exception, this.ExceptionEventType);
				}
				throw;
			}
		}

		private void ThrowIfClosed()
		{
			if (this.closeState == HybridConnection.CloseState.Closing || this.closeState == HybridConnection.CloseState.Closed)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelper(this.ConvertObjectDisposedException(new ObjectDisposedException(this.GetType().ToString(), Microsoft.ServiceBus.SR.GetString(Resources.SocketConnectionDisposed, new object[0]))), this.ExceptionEventType);
			}
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
				lock (this.ThisLock)
				{
					if (this.writeConnectionQueue.Count > 0)
					{
						this.writeConnection.Shutdown(timeoutHelper.RemainingTime());
						this.writeConnection = this.writeConnectionQueue.Dequeue();
					}
				}
				int num = size;
				while (num > 0)
				{
					size = Math.Min(num, 65536);
					this.writeConnection.Write(buffer, offset, size, immediate, timeoutHelper.RemainingTime());
					num = num - size;
					offset = offset + size;
					timeout = timeoutHelper.RemainingTime();
				}
			}
			catch (ObjectDisposedException objectDisposedException1)
			{
				ObjectDisposedException objectDisposedException = objectDisposedException1;
				Exception exception = this.ConvertObjectDisposedException(objectDisposedException);
				if (!object.ReferenceEquals(exception, objectDisposedException))
				{
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelper(exception, this.ExceptionEventType);
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

		public event EventHandler Closed;

		public event EventHandler<HybridConnectionStateChangedArgs> ConnectionStateChanged;

		private enum CloseState
		{
			Open,
			Closing,
			Closed
		}
	}
}