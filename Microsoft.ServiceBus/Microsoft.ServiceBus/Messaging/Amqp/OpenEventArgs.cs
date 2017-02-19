using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal sealed class OpenEventArgs : EventArgs
	{
		private readonly Performative command;

		public Performative Command
		{
			get
			{
				return this.command;
			}
		}

		public OpenEventArgs(Performative command)
		{
			this.command = command;
		}
	}
}