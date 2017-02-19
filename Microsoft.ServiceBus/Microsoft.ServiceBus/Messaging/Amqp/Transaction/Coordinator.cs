using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Amqp.Transaction
{
	internal sealed class Coordinator : DescribedList
	{
		private const int Fields = 1;

		public readonly static string Name;

		public readonly static ulong Code;

		public Multiple<AmqpSymbol> Capabilities
		{
			get;
			set;
		}

		protected override int FieldCount
		{
			get
			{
				return 1;
			}
		}

		static Coordinator()
		{
			Coordinator.Name = "amqp:coordinator:list";
			Coordinator.Code = (ulong)48;
		}

		public Coordinator() : base(Coordinator.Name, Coordinator.Code)
		{
		}

		protected override void EnsureRequired()
		{
		}

		protected override void OnDecode(ByteBuffer buffer, int count)
		{
			int num = count;
			count = num - 1;
			if (num > 0)
			{
				this.Capabilities = AmqpCodec.DecodeMultiple<AmqpSymbol>(buffer);
			}
		}

		protected override void OnEncode(ByteBuffer buffer)
		{
			AmqpCodec.EncodeMultiple<AmqpSymbol>(this.Capabilities, buffer);
		}

		protected override int OnValueSize()
		{
			return 0 + AmqpCodec.GetMultipleEncodeSize<AmqpSymbol>(this.Capabilities);
		}
	}
}