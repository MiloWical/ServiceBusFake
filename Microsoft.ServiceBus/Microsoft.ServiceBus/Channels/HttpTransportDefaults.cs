using Microsoft.ServiceBus;
using System;
using System.Net;
using System.ServiceModel;

namespace Microsoft.ServiceBus.Channels
{
	internal static class HttpTransportDefaults
	{
		internal const bool AllowCookies = false;

		internal const RelayClientAuthenticationType DefaultRelayClientAuthenticationType = RelayClientAuthenticationType.RelayAccessToken;

		internal const bool BypassProxyOnLocal = false;

		internal const System.ServiceModel.HostNameComparisonMode HostNameComparisonMode = System.ServiceModel.HostNameComparisonMode.StrongWildcard;

		internal const bool KeepAliveEnabled = true;

		internal const Uri ProxyAddress = null;

		internal const AuthenticationSchemes ProxyAuthenticationScheme = AuthenticationSchemes.Anonymous;

		internal const string Realm = "";

		internal const System.ServiceModel.TransferMode TransferMode = System.ServiceModel.TransferMode.Buffered;

		internal const bool UnsafeConnectionNtlmAuthentication = false;

		internal const bool UseDefaultWebProxy = true;

		internal const bool IsDynamic = true;
	}
}