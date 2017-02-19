using System;
using System.Configuration;

namespace Microsoft.ServiceBus.Configuration
{
	public class IssuerElement : ConfigurationElement
	{
		private ConfigurationPropertyCollection properties;

		[ConfigurationProperty("issuerAddress", DefaultValue="")]
		[StringValidator(MinLength=0, MaxLength=2048)]
		public string Address
		{
			get
			{
				return (string)base["issuerAddress"];
			}
			set
			{
				base["issuerAddress"] = value;
			}
		}

		protected override ConfigurationPropertyCollection Properties
		{
			get
			{
				if (this.properties == null)
				{
					ConfigurationPropertyCollection properties = base.Properties;
					properties.Add(new ConfigurationProperty("address", typeof(string), string.Empty, null, new StringValidator(0, 2048), ConfigurationPropertyOptions.None));
					this.properties = properties;
				}
				return this.properties;
			}
		}

		internal IssuerElement()
		{
		}

		public void CopyFrom(ConfigurationElement from)
		{
			this.Address = ((IssuerElement)from).Address;
		}
	}
}