using System;
using System.Xml;

namespace Microsoft.ServiceBus.Messaging
{
	internal interface IBrokeredMessageEncoder
	{
		long ReadHeader(XmlReader reader, BrokeredMessage brokeredMessage, SerializationTarget serializationTarget);

		long WriteHeader(XmlWriter writer, BrokeredMessage brokeredMessage, SerializationTarget serializationTarget);
	}
}