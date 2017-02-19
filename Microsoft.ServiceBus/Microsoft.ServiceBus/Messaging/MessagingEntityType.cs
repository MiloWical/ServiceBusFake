using System;

namespace Microsoft.ServiceBus.Messaging
{
	internal enum MessagingEntityType
	{
		Queue,
		Topic,
		Subscriber,
		Filter,
		Namespace,
		VolatileTopic,
		VolatileTopicSubscription,
		EventHub,
		ConsumerGroup,
		Partition,
		Checkpoint
	}
}