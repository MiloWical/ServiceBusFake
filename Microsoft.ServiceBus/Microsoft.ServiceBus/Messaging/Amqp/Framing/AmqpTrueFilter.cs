using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class AmqpTrueFilter : AmqpFilter
	{
		public readonly static string Name;

		public readonly static ulong Code;

		static AmqpTrueFilter()
		{
			AmqpTrueFilter.Name = "com.microsoft:true-filter:list";
			AmqpTrueFilter.Code = 83483426823L;
		}

		public AmqpTrueFilter() : base(AmqpTrueFilter.Name, AmqpTrueFilter.Code)
		{
		}

		public override string ToString()
		{
			return "true()";
		}
	}
}