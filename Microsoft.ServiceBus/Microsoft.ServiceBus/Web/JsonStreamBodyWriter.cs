using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.ServiceModel.Channels;
using System.Xml;

namespace Microsoft.ServiceBus.Web
{
	internal class JsonStreamBodyWriter : BodyWriter
	{
		private Stream jsonStream;

		public JsonStreamBodyWriter(Stream jsonStream) : base(false)
		{
			this.jsonStream = jsonStream;
		}

		protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
		{
			writer.WriteNode(JsonReaderWriterFactory.CreateJsonReader(this.jsonStream, XmlDictionaryReaderQuotas.Max), false);
			writer.Flush();
		}
	}
}