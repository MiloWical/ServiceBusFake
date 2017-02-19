using System;

namespace Microsoft.ServiceBus.Messaging
{
	internal enum BrokeredMessageState
	{
		Active,
		Acknowledged,
		Deferred,
		Abandoned
	}
}