using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus
{
	internal class HttpConnectivitySettings
	{
		public HttpConnectivityMode Mode
		{
			get;
			private set;
		}

		public HttpConnectivitySettings(HttpConnectivityMode option)
		{
			this.Mode = option;
		}
	}
}