using System;

namespace Microsoft.ServiceBus.Messaging
{
	public class DefaultEventProcessorFactory<T> : IEventProcessorFactory
	where T : IEventProcessor
	{
		private T instance;

		public DefaultEventProcessorFactory()
		{
		}

		public DefaultEventProcessorFactory(T instance)
		{
			this.instance = instance;
		}

		public IEventProcessor CreateEventProcessor(PartitionContext context)
		{
			if (this.instance == null)
			{
				return (object)Activator.CreateInstance<T>();
			}
			return (object)this.instance;
		}
	}
}