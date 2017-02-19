using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using System;
using System.Diagnostics;
using System.ServiceModel;
using System.Threading;

namespace Microsoft.ServiceBus.Channels
{
	internal sealed class ConnectionModeReader : InitialServerConnectionReader
	{
		private Exception readException;

		private ServerModeDecoder decoder;

		private byte[] buffer;

		private int offset;

		private int size;

		private ConnectionModeCallback callback;

		private static WaitCallback readCallback;

		private Microsoft.ServiceBus.Common.TimeoutHelper receiveTimeoutHelper;

		public int BufferOffset
		{
			get
			{
				return this.offset;
			}
		}

		public int BufferSize
		{
			get
			{
				return this.size;
			}
		}

		public long StreamPosition
		{
			get
			{
				return this.decoder.StreamPosition;
			}
		}

		public ConnectionModeReader(IConnection connection, ConnectionModeCallback callback, ConnectionClosedCallback closedCallback) : base(connection, closedCallback)
		{
			this.callback = callback;
		}

		private void Complete(Exception e)
		{
			this.readException = e;
			this.Complete();
		}

		private void Complete()
		{
			this.callback(this);
		}

		private bool ContinueReading()
		{
			int num;
			string str;
			while (true)
			{
				if (this.size == 0)
				{
					if (ConnectionModeReader.readCallback == null)
					{
						ConnectionModeReader.readCallback = new WaitCallback(ConnectionModeReader.ReadCallback);
					}
					if (base.Connection.BeginRead(0, base.Connection.AsyncReadBufferSize, this.GetRemainingTimeout(), ConnectionModeReader.readCallback, this) == AsyncReadResult.Queued)
					{
						break;
					}
					if (!this.GetReadResult())
					{
						return false;
					}
				}
				do
				{
					try
					{
						num = this.decoder.Decode(this.buffer, this.offset, this.size);
					}
					catch (CommunicationException communicationException)
					{
						if (FramingEncodingString.TryGetFaultString(communicationException, out str))
						{
							byte[] numArray = new byte[128];
							InitialServerConnectionReader.SendFault(base.Connection, str, numArray, this.GetRemainingTimeout(), base.MaxViaSize + base.MaxContentTypeSize);
							base.Close(this.GetRemainingTimeout());
						}
						throw;
					}
					if (num > 0)
					{
						ConnectionModeReader connectionModeReader = this;
						connectionModeReader.offset = connectionModeReader.offset + num;
						ConnectionModeReader connectionModeReader1 = this;
						connectionModeReader1.size = connectionModeReader1.size - num;
					}
					if (this.decoder.CurrentState != ServerModeDecoder.State.Done)
					{
						continue;
					}
					return true;
				}
				while (this.size != 0);
			}
			return false;
		}

		public FramingMode GetConnectionMode()
		{
			if (this.readException != null)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelper(this.readException, base.Connection.ExceptionEventType);
			}
			return this.decoder.Mode;
		}

		private bool GetReadResult()
		{
			this.offset = 0;
			this.size = base.Connection.EndRead();
			if (this.size == 0)
			{
				if (this.decoder.StreamPosition != (long)0)
				{
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(this.decoder.CreatePrematureEOFException());
				}
				base.Close(this.GetRemainingTimeout());
				return false;
			}
			base.Connection.ExceptionEventType = TraceEventType.Warning;
			if (this.buffer == null)
			{
				this.buffer = base.Connection.AsyncReadBuffer;
			}
			return true;
		}

		public TimeSpan GetRemainingTimeout()
		{
			return this.receiveTimeoutHelper.RemainingTime();
		}

		private static void ReadCallback(object state)
		{
			ConnectionModeReader connectionModeReader = (ConnectionModeReader)state;
			bool flag = false;
			Exception exception = null;
			try
			{
				if (connectionModeReader.GetReadResult())
				{
					flag = connectionModeReader.ContinueReading();
				}
			}
			catch (Exception exception2)
			{
				Exception exception1 = exception2;
				if (Fx.IsFatal(exception1))
				{
					throw;
				}
				flag = true;
				exception = exception1;
			}
			if (flag)
			{
				connectionModeReader.Complete(exception);
			}
		}

		public void StartReading(TimeSpan receiveTimeout, Action connectionDequeuedCallback)
		{
			this.decoder = new ServerModeDecoder();
			this.receiveTimeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(receiveTimeout);
			base.ConnectionDequeuedCallback = connectionDequeuedCallback;
			bool flag = false;
			Exception exception = null;
			try
			{
				flag = this.ContinueReading();
			}
			catch (Exception exception2)
			{
				Exception exception1 = exception2;
				if (Fx.IsFatal(exception1))
				{
					throw;
				}
				flag = true;
				exception = exception1;
			}
			if (flag)
			{
				this.Complete(exception);
			}
		}
	}
}