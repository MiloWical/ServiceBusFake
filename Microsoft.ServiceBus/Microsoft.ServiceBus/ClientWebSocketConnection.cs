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
	internal class ClientWebSocketConnection : Microsoft.ServiceBus.Channels.IConnection
	{
		private readonly ServiceBusClientWebSocket webSocket;

		private readonly AsyncCallback onRead;

		private WaitCallback readCallback;

		private object readObject;

		private int bytesRead;

		private IAsyncResult readResult;

		private int asyncReadBufferOffset;

		private int remainingBytes;

		private int asyncReadPending;

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
				return JustDecompileGenerated_get_AsyncReadBuffer();
			}
			set
			{
				JustDecompileGenerated_set_AsyncReadBuffer(value);
			}
		}

		private byte[] JustDecompileGenerated_AsyncReadBuffer_k__BackingField;

		public byte[] JustDecompileGenerated_get_AsyncReadBuffer()
		{
			return this.JustDecompileGenerated_AsyncReadBuffer_k__BackingField;
		}

		private void JustDecompileGenerated_set_AsyncReadBuffer(byte[] value)
		{
			this.JustDecompileGenerated_AsyncReadBuffer_k__BackingField = value;
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

		private int JustDecompileGenerated_AsyncReadBufferSize_k__BackingField;

		public int JustDecompileGenerated_get_AsyncReadBufferSize()
		{
			return this.JustDecompileGenerated_AsyncReadBufferSize_k__BackingField;
		}

		private void JustDecompileGenerated_set_AsyncReadBufferSize(int value)
		{
			this.JustDecompileGenerated_AsyncReadBufferSize_k__BackingField = value;
		}

		public TraceEventType ExceptionEventType
		{
			get;
			set;
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

		private IPEndPoint JustDecompileGenerated_RemoteIPEndPoint_k__BackingField;

		public IPEndPoint JustDecompileGenerated_get_RemoteIPEndPoint()
		{
			return this.JustDecompileGenerated_RemoteIPEndPoint_k__BackingField;
		}

		public void JustDecompileGenerated_set_RemoteIPEndPoint(IPEndPoint value)
		{
			this.JustDecompileGenerated_RemoteIPEndPoint_k__BackingField = value;
		}

		public System.Uri Uri
		{
			get;
			private set;
		}

		public ClientWebSocketConnection(ServiceBusClientWebSocket webSocket, int asyncReadBufferSize, System.Uri uri, EventTraceActivity activity)
		{
			this.Activity = activity;
			this.AsyncReadBufferSize = asyncReadBufferSize;
			this.AsyncReadBuffer = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.Utility.AllocateByteArray(asyncReadBufferSize);
			this.webSocket = webSocket;
			this.Uri = uri;
			this.Activity = activity;
			this.onRead = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(this.OnRead));
		}

		public void Abort()
		{
			MessagingClientEtwProvider.Provider.WebSocketConnectionAborted(this.Activity, this.Uri.AbsoluteUri);
			if (this.webSocket.State == ServiceBusClientWebSocket.WebSocketState.Open)
			{
				try
				{
					this.webSocket.EndClose(this.webSocket.BeginClose(null, null));
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					Fx.Exception.TraceHandled(exception, "ClientWebSocketConnection.Abort", this.Activity);
				}
			}
		}

		public AsyncReadResult BeginRead(int offset, int size, TimeSpan timeout, WaitCallback callback, object state)
		{
			this.ThrowIfNotOpen();
			Microsoft.ServiceBus.Channels.ConnectionUtilities.ValidateBufferBounds(this.AsyncReadBufferSize, offset, size);
			this.readCallback = callback;
			this.readObject = state;
			AsyncReadResult asyncReadResult = AsyncReadResult.Completed;
			bool flag = false;
			try
			{
				try
				{
					if (Interlocked.CompareExchange(ref this.asyncReadPending, 1, 0) != 0)
					{
						throw Fx.Exception.AsError(new InvalidOperationException(Resources.ClientWebSocketConnectionReadPending), this.Activity);
					}
					IAsyncResult asyncResult = this.webSocket.BeginReceive(this.AsyncReadBuffer, offset, size, timeout, this.onRead, this);
					if (asyncResult.CompletedSynchronously)
					{
						this.readCallback = null;
						this.readObject = null;
						this.bytesRead = this.webSocket.EndReceive(asyncResult);
					}
					else
					{
						asyncReadResult = AsyncReadResult.Queued;
					}
					flag = true;
				}
				catch (IOException oException1)
				{
					IOException oException = oException1;
					throw Fx.Exception.AsError(ClientWebSocketConnection.ConvertIOException(oException), this.Activity);
				}
				catch (ObjectDisposedException objectDisposedException1)
				{
					ObjectDisposedException objectDisposedException = objectDisposedException1;
					if (this.webSocket.State != ServiceBusClientWebSocket.WebSocketState.Faulted)
					{
						throw Fx.Exception.AsError(new CommunicationObjectAbortedException(objectDisposedException.Message, objectDisposedException), this.Activity);
					}
					throw Fx.Exception.AsError(new CommunicationObjectFaultedException(objectDisposedException.Message, objectDisposedException), this.Activity);
				}
			}
			finally
			{
				if (!flag || asyncReadResult == AsyncReadResult.Completed)
				{
					Interlocked.Exchange(ref this.asyncReadPending, 0);
				}
			}
			return asyncReadResult;
		}

		public IAsyncResult BeginWrite(byte[] buffer, int offset, int size, bool immediate, TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult asyncResult;
			this.ThrowIfNotOpen();
			try
			{
				asyncResult = this.webSocket.BeginSend(buffer, offset, size, ServiceBusClientWebSocket.WebSocketMessageType.Binary, timeout, callback, state);
			}
			catch (IOException oException1)
			{
				IOException oException = oException1;
				throw Fx.Exception.AsError(ClientWebSocketConnection.ConvertIOException(oException), this.Activity);
			}
			catch (ObjectDisposedException objectDisposedException1)
			{
				ObjectDisposedException objectDisposedException = objectDisposedException1;
				if (this.webSocket.State != ServiceBusClientWebSocket.WebSocketState.Faulted)
				{
					throw Fx.Exception.AsError(new CommunicationObjectAbortedException(objectDisposedException.Message, objectDisposedException), this.Activity);
				}
				throw Fx.Exception.AsError(new CommunicationObjectFaultedException(objectDisposedException.Message, objectDisposedException), this.Activity);
			}
			return asyncResult;
		}

		public void Close(TimeSpan timeout)
		{
			MessagingClientEtwProvider.Provider.WebSocketConnectionClosed(this.Activity, this.Uri.AbsoluteUri);
			if (this.webSocket.State == ServiceBusClientWebSocket.WebSocketState.Open)
			{
				try
				{
					this.webSocket.EndClose(this.webSocket.BeginClose(null, null));
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					Fx.Exception.TraceHandled(exception, "ClientWebSocketConnection.Close", this.Activity);
				}
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

		public int EndRead()
		{
			if (Thread.VolatileRead(ref this.asyncReadPending) != 0)
			{
				throw Fx.Exception.AsError(new InvalidOperationException(Resources.ClientWebSocketConnectionPrematureEndRead), this.Activity);
			}
			IAsyncResult asyncResult = this.readResult;
			if (asyncResult != null)
			{
				this.readResult = null;
				this.readCallback = null;
				this.readObject = null;
				try
				{
					this.bytesRead = this.webSocket.EndReceive(asyncResult);
				}
				catch (IOException oException1)
				{
					IOException oException = oException1;
					throw Fx.Exception.AsError(ClientWebSocketConnection.ConvertIOException(oException), this.Activity);
				}
				catch (ObjectDisposedException objectDisposedException1)
				{
					ObjectDisposedException objectDisposedException = objectDisposedException1;
					if (this.webSocket.State != ServiceBusClientWebSocket.WebSocketState.Faulted)
					{
						throw Fx.Exception.AsError(new CommunicationObjectAbortedException(objectDisposedException.Message, objectDisposedException), this.Activity);
					}
					throw Fx.Exception.AsError(new CommunicationObjectFaultedException(objectDisposedException.Message, objectDisposedException), this.Activity);
				}
			}
			return this.bytesRead;
		}

		public void EndWrite(IAsyncResult result)
		{
			try
			{
				this.webSocket.EndSend(result);
			}
			catch (IOException oException1)
			{
				IOException oException = oException1;
				throw Fx.Exception.AsError(ClientWebSocketConnection.ConvertIOException(oException), this.Activity);
			}
			catch (ObjectDisposedException objectDisposedException1)
			{
				ObjectDisposedException objectDisposedException = objectDisposedException1;
				if (this.webSocket.State != ServiceBusClientWebSocket.WebSocketState.Faulted)
				{
					throw Fx.Exception.AsError(new CommunicationObjectAbortedException(objectDisposedException.Message, objectDisposedException), this.Activity);
				}
				throw Fx.Exception.AsError(new CommunicationObjectFaultedException(objectDisposedException.Message, objectDisposedException), this.Activity);
			}
		}

		public virtual T GetProperty<T>()
		{
			if (typeof(T) == typeof(System.Uri))
			{
				return (T)this.Uri;
			}
			return default(T);
		}

		private void OnRead(IAsyncResult result)
		{
			if (result.CompletedSynchronously)
			{
				return;
			}
			Fx.AssertAndThrow(this.readResult == null, "ClientWebSocketConnection.OnRead called twice, readResult is null");
			this.readResult = result;
			if (Interlocked.CompareExchange(ref this.asyncReadPending, 0, 1) != 1)
			{
				throw Fx.AssertAndThrow("OnRead method should not get called when there is no read operation pending.");
			}
			this.readCallback(this.readObject);
		}

		public int Read(byte[] buffer, int offset, int size, TimeSpan timeout)
		{
			int num;
			this.ThrowIfNotOpen();
			Microsoft.ServiceBus.Channels.ConnectionUtilities.ValidateBufferBounds(buffer, offset, size);
			try
			{
				if (this.asyncReadBufferOffset <= 0)
				{
					IAsyncResult asyncResult = this.webSocket.BeginReceive(this.AsyncReadBuffer, 0, size, timeout, null, null);
					this.bytesRead = this.webSocket.EndReceive(asyncResult);
					num = this.TransferData(this.bytesRead, buffer, offset, size);
				}
				else
				{
					Fx.AssertAndThrow(this.remainingBytes > 0, "Must have data in buffer to transfer");
					num = this.TransferData(this.remainingBytes, buffer, offset, size);
				}
			}
			catch (IOException oException1)
			{
				IOException oException = oException1;
				throw Fx.Exception.AsError(ClientWebSocketConnection.ConvertIOException(oException), this.Activity);
			}
			catch (ObjectDisposedException objectDisposedException1)
			{
				ObjectDisposedException objectDisposedException = objectDisposedException1;
				if (this.webSocket.State != ServiceBusClientWebSocket.WebSocketState.Faulted)
				{
					throw Fx.Exception.AsError(new CommunicationObjectAbortedException(objectDisposedException.Message, objectDisposedException), this.Activity);
				}
				throw Fx.Exception.AsError(new CommunicationObjectFaultedException(objectDisposedException.Message, objectDisposedException), this.Activity);
			}
			return num;
		}

		public void Shutdown(TimeSpan timeout)
		{
			throw Fx.Exception.AsError(new NotImplementedException("ClientWebSocketConnection does not support Shutdown Interface"), null);
		}

		private void ThrowIfNotOpen()
		{
			if (this.webSocket.State != ServiceBusClientWebSocket.WebSocketState.Open)
			{
				if (this.webSocket.State != ServiceBusClientWebSocket.WebSocketState.Aborted)
				{
					if (this.webSocket.State != ServiceBusClientWebSocket.WebSocketState.Closed)
					{
						throw Fx.Exception.AsWarning(new CommunicationException(Resources.CommunicationObjectCannotBeUsed), this.Activity);
					}
					throw Fx.Exception.AsWarning(new ObjectDisposedException(Microsoft.ServiceBus.SR.GetString(Resources.ClientWebSocketConnectionClosed, new object[0])), this.Activity);
				}
				throw Fx.Exception.AsWarning(new CommunicationObjectAbortedException(), this.Activity);
			}
		}

		private int TransferData(int numBytesRead, byte[] buffer, int offset, int size)
		{
			if (numBytesRead <= size)
			{
				Buffer.BlockCopy(this.AsyncReadBuffer, this.asyncReadBufferOffset, buffer, offset, numBytesRead);
				this.asyncReadBufferOffset = 0;
				this.remainingBytes = 0;
				return numBytesRead;
			}
			Buffer.BlockCopy(this.AsyncReadBuffer, this.asyncReadBufferOffset, buffer, offset, size);
			ClientWebSocketConnection clientWebSocketConnection = this;
			clientWebSocketConnection.asyncReadBufferOffset = clientWebSocketConnection.asyncReadBufferOffset + size;
			this.remainingBytes = numBytesRead - size;
			return size;
		}

		public bool Validate(System.Uri uri)
		{
			return true;
		}

		public void Write(byte[] buffer, int offset, int size, bool immediate, TimeSpan timeout)
		{
			this.ThrowIfNotOpen();
			try
			{
				IAsyncResult asyncResult = this.webSocket.BeginSend(buffer, offset, size, ServiceBusClientWebSocket.WebSocketMessageType.Binary, timeout, null, null);
				this.webSocket.EndSend(asyncResult);
			}
			catch (IOException oException1)
			{
				IOException oException = oException1;
				throw Fx.Exception.AsError(ClientWebSocketConnection.ConvertIOException(oException), this.Activity);
			}
			catch (ObjectDisposedException objectDisposedException1)
			{
				ObjectDisposedException objectDisposedException = objectDisposedException1;
				if (this.webSocket.State != ServiceBusClientWebSocket.WebSocketState.Faulted)
				{
					throw Fx.Exception.AsError(new CommunicationObjectAbortedException(objectDisposedException.Message, objectDisposedException), this.Activity);
				}
				throw Fx.Exception.AsError(new CommunicationObjectFaultedException(objectDisposedException.Message, objectDisposedException), this.Activity);
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
	}
}