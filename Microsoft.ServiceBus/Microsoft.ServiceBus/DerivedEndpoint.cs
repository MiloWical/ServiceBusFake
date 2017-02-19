using System;
using System.Net;

namespace Microsoft.ServiceBus
{
	internal class DerivedEndpoint
	{
		public IPEndPoint ExternalEndpoint;

		public IPEndPoint LocalEndpoint;

		public DerivedEndpoint()
		{
		}

		public DerivedEndpoint(IPEndPoint localEndpoint, IPEndPoint externalEndpoint)
		{
			this.LocalEndpoint = localEndpoint;
			this.ExternalEndpoint = externalEndpoint;
		}
	}
}