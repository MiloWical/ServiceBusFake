using System;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus.Messaging
{
	public abstract class MessageSessionAsyncHandler : IMessageSessionAsyncHandler
	{
		protected MessageSessionAsyncHandler()
		{
		}

		Task Microsoft.ServiceBus.Messaging.IMessageSessionAsyncHandler.OnCloseSessionAsync(MessageSession session)
		{
			return this.OnCloseSessionAsync(session);
		}

		Task Microsoft.ServiceBus.Messaging.IMessageSessionAsyncHandler.OnMessageAsync(MessageSession session, BrokeredMessage message)
		{
			return this.OnMessageAsync(session, message);
		}

		Task Microsoft.ServiceBus.Messaging.IMessageSessionAsyncHandler.OnSessionLostAsync(Exception exception)
		{
			return this.OnSessionLostAsync(exception);
		}

		protected virtual Task OnCloseSessionAsync(MessageSession session)
		{
			return CompletedTask.Default;
		}

		protected abstract Task OnMessageAsync(MessageSession session, BrokeredMessage message);

		protected virtual Task OnSessionLostAsync(Exception exception)
		{
			return CompletedTask.Default;
		}
	}
}