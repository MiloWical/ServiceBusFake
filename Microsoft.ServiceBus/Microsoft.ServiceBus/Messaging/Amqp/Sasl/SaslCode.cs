using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Sasl
{
	internal enum SaslCode : byte
	{
		Ok,
		Auth,
		Sys,
		SysPerm,
		SysTemp
	}
}