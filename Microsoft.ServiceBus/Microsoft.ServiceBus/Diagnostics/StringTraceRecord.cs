using Microsoft.ServiceBus.Common.Diagnostics;
using System;
using System.Xml;

namespace Microsoft.ServiceBus.Diagnostics
{
	internal class StringTraceRecord : TraceRecord
	{
		private string elementName;

		private string content;

		internal override string EventId
		{
			get
			{
				return "http://schemas.microsoft.com/2006/08/ServiceModel/StringTraceRecord";
			}
		}

		internal StringTraceRecord(string elementName, string content)
		{
			this.elementName = elementName;
			this.content = content;
		}

		internal override void WriteTo(XmlWriter writer)
		{
			writer.WriteElementString(this.elementName, this.content);
		}
	}
}