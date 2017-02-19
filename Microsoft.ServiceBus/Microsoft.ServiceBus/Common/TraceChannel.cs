using System;

namespace Microsoft.ServiceBus.Common
{
	internal enum TraceChannel
	{
		Application = 9,
		Admin = 16,
		Operational = 17,
		Analytic = 18,
		Debug = 19,
		Perf = 20
	}
}