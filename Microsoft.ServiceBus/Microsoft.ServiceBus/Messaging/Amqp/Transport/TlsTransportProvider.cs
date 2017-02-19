using Microsoft.ServiceBus.Messaging.Amqp;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
	internal sealed class TlsTransportProvider : TransportProvider
	{
		private TlsTransportSettings tlsSettings;

		public TlsTransportSettings Settings
		{
			get
			{
				return this.tlsSettings;
			}
		}

		public TlsTransportProvider(TlsTransportSettings tlsSettings)
		{
			this.tlsSettings = tlsSettings;
			base.ProtocolId = Microsoft.ServiceBus.Messaging.Amqp.ProtocolId.AmqpTls;
		}

		protected override TransportBase OnCreateTransport(TransportBase innerTransport, bool isInitiator)
		{
			return new TlsTransport(innerTransport, this.tlsSettings);
		}

		public override string ToString()
		{
			return "tls-provider";
		}
	}
}