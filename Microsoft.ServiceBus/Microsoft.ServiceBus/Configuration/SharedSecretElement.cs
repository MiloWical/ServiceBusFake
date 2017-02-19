using Microsoft.ServiceBus;
using System;
using System.Configuration;

namespace Microsoft.ServiceBus.Configuration
{
	public class SharedSecretElement : ConfigurationElement
	{
		private const int MinIssuerNameSize = 0;

		private const int MinIssuerSecretSize = 0;

		private ConfigurationPropertyCollection properties;

		[ConfigurationProperty("issuerName", IsRequired=true)]
		[StringValidator(MinLength=0, MaxLength=128)]
		public string IssuerName
		{
			get
			{
				return (string)base["issuerName"];
			}
			set
			{
				base["issuerName"] = value;
			}
		}

		[ConfigurationProperty("issuerSecret", IsRequired=true)]
		[StringValidator(MinLength=0, MaxLength=128)]
		public string IssuerSecret
		{
			get
			{
				return (string)base["issuerSecret"];
			}
			set
			{
				base["issuerSecret"] = value;
			}
		}

		internal bool IsValid
		{
			get
			{
				if (string.IsNullOrWhiteSpace(this.IssuerName))
				{
					return false;
				}
				return !string.IsNullOrWhiteSpace(this.IssuerSecret);
			}
		}

		protected override ConfigurationPropertyCollection Properties
		{
			get
			{
				if (this.properties == null)
				{
					ConfigurationPropertyCollection properties = base.Properties;
					properties.Add(new ConfigurationProperty("issuerName", typeof(string), string.Empty, null, new StringValidator(0, 128), ConfigurationPropertyOptions.IsRequired));
					properties.Add(new ConfigurationProperty("issuerSecret", typeof(string), string.Empty, null, new StringValidator(0, 128), ConfigurationPropertyOptions.IsRequired));
					properties.Add(new ConfigurationProperty("tokenScope", typeof(Microsoft.ServiceBus.TokenScope), (object)Microsoft.ServiceBus.TokenScope.Entity, null, new ServiceModelEnumValidator(typeof(TokenScopeHelper)), ConfigurationPropertyOptions.None));
					this.properties = properties;
				}
				return this.properties;
			}
		}

		[ConfigurationProperty("tokenScope", IsRequired=false, DefaultValue=Microsoft.ServiceBus.TokenScope.Entity)]
		[ServiceModelEnumValidator(typeof(TokenScopeHelper))]
		public Microsoft.ServiceBus.TokenScope TokenScope
		{
			get
			{
				return (Microsoft.ServiceBus.TokenScope)base["tokenScope"];
			}
			set
			{
				base["tokenScope"] = value;
			}
		}

		internal SharedSecretElement()
		{
		}

		public void CopyFrom(SharedSecretElement source)
		{
			this.IssuerName = source.IssuerName;
			this.IssuerSecret = source.IssuerSecret;
			this.TokenScope = source.TokenScope;
		}
	}
}