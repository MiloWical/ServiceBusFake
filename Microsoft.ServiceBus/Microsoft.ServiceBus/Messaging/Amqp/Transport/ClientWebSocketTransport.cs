using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Properties;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Net;
using System.Reflection;

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
	internal sealed class ClientWebSocketTransport : TransportBase
	{
		private readonly ServiceBusClientWebSocket webSocket;

		private readonly Uri uri;

		private readonly EventTraceActivity activity;

		private readonly int asyncReadBufferSize;

		private readonly byte[] asyncReadBuffer;

		private readonly AsyncCallback onReadComplete;

		private int asyncReadBufferOffset;

		private int remainingBytes;

		public override EndPoint LocalEndPoint
		{
			get
			{
				return this.webSocket.LocalEndpoint;
			}
		}

		public override EndPoint RemoteEndPoint
		{
			get
			{
				return this.webSocket.RemoteEndpoint;
			}
		}

		public override bool RequiresCompleteFrames
		{
			get
			{
				return true;
			}
		}

		public ClientWebSocketTransport(ServiceBusClientWebSocket webSocket, Uri uri, TransportSettings transportSettings, EventTraceActivity activity) : base("clientwebsocket")
		{
			this.webSocket = webSocket;
			this.uri = uri;
			this.activity = activity;
			this.asyncReadBufferSize = transportSettings.ReceiveBufferSize;
			this.asyncReadBuffer = DiagnosticUtility.Utility.AllocateByteArray(this.asyncReadBufferSize);
			this.onReadComplete = new AsyncCallback(this.OnReadComplete);
		}

		protected override void AbortInternal()
		{
			MessagingClientEtwProvider.Provider.WebSocketTransportAborted(this.activity, this.uri.AbsoluteUri);
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
					Fx.Exception.TraceHandled(exception, "ClientWebSocketTransport.AbortInternal", this.activity);
				}
			}
		}

		protected override bool CloseInternal()
		{
			MessagingClientEtwProvider.Provider.WebSocketTransportClosed(this.activity, this.uri.AbsoluteUri);
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
				Fx.Exception.TraceHandled(exception, "ClientWebSocketTransport.CloseInternal", this.activity);
			}
			return true;
		}

		private TransportAsyncCallbackArgs HandleReadComplete(IAsyncResult result)
		{
			TransportAsyncCallbackArgs asyncState = (TransportAsyncCallbackArgs)result.AsyncState;
			asyncState.CompletedSynchronously = result.CompletedSynchronously;
			try
			{
				this.TransferData(this.webSocket.EndReceive(result), asyncState);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				asyncState.Exception = exception;
			}
			return asyncState;
		}

		private void OnReadComplete(IAsyncResult result)
		{
			if (result.CompletedSynchronously)
			{
				return;
			}
			TransportAsyncCallbackArgs transportAsyncCallbackArg = this.HandleReadComplete(result);
			transportAsyncCallbackArg.CompletedCallback(transportAsyncCallbackArg);
		}

		protected override bool OpenInternal()
		{
			this.ThrowIfNotOpen();
			return true;
		}

		public override bool ReadAsync(TransportAsyncCallbackArgs args)
		{
			this.ThrowIfNotOpen();
			Fx.AssertAndThrow(args.Buffer != null, "must have buffer to read");
			Fx.AssertAndThrow(args.CompletedCallback != null, "must have a valid callback");
			Fx.AssertAndThrow(args.Count <= this.asyncReadBufferSize, Resources.ClientWebSocketTransportReadBufferTooSmall);
			ConnectionUtilities.ValidateBufferBounds(args.Buffer, args.Offset, args.Count);
			args.Exception = null;
			if (this.asyncReadBufferOffset > 0)
			{
				Fx.AssertAndThrow(this.remainingBytes > 0, "Must have data in buffer to transfer");
				this.TransferData(this.remainingBytes, args);
				return false;
			}
			IAsyncResult asyncResult = this.webSocket.BeginReceive(this.asyncReadBuffer, 0, this.asyncReadBufferSize, TimeSpan.FromMinutes(1), this.onReadComplete, args);
			if (!asyncResult.CompletedSynchronously)
			{
				return true;
			}
			this.HandleReadComplete(asyncResult);
			return false;
		}

		private void ThrowIfNotOpen()
		{
			if (this.webSocket.State != ServiceBusClientWebSocket.WebSocketState.Open)
			{
				if (this.webSocket.State != ServiceBusClientWebSocket.WebSocketState.Aborted)
				{
					if (this.webSocket.State != ServiceBusClientWebSocket.WebSocketState.Closed)
					{
						throw Fx.Exception.AsWarning(new AmqpException(AmqpError.IllegalState, SRAmqp.AmqpUnopenObject), this.activity);
					}
					throw Fx.Exception.AsWarning(new ObjectDisposedException(SRAmqp.AmqpUnopenObject), this.activity);
				}
				throw Fx.Exception.AsWarning(new ObjectDisposedException(SRAmqp.AmqpObjectAborted(base.GetType().Name)), this.activity);
			}
		}

		private void TransferData(int bytesRead, TransportAsyncCallbackArgs args)
		{
			if (bytesRead <= args.Count)
			{
				Buffer.BlockCopy(this.asyncReadBuffer, this.asyncReadBufferOffset, args.Buffer, args.Offset, bytesRead);
				this.asyncReadBufferOffset = 0;
				this.remainingBytes = 0;
				args.BytesTransfered = bytesRead;
				return;
			}
			Buffer.BlockCopy(this.asyncReadBuffer, this.asyncReadBufferOffset, args.Buffer, args.Offset, args.Count);
			ClientWebSocketTransport count = this;
			count.asyncReadBufferOffset = count.asyncReadBufferOffset + args.Count;
			this.remainingBytes = bytesRead - args.Count;
			args.BytesTransfered = args.Count;
		}

		public override bool WriteAsync(TransportAsyncCallbackArgs args)
		{
			this.ThrowIfNotOpen();
			Fx.AssertAndThrow((args.Buffer != null ? true : args.ByteBufferList != null), "must have a buffer to write");
			Fx.AssertAndThrow(args.CompletedCallback != null, "must have a valid callback");
			return this.webSocket.WriteAsync(args);
		}
	}
}