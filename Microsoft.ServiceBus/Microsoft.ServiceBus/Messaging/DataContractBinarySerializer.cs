using Microsoft.ServiceBus.Common;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

namespace Microsoft.ServiceBus.Messaging
{
	internal sealed class DataContractBinarySerializer : XmlObjectSerializer
	{
		private readonly DataContractSerializer dataContractSerializer;

		public DataContractBinarySerializer(Type type)
		{
			this.dataContractSerializer = new DataContractSerializer(type);
		}

		public override bool IsStartObject(XmlDictionaryReader reader)
		{
			return this.dataContractSerializer.IsStartObject(reader);
		}

		public override object ReadObject(Stream stream)
		{
			if (stream == null)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("stream");
			}
			return this.ReadObject(XmlDictionaryReader.CreateBinaryReader(stream, XmlDictionaryReaderQuotas.Max));
		}

		public override object ReadObject(XmlDictionaryReader reader, bool verifyObjectName)
		{
			return this.dataContractSerializer.ReadObject(reader, verifyObjectName);
		}

		public override void WriteEndObject(XmlDictionaryWriter writer)
		{
			this.dataContractSerializer.WriteEndObject(writer);
		}

		public override void WriteObject(Stream stream, object graph)
		{
			if (stream == null)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("stream");
			}
			XmlDictionaryWriter xmlDictionaryWriter = XmlDictionaryWriter.CreateBinaryWriter(stream, null, null, false);
			this.WriteObject(xmlDictionaryWriter, graph);
			xmlDictionaryWriter.Flush();
		}

		public override void WriteObject(XmlDictionaryWriter writer, object graph)
		{
			if (writer == null)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("writer");
			}
			this.dataContractSerializer.WriteObject(writer, graph);
		}

		public override void WriteObjectContent(XmlDictionaryWriter writer, object graph)
		{
			this.dataContractSerializer.WriteObjectContent(writer, graph);
		}

		public override void WriteStartObject(XmlDictionaryWriter writer, object graph)
		{
			this.dataContractSerializer.WriteStartObject(writer, graph);
		}
	}
}