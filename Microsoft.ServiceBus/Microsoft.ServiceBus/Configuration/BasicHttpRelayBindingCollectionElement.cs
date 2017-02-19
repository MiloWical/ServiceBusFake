using Microsoft.ServiceBus;
using System;
using System.ServiceModel.Configuration;

namespace Microsoft.ServiceBus.Configuration
{
	public class BasicHttpRelayBindingCollectionElement : StandardBindingCollectionElement<BasicHttpRelayBinding, BasicHttpRelayBindingElement>
	{
		public BasicHttpRelayBindingCollectionElement()
		{
		}

		internal static BasicHttpRelayBindingCollectionElement GetBindingCollectionElement()
		{
			return (BasicHttpRelayBindingCollectionElement)Microsoft.ServiceBus.Configuration.ConfigurationHelpers.GetBindingCollectionElement("basicHttpRelayBinding");
		}
	}
}