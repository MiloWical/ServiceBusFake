using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Transport;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Amqp.Sasl
{
	internal sealed class SaslTransportProvider : TransportProvider
	{
		private Dictionary<string, SaslHandler> handlers;

		public IEnumerable<string> Mechanisms
		{
			get
			{
				return this.handlers.Keys;
			}
		}

		public SaslTransportProvider()
		{
			base.ProtocolId = Microsoft.ServiceBus.Messaging.Amqp.ProtocolId.AmqpSasl;
			this.handlers = new Dictionary<string, SaslHandler>();
		}

		public void AddHandler(SaslHandler handler)
		{
			MessagingClientEtwProvider.TraceClient<SaslTransportProvider, SaslHandler>((SaslTransportProvider source, SaslHandler detail) => MessagingClientEtwProvider.Provider.EventWriteAmqpLogOperation(source, TraceOperation.Add, detail), this, handler);
			this.handlers.Add(handler.Mechanism, handler);
		}

		public SaslHandler GetHandler(string mechanism, bool clone)
		{
			SaslHandler saslHandler;
			if (!this.handlers.TryGetValue(mechanism, out saslHandler))
			{
				MessagingClientEtwProvider.TraceClient<SaslTransportProvider, string>((SaslTransportProvider source, string message) => MessagingClientEtwProvider.Provider.EventWriteAmqpLogError(source, "GetHandler", message), this, mechanism);
				throw new AmqpException(AmqpError.NotImplemented, mechanism);
			}
			if (!clone)
			{
				return saslHandler;
			}
			return saslHandler.Clone();
		}

		protected override TransportBase OnCreateTransport(TransportBase innerTransport, bool isInitiator)
		{
			return new SaslTransport(innerTransport, this, isInitiator);
		}

		public override string ToString()
		{
			return "sasl-provider";
		}
	}
}