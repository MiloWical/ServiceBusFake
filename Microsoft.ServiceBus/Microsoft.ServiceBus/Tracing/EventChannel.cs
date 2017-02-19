using System;

namespace Microsoft.ServiceBus.Tracing
{
	public enum EventChannel : byte
	{
		Default = 0,
		Application = 1,
		Security = 2,
		Setup = 3,
		System = 4,
		Reserved = 15
	}
}