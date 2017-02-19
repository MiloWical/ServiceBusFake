using System;

namespace Microsoft.ServiceBus.Messaging
{
	public abstract class MessageSessionHandler : IMessageSessionHandler
	{
		protected MessageSessionHandler()
		{
		}

		void Microsoft.ServiceBus.Messaging.IMessageSessionHandler.OnCloseSession(MessageSession session)
		{
			this.OnCloseSession(session);
		}

		void Microsoft.ServiceBus.Messaging.IMessageSessionHandler.OnMessage(MessageSession session, BrokeredMessage message)
		{
			this.OnMessage(session, message);
		}

		void Microsoft.ServiceBus.Messaging.IMessageSessionHandler.OnSessionLost(Exception exception)
		{
			this.OnSessionLost(exception);
		}

		protected virtual void OnCloseSession(MessageSession session)
		{
		}

		protected abstract void OnMessage(MessageSession session, BrokeredMessage message);

		protected virtual void OnSessionLost(Exception exception)
		{
		}
	}
}