using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
	internal sealed class WebSocketTransportSettings : TransportSettings
	{
		private readonly Uri uri;

		public WebSocketTransportSettings(Uri uri)
		{
			this.uri = uri;
		}

		public override TransportInitiator CreateInitiator()
		{
			return new WebSocketTransportInitiator(this.uri, this);
		}

		public override TransportListener CreateListener()
		{
			throw new NotImplementedException("WebSocketTransportSettings does not support creating a listener object");
		}
	}
}