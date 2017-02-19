using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Sbmp;
using System;
using System.Xml;

namespace Microsoft.ServiceBus.Messaging
{
	internal static class BrokeredMessageEncoder
	{
		private static IBrokeredMessageEncoder[] encoders;

		static BrokeredMessageEncoder()
		{
			IBrokeredMessageEncoder[] sbmpMessageEncoder = new IBrokeredMessageEncoder[] { new SbmpMessageEncoder(), new AmqpMessageEncoder(), new BrokeredMessageEncoder.NullMessageEncoder(), new BrokeredMessageEncoder.NullMessageEncoder() };
			BrokeredMessageEncoder.encoders = sbmpMessageEncoder;
		}

		public static IBrokeredMessageEncoder GetEncoder(BrokeredMessageFormat format)
		{
			int num = (int)format;
			if (num >= (int)BrokeredMessageEncoder.encoders.Length)
			{
				throw new NotSupportedException(format.ToString());
			}
			return BrokeredMessageEncoder.encoders[num];
		}

		private sealed class NullMessageEncoder : IBrokeredMessageEncoder
		{
			public NullMessageEncoder()
			{
			}

			public long ReadHeader(XmlReader reader, BrokeredMessage brokeredMessage, SerializationTarget serializationTarget)
			{
				return (long)0;
			}

			public long WriteHeader(XmlWriter writer, BrokeredMessage brokeredMessage, SerializationTarget serializationTarget)
			{
				return (long)0;
			}
		}
	}
}