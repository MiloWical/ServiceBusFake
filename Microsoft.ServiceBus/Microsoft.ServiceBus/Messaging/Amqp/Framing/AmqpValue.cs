using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class AmqpValue : AmqpDescribed
	{
		public readonly static string Name;

		public readonly static ulong Code;

		static AmqpValue()
		{
			AmqpValue.Name = "amqp:amqp-value:*";
			AmqpValue.Code = (ulong)119;
		}

		public AmqpValue() : base(AmqpValue.Name, AmqpValue.Code)
		{
		}

		public override void DecodeValue(ByteBuffer buffer)
		{
			base.Value = AmqpCodec.DecodeObject(buffer);
		}

		public override void EncodeValue(ByteBuffer buffer)
		{
			IAmqpSerializable value = base.Value as IAmqpSerializable;
			if (value != null)
			{
				value.Encode(buffer);
				return;
			}
			base.EncodeValue(buffer);
		}

		public override int GetValueEncodeSize()
		{
			IAmqpSerializable value = base.Value as IAmqpSerializable;
			if (value != null)
			{
				return value.EncodeSize;
			}
			return base.GetValueEncodeSize();
		}

		public override string ToString()
		{
			return "value()";
		}
	}
}