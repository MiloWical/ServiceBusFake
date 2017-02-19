using Microsoft.ServiceBus;
using System;
using System.ServiceModel.Configuration;

namespace Microsoft.ServiceBus.Configuration
{
	public class NetTcpRelayBindingCollectionElement : StandardBindingCollectionElement<NetTcpRelayBinding, NetTcpRelayBindingElement>
	{
		public NetTcpRelayBindingCollectionElement()
		{
		}

		internal static NetTcpRelayBindingCollectionElement GetBindingCollectionElement()
		{
			return (NetTcpRelayBindingCollectionElement)Microsoft.ServiceBus.Configuration.ConfigurationHelpers.GetBindingCollectionElement("netTcpRelayBinding");
		}
	}
}