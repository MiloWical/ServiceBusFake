using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus.Messaging
{
	internal interface IMessageSender
	{
		IAsyncResult BeginSend(BrokeredMessage message, AsyncCallback callback, object state);

		IAsyncResult BeginSendBatch(IEnumerable<BrokeredMessage> message, AsyncCallback callback, object state);

		void EndSend(IAsyncResult result);

		void EndSendBatch(IAsyncResult result);

		void Send(BrokeredMessage message);

		Task SendAsync(BrokeredMessage message);

		void SendBatch(IEnumerable<BrokeredMessage> message);
	}
}