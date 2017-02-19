using System;

namespace Microsoft.ServiceBus
{
	public enum EndToEndBasicHttpSecurityMode
	{
		None,
		Transport,
		Message,
		TransportWithMessageCredential
	}
}