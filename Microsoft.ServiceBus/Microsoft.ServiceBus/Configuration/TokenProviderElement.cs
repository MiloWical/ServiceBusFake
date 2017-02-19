using Microsoft.ServiceBus;
using System;
using System.Configuration;
using System.Net;
using System.ServiceModel.Configuration;

namespace Microsoft.ServiceBus.Configuration
{
	public class TokenProviderElement : ClientCredentialsElement
	{
		private ConfigurationPropertyCollection properties;

		internal bool IsValid
		{
			get
			{
				if (this.SharedSecret.IsValid || this.WindowsAuthentication.IsValid)
				{
					return true;
				}
				return this.SharedAccessSignature.IsValid;
			}
		}

		[ConfigurationProperty("name", IsRequired=false, IsKey=true)]
		[StringValidator(MinLength=0, InvalidCharacters=" ")]
		public string Name
		{
			get
			{
				return (string)base["name"];
			}
			set
			{
				base["name"] = value;
			}
		}

		protected override ConfigurationPropertyCollection Properties
		{
			get
			{
				if (this.properties == null)
				{
					ConfigurationPropertyCollection properties = base.Properties;
					properties.Add(new ConfigurationProperty("name", typeof(string), null, null, new StringValidator(1, 2147483647, " "), ConfigurationPropertyOptions.IsKey));
					properties.Add(new ConfigurationProperty("sharedSecret", typeof(SharedSecretElement), null, null, null, ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("windowsAuthentication", typeof(WindowsElement), null, null, null, ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("sharedAccessSignature", typeof(SharedAccessSignatureElement), null, null, null, ConfigurationPropertyOptions.None));
					this.properties = properties;
				}
				return this.properties;
			}
		}

		[ConfigurationProperty("sharedAccessSignature")]
		public SharedAccessSignatureElement SharedAccessSignature
		{
			get
			{
				return (SharedAccessSignatureElement)base["sharedAccessSignature"];
			}
		}

		[ConfigurationProperty("sharedSecret")]
		public SharedSecretElement SharedSecret
		{
			get
			{
				return (SharedSecretElement)base["sharedSecret"];
			}
		}

		[ConfigurationProperty("windowsAuthentication")]
		public WindowsElement WindowsAuthentication
		{
			get
			{
				return (WindowsElement)base["windowsAuthentication"];
			}
		}

		internal TokenProviderElement()
		{
		}

		public override void CopyFrom(ServiceModelExtensionElement from)
		{
			TokenProviderElement tokenProviderElement = (TokenProviderElement)from;
			base.CopyFrom(from);
			this.SharedSecret.CopyFrom(tokenProviderElement.SharedSecret);
			this.WindowsAuthentication.CopyFrom(tokenProviderElement.WindowsAuthentication);
			this.SharedAccessSignature.CopyFrom(tokenProviderElement.SharedAccessSignature);
			this.Name = tokenProviderElement.Name;
		}

		internal TokenProvider CreateTokenProvider()
		{
			if (this.WindowsAuthentication != null && this.WindowsAuthentication.IsValid)
			{
				if (this.WindowsAuthentication.UseDefaultCredentials)
				{
					return TokenProvider.CreateWindowsTokenProvider(this.WindowsAuthentication.StsUris.Addresses);
				}
				return TokenProvider.CreateWindowsTokenProvider(this.WindowsAuthentication.StsUris.Addresses, new NetworkCredential(this.WindowsAuthentication.UserName, this.WindowsAuthentication.Password, this.WindowsAuthentication.Domain));
			}
			if (this.SharedAccessSignature != null && this.SharedAccessSignature.IsValid)
			{
				return TokenProvider.CreateSharedAccessSignatureTokenProvider(this.SharedAccessSignature.KeyName, this.SharedAccessSignature.Key, this.SharedAccessSignature.TokenScope);
			}
			return TokenProvider.CreateSharedSecretTokenProvider(this.SharedSecret.IssuerName, this.SharedSecret.IssuerSecret, this.SharedSecret.TokenScope);
		}
	}
}