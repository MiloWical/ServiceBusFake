using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
	internal abstract class TransportSettings
	{
		public int ListenerAcceptorCount
		{
			get;
			set;
		}

		public int ReceiveBufferSize
		{
			get;
			set;
		}

		public int SendBufferSize
		{
			get;
			set;
		}

		protected TransportSettings()
		{
			this.SendBufferSize = 65536;
			this.ReceiveBufferSize = 65536;
		}

		public abstract TransportInitiator CreateInitiator();

		public abstract TransportListener CreateListener();
	}
}