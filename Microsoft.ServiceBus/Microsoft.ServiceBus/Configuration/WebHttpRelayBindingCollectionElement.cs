using Microsoft.ServiceBus;
using System;
using System.Configuration;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.Web.Configuration;
using System.Web.Hosting;

namespace Microsoft.ServiceBus.Configuration
{
	public class WebHttpRelayBindingCollectionElement : StandardBindingCollectionElement<WebHttpRelayBinding, WebHttpRelayBindingElement>
	{
		public WebHttpRelayBindingCollectionElement()
		{
		}

		internal static WebHttpRelayBindingCollectionElement GetBindingCollectionElement()
		{
			BindingsSection section;
			if (!HostingEnvironment.IsHosted)
			{
				section = (BindingsSection)ConfigurationManager.GetSection("system.serviceModel/bindings");
			}
			else
			{
				section = (HostingEnvironment.ApplicationVirtualPath == null ? (BindingsSection)WebConfigurationManager.GetSection("system.serviceModel/bindings") : (BindingsSection)WebConfigurationManager.GetSection("system.serviceModel/bindings", HostingEnvironment.ApplicationVirtualPath));
			}
			return (WebHttpRelayBindingCollectionElement)section["webHttpRelayBinding"];
		}

		protected override Binding GetDefault()
		{
			return new WebHttpRelayBinding();
		}
	}
}