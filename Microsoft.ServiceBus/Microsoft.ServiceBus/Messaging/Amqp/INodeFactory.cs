using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal interface INodeFactory
	{
		IAsyncResult BeginCreateNode(string address, Fields properties, TimeSpan timeout, AsyncCallback callback, object state);

		IAsyncResult BeginDeleteNode(string address, TimeSpan timeout, AsyncCallback callback, object state);

		void EndCreateNode(IAsyncResult result);

		void EndDeleteNode(IAsyncResult result);
	}
}