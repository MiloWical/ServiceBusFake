using Microsoft.ServiceBus;
using System;
using System.ServiceModel.Configuration;

namespace Microsoft.ServiceBus.Configuration
{
	public class NetEventRelayBindingCollectionElement : StandardBindingCollectionElement<NetEventRelayBinding, NetEventRelayBindingElement>
	{
		public NetEventRelayBindingCollectionElement()
		{
		}

		internal static NetEventRelayBindingCollectionElement GetBindingCollectionElement()
		{
			return (NetEventRelayBindingCollectionElement)Microsoft.ServiceBus.Configuration.ConfigurationHelpers.GetBindingCollectionElement("netEventRelayBinding");
		}
	}
}