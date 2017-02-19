using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal class DistributionMode
	{
		public readonly static AmqpSymbol Move;

		public readonly static AmqpSymbol Copy;

		static DistributionMode()
		{
			DistributionMode.Move = "move";
			DistributionMode.Copy = "copy";
		}

		private DistributionMode()
		{
		}
	}
}