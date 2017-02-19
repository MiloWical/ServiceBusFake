using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using System;
using System.Configuration;

namespace Microsoft.ServiceBus.Configuration
{
	public sealed class WSHttpRelaySecurityElement : ConfigurationElement
	{
		private ConfigurationPropertyCollection properties;

		[ConfigurationProperty("message")]
		public NonDualMessageSecurityOverRelayHttpElement Message
		{
			get
			{
				return (NonDualMessageSecurityOverRelayHttpElement)base["message"];
			}
		}

		[ConfigurationProperty("mode", DefaultValue=EndToEndSecurityMode.Transport)]
		[ServiceModelEnumValidator(typeof(EndToEndSecurityModeHelper))]
		public EndToEndSecurityMode Mode
		{
			get
			{
				return (EndToEndSecurityMode)base["mode"];
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
						new ConfigurationProperty("mode", typeof(EndToEndSecurityMode), (object)EndToEndSecurityMode.Transport, null, new ServiceModelEnumValidator(typeof(EndToEndSecurityModeHelper)), ConfigurationPropertyOptions.None),
						new ConfigurationProperty("relayClientAuthenticationType", typeof(Microsoft.ServiceBus.RelayClientAuthenticationType), (object)Microsoft.ServiceBus.RelayClientAuthenticationType.RelayAccessToken, null, new ServiceModelEnumValidator(typeof(RelayClientAuthenticationTypeHelper)), ConfigurationPropertyOptions.None),
						new ConfigurationProperty("transport", typeof(WSHttpRelayTransportSecurityElement), null, null, null, ConfigurationPropertyOptions.None),
						new ConfigurationProperty("message", typeof(NonDualMessageSecurityOverRelayHttpElement), null, null, null, ConfigurationPropertyOptions.None)
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
		public WSHttpRelayTransportSecurityElement Transport
		{
			get
			{
				return (WSHttpRelayTransportSecurityElement)base["transport"];
			}
		}

		public WSHttpRelaySecurityElement()
		{
		}

		internal void ApplyConfiguration(WSHttpRelaySecurity security)
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

		internal void InitializeFrom(WSHttpRelaySecurity security)
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