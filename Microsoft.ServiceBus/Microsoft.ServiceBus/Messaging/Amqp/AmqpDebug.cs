using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using System;
using System.Diagnostics;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal static class AmqpDebug
	{
		[Conditional("AMQP_DEBUG")]
		public static void Dump(object source)
		{
		}

		[Conditional("AMQP_DEBUG")]
		public static void Log(object source, bool send, Performative command)
		{
		}

		[Conditional("AMQP_DEBUG")]
		public static void Log(object source, bool send, ulong code, uint p1, uint p2)
		{
		}
	}
}