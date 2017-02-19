using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using System;
using System.Configuration;

namespace Microsoft.ServiceBus.Configuration
{
	public sealed class BasicHttpRelaySecurityElement : ConfigurationElement
	{
		private ConfigurationPropertyCollection properties;

		[ConfigurationProperty("message")]
		public BasicHttpRelayMessageSecurityElement Message
		{
			get
			{
				return (BasicHttpRelayMessageSecurityElement)base["message"];
			}
		}

		[ConfigurationProperty("mode", DefaultValue=EndToEndBasicHttpSecurityMode.Transport)]
		[ServiceModelEnumValidator(typeof(EndToEndBasicHttpSecurityModeHelper))]
		public EndToEndBasicHttpSecurityMode Mode
		{
			get
			{
				return (EndToEndBasicHttpSecurityMode)base["mode"];
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
						new ConfigurationProperty("mode", typeof(EndToEndBasicHttpSecurityMode), (object)EndToEndBasicHttpSecurityMode.Transport, null, new ServiceModelEnumValidator(typeof(EndToEndBasicHttpSecurityModeHelper)), ConfigurationPropertyOptions.None),
						new ConfigurationProperty("relayClientAuthenticationType", typeof(Microsoft.ServiceBus.RelayClientAuthenticationType), (object)Microsoft.ServiceBus.RelayClientAuthenticationType.RelayAccessToken, null, new ServiceModelEnumValidator(typeof(RelayClientAuthenticationTypeHelper)), ConfigurationPropertyOptions.None),
						new ConfigurationProperty("transport", typeof(HttpRelayTransportSecurityElement), null, null, null, ConfigurationPropertyOptions.None),
						new ConfigurationProperty("message", typeof(BasicHttpRelayMessageSecurityElement), null, null, null, ConfigurationPropertyOptions.None)
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

		public BasicHttpRelaySecurityElement()
		{
		}

		internal void ApplyConfiguration(BasicHttpRelaySecurity security)
		{
			if (security == null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("security");
			}
			security.Mode = this.Mode;
			security.RelayClientAuthenticationType = this.RelayClientAuthenticationType;
			this.Transport.ApplyConfiguration(security.Transport);
			this.Message.ApplyConfiguration(security.Message);
		}

		internal void InitializeFrom(BasicHttpRelaySecurity security)
		{
			if (security == null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("security");
			}
			this.Mode = security.Mode;
			this.RelayClientAuthenticationType = security.RelayClientAuthenticationType;
			this.Transport.InitializeFrom(security.Transport);
			this.Message.InitializeFrom(security.Message);
		}
	}
}