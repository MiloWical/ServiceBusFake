using System;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal enum SettleMode : byte
	{
		SettleOnSend,
		SettleOnReceive,
		SettleOnDispose
	}
}