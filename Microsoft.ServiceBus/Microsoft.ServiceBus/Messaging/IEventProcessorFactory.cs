namespace Microsoft.ServiceBus.Messaging
{
	public interface IEventProcessorFactory
	{
		IEventProcessor CreateEventProcessor(PartitionContext context);
	}
}