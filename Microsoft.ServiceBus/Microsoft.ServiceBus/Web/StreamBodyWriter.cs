using System;
using System.IO;
using System.ServiceModel.Channels;
using System.Xml;

namespace Microsoft.ServiceBus.Web
{
	internal class StreamBodyWriter : BodyWriter
	{
		private readonly Stream stream;

		public StreamBodyWriter(Stream stream) : base(false)
		{
			this.stream = stream;
		}

		protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
		{
			writer.WriteStartElement("Binary");
			writer.WriteValue(new Microsoft.ServiceBus.Web.StreamBodyWriter.StreamProvider(this.stream));
			writer.WriteEndElement();
		}

		private class StreamProvider : IStreamProvider
		{
			private readonly Stream stream;

			public StreamProvider(Stream stream)
			{
				this.stream = stream;
			}

			public Stream GetStream()
			{
				return this.stream;
			}

			public void ReleaseStream(Stream stream)
			{
			}
		}
	}
}