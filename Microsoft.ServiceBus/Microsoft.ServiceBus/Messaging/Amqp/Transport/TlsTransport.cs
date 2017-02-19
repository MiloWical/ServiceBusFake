using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging.Amqp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
	internal sealed class TlsTransport : TransportBase
	{
		private readonly static AsyncCallback onOpenComplete;

		private readonly static AsyncCallback onWriteComplete;

		private readonly static AsyncCallback onReadComplete;

		private readonly TransportBase innerTransport;

		private readonly SslStream sslStream;

		private TlsTransportSettings tlsSettings;

		private TlsTransport.OperationState writeState;

		private TlsTransport.OperationState readState;

		public override bool IsSecure
		{
			get
			{
				return true;
			}
		}

		public override EndPoint LocalEndPoint
		{
			get
			{
				return this.innerTransport.LocalEndPoint;
			}
		}

		public override EndPoint RemoteEndPoint
		{
			get
			{
				return this.innerTransport.RemoteEndPoint;
			}
		}

		static TlsTransport()
		{
			TlsTransport.onOpenComplete = new AsyncCallback(TlsTransport.OnOpenComplete);
			TlsTransport.onWriteComplete = new AsyncCallback(TlsTransport.OnWriteComplete);
			TlsTransport.onReadComplete = new AsyncCallback(TlsTransport.OnReadComplete);
		}

		public TlsTransport(TransportBase innerTransport, TlsTransportSettings tlsSettings) : base("tls")
		{
			this.innerTransport = innerTransport;
			this.tlsSettings = tlsSettings;
			this.sslStream = (tlsSettings.CertificateValidationCallback == null ? new SslStream(new TransportStream(this.innerTransport), false) : new SslStream(new TransportStream(this.innerTransport), false, tlsSettings.CertificateValidationCallback));
		}

		protected override void AbortInternal()
		{
			this.innerTransport.Abort();
		}

		protected override bool CloseInternal()
		{
			this.sslStream.Close();
			return true;
		}

		private static X509CertificateCollection GetX509CertificateCollection(X509Certificate2 certificate)
		{
			X509CertificateCollection x509CertificateCollection = new X509CertificateCollection();
			x509CertificateCollection.Add(certificate);
			return x509CertificateCollection;
		}

		private void HandleOpenComplete(IAsyncResult result, bool syncComplete)
		{
			Exception exception = null;
			try
			{
				bool isInitiator = this.tlsSettings.IsInitiator;
				this.tlsSettings = null;
				if (!isInitiator)
				{
					this.sslStream.EndAuthenticateAsServer(result);
				}
				else
				{
					this.sslStream.EndAuthenticateAsClient(result);
				}
			}
			catch (Exception exception2)
			{
				Exception exception1 = exception2;
				if (Fx.IsFatal(exception1) || syncComplete)
				{
					throw;
				}
				else
				{
					exception = exception1;
				}
			}
			if (!syncComplete)
			{
				base.CompleteOpen(false, exception);
			}
		}

		private void HandleOperationComplete(IAsyncResult result, bool write, bool syncComplete)
		{
			TransportAsyncCallbackArgs args = null;
			try
			{
				if (!write)
				{
					args = this.readState.Args;
					this.readState.Reset();
					args.BytesTransfered = this.sslStream.EndRead(result);
				}
				else
				{
					args = this.writeState.Args;
					ByteBuffer buffer = this.writeState.Buffer;
					this.writeState.Reset();
					if (buffer != null)
					{
						buffer.Dispose();
					}
					this.sslStream.EndWrite(result);
					args.BytesTransfered = args.Count;
				}
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				args.Exception = exception;
			}
			args.CompletedSynchronously = syncComplete;
			if (!syncComplete && args.CompletedCallback != null)
			{
				args.CompletedCallback(args);
			}
		}

		private static void OnOpenComplete(IAsyncResult result)
		{
			if (!result.CompletedSynchronously)
			{
				((TlsTransport)result.AsyncState).HandleOpenComplete(result, false);
			}
		}

		private static void OnReadComplete(IAsyncResult result)
		{
			if (!result.CompletedSynchronously)
			{
				((TlsTransport)result.AsyncState).HandleOperationComplete(result, false, false);
			}
		}

		private static void OnWriteComplete(IAsyncResult result)
		{
			if (!result.CompletedSynchronously)
			{
				((TlsTransport)result.AsyncState).HandleOperationComplete(result, true, false);
			}
		}

		protected override bool OpenInternal()
		{
			IAsyncResult asyncResult;
			if (!this.tlsSettings.IsInitiator)
			{
				asyncResult = (this.tlsSettings.CertificateValidationCallback != null ? this.sslStream.BeginAuthenticateAsServer(this.tlsSettings.Certificate, true, SslProtocols.Default, true, TlsTransport.onOpenComplete, this) : this.sslStream.BeginAuthenticateAsServer(this.tlsSettings.Certificate, TlsTransport.onOpenComplete, this));
			}
			else
			{
				asyncResult = (this.tlsSettings.Certificate != null ? this.sslStream.BeginAuthenticateAsClient(this.tlsSettings.TargetHost, TlsTransport.GetX509CertificateCollection(this.tlsSettings.Certificate), SslProtocols.Default, true, TlsTransport.onOpenComplete, this) : this.sslStream.BeginAuthenticateAsClient(this.tlsSettings.TargetHost, TlsTransport.onOpenComplete, this));
			}
			bool completedSynchronously = asyncResult.CompletedSynchronously;
			if (completedSynchronously)
			{
				this.HandleOpenComplete(asyncResult, true);
			}
			return completedSynchronously;
		}

		public override bool ReadAsync(TransportAsyncCallbackArgs args)
		{
			this.readState.Args = args;
			IAsyncResult asyncResult = this.sslStream.BeginRead(args.Buffer, args.Offset, args.Count, TlsTransport.onReadComplete, this);
			bool completedSynchronously = asyncResult.CompletedSynchronously;
			if (completedSynchronously)
			{
				this.HandleOperationComplete(asyncResult, false, true);
			}
			return !completedSynchronously;
		}

		public override bool WriteAsync(TransportAsyncCallbackArgs args)
		{
			ArraySegment<byte> nums;
			if (args.Buffer != null)
			{
				nums = new ArraySegment<byte>(args.Buffer, args.Offset, args.Count);
				this.writeState.Args = args;
			}
			else if (args.ByteBufferList.Count != 1)
			{
				ByteBuffer byteBuffer = new ByteBuffer(args.Count, false, false);
				for (int i = 0; i < args.ByteBufferList.Count; i++)
				{
					ByteBuffer item = args.ByteBufferList[i];
					Buffer.BlockCopy(item.Buffer, item.Offset, byteBuffer.Buffer, byteBuffer.Length, item.Length);
					byteBuffer.Append(item.Length);
				}
				nums = new ArraySegment<byte>(byteBuffer.Buffer, 0, byteBuffer.Length);
				this.writeState.Args = args;
				this.writeState.Buffer = byteBuffer;
			}
			else
			{
				ByteBuffer item1 = args.ByteBufferList[0];
				nums = new ArraySegment<byte>(item1.Buffer, item1.Offset, item1.Length);
				this.writeState.Args = args;
			}
			IAsyncResult asyncResult = this.sslStream.BeginWrite(nums.Array, nums.Offset, nums.Count, TlsTransport.onWriteComplete, this);
			bool completedSynchronously = asyncResult.CompletedSynchronously;
			if (completedSynchronously)
			{
				this.HandleOperationComplete(asyncResult, true, true);
			}
			return !completedSynchronously;
		}

		private struct OperationState
		{
			public TransportAsyncCallbackArgs Args;

			public ByteBuffer Buffer;

			public void Reset()
			{
				this.Args = null;
				this.Buffer = null;
			}
		}
	}
}