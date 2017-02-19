using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal abstract class DescribedMap : AmqpDescribed
	{
		private AmqpMap innerMap;

		protected AmqpMap InnerMap
		{
			get
			{
				return this.innerMap;
			}
		}

		public DescribedMap(AmqpSymbol name, ulong code) : base(name, code)
		{
			this.innerMap = new AmqpMap();
		}

		public override void DecodeValue(ByteBuffer buffer)
		{
			this.innerMap = MapEncoding.Decode(buffer, 0);
		}

		public void DecodeValue(ByteBuffer buffer, int size, int count)
		{
			MapEncoding.ReadMapValue(buffer, this.innerMap, size, count);
		}

		public override void EncodeValue(ByteBuffer buffer)
		{
			MapEncoding.Encode(this.innerMap, buffer);
		}

		public override int GetValueEncodeSize()
		{
			return MapEncoding.GetEncodeSize(this.innerMap);
		}
	}
}