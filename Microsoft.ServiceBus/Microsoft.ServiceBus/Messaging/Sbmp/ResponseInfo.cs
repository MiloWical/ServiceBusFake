using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	internal class ResponseInfo : IXmlSerializable
	{
		public ResponseInfo()
		{
		}

		public XmlSchema GetSchema()
		{
			throw new NotImplementedException();
		}

		public void ReadXml(XmlReader reader)
		{
			throw new NotImplementedException();
		}

		public void WriteXml(XmlWriter writer)
		{
			throw new NotImplementedException();
		}
	}
}