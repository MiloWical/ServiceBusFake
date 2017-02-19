using Microsoft.ServiceBus.Messaging;
using System;
using System.Configuration;
using System.ServiceModel.Configuration;

namespace Microsoft.ServiceBus.Messaging.Configuration
{
	public sealed class NetMessagingBindingCollectionElement : StandardBindingCollectionElement<NetMessagingBinding, NetMessagingBindingExtensionElement>
	{
		public NetMessagingBindingCollectionElement()
		{
		}

		internal static NetMessagingBindingCollectionElement GetBindingCollectionElement()
		{
			return (NetMessagingBindingCollectionElement)((BindingsSection)ConfigurationManager.GetSection("system.serviceModel/bindings"))["netMessagingBinding"];
		}
	}
}