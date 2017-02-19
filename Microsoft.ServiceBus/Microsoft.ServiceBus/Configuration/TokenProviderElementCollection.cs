using Microsoft.ServiceBus.Common;
using System;
using System.Configuration;
using System.Reflection;

namespace Microsoft.ServiceBus.Configuration
{
	[ConfigurationCollection(typeof(TokenProviderElement), CollectionType=ConfigurationElementCollectionType.AddRemoveClearMap)]
	public sealed class TokenProviderElementCollection : ConfigurationElementCollection
	{
		public new TokenProviderElement this[string name]
		{
			get
			{
				return (TokenProviderElement)base.BaseGet(name);
			}
		}

		public TokenProviderElementCollection()
		{
		}

		protected override ConfigurationElement CreateNewElement()
		{
			return new TokenProviderElement();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			TokenProviderElement tokenProviderElement = (TokenProviderElement)element;
			if (tokenProviderElement == null || string.IsNullOrEmpty(tokenProviderElement.Name))
			{
				throw new ConfigurationErrorsException(SRCore.NullOrEmptyConfigurationAttribute("name", "tokenProvider"));
			}
			return tokenProviderElement.Name;
		}
	}
}