using System;
using System.ServiceModel.Channels;
using System.Xml;

namespace Microsoft.ServiceBus.Web
{
	internal class DelegateBodyWriter : BodyWriter
	{
		private StreamWriterDelegate writer;

		public DelegateBodyWriter(StreamWriterDelegate writer) : base(false)
		{
			this.writer = writer;
		}

		protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
		{
			writer.WriteStartElement("Binary");
			using (XmlWriterStream xmlWriterStream = new XmlWriterStream(writer))
			{
				this.writer(xmlWriterStream);
			}
			writer.WriteEndElement();
		}
	}
}