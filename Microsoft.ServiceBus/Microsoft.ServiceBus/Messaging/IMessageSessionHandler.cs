using System;

namespace Microsoft.ServiceBus.Messaging
{
	public interface IMessageSessionHandler
	{
		void OnCloseSession(MessageSession session);

		void OnMessage(MessageSession session, BrokeredMessage message);

		void OnSessionLost(Exception exception);
	}
}