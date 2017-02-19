using System;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal interface IWork<TOutcome>
	{
		void Cancel(bool completedSynchronously, Exception exception);

		void Done(bool completedSynchronously, TOutcome outcome);

		void Start();
	}
}