using System;

namespace Microsoft.ServiceBus.Messaging
{
	internal enum SubqueueType
	{
		Active = 0,
		DeadLettered = 3,
		Scheduled = 4
	}
}