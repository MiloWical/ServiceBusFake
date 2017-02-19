using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using Microsoft.ServiceBus.Messaging.Amqp.Transport;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Principal;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal abstract class AmqpConnectionBase : AmqpObject, IIoHandler
	{
		private readonly AmqpConnectionSettings settings;

		private readonly Microsoft.ServiceBus.Messaging.Amqp.AsyncIO asyncIO;

		private IAmqpUsageMeter usageMeter;

		protected Microsoft.ServiceBus.Messaging.Amqp.AsyncIO AsyncIO
		{
			get
			{
				return this.asyncIO;
			}
		}

		public EndPoint LocalEndpoint
		{
			get
			{
				return this.asyncIO.Transport.LocalEndPoint;
			}
		}

		public IPrincipal Principal
		{
			get
			{
				return this.asyncIO.Transport.Principal;
			}
		}

		public EndPoint RemoteEndpoint
		{
			get
			{
				return this.asyncIO.Transport.RemoteEndPoint;
			}
		}

		public AmqpConnectionSettings Settings
		{
			get
			{
				return this.settings;
			}
		}

		public IAmqpUsageMeter UsageMeter
		{
			get
			{
				return this.usageMeter;
			}
			set
			{
				this.usageMeter = value;
			}
		}

		protected AmqpConnectionBase(string type, TransportBase transport, AmqpConnectionSettings settings, bool isInitiator) : base(type)
		{
			if (settings == null)
			{
				throw new ArgumentNullException("settings");
			}
			this.settings = settings;
			this.asyncIO = new Microsoft.ServiceBus.Messaging.Amqp.AsyncIO(this, (int)this.settings.MaxFrameSize(), transport, isInitiator);
		}

		void Microsoft.ServiceBus.Messaging.Amqp.IIoHandler.OnIoFault(Exception exception)
		{
			MessagingClientEtwProvider.TraceClient<AmqpConnectionBase, Exception>((AmqpConnectionBase source, Exception ex) => MessagingClientEtwProvider.Provider.EventWriteAmqpLogError(source, "AsyncIoFault", ex.Message), this, exception);
			base.TerminalException = exception;
			base.Abort();
		}

		void Microsoft.ServiceBus.Messaging.Amqp.IIoHandler.OnReceiveBuffer(ByteBuffer buffer)
		{
			this.OnReceiveFrameBuffer(buffer);
		}

		protected abstract void OnFrameBuffer(ByteBuffer buffer);

		protected abstract void OnProtocolHeader(ProtocolHeader header);

		protected virtual void OnReceiveFrameBuffer(ByteBuffer buffer)
		{
			string empty = string.Empty;
			try
			{
				empty = "UsageMeter";
				if (this.usageMeter != null)
				{
					this.usageMeter.OnBytesRead(buffer.Length);
				}
				if (base.State > AmqpObjectState.OpenClosePipe)
				{
					empty = "FrameBuffer";
					this.OnFrameBuffer(buffer);
				}
				else
				{
					empty = "ProtocolHeader";
					ProtocolHeader protocolHeader = new ProtocolHeader();
					protocolHeader.Decode(buffer);
					this.OnProtocolHeader(protocolHeader);
				}
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				MessagingClientEtwProvider.TraceClient<AmqpConnectionBase, string, string>((AmqpConnectionBase a, string b, string c) => MessagingClientEtwProvider.Provider.EventWriteAmqpLogError(a, b, c), this, empty, exception.Message);
				base.SafeClose(exception);
			}
		}

		protected virtual void OnSendBuffer(int totalCount)
		{
			if (this.usageMeter != null)
			{
				this.usageMeter.OnBytesWritten(totalCount);
			}
		}

		public void SendBuffer(ByteBuffer buffer)
		{
			int length = buffer.Length;
			this.asyncIO.WriteBuffer(buffer);
			this.OnSendBuffer(length);
		}

		public void SendBuffers(ByteBuffer[] buffers)
		{
			int length = 0;
			ByteBuffer[] byteBufferArray = buffers;
			for (int i = 0; i < (int)byteBufferArray.Length; i++)
			{
				length = length + byteBufferArray[i].Length;
			}
			this.asyncIO.WriteBuffer(buffers);
			this.OnSendBuffer(length);
		}

		public void SendDatablock(IAmqpSerializable dataBlock)
		{
			ByteBuffer byteBuffer = new ByteBuffer(new byte[dataBlock.EncodeSize]);
			dataBlock.Encode(byteBuffer);
			int length = byteBuffer.Length;
			this.asyncIO.WriteBuffer(byteBuffer);
			this.OnSendBuffer(length);
		}
	}
}