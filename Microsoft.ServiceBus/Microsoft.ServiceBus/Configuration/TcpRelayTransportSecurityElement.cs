using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using System;
using System.Configuration;
using System.Net.Security;

namespace Microsoft.ServiceBus.Configuration
{
	public sealed class TcpRelayTransportSecurityElement : ConfigurationElement
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
						new ConfigurationProperty("protectionLevel", typeof(System.Net.Security.ProtectionLevel), (object)System.Net.Security.ProtectionLevel.EncryptAndSign, null, new ServiceModelEnumValidator(typeof(ProtectionLevelHelper)), ConfigurationPropertyOptions.None)
					};
					this.properties = configurationPropertyCollections;
				}
				return this.properties;
			}
		}

		[ConfigurationProperty("protectionLevel", DefaultValue=System.Net.Security.ProtectionLevel.EncryptAndSign)]
		[ServiceModelEnumValidator(typeof(ProtectionLevelHelper))]
		public System.Net.Security.ProtectionLevel ProtectionLevel
		{
			get
			{
				return (System.Net.Security.ProtectionLevel)base["protectionLevel"];
			}
			set
			{
				base["protectionLevel"] = value;
			}
		}

		public TcpRelayTransportSecurityElement()
		{
		}

		internal void ApplyConfiguration(TcpRelayTransportSecurity security)
		{
			if (security == null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("security");
			}
			security.ProtectionLevel = this.ProtectionLevel;
		}

		internal void InitializeFrom(TcpRelayTransportSecurity security)
		{
			if (security == null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("security");
			}
			this.ProtectionLevel = security.ProtectionLevel;
		}
	}
}