using Microsoft.ServiceBus.Messaging;
using System;
using System.Xml;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	internal class SbmpMessageEncoder : IBrokeredMessageEncoder
	{
		public SbmpMessageEncoder()
		{
		}

		public long ReadHeader(XmlReader reader, BrokeredMessage brokeredMessage, SerializationTarget serializationTarget)
		{
			long num = (long)0 + BrokeredMessage.DeserializeHeadersFromBinary(brokeredMessage, reader);
			return num + BrokeredMessage.DeserializePropertiesFromBinary(brokeredMessage, reader);
		}

		public long WriteHeader(XmlWriter writer, BrokeredMessage brokeredMessage, SerializationTarget serializationTarget)
		{
			long num = (long)0;
			num = num + BrokeredMessage.SerializeHeadersAsBinary(brokeredMessage, writer, serializationTarget);
			return num + BrokeredMessage.SerializePropertiesAsBinary(brokeredMessage, writer);
		}
	}
}