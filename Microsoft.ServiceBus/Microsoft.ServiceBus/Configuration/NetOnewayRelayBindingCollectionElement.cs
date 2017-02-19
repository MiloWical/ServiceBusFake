using Microsoft.ServiceBus;
using System;
using System.ServiceModel.Configuration;

namespace Microsoft.ServiceBus.Configuration
{
	public class NetOnewayRelayBindingCollectionElement : StandardBindingCollectionElement<NetOnewayRelayBinding, NetOnewayRelayBindingElement>
	{
		public NetOnewayRelayBindingCollectionElement()
		{
		}

		internal static NetOnewayRelayBindingCollectionElement GetBindingCollectionElement()
		{
			return (NetOnewayRelayBindingCollectionElement)Microsoft.ServiceBus.Configuration.ConfigurationHelpers.GetBindingCollectionElement("netOnewayRelayBinding");
		}
	}
}