using Microsoft.ServiceBus;
using System;
using System.Configuration;

namespace Microsoft.ServiceBus.Configuration
{
	public class SharedAccessSignatureElement : ConfigurationElement
	{
		private const int MinKeyNameSize = 0;

		private const int MinKeySize = 0;

		private ConfigurationPropertyCollection properties;

		internal bool IsValid
		{
			get
			{
				if (string.IsNullOrWhiteSpace(this.KeyName))
				{
					return false;
				}
				return !string.IsNullOrWhiteSpace(this.Key);
			}
		}

		[ConfigurationProperty("key", IsRequired=true)]
		[StringValidator(MinLength=0, MaxLength=256)]
		public string Key
		{
			get
			{
				return (string)base["key"];
			}
			set
			{
				base["key"] = value;
			}
		}

		[ConfigurationProperty("keyName", IsRequired=true)]
		[StringValidator(MinLength=0, MaxLength=256)]
		public string KeyName
		{
			get
			{
				return (string)base["keyName"];
			}
			set
			{
				base["keyName"] = value;
			}
		}

		protected override ConfigurationPropertyCollection Properties
		{
			get
			{
				if (this.properties == null)
				{
					ConfigurationPropertyCollection properties = base.Properties;
					properties.Add(new ConfigurationProperty("key", typeof(string), string.Empty, null, new StringValidator(0, 256), ConfigurationPropertyOptions.IsRequired));
					properties.Add(new ConfigurationProperty("keyName", typeof(string), string.Empty, null, new StringValidator(0, 256), ConfigurationPropertyOptions.IsRequired));
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

		internal SharedAccessSignatureElement()
		{
		}

		public void CopyFrom(SharedAccessSignatureElement source)
		{
			this.Key = source.Key;
			this.KeyName = source.KeyName;
			this.TokenScope = source.TokenScope;
		}
	}
}