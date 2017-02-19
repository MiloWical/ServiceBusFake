using System;

namespace Microsoft.ServiceBus.Messaging
{
	public interface IMessageSessionAsyncHandlerFactory
	{
		IMessageSessionAsyncHandler CreateInstance(MessageSession session, BrokeredMessage message);

		void DisposeInstance(IMessageSessionAsyncHandler handler);
	}
}