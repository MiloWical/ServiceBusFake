using System;

namespace Microsoft.ServiceBus.Messaging
{
	internal enum DispositionStatus
	{
		Completed = 1,
		Defered = 2,
		Suspended = 3,
		Abandoned = 4,
		Renewed = 5,
		Unlocked = 6
	}
}