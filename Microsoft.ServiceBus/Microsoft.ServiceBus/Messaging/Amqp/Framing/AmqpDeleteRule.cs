using Microsoft.ServiceBus.Messaging.Amqp;
using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class AmqpDeleteRule : Performative
	{
		private const int Fields = 3;

		public readonly static string Name;

		public readonly static ulong Code;

		protected override int FieldCount
		{
			get
			{
				return 3;
			}
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

		static AmqpDeleteRule()
		{
			AmqpDeleteRule.Name = "com.microsoft:delete-rule:list";
			AmqpDeleteRule.Code = 1335734829059L;
		}

		public AmqpDeleteRule() : base(AmqpDeleteRule.Name, AmqpDeleteRule.Code)
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
				this.Transactionid = AmqpCodec.DecodeString(buffer);
			}
			int num2 = count;
			count = num2 - 1;
			if (num2 > 0)
			{
				this.Timeout = AmqpCodec.DecodeUInt(buffer);
			}
		}

		protected override void OnEncode(ByteBuffer buffer)
		{
			AmqpCodec.EncodeString(this.RuleName, buffer);
			AmqpCodec.EncodeString(this.Transactionid, buffer);
			AmqpCodec.EncodeUInt(this.Timeout, buffer);
		}

		protected override int OnValueSize()
		{
			int stringEncodeSize = AmqpCodec.GetStringEncodeSize(this.RuleName);
			stringEncodeSize = stringEncodeSize + AmqpCodec.GetStringEncodeSize(this.Transactionid);
			return stringEncodeSize + AmqpCodec.GetUIntEncodeSize(this.Timeout);
		}
	}
}