using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.PerformanceCounters
{
	internal sealed class ClientPerformanceCounterScope
	{
		public ClientPerformanceCounterDetail Detail
		{
			get;
			internal set;
		}

		public ClientPerformanceCounterLevel Level
		{
			get;
			internal set;
		}

		public ClientPerformanceCounterScope() : this(ClientPerformanceCounterLevel.Endpoint)
		{
		}

		public ClientPerformanceCounterScope(ClientPerformanceCounterLevel level) : this(level, ClientPerformanceCounterDetail.Verbose)
		{
		}

		public ClientPerformanceCounterScope(ClientPerformanceCounterLevel level, ClientPerformanceCounterDetail detail)
		{
			this.Level = level;
			this.Detail = detail;
		}

		public static bool IsValid(ClientPerformanceCounterLevel level, ClientPerformanceCounterDetail detail)
		{
			bool flag = Enum.IsDefined(typeof(ClientPerformanceCounterLevel), level);
			bool flag1 = Enum.IsDefined(typeof(ClientPerformanceCounterDetail), detail);
			if (flag)
			{
				return flag1;
			}
			return false;
		}
	}
}