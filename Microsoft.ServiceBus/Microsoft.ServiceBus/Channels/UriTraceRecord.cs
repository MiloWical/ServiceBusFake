using Microsoft.ServiceBus.Common.Diagnostics;
using Microsoft.ServiceBus.Diagnostics;
using System;
using System.Xml;

namespace Microsoft.ServiceBus.Channels
{
	internal class UriTraceRecord : TraceRecord
	{
		private Uri uri;

		public UriTraceRecord(Uri uri)
		{
			DiagnosticUtility.DebugAssert(uri != null, "UriTraceRecord: Uri is null");
			this.uri = uri;
		}

		internal override void WriteTo(XmlWriter xml)
		{
			xml.WriteElementString("Uri", this.uri.AbsoluteUri);
		}
	}
}