using System;

namespace Microsoft.ServiceBus.Channels
{
	internal enum FramingMode
	{
		Singleton = 1,
		Duplex = 2,
		Simplex = 3,
		SingletonSized = 4
	}
}