using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus.Messaging
{
	public interface IEventProcessor
	{
		Task CloseAsync(PartitionContext context, CloseReason reason);

		Task OpenAsync(PartitionContext context);

		Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages);
	}
}