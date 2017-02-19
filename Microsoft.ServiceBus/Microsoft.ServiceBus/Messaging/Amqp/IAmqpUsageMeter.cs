using System;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal interface IAmqpUsageMeter
	{
		void OnBytesRead(int numberOfBytes);

		void OnBytesWritten(int numberOfBytes);
	}
}