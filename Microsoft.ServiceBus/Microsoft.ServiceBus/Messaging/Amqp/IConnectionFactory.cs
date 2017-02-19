using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using Microsoft.ServiceBus.Messaging.Amqp.Transport;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal interface IConnectionFactory
	{
		AmqpConnection CreateConnection(TransportBase transport, ProtocolHeader protocolHeader, bool isInitiator, AmqpSettings amqpSettings, AmqpConnectionSettings connectionSettings);
	}
}