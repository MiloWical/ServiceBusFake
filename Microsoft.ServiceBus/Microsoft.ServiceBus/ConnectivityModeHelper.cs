using Microsoft.ServiceBus.Configuration;
using System;

namespace Microsoft.ServiceBus
{
	internal static class ConnectivityModeHelper
	{
		private readonly static InternalConnectivityMode? OverrideInternalConnectivityMode;

		internal static ConnectivityMode SystemConnectivityMode
		{
			get
			{
				if (ServiceBusEnvironment.SystemConnectivity.Mode != ConnectivityMode.AutoDetect || !ConnectivityModeHelper.OverrideInternalConnectivityMode.HasValue)
				{
					return ServiceBusEnvironment.SystemConnectivity.Mode;
				}
				InternalConnectivityMode? overrideInternalConnectivityMode = ConnectivityModeHelper.OverrideInternalConnectivityMode;
				if ((overrideInternalConnectivityMode.GetValueOrDefault() != InternalConnectivityMode.Tcp ? true : !overrideInternalConnectivityMode.HasValue))
				{
					return ConnectivityMode.Http;
				}
				return ConnectivityMode.Tcp;
			}
		}

		static ConnectivityModeHelper()
		{
			ConnectivityModeHelper.OverrideInternalConnectivityMode = ConfigurationHelpers.GetOverrideConnectivityMode();
		}

		internal static InternalConnectivityMode GetInternalConnectivityMode(ConnectivitySettings connectivitySettings, HttpConnectivitySettings httpConnectivitySettings, Uri uri)
		{
			if (connectivitySettings == null && httpConnectivitySettings == null && ServiceBusEnvironment.SystemConnectivity.Mode == ConnectivityMode.AutoDetect && ConnectivityModeHelper.OverrideInternalConnectivityMode.HasValue)
			{
				return ConnectivityModeHelper.OverrideInternalConnectivityMode.Value;
			}
			ConnectivityMode connectivityMode = (connectivitySettings != null ? connectivitySettings.Mode : ServiceBusEnvironment.SystemConnectivity.Mode);
			return ConnectivityModeHelper.GetInternalConnectivityMode(connectivityMode, (httpConnectivitySettings != null ? httpConnectivitySettings.Mode : HttpConnectivityMode.AutoDetect), uri);
		}

		private static InternalConnectivityMode GetInternalConnectivityMode(ConnectivityMode detectMode, HttpConnectivityMode defaultHttpDetectMode, Uri uri)
		{
			if (detectMode == ConnectivityMode.Tcp)
			{
				return InternalConnectivityMode.Tcp;
			}
			if (detectMode != ConnectivityMode.Http || defaultHttpDetectMode == HttpConnectivityMode.AutoDetect)
			{
				if (detectMode != ConnectivityMode.AutoDetect)
				{
					return NetworkDetector.DetectInternalConnectivityModeForHttp(uri);
				}
				return NetworkDetector.DetectInternalConnectivityModeForAutoDetect(uri);
			}
			if (defaultHttpDetectMode == HttpConnectivityMode.Http)
			{
				return InternalConnectivityMode.Http;
			}
			if (defaultHttpDetectMode == HttpConnectivityMode.Https)
			{
				return InternalConnectivityMode.Https;
			}
			return InternalConnectivityMode.HttpsWebSocket;
		}
	}
}