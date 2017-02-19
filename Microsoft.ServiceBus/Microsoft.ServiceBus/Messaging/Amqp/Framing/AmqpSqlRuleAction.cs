using Microsoft.ServiceBus.Messaging.Amqp;
using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class AmqpSqlRuleAction : AmqpRuleAction
	{
		private const int Fields = 0;

		public readonly static string Name;

		public readonly static ulong Code;

		public int? CompatibilityLevel
		{
			get;
			set;
		}

		protected override int FieldCount
		{
			get
			{
				return 0;
			}
		}

		public string SqlExpression
		{
			get;
			set;
		}

		static AmqpSqlRuleAction()
		{
			AmqpSqlRuleAction.Name = "com.microsoft:sql-rule-action:list";
			AmqpSqlRuleAction.Code = 1335734829062L;
		}

		public AmqpSqlRuleAction() : base(AmqpSqlRuleAction.Name, AmqpSqlRuleAction.Code)
		{
		}

		protected override void OnDecode(ByteBuffer buffer, int count)
		{
			int num = count;
			count = num - 1;
			if (num > 0)
			{
				this.SqlExpression = AmqpCodec.DecodeString(buffer);
				this.CompatibilityLevel = AmqpCodec.DecodeInt(buffer);
			}
		}

		protected override void OnEncode(ByteBuffer buffer)
		{
			AmqpCodec.EncodeString(this.SqlExpression, buffer);
			AmqpCodec.EncodeInt(this.CompatibilityLevel, buffer);
		}

		protected override int OnValueSize()
		{
			int stringEncodeSize = 0;
			stringEncodeSize = AmqpCodec.GetStringEncodeSize(this.SqlExpression);
			return stringEncodeSize + AmqpCodec.GetIntEncodeSize(this.CompatibilityLevel);
		}
	}
}