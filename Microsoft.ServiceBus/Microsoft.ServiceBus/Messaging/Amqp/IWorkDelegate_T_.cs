using System;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal interface IWorkDelegate<T>
	{
		bool Invoke(T work);
	}
}