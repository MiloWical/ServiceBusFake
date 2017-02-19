using Microsoft.ServiceBus.Messaging.Amqp;
using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class AmqpRuleDescription : DescribedList
	{
		private const int Fields = 2;

		public readonly static string Name;

		public readonly static ulong Code;

		public AmqpRuleAction Action
		{
			get;
			set;
		}

		protected override int FieldCount
		{
			get
			{
				return 2;
			}
		}

		public AmqpFilter Filter
		{
			get;
			set;
		}

		static AmqpRuleDescription()
		{
			AmqpRuleDescription.Name = "com.microsoft:rule-description:list";
			AmqpRuleDescription.Code = 1335734829060L;
		}

		public AmqpRuleDescription() : base(AmqpRuleDescription.Name, AmqpRuleDescription.Code)
		{
		}

		protected override void OnDecode(ByteBuffer buffer, int count)
		{
			int num = count;
			count = num - 1;
			if (num > 0)
			{
				this.Filter = (AmqpFilter)ExtendedCodec.DecodeAmqpDescribed(buffer);
			}
			int num1 = count;
			count = num1 - 1;
			if (num1 > 0)
			{
				this.Action = (AmqpRuleAction)ExtendedCodec.DecodeAmqpDescribed(buffer);
			}
		}

		protected override void OnEncode(ByteBuffer buffer)
		{
			AmqpCodec.EncodeSerializable(this.Filter, buffer);
			AmqpCodec.EncodeSerializable(this.Action, buffer);
		}

		protected override int OnValueSize()
		{
			int serializableEncodeSize = AmqpCodec.GetSerializableEncodeSize(this.Filter);
			return serializableEncodeSize + AmqpCodec.GetSerializableEncodeSize(this.Action);
		}
	}
}