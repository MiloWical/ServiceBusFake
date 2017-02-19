using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal abstract class LinkPerformative : Performative
	{
		public uint? Handle
		{
			get;
			set;
		}

		protected LinkPerformative(AmqpSymbol name, ulong code) : base(name, code)
		{
		}
	}
}