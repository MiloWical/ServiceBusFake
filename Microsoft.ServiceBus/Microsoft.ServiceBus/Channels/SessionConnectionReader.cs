using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using System;
using System.Diagnostics;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using System.Threading;

namespace Microsoft.ServiceBus.Channels
{
	internal abstract class SessionConnectionReader : Microsoft.ServiceBus.Channels.IMessageSource
	{
		private bool isAtEOF;

		private bool usingAsyncReadBuffer;

		private Microsoft.ServiceBus.Channels.IConnection connection;

		private byte[] buffer;

		private int offset;

		private int size;

		private byte[] envelopeBuffer;

		private int envelopeOffset;

		private int envelopeSize;

		private bool readIntoEnvelopeBuffer;

		private WaitCallback onAsyncReadComplete;

		private Message pendingMessage;

		private Exception pendingException;

		private WaitCallback pendingCallback;

		private object pendingCallbackState;

		private SecurityMessageProperty security;

		private Microsoft.ServiceBus.Common.TimeoutHelper readTimeoutHelper;

		private Microsoft.ServiceBus.Channels.IConnection rawConnection;

		protected byte[] EnvelopeBuffer
		{
			get
			{
				return this.envelopeBuffer;
			}
			set
			{
				this.envelopeBuffer = value;
			}
		}

		protected int EnvelopeOffset
		{
			get
			{
				return this.envelopeOffset;
			}
			set
			{
				this.envelopeOffset = value;
			}
		}

		protected int EnvelopeSize
		{
			get
			{
				return this.envelopeSize;
			}
			set
			{
				this.envelopeSize = value;
			}
		}

		protected SessionConnectionReader(Microsoft.ServiceBus.Channels.IConnection connection, Microsoft.ServiceBus.Channels.IConnection rawConnection, int offset, int size, SecurityMessageProperty security)
		{
			this.offset = offset;
			this.size = size;
			if (size > 0)
			{
				this.buffer = connection.AsyncReadBuffer;
			}
			this.connection = connection;
			this.rawConnection = rawConnection;
			this.onAsyncReadComplete = new WaitCallback(this.OnAsyncReadComplete);
			this.security = security;
		}

		public Microsoft.ServiceBus.Channels.AsyncReceiveResult BeginReceive(TimeSpan timeout, WaitCallback callback, object state)
		{
			AsyncReadResult asyncReadResult;
			if (this.pendingMessage != null || this.pendingException != null)
			{
				return Microsoft.ServiceBus.Channels.AsyncReceiveResult.Completed;
			}
			this.readTimeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
			while (!this.isAtEOF)
			{
				if (this.size > 0)
				{
					this.pendingMessage = this.DecodeMessage(this.readTimeoutHelper.RemainingTime());
					if (this.pendingMessage != null)
					{
						this.PrepareMessage(this.pendingMessage);
						return Microsoft.ServiceBus.Channels.AsyncReceiveResult.Completed;
					}
					if (this.isAtEOF)
					{
						return Microsoft.ServiceBus.Channels.AsyncReceiveResult.Completed;
					}
				}
				if (this.size != 0)
				{
					throw Fx.AssertAndThrow("BeginReceive: DecodeMessage() should consume the outstanding buffer or return a message.");
				}
				if (!this.usingAsyncReadBuffer)
				{
					this.buffer = this.connection.AsyncReadBuffer;
					this.usingAsyncReadBuffer = true;
				}
				this.pendingCallback = callback;
				this.pendingCallbackState = state;
				bool flag = true;
				try
				{
					asyncReadResult = this.connection.BeginRead(0, (int)this.buffer.Length, this.readTimeoutHelper.RemainingTime(), this.onAsyncReadComplete, null);
					flag = false;
				}
				finally
				{
					if (flag)
					{
						this.pendingCallback = null;
						this.pendingCallbackState = null;
					}
				}
				if (asyncReadResult == AsyncReadResult.Queued)
				{
					return Microsoft.ServiceBus.Channels.AsyncReceiveResult.Pending;
				}
				this.pendingCallback = null;
				this.pendingCallbackState = null;
				this.HandleReadComplete(this.connection.EndRead(), false);
			}
			return Microsoft.ServiceBus.Channels.AsyncReceiveResult.Completed;
		}

		public Microsoft.ServiceBus.Channels.AsyncReceiveResult BeginWaitForMessage(TimeSpan timeout, WaitCallback callback, object state)
		{
			Microsoft.ServiceBus.Channels.AsyncReceiveResult asyncReceiveResult;
			try
			{
				asyncReceiveResult = this.BeginReceive(timeout, callback, state);
			}
			catch (TimeoutException timeoutException)
			{
				this.pendingException = timeoutException;
				asyncReceiveResult = Microsoft.ServiceBus.Channels.AsyncReceiveResult.Completed;
			}
			return asyncReceiveResult;
		}

		private Message DecodeMessage(TimeSpan timeout)
		{
			if (DiagnosticUtility.ShouldUseActivity && ServiceModelActivity.Current != null && ServiceModelActivity.Current.ActivityType == ActivityType.ProcessAction)
			{
				ServiceModelActivity.Current.Resume();
			}
			if (!this.readIntoEnvelopeBuffer)
			{
				return this.DecodeMessage(this.buffer, ref this.offset, ref this.size, ref this.isAtEOF, timeout);
			}
			int num = this.envelopeOffset;
			return this.DecodeMessage(this.envelopeBuffer, ref num, ref this.size, ref this.isAtEOF, timeout);
		}

		protected abstract Message DecodeMessage(byte[] buffer, ref int offset, ref int size, ref bool isAtEof, TimeSpan timeout);

		public Message EndReceive()
		{
			return this.GetPendingMessage();
		}

		public bool EndWaitForMessage()
		{
			bool flag;
			try
			{
				this.pendingMessage = this.EndReceive();
				flag = true;
			}
			catch (TimeoutException timeoutException1)
			{
				TimeoutException timeoutException = timeoutException1;
				if (DiagnosticUtility.ShouldTraceInformation)
				{
					DiagnosticUtility.ExceptionUtility.TraceHandledException(timeoutException, TraceEventType.Information);
				}
				flag = false;
			}
			return flag;
		}

		protected abstract void EnsureDecoderAtEof();

		private Message GetPendingMessage()
		{
			if (this.pendingException != null)
			{
				Exception exception = this.pendingException;
				this.pendingException = null;
				throw TraceUtility.ThrowHelperError(exception, this.pendingMessage);
			}
			if (this.pendingMessage == null)
			{
				return null;
			}
			Message message = this.pendingMessage;
			this.pendingMessage = null;
			return message;
		}

		public Microsoft.ServiceBus.Channels.IConnection GetRawConnection()
		{
			Microsoft.ServiceBus.Channels.IConnection preReadConnection = null;
			if (this.rawConnection != null)
			{
				preReadConnection = this.rawConnection;
				this.rawConnection = null;
				if (this.size > 0)
				{
					Microsoft.ServiceBus.Channels.PreReadConnection preReadConnection1 = preReadConnection as Microsoft.ServiceBus.Channels.PreReadConnection;
					if (preReadConnection1 == null)
					{
						preReadConnection = new Microsoft.ServiceBus.Channels.PreReadConnection(preReadConnection, this.buffer, this.offset, this.size);
					}
					else
					{
						preReadConnection1.AddPreReadData(this.buffer, this.offset, this.size);
					}
				}
			}
			return preReadConnection;
		}

		private void HandleReadComplete(int bytesRead, bool readIntoEnvelopeBuffer)
		{
			this.readIntoEnvelopeBuffer = readIntoEnvelopeBuffer;
			if (bytesRead == 0)
			{
				this.EnsureDecoderAtEof();
				this.isAtEOF = true;
				return;
			}
			this.offset = 0;
			this.size = bytesRead;
		}

		private void OnAsyncReadComplete(object state)
		{
			try
			{
				while (true)
				{
					this.HandleReadComplete(this.connection.EndRead(), false);
					if (this.isAtEOF)
					{
						break;
					}
					Message message = this.DecodeMessage(this.readTimeoutHelper.RemainingTime());
					if (message == null)
					{
						if (this.isAtEOF)
						{
							break;
						}
						if (this.size != 0)
						{
							throw Fx.AssertAndThrow("OnAsyncReadComplete: DecodeMessage() should consume the outstanding buffer or return a message.");
						}
						if (this.connection.BeginRead(0, (int)this.buffer.Length, this.readTimeoutHelper.RemainingTime(), this.onAsyncReadComplete, null) == AsyncReadResult.Queued)
						{
							return;
						}
					}
					else
					{
						this.PrepareMessage(message);
						this.pendingMessage = message;
						break;
					}
				}
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				this.pendingException = exception;
			}
			WaitCallback waitCallback = this.pendingCallback;
			object obj = this.pendingCallbackState;
			this.pendingCallback = null;
			this.pendingCallbackState = null;
			waitCallback(obj);
		}

		protected virtual void PrepareMessage(Message message)
		{
			if (this.security != null)
			{
				message.Properties.Security = (SecurityMessageProperty)this.security.CreateCopy();
			}
		}

		public Message Receive(TimeSpan timeout)
		{
			int num;
			Message pendingMessage = this.GetPendingMessage();
			if (pendingMessage != null)
			{
				return pendingMessage;
			}
			Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
			while (!this.isAtEOF)
			{
				if (this.size > 0)
				{
					pendingMessage = this.DecodeMessage(timeoutHelper.RemainingTime());
					if (pendingMessage != null)
					{
						this.PrepareMessage(pendingMessage);
						return pendingMessage;
					}
					if (this.isAtEOF)
					{
						return null;
					}
				}
				if (this.size != 0)
				{
					throw Fx.AssertAndThrow("Receive: DecodeMessage() should consume the outstanding buffer or return a message.");
				}
				if (this.buffer == null)
				{
					this.buffer = DiagnosticUtility.Utility.AllocateByteArray(this.connection.AsyncReadBufferSize);
				}
				if (this.EnvelopeBuffer == null || this.EnvelopeSize - this.EnvelopeOffset < (int)this.buffer.Length)
				{
					num = this.connection.Read(this.buffer, 0, (int)this.buffer.Length, timeoutHelper.RemainingTime());
					this.HandleReadComplete(num, false);
				}
				else
				{
					num = this.connection.Read(this.EnvelopeBuffer, this.EnvelopeOffset, (int)this.buffer.Length, timeoutHelper.RemainingTime());
					this.HandleReadComplete(num, true);
				}
			}
			return null;
		}

		protected void SendFault(string faultString, TimeSpan timeout)
		{
			byte[] numArray = new byte[128];
			Microsoft.ServiceBus.Channels.InitialServerConnectionReader.SendFault(this.connection, faultString, numArray, timeout, 65536);
		}

		public bool WaitForMessage(TimeSpan timeout)
		{
			bool flag;
			try
			{
				this.pendingMessage = this.Receive(timeout);
				flag = true;
			}
			catch (TimeoutException timeoutException1)
			{
				TimeoutException timeoutException = timeoutException1;
				if (DiagnosticUtility.ShouldTraceInformation)
				{
					DiagnosticUtility.ExceptionUtility.TraceHandledException(timeoutException, TraceEventType.Information);
				}
				flag = false;
			}
			return flag;
		}
	}
}