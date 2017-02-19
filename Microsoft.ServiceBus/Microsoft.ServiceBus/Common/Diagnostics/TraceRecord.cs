using System;
using System.Xml;

namespace Microsoft.ServiceBus.Common.Diagnostics
{
	[Serializable]
	internal class TraceRecord
	{
		protected const string EventIdBase = "http://schemas.microsoft.com/2006/08/ServiceModel/";

		protected const string NamespaceSuffix = "TraceRecord";

		internal virtual string EventId
		{
			get
			{
				return TraceRecord.BuildEventId("Empty");
			}
		}

		public TraceRecord()
		{
		}

		protected static string BuildEventId(string eventId)
		{
			return string.Concat("http://schemas.microsoft.com/2006/08/ServiceModel/", eventId, "TraceRecord");
		}

		internal virtual void WriteTo(XmlWriter writer)
		{
		}
	}
}