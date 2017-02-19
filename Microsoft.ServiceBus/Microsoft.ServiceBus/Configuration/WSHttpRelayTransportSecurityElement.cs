using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using System;
using System.Configuration;
using System.ServiceModel;

namespace Microsoft.ServiceBus.Configuration
{
	public sealed class WSHttpRelayTransportSecurityElement : ConfigurationElement
	{
		private ConfigurationPropertyCollection properties;

		protected override ConfigurationPropertyCollection Properties
		{
			get
			{
				if (this.properties == null)
				{
					ConfigurationPropertyCollection configurationPropertyCollections = new ConfigurationPropertyCollection()
					{
						new ConfigurationProperty("proxyCredentialType", typeof(HttpProxyCredentialType), (object)HttpProxyCredentialType.None, null, null, ConfigurationPropertyOptions.None)
					};
					this.properties = configurationPropertyCollections;
				}
				return this.properties;
			}
		}

		[ConfigurationProperty("proxyCredentialType", DefaultValue=HttpProxyCredentialType.None)]
		[ServiceModelEnumValidator(typeof(Microsoft.ServiceBus.HttpProxyCredentialTypeHelper))]
		public HttpProxyCredentialType ProxyCredentialType
		{
			get
			{
				return (HttpProxyCredentialType)base["proxyCredentialType"];
			}
			set
			{
				base["proxyCredentialType"] = value;
			}
		}

		public WSHttpRelayTransportSecurityElement()
		{
		}

		internal void ApplyConfiguration(HttpRelayTransportSecurity security)
		{
			if (security == null)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("security");
			}
			security.ProxyCredentialType = this.ProxyCredentialType;
		}

		internal void InitializeFrom(HttpRelayTransportSecurity security)
		{
			if (security == null)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("security");
			}
			this.ProxyCredentialType = security.ProxyCredentialType;
		}
	}
}