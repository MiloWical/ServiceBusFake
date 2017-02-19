using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using System;
using System.Configuration;

namespace Microsoft.ServiceBus.Configuration
{
	public sealed class WebHttpRelaySecurityElement : ConfigurationElement
	{
		private ConfigurationPropertyCollection properties;

		[ConfigurationProperty("mode", DefaultValue=EndToEndWebHttpSecurityMode.Transport)]
		[ServiceModelEnumValidator(typeof(EndToEndWebHttpSecurityModeHelper))]
		public EndToEndWebHttpSecurityMode Mode
		{
			get
			{
				return (EndToEndWebHttpSecurityMode)base["mode"];
			}
			set
			{
				base["mode"] = value;
			}
		}

		protected override ConfigurationPropertyCollection Properties
		{
			get
			{
				if (this.properties == null)
				{
					ConfigurationPropertyCollection configurationPropertyCollections = new ConfigurationPropertyCollection()
					{
						new ConfigurationProperty("mode", typeof(EndToEndWebHttpSecurityMode), (object)EndToEndWebHttpSecurityMode.Transport, null, new ServiceModelEnumValidator(typeof(EndToEndWebHttpSecurityModeHelper)), ConfigurationPropertyOptions.None),
						new ConfigurationProperty("relayClientAuthenticationType", typeof(Microsoft.ServiceBus.RelayClientAuthenticationType), (object)Microsoft.ServiceBus.RelayClientAuthenticationType.RelayAccessToken, null, new ServiceModelEnumValidator(typeof(RelayClientAuthenticationTypeHelper)), ConfigurationPropertyOptions.None),
						new ConfigurationProperty("transport", typeof(HttpRelayTransportSecurityElement), null, null, null, ConfigurationPropertyOptions.None)
					};
					this.properties = configurationPropertyCollections;
				}
				return this.properties;
			}
		}

		[ConfigurationProperty("relayClientAuthenticationType")]
		[ServiceModelEnumValidator(typeof(RelayClientAuthenticationTypeHelper))]
		public Microsoft.ServiceBus.RelayClientAuthenticationType RelayClientAuthenticationType
		{
			get
			{
				return (Microsoft.ServiceBus.RelayClientAuthenticationType)base["relayClientAuthenticationType"];
			}
			set
			{
				base["relayClientAuthenticationType"] = value;
			}
		}

		[ConfigurationProperty("transport")]
		public HttpRelayTransportSecurityElement Transport
		{
			get
			{
				return (HttpRelayTransportSecurityElement)base["transport"];
			}
		}

		public WebHttpRelaySecurityElement()
		{
		}

		internal void ApplyConfiguration(WebHttpRelaySecurity security)
		{
			if (security == null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("security");
			}
			security.Mode = this.Mode;
			security.RelayClientAuthenticationType = this.RelayClientAuthenticationType;
			this.Transport.ApplyConfiguration(security.Transport);
		}

		internal void InitializeFrom(WebHttpRelaySecurity security)
		{
			if (security == null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("security");
			}
			this.Mode = security.Mode;
			this.RelayClientAuthenticationType = security.RelayClientAuthenticationType;
			this.InitializeTransportSecurity(security.Transport);
		}

		private void InitializeTransportSecurity(HttpRelayTransportSecurity security)
		{
			if (security == null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("security");
			}
			this.Transport.ProxyCredentialType = security.ProxyCredentialType;
		}
	}
}