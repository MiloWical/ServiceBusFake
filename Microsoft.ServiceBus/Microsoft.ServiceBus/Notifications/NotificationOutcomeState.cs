using System;

namespace Microsoft.ServiceBus.Notifications
{
	public enum NotificationOutcomeState
	{
		Enqueued,
		DetailedStateAvailable,
		Processing,
		Completed,
		Abandoned,
		Unknown
	}
}