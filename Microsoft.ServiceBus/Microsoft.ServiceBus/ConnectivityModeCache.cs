using System;

namespace Microsoft.ServiceBus
{
	internal class ConnectivityModeCache
	{
		private readonly ConnectivitySettings connectivitySettings;

		private readonly HttpConnectivitySettings httpConnectivitySettings;

		public ConnectivityModeCache(ConnectivitySettings connectivitySettings, HttpConnectivitySettings httpConnectivitySettings)
		{
			this.connectivitySettings = connectivitySettings;
			this.httpConnectivitySettings = httpConnectivitySettings;
		}

		internal InternalConnectivityMode GetInternalConnectivityMode(Uri uri)
		{
			return ConnectivityModeHelper.GetInternalConnectivityMode(this.connectivitySettings, this.httpConnectivitySettings, uri);
		}
	}
}