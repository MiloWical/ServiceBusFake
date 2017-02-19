using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal enum TerminusDurability : uint
	{
		None,
		Configuration,
		UnsettledState
	}
}