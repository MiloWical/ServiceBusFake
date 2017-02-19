using System;

namespace Microsoft.ServiceBus
{
	[Flags]
	internal enum UnifiedSecurityMode
	{
		None = 1,
		Transport = 4,
		Message = 8,
		Both = 16,
		TransportWithMessageCredential = 32
	}
}