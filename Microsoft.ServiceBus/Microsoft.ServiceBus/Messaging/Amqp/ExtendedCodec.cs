using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal sealed class ExtendedCodec
	{
		public const string SqlFilterType = "sql";

		public const string TrueFilterType = "true";

		public const string FalseFilterType = "false";

		public const string CorrelationFilterType = "correlation";

		private static Dictionary<string, Func<AmqpDescribed>> knownTypesByName;

		private static Dictionary<ulong, Func<AmqpDescribed>> knownTypesByCode;

		static ExtendedCodec()
		{
			Dictionary<string, Func<AmqpDescribed>> strs = new Dictionary<string, Func<AmqpDescribed>>()
			{
				{ AmqpAddRule.Name, new Func<AmqpDescribed>(() => new AmqpAddRule()) },
				{ AmqpDeleteRule.Name, new Func<AmqpDescribed>(() => new AmqpDeleteRule()) },
				{ AmqpRuleDescription.Name, new Func<AmqpDescribed>(() => new AmqpRuleDescription()) },
				{ AmqpSqlFilter.Name, new Func<AmqpDescribed>(() => new AmqpSqlFilter()) },
				{ AmqpTrueFilter.Name, new Func<AmqpDescribed>(() => new AmqpTrueFilter()) },
				{ AmqpFalseFilter.Name, new Func<AmqpDescribed>(() => new AmqpFalseFilter()) },
				{ AmqpCorrelationFilter.Name, new Func<AmqpDescribed>(() => new AmqpCorrelationFilter()) },
				{ AmqpEmptyRuleAction.Name, new Func<AmqpDescribed>(() => new AmqpEmptyRuleAction()) },
				{ AmqpSqlRuleAction.Name, new Func<AmqpDescribed>(() => new AmqpSqlRuleAction()) }
			};
			ExtendedCodec.knownTypesByName = strs;
			Dictionary<ulong, Func<AmqpDescribed>> nums = new Dictionary<ulong, Func<AmqpDescribed>>()
			{
				{ AmqpAddRule.Code, new Func<AmqpDescribed>(() => new AmqpAddRule()) },
				{ AmqpDeleteRule.Code, new Func<AmqpDescribed>(() => new AmqpDeleteRule()) },
				{ AmqpRuleDescription.Code, new Func<AmqpDescribed>(() => new AmqpRuleDescription()) },
				{ AmqpSqlFilter.Code, new Func<AmqpDescribed>(() => new AmqpSqlFilter()) },
				{ AmqpTrueFilter.Code, new Func<AmqpDescribed>(() => new AmqpTrueFilter()) },
				{ AmqpFalseFilter.Code, new Func<AmqpDescribed>(() => new AmqpFalseFilter()) },
				{ AmqpCorrelationFilter.Code, new Func<AmqpDescribed>(() => new AmqpCorrelationFilter()) },
				{ AmqpEmptyRuleAction.Code, new Func<AmqpDescribed>(() => new AmqpEmptyRuleAction()) },
				{ AmqpSqlRuleAction.Code, new Func<AmqpDescribed>(() => new AmqpSqlRuleAction()) }
			};
			ExtendedCodec.knownTypesByCode = nums;
		}

		public ExtendedCodec()
		{
		}

		public static AmqpDescribed DecodeAmqpDescribed(ByteBuffer buffer)
		{
			return AmqpCodec.DecodeAmqpDescribed(buffer, ExtendedCodec.knownTypesByName, ExtendedCodec.knownTypesByCode);
		}
	}
}