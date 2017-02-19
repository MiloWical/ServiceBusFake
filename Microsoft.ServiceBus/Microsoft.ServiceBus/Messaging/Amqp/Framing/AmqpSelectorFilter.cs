using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class AmqpSelectorFilter : AmqpDescribed
	{
		public readonly static string Name;

		public readonly static ulong Code;

		public string SqlExpression
		{
			get
			{
				return (string)base.Value;
			}
		}

		static AmqpSelectorFilter()
		{
			AmqpSelectorFilter.Name = "apache.org:selector-filter:string";
			AmqpSelectorFilter.Code = 83483426826L;
		}

		public AmqpSelectorFilter(string sqlExpression) : base(AmqpSelectorFilter.Name, AmqpSelectorFilter.Code)
		{
			base.Value = sqlExpression;
		}
	}
}