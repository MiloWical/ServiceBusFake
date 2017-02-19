using System;

namespace Microsoft.ServiceBus.Messaging
{
	[Flags]
	internal enum AuthenticationModes
	{
		Anonymous = 1,
		Windows = 2,
		IssuedToken = 4
	}
}