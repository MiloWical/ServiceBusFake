using Microsoft.ServiceBus;
using System;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Configuration
{
	public class HttpsRelayTransportElement : HttpRelayTransportElement
	{
		public override Type BindingElementType
		{
			get
			{
				return typeof(HttpsRelayTransportBindingElement);
			}
		}

		public HttpsRelayTransportElement()
		{
		}

		protected override TransportBindingElement CreateDefaultBindingElement()
		{
			return new HttpsRelayTransportBindingElement();
		}
	}
}