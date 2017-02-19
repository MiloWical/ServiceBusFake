using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using System.Xml;

namespace Microsoft.ServiceBus.Channels
{
	internal abstract class SingletonConnectionReader
	{
		private Microsoft.ServiceBus.Channels.IConnection connection;

		private bool doneReceiving;

		private bool doneSending;

		private bool isAtEof;

		private bool isClosed;

		private SecurityMessageProperty security;

		private object thisLock = new object();

		private int offset;

		private int size;

		private Microsoft.ServiceBus.Channels.IConnectionOrientedTransportFactorySettings transportSettings;

		private Uri via;

		private Stream inputStream;

		protected Microsoft.ServiceBus.Channels.IConnection Connection
		{
			get
			{
				return this.connection;
			}
		}

		protected virtual string ContentType
		{
			get
			{
				return null;
			}
		}

		protected abstract long StreamPosition
		{
			get;
		}

		protected object ThisLock
		{
			get
			{
				return this.thisLock;
			}
		}

		protected SingletonConnectionReader(Microsoft.ServiceBus.Channels.IConnection connection, int offset, int size, SecurityMessageProperty security, Microsoft.ServiceBus.Channels.IConnectionOrientedTransportFactorySettings transportSettings, Uri via)
		{
			this.connection = connection;
			this.offset = offset;
			this.size = size;
			this.security = security;
			this.transportSettings = transportSettings;
			this.via = via;
		}

		public void Abort()
		{
			this.connection.Abort();
		}

		public IAsyncResult BeginReceive(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new Microsoft.ServiceBus.Channels.SingletonConnectionReader.ReceiveAsyncResult(this, timeout, callback, state);
		}

		public void Close(TimeSpan timeout)
		{
			lock (this.ThisLock)
			{
				if (!this.isClosed)
				{
					this.isClosed = true;
				}
				else
				{
					return;
				}
			}
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			bool flag = false;
			byte[] numArray = null;
			try
			{
				if (this.inputStream != null)
				{
					numArray = this.transportSettings.BufferManager.TakeBuffer(this.connection.AsyncReadBufferSize);
					while (!this.isAtEof)
					{
						this.inputStream.ReadTimeout = TimeoutHelper.ToMilliseconds(timeoutHelper.RemainingTime());
						if (this.inputStream.Read(numArray, 0, (int)numArray.Length) != 0)
						{
							continue;
						}
						this.isAtEof = true;
					}
				}
				this.OnClose(timeoutHelper.RemainingTime());
				flag = true;
			}
			finally
			{
				if (numArray != null)
				{
					this.transportSettings.BufferManager.ReturnBuffer(numArray);
				}
				if (!flag)
				{
					this.Abort();
				}
			}
		}

		protected abstract bool DecodeBytes(byte[] buffer, ref int offset, ref int size, ref bool isAtEof);

		public void DoneReceiving(bool atEof)
		{
			this.DoneReceiving(atEof, this.transportSettings.CloseTimeout);
		}

		private void DoneReceiving(bool atEof, TimeSpan timeout)
		{
			if (!this.doneReceiving)
			{
				this.isAtEof = atEof;
				this.doneReceiving = true;
				if (this.doneSending)
				{
					this.Close(timeout);
				}
			}
		}

		public void DoneSending(TimeSpan timeout)
		{
			this.doneSending = true;
			if (this.doneReceiving)
			{
				this.Close(timeout);
			}
		}

		public virtual Message EndReceive(IAsyncResult result)
		{
			return Microsoft.ServiceBus.Channels.SingletonConnectionReader.ReceiveAsyncResult.End(result);
		}

		protected abstract void OnClose(TimeSpan timeout);

		protected virtual void PrepareMessage(Message message)
		{
			SecurityMessageProperty securityMessageProperty;
			message.Properties.Via = this.via;
			MessageProperties properties = message.Properties;
			if (this.security != null)
			{
				securityMessageProperty = (SecurityMessageProperty)this.security.CreateCopy();
			}
			else
			{
				securityMessageProperty = null;
			}
			properties.Security = securityMessageProperty;
		}

