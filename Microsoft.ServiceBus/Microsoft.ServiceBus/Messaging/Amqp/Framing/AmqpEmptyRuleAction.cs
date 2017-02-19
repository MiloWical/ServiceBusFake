using Microsoft.ServiceBus.Messaging.Amqp;
using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class AmqpEmptyRuleAction : AmqpRuleAction
	{
		private const int Fields = 0;

		public readonly static string Name;

		public readonly static ulong Code;

		protected override int FieldCount
		{
			get
			{
				return 0;
			}
		}

		public string RuleName
		{
			get;
			set;
		}

		static AmqpEmptyRuleAction()
		{
			AmqpEmptyRuleAction.Name = "sb:empty-rule-action:list";
			AmqpEmptyRuleAction.Code = 1335734829061L;
		}

		public AmqpEmptyRuleAction() : base(AmqpEmptyRuleAction.Name, AmqpEmptyRuleAction.Code)
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