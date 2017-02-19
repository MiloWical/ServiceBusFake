using System;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal interface IIoHandler
	{
		void OnIoFault(Exception exception);

		void OnReceiveBuffer(ByteBuffer buffer);
	}
}