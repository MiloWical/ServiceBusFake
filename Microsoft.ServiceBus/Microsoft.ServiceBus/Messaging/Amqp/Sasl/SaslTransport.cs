using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Transport;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Principal;

namespace Microsoft.ServiceBus.Messaging.Amqp.Sasl
{
	internal sealed class SaslTransport : TransportBase
	{
		private readonly TransportBase innerTransport;

		private SaslNegotiator negotiator;

		public override bool IsSecure
		{
			get
			{
				return this.innerTransport.IsSecure;
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

		public override bool RequiresCompleteFrames
		{
			get
			{
				return this.innerTransport.RequiresCompleteFrames;
			}
		}

		public SaslTransport(TransportBase transport, SaslTransportProvider provider, bool isInitiator) : base("sasl")
		{
			this.innerTransport = transport;
			this.negotiator = new SaslNegotiator(this, provider, isInitiator);
		}

		protected override void AbortInternal()
		{
			this.innerTransport.Abort();
		}

		protected override bool CloseInternal()
		{
			this.innerTransport.Close();
			return true;
		}

		public void OnNegotiationFail(Exception exception)
		{
			MessagingClientEtwProvider.TraceClient<SaslTransport, Exception>((SaslTransport source, Exception ex) => MessagingClientEtwProvider.Provider.EventWriteAmqpLogError(source, "OnNegotiationFail", ex.Message), this, exception);
			this.negotiator = null;
			this.innerTransport.SafeClose(exception);
			base.CompleteOpen(false, exception);
		}

		public void OnNegotiationSucceed(IPrincipal principal)
		{
			MessagingClientEtwProvider.TraceClient<SaslTransport>((SaslTransport source) => MessagingClientEtwProvider.Provider.EventWriteAmqpLogOperation(source, TraceOperation.Execute, "OnNegotiationSucceed"), this);
			this.negotiator = null;
			base.Principal = principal;
			base.CompleteOpen(false, null);
		}

		protected override bool OpenInternal()
		{
			return this.negotiator.Start();
		}

		public override bool ReadAsync(TransportAsyncCallbackArgs args)
		{
			return this.innerTransport.ReadAsync(args);
		}

		public override bool WriteAsync(TransportAsyncCallbackArgs args)
		{
			return this.innerTransport.WriteAsync(args);
		}
	}
}