using System;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus.Messaging
{
	public interface IMessageSessionAsyncHandler
	{
		Task OnCloseSessionAsync(MessageSession session);

		Task OnMessageAsync(MessageSession session, BrokeredMessage message);

		Task OnSessionLostAsync(Exception exception);
	}
}