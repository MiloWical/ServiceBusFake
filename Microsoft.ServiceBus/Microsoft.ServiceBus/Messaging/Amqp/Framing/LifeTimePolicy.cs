using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal abstract class LifeTimePolicy : DescribedList
	{
		private const int Fields = 0;

		protected override int FieldCount
		{
			get
			{
				return 0;
			}
		}

		protected LifeTimePolicy(AmqpSymbol name, ulong code) : base(name, code)
		{
		}

		protected override void OnDecode(ByteBuffer buffer, int count)
		{
		}

		protected override void OnEncode(ByteBuffer buffer)
		{
		}

		protected override int OnValueSize()
		{
			return 0;
		}
	}
}