using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Configuration
{
	[ConfigurationCollection(typeof(StsUriElement), AddItemName="stsUri", CollectionType=ConfigurationElementCollectionType.BasicMap)]
	public sealed class StsUriElementCollection : ConfigurationElementCollection
	{
		private volatile List<Uri> stsUris;

		private readonly object syncLock;

		public IEnumerable<Uri> Addresses
		{
			get
			{
				if (this.stsUris == null)
				{
					lock (this.syncLock)
					{
						if (this.stsUris == null)
						{
							this.stsUris = (
								from StsUriElement stsUri in this
								select stsUri.Value).ToList<Uri>();
						}
					}
				}
				return this.stsUris;
			}
		}

		protected override string ElementName
		{
			get
			{
				return string.Empty;
			}
		}

		public StsUriElementCollection()
		{
			this.syncLock = new object();
		}

		protected override ConfigurationElement CreateNewElement()
		{
			return new StsUriElement();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			return ((StsUriElement)element).Value;
		}

		protected override bool IsElementName(string elementName)
		{
			return elementName == "stsUri";
		}
	}
}