using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
	internal sealed class TcpTransportSettings : TransportSettings
	{
		private const int DefaultTcpBacklog = 200;

		private const int DefaultTcpAcceptorCount = 1;

		public string Host
		{
			get;
			set;
		}

		public int Port
		{
			get;
			set;
		}

		public int TcpBacklog
		{
			get;
			set;
		}

		public TcpTransportSettings()
		{
			this.TcpBacklog = 200;
			base.ListenerAcceptorCount = 1;
		}

		public override TransportInitiator CreateInitiator()
		{
			return new TcpTransportInitiator(this);
		}

		public override TransportListener CreateListener()
		{
			return new TcpTransportListener(this);
		}

		public override string ToString()
		{
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] host = new object[] { this.Host, this.Port };
			return string.Format(invariantCulture, "{0}:{1}", host);
		}
	}
}