		public Message Receive(TimeSpan timeout)
		{
			Message message;
			ServiceModelActivity serviceModelActivity;
			SecurityMessageProperty securityMessageProperty;
			byte[] numArray = this.transportSettings.BufferManager.TakeBuffer(this.connection.AsyncReadBufferSize);
			if (this.size > 0)
			{
				Buffer.BlockCopy(this.connection.AsyncReadBuffer, this.offset, numArray, this.offset, this.size);
			}
			try
			{
				TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
				while (!this.DecodeBytes(numArray, ref this.offset, ref this.size, ref this.isAtEof))
				{
					if (!this.isAtEof)
					{
						if (this.size != 0)
						{
							continue;
						}
						this.offset = 0;
						this.size = this.connection.Read(numArray, 0, (int)numArray.Length, timeoutHelper.RemainingTime());
						if (this.size != 0)
						{
							continue;
						}
						this.DoneReceiving(true, timeoutHelper.RemainingTime());
						message = null;
						return message;
					}
					else
					{
						this.DoneReceiving(true, timeoutHelper.RemainingTime());
						message = null;
						return message;
					}
				}
				Microsoft.ServiceBus.Channels.IConnection preReadConnection = this.connection;
				if (this.size > 0)
				{
					byte[] numArray1 = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.Utility.AllocateByteArray(this.size);
					Buffer.BlockCopy(numArray, this.offset, numArray1, 0, this.size);
					preReadConnection = new Microsoft.ServiceBus.Channels.PreReadConnection(preReadConnection, numArray1);
				}
				Stream singletonInputConnectionStream = new Microsoft.ServiceBus.Channels.SingletonConnectionReader.SingletonInputConnectionStream(this, preReadConnection, this.transportSettings);
				this.inputStream = new Microsoft.ServiceBus.Channels.MaxMessageSizeStream(singletonInputConnectionStream, this.transportSettings.MaxReceivedMessageSize);
				if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldUseActivity)
				{
					serviceModelActivity = ServiceModelActivity.CreateBoundedActivity(true);
				}
				else
				{
					serviceModelActivity = null;
				}
				using (serviceModelActivity)
				{
					Message message1 = null;
					try
					{
						message1 = this.transportSettings.MessageEncoderFactory.Encoder.ReadMessage(this.inputStream, this.transportSettings.MaxBufferSize, this.ContentType);
					}
					catch (XmlException xmlException1)
					{
						XmlException xmlException = xmlException1;
						throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ProtocolException(Microsoft.ServiceBus.SR.GetString(Resources.MessageXmlProtocolError, new object[0]), xmlException));
					}
					message1.Properties.Via = this.via;
					MessageProperties properties = message1.Properties;
					if (this.security != null)
					{
						securityMessageProperty = (SecurityMessageProperty)this.security.CreateCopy();
					}
					else
					{
						securityMessageProperty = null;
					}
					properties.Security = securityMessageProperty;
					this.PrepareMessage(message1);
					message = message1;
				}
			}
			finally
			{
				this.transportSettings.BufferManager.ReturnBuffer(numArray);
			}
			return message;
		}

		public RequestContext ReceiveRequest(TimeSpan timeout)
		{
			return new Microsoft.ServiceBus.Channels.SingletonConnectionReader.StreamedFramingRequestContext(this, this.Receive(timeout));
		}

		private class ReceiveAsyncResult : AsyncResult
		{
			private readonly static Action<object> onReceiveScheduled;

			private Message message;

			private Microsoft.ServiceBus.Channels.SingletonConnectionReader parent;

			private TimeSpan timeout;

			static ReceiveAsyncResult()
			{
				Microsoft.ServiceBus.Channels.SingletonConnectionReader.ReceiveAsyncResult.onReceiveScheduled = new Action<object>(Microsoft.ServiceBus.Channels.SingletonConnectionReader.ReceiveAsyncResult.OnReceiveScheduled);
			}

			public ReceiveAsyncResult(Microsoft.ServiceBus.Channels.SingletonConnectionReader parent, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				this.parent = parent;
				this.timeout = timeout;
				IOThreadScheduler.ScheduleCallbackNoFlow(Microsoft.ServiceBus.Channels.SingletonConnectionReader.ReceiveAsyncResult.onReceiveScheduled, this);
			}

			public static new Message End(IAsyncResult result)
			{
				return AsyncResult.End<Microsoft.ServiceBus.Channels.SingletonConnectionReader.ReceiveAsyncResult>(result).message;
			}

			private static void OnReceiveScheduled(object state)
			{
				Microsoft.ServiceBus.Channels.SingletonConnectionReader.ReceiveAsyncResult receiveAsyncResult = (Microsoft.ServiceBus.Channels.SingletonConnectionReader.ReceiveAsyncResult)state;
				Exception exception = null;
				try
				{
					receiveAsyncResult.message = receiveAsyncResult.parent.Receive(receiveAsyncResult.timeout);
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
				receiveAsyncResult.Complete(false, exception);
			}
		}

		private class SingletonInputConnectionStream : Microsoft.ServiceBus.Channels.ConnectionStream
		{
			private Microsoft.ServiceBus.Channels.SingletonMessageDecoder decoder;

			private Microsoft.ServiceBus.Channels.SingletonConnectionReader reader;

			private bool atEof;

			private byte[] chunkBuffer;

			private int chunkBufferOffset;

			private int chunkBufferSize;

			private int chunkBytesRemaining;

			public SingletonInputConnectionStream(Microsoft.ServiceBus.Channels.SingletonConnectionReader reader, Microsoft.ServiceBus.Channels.IConnection connection, IDefaultCommunicationTimeouts defaultTimeouts) : base(connection, defaultTimeouts)
			{
				this.reader = reader;
				this.decoder = new Microsoft.ServiceBus.Channels.SingletonMessageDecoder(reader.StreamPosition);
				this.chunkBuffer = new byte[5];
			}

			private void AbortReader()
			{
				this.reader.Abort();
			}

			public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
			{
				return new Microsoft.ServiceBus.Channels.SingletonConnectionReader.SingletonInputConnectionStream.ReadAsyncResult(this, buffer, offset, count, callback, state);
			}

			public override void Close()
			{
				this.reader.DoneReceiving(this.atEof);
			}

			private void DecodeData(byte[] buffer, int offset, int size)
			{
				while (size > 0)
				{
					int num = this.decoder.Decode(buffer, offset, size);
					offset = offset + num;
					size = size - num;
					Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DebugAssert((this.decoder.CurrentState == Microsoft.ServiceBus.Channels.SingletonMessageDecoder.State.ReadingEnvelopeBytes ? true : this.decoder.CurrentState == Microsoft.ServiceBus.Channels.SingletonMessageDecoder.State.ChunkEnd), "");
				}
			}

			private void DecodeSize(byte[] buffer, ref int offset, ref int size)
			{
				while (size > 0)
				{
					int num = this.decoder.Decode(buffer, offset, size);
					if (num > 0)
					{
						offset = offset + num;
						size = size - num;
					}
					Microsoft.ServiceBus.Channels.SingletonMessageDecoder.State currentState = this.decoder.CurrentState;
					if (currentState == Microsoft.ServiceBus.Channels.SingletonMessageDecoder.State.ChunkStart)
					{
						this.chunkBytesRemaining = this.decoder.ChunkSize;
						if (size > 0 && !object.ReferenceEquals(buffer, this.chunkBuffer))
						{
							Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DebugAssert(size <= (int)this.chunkBuffer.Length, "");
							Buffer.BlockCopy(buffer, offset, this.chunkBuffer, 0, size);
							this.chunkBufferOffset = 0;
							this.chunkBufferSize = size;
						}
						return;
					}
					if (currentState == Microsoft.ServiceBus.Channels.SingletonMessageDecoder.State.End)
					{
						this.ProcessEof();
						return;
					}
				}
			}

			public override int EndRead(IAsyncResult result)
			{
				return Microsoft.ServiceBus.Channels.SingletonConnectionReader.SingletonInputConnectionStream.ReadAsyncResult.End(result);
			}

			private void ProcessEof()
			{
				if (!this.atEof)
				{
					this.atEof = true;
					if (this.chunkBufferSize > 0 || this.chunkBytesRemaining > 0 || this.decoder.CurrentState != Microsoft.ServiceBus.Channels.SingletonMessageDecoder.State.End)
					{
						throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(this.decoder.CreatePrematureEOFException());
					}
					this.reader.DoneReceiving(true);
				}
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				int num = 0;
				while (count != 0)
				{
					if (this.atEof)
					{
						return num;
					}
					if (this.chunkBufferSize <= 0)
					{
						if (this.chunkBytesRemaining > 0)
						{
							int num1 = this.ReadCore(buffer, offset, Math.Min(count, this.chunkBytesRemaining + 5));
							this.DecodeData(buffer, offset, Math.Min(num1, this.chunkBytesRemaining));
							if (num1 <= this.chunkBytesRemaining)
							{
								num = num + num1;
								Microsoft.ServiceBus.Channels.SingletonConnectionReader.SingletonInputConnectionStream singletonInputConnectionStream = this;
								singletonInputConnectionStream.chunkBytesRemaining = singletonInputConnectionStream.chunkBytesRemaining - num1;
							}
							else
							{
								num = num + this.chunkBytesRemaining;
								int num2 = num1 - this.chunkBytesRemaining;
								int num3 = offset + this.chunkBytesRemaining;
								this.chunkBytesRemaining = 0;
								this.DecodeSize(buffer, ref num3, ref num2);
							}
							return num;
						}
						if (count >= 5)
						{
							int num4 = this.ReadCore(buffer, offset, 5);
							int num5 = offset;
							this.DecodeSize(buffer, ref num5, ref num4);
						}
						else
						{
							this.chunkBufferOffset = 0;
							this.chunkBufferSize = this.ReadCore(this.chunkBuffer, 0, (int)this.chunkBuffer.Length);
							this.DecodeSize(this.chunkBuffer, ref this.chunkBufferOffset, ref this.chunkBufferSize);
						}
					}
					else
					{
						int num6 = Math.Min(this.chunkBytesRemaining, Math.Min(this.chunkBufferSize, count));
						Buffer.BlockCopy(this.chunkBuffer, this.chunkBufferOffset, buffer, offset, num6);
						this.DecodeData(this.chunkBuffer, this.chunkBufferOffset, num6);
						Microsoft.ServiceBus.Channels.SingletonConnectionReader.SingletonInputConnectionStream singletonInputConnectionStream1 = this;
						singletonInputConnectionStream1.chunkBufferOffset = singletonInputConnectionStream1.chunkBufferOffset + num6;
						Microsoft.ServiceBus.Channels.SingletonConnectionReader.SingletonInputConnectionStream singletonInputConnectionStream2 = this;
						singletonInputConnectionStream2.chunkBufferSize = singletonInputConnectionStream2.chunkBufferSize - num6;
						Microsoft.ServiceBus.Channels.SingletonConnectionReader.SingletonInputConnectionStream singletonInputConnectionStream3 = this;
						singletonInputConnectionStream3.chunkBytesRemaining = singletonInputConnectionStream3.chunkBytesRemaining - num6;
						if (this.chunkBytesRemaining == 0 && this.chunkBufferSize > 0)
						{
							this.DecodeSize(this.chunkBuffer, ref this.chunkBufferOffset, ref this.chunkBufferSize);
						}
						num = num + num6;
						offset = offset + num6;
						count = count - num6;
					}
				}
				return num;
			}

			private int ReadCore(byte[] buffer, int offset, int count)
			{
				int num = -1;
				try
				{
					num = base.Read(buffer, offset, count);
					if (num == 0)
					{
						this.ProcessEof();
					}
				}
				finally
				{
					if (num == -1)
					{
						this.AbortReader();
					}
				}
				return num;
			}

			public class ReadAsyncResult : AsyncResult
			{
				private Microsoft.ServiceBus.Channels.SingletonConnectionReader.SingletonInputConnectionStream parent;

				private int result;

				public ReadAsyncResult(Microsoft.ServiceBus.Channels.SingletonConnectionReader.SingletonInputConnectionStream parent, byte[] buffer, int offset, int count, AsyncCallback callback, object state) : base(callback, state)
				{
					this.parent = parent;
					this.result = this.parent.Read(buffer, offset, count);
					base.Complete(true);
				}

				public static new int End(IAsyncResult result)
				{
					return AsyncResult.End<Microsoft.ServiceBus.Channels.SingletonConnectionReader.SingletonInputConnectionStream.ReadAsyncResult>(result).result;
				}
			}
		}

		private class StreamedFramingRequestContext : Microsoft.ServiceBus.Channels.RequestContextBase
		{
			private Microsoft.ServiceBus.Channels.IConnection connection;

			private Microsoft.ServiceBus.Channels.SingletonConnectionReader parent;

			private Microsoft.ServiceBus.Channels.IConnectionOrientedTransportFactorySettings settings;

			private TimeoutHelper timeoutHelper;

			public StreamedFramingRequestContext(Microsoft.ServiceBus.Channels.SingletonConnectionReader parent, Message requestMessage) : base(requestMessage, parent.transportSettings.CloseTimeout, parent.transportSettings.SendTimeout)
			{
				this.parent = parent;
				this.connection = parent.connection;
				this.settings = parent.transportSettings;
			}

			protected override void OnAbort()
			{
				this.parent.Abort();
			}

			protected override IAsyncResult OnBeginReply(Message message, TimeSpan timeout, AsyncCallback callback, object state)
			{
				this.timeoutHelper = new TimeoutHelper(timeout);
				return Microsoft.ServiceBus.Channels.StreamingConnectionHelper.BeginWriteMessage(message, this.connection, false, this.settings, ref this.timeoutHelper, callback, state);
			}

			protected override void OnClose(TimeSpan timeout)
			{
				this.parent.Close(timeout);
			}

			protected override void OnEndReply(IAsyncResult result)
			{
				Microsoft.ServiceBus.Channels.StreamingConnectionHelper.EndWriteMessage(result);
				this.parent.DoneSending(this.timeoutHelper.RemainingTime());
			}

			protected override void OnReply(Message message, TimeSpan timeout)
			{
				this.timeoutHelper = new TimeoutHelper(timeout);
				Microsoft.ServiceBus.Channels.StreamingConnectionHelper.WriteMessage(message, this.connection, false, this.settings, ref this.timeoutHelper);
				this.parent.DoneSending(this.timeoutHelper.RemainingTime());
			}
		}
	}
}