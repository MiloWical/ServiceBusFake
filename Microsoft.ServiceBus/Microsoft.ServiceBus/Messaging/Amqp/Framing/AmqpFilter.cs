using Microsoft.ServiceBus.Messaging.Amqp;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal abstract class AmqpFilter : DescribedList
	{
		public readonly static string SessionName;

		protected override int FieldCount
		{
			get
			{
				return 0;
			}
		}

		static AmqpFilter()
		{
			AmqpFilter.SessionName = "group-id";
		}

		protected AmqpFilter(string Name, ulong code) : base(Name, code)
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