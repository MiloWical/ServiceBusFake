using System;
using System.Collections;
using System.Xml;

namespace Microsoft.ServiceBus.Common.Diagnostics
{
	internal class DictionaryTraceRecord : TraceRecord
	{
		private IDictionary dictionary;

		internal override string EventId
		{
			get
			{
				return "http://schemas.microsoft.com/2006/08/ServiceModel/DictionaryTraceRecord";
			}
		}

		internal DictionaryTraceRecord(IDictionary dictionary)
		{
			this.dictionary = dictionary;
		}

		internal override void WriteTo(XmlWriter xml)
		{
			if (this.dictionary != null)
			{
				foreach (object key in this.dictionary.Keys)
				{
					object item = this.dictionary[key];
					xml.WriteElementString(key.ToString(), (item == null ? string.Empty : item.ToString()));
				}
			}
		}
	}
}