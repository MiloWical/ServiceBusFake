using Microsoft.ServiceBus.Messaging.Amqp;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
	internal sealed class AmqpTransportProvider : TransportProvider
	{
		public AmqpTransportProvider()
		{
			base.ProtocolId = Microsoft.ServiceBus.Messaging.Amqp.ProtocolId.Amqp;
		}

		protected override TransportBase OnCreateTransport(TransportBase innerTransport, bool isInitiator)
		{
			return innerTransport;
		}
	}
}