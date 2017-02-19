using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal class TerminusExpiryPolicy
	{
		public readonly static AmqpSymbol LinkDetach;

		public readonly static AmqpSymbol SessionEnd;

		public readonly static AmqpSymbol ConnectionClose;

		public readonly static AmqpSymbol Never;

		static TerminusExpiryPolicy()
		{
			TerminusExpiryPolicy.LinkDetach = "link-detach";
			TerminusExpiryPolicy.SessionEnd = "session-end";
			TerminusExpiryPolicy.ConnectionClose = "connection-close";
			TerminusExpiryPolicy.Never = "never";
		}

		private TerminusExpiryPolicy()
		{
		}
	}
}