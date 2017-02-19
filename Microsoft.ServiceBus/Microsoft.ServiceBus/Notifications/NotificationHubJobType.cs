using System;

namespace Microsoft.ServiceBus.Notifications
{
	public enum NotificationHubJobType
	{
		ExportRegistrations,
		ImportCreateRegistrations,
		ImportUpdateRegistrations,
		ImportDeleteRegistrations
	}
}