using System;

namespace Microsoft.ServiceBus.Messaging
{
	internal interface IPairedNamespaceFactory
	{
		IAsyncResult BeginClose(TimeSpan timeout, AsyncCallback callback, object state);

		IAsyncResult BeginStart(MessagingFactory primary, TimeSpan timeout, AsyncCallback callback, object state);

		MessageSender CreateMessageSender(MessageSender primary);

		void EndClose(IAsyncResult result);

		void EndStart(IAsyncResult result);
	}
}