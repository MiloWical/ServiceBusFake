using System;

namespace Microsoft.ServiceBus.Messaging
{
	public enum MessageState
	{
		Active,
		Deferred,
		Scheduled
	}
}