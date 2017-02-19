using System;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal enum ProtocolId : byte
	{
		Amqp = 0,
		AmqpTls = 2,
		AmqpSasl = 3
	}
}