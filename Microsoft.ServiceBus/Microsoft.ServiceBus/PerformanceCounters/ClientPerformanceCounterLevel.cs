using System;

namespace Microsoft.ServiceBus.PerformanceCounters
{
	internal enum ClientPerformanceCounterLevel
	{
		Off = 0,
		Default = 1,
		Endpoint = 1,
		All = 3
	}
}