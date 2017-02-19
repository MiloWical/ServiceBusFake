using System;
using System.Configuration;

namespace Microsoft.ServiceBus.Configuration
{
	public class WindowsElement : ConfigurationElement
	{
		private ConfigurationPropertyCollection properties;

		[ConfigurationProperty("domain", IsRequired=false)]
		public string Domain
		{
			get
			{
				return (string)base["domain"];
			}
			set
			{
				base["domain"] = value;
			}
		}

		internal bool IsValid
		{
			get
			{
				bool flag;
				if (!this.UseDefaultCredentials || !string.IsNullOrWhiteSpace(this.UserName) || !string.IsNullOrWhiteSpace(this.Password) || !string.IsNullOrWhiteSpace(this.Domain))
				{
					flag = (this.UseDefaultCredentials ? false : !string.IsNullOrWhiteSpace(this.UserName));
				}
				else
				{
					flag = true;
				}
				bool flag1 = flag;
				if (this.StsUris != null && this.StsUris.Count > 0)
				{
					return flag1;
				}
				return false;
			}
		}

		[ConfigurationProperty("password", IsRequired=false)]
		public string Password
		{
			get
			{
				return (string)base["password"];
			}
			set
			{
				base["password"] = value;
			}
		}

		protected override ConfigurationPropertyCollection Properties
		{
			get
			{
				if (this.properties == null)
				{
					ConfigurationPropertyCollection properties = base.Properties;
					properties.Add(new ConfigurationProperty("useDefaultCredentials", typeof(bool), true));
					properties.Add(new ConfigurationProperty("userName", typeof(string)));
					properties.Add(new ConfigurationProperty("password", typeof(string)));
					properties.Add(new ConfigurationProperty("domain", typeof(string)));
					properties.Add(new ConfigurationProperty("stsUris", typeof(StsUriElementCollection)));
					this.properties = properties;
				}
				return this.properties;
			}
		}

		[ConfigurationProperty("stsUris", IsRequired=true, IsDefaultCollection=false)]
		public virtual StsUriElementCollection StsUris
		{
			get
			{
				return (StsUriElementCollection)base["stsUris"];
			}
		}

		[ConfigurationProperty("useDefaultCredentials", IsRequired=false, DefaultValue=true)]
		public bool UseDefaultCredentials
		{
			get
			{
				return (bool)base["useDefaultCredentials"];
			}
			set
			{
				base["useDefaultCredentials"] = value;
			}
		}

		[ConfigurationProperty("userName", IsRequired=false)]
		public string UserName
		{
			get
			{
				return (string)base["userName"];
			}
			set
			{
				base["userName"] = value;
			}
		}

		internal WindowsElement()
		{
		}

		public void CopyFrom(WindowsElement source)
		{
			this.UseDefaultCredentials = source.UseDefaultCredentials;
			this.UserName = source.UserName;
			this.Password = source.Password;
			this.Domain = source.Domain;
		}
	}
}