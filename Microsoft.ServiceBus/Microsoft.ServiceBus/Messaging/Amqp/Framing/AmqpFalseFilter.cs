using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class AmqpFalseFilter : AmqpFilter
	{
		public readonly static string Name;

		public readonly static ulong Code;

		static AmqpFalseFilter()
		{
			AmqpFalseFilter.Name = "com.microsoft:false-filter:list";
			AmqpFalseFilter.Code = 83483426824L;
		}

		public AmqpFalseFilter() : base(AmqpFalseFilter.Name, AmqpFalseFilter.Code)
		{
		}

		public override string ToString()
		{
			return "false()";
		}
	}
}