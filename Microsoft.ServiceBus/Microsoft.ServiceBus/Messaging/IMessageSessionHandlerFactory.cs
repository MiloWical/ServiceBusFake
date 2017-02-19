using System;

namespace Microsoft.ServiceBus.Messaging
{
	public interface IMessageSessionHandlerFactory
	{
		IMessageSessionHandler CreateInstance(MessageSession session, BrokeredMessage message);

		void DisposeInstance(IMessageSessionHandler handler);
	}
}