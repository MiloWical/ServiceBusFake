using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Configuration
{
	[ConfigurationCollection(typeof(AddressConfigurationElement), AddItemName="address", CollectionType=ConfigurationElementCollectionType.BasicMap)]
	public sealed class AddressConfigurationElementCollection : ConfigurationElementCollection
	{
		private readonly object syncLock;

		private volatile List<Uri> addresses;

		public IEnumerable<Uri> Addresses
		{
			get
			{
				if (this.addresses == null)
				{
					lock (this.syncLock)
					{
						if (this.addresses == null)
						{
							this.addresses = (
								from AddressConfigurationElement address in this
								select address.Value).ToList<Uri>();
						}
					}
				}
				return this.addresses;
			}
		}

		protected override string ElementName
		{
			get
			{
				return string.Empty;
			}
		}

		public AddressConfigurationElementCollection()
		{
			this.syncLock = new object();
		}

		protected override ConfigurationElement CreateNewElement()
		{
			return new AddressConfigurationElement();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			return ((AddressConfigurationElement)element).Value;
		}

		protected override bool IsElementName(string elementName)
		{
			return elementName == "address";
		}
	}
}