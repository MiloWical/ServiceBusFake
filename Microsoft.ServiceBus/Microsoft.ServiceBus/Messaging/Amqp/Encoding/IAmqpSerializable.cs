using Microsoft.ServiceBus.Messaging.Amqp;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
	internal interface IAmqpSerializable
	{
		int EncodeSize
		{
			get;
		}

		void Decode(ByteBuffer buffer);

		void Encode(ByteBuffer buffer);
	}
}