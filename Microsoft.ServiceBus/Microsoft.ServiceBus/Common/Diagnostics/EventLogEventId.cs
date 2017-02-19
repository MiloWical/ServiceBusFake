using System;

namespace Microsoft.ServiceBus.Common.Diagnostics
{
	internal enum EventLogEventId : uint
	{
		FailedToSetupTracing = 3221291108,
		FailedToInitializeTraceSource = 3221291109,
		FailFast = 3221291110,
		FailFastException = 3221291111,
		FailedToTraceEvent = 3221291112,
		FailedToTraceEventWithException = 3221291113
	}
}