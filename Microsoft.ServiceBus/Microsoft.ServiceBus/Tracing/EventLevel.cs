using System;

namespace Microsoft.ServiceBus.Tracing
{
	public enum EventLevel : byte
	{
		LogAlways,
		Critical,
		Error,
		Warning,
		Informational,
		Verbose
	}
}