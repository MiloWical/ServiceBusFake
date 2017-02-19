using Microsoft.ServiceBus.Messaging.Amqp;
using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class AmqpAddRule : Performative
	{
		private const int Fields = 4;

		public readonly static string Name;

		public readonly static ulong Code;

		protected override int FieldCount
		{
			get
			{
				return 4;
			}
		}

		public AmqpRuleDescription RuleDescription
		{
			get;
			set;
		}

		public string RuleName
		{
			get;
			set;
		}

		public uint? Timeout
		{
			get;
			set;
		}

		public string Transactionid
		{
			get;
			set;
		}

		static AmqpAddRule()
		{
			AmqpAddRule.Name = "com.microsoft:add-rule:list";
			AmqpAddRule.Code = 1335734829058L;
		}

		public AmqpAddRule() : base(AmqpAddRule.Name, AmqpAddRule.Code)
		{
		}

		protected override void OnDecode(ByteBuffer buffer, int count)
		{
			int num = count;
			count = num - 1;
			if (num > 0)
			{
				this.RuleName = AmqpCodec.DecodeString(buffer);
			}
			int num1 = count;
			count = num1 - 1;
			if (num1 > 0)
			{
				this.RuleDescription = AmqpCodec.DecodeKnownType<AmqpRuleDescription>(buffer);
			}
			int num2 = count;
			count = num2 - 1;
			if (num2 > 0)
			{
				this.Transactionid = AmqpCodec.DecodeString(buffer);
			}
			int num3 = count;
			count = num3 - 1;
			if (num3 > 0)
			{
				this.Timeout = AmqpCodec.DecodeUInt(buffer);
			}
		}

		protected override void OnEncode(ByteBuffer buffer)
		{
			AmqpCodec.EncodeString(this.RuleName, buffer);
			AmqpCodec.EncodeSerializable(this.RuleDescription, buffer);
			AmqpCodec.EncodeString(this.Transactionid, buffer);
			AmqpCodec.EncodeUInt(this.Timeout, buffer);
		}

		protected override int OnValueSize()
		{
			long? nullable;
			int stringEncodeSize = AmqpCodec.GetStringEncodeSize(this.RuleName);
			stringEncodeSize = stringEncodeSize + AmqpCodec.GetSerializableEncodeSize(this.RuleDescription);
			int num = stringEncodeSize + AmqpCodec.GetStringEncodeSize(this.Transactionid);
			uint? timeout = this.Timeout;
			if (timeout.HasValue)
			{
				nullable = new long?((long)timeout.GetValueOrDefault());
			}
			else
			{
				nullable = null;
			}
			return num + AmqpCodec.GetLongEncodeSize(nullable);
		}
	}
}