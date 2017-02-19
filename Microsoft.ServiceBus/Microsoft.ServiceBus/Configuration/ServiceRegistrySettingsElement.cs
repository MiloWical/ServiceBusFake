using Microsoft.ServiceBus;
using System;
using System.Configuration;
using System.ServiceModel.Configuration;

namespace Microsoft.ServiceBus.Configuration
{
	public class ServiceRegistrySettingsElement : BehaviorExtensionElement
	{
		public override Type BehaviorType
		{
			get
			{
				return typeof(ServiceRegistrySettings);
			}
		}

		[ConfigurationProperty("discoveryMode", DefaultValue=DiscoveryType.Private)]
		public DiscoveryType DiscoveryMode
		{
			get
			{
				return (DiscoveryType)base["discoveryMode"];
			}
			set
			{
				base["discoveryMode"] = value;
			}
		}

		[ConfigurationProperty("displayName")]
		public string DisplayName
		{
			get
			{
				return (string)base["displayName"];
			}
			set
			{
				base["displayName"] = value;
			}
		}

		public ServiceRegistrySettingsElement()
		{
		}

		protected override object CreateBehavior()
		{
			ServiceRegistrySettings serviceRegistrySetting = new ServiceRegistrySettings()
			{
				DiscoveryMode = this.DiscoveryMode,
				DisplayName = this.DisplayName
			};
			return serviceRegistrySetting;
		}
	}
}