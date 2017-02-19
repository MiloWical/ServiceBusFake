using Microsoft.ServiceBus;
using System;
using System.ServiceModel.Configuration;

namespace Microsoft.ServiceBus.Configuration
{
	public class WS2007HttpRelayBindingCollectionElement : StandardBindingCollectionElement<WS2007HttpRelayBinding, WS2007HttpRelayBindingElement>
	{
		public WS2007HttpRelayBindingCollectionElement()
		{
		}

		internal static WS2007HttpRelayBindingCollectionElement GetBindingCollectionElement()
		{
			return (WS2007HttpRelayBindingCollectionElement)Microsoft.ServiceBus.Configuration.ConfigurationHelpers.GetBindingCollectionElement("ws2007HttpRelayBinding");
		}
	}
}