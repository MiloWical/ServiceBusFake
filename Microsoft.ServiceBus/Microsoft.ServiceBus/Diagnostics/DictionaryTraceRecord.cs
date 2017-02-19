using Microsoft.ServiceBus.Common.Diagnostics;
using System;
using System.Collections;
using System.Xml;

namespace Microsoft.ServiceBus.Diagnostics
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
					xml.WriteElementString(key.ToString(), (this.dictionary[key] == null ? string.Empty : this.dictionary[key].ToString()));
				}
			}
		}
	}
}