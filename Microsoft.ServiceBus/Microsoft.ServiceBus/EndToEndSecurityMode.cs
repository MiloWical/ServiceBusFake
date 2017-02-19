using System;

namespace Microsoft.ServiceBus
{
	public enum EndToEndSecurityMode
	{
		None,
		Transport,
		Message,
		TransportWithMessageCredential
	}
}