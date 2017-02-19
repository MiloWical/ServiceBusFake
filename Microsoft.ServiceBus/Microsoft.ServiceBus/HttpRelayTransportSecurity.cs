using Microsoft.ServiceBus.Diagnostics;
using System;
using System.Net;
using System.ServiceModel;

namespace Microsoft.ServiceBus
{
	public sealed class HttpRelayTransportSecurity
	{
		internal const HttpProxyCredentialType DefaultProxyCredentialType = HttpProxyCredentialType.None;

		private HttpProxyCredentialType proxyCredentialType;

		public HttpProxyCredentialType ProxyCredentialType
		{
			get
			{
				return this.proxyCredentialType;
			}
			set
			{
				if (!Microsoft.ServiceBus.HttpProxyCredentialTypeHelper.IsDefined(value))
				{
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value"));
				}
				this.proxyCredentialType = value;
			}
		}

		internal HttpRelayTransportSecurity()
		{
			this.proxyCredentialType = HttpProxyCredentialType.None;
		}

		private void ConfigureAuthentication(HttpRelayTransportBindingElement http)
		{
			http.ProxyAuthenticationScheme = Microsoft.ServiceBus.HttpProxyCredentialTypeHelper.MapToAuthenticationScheme(this.proxyCredentialType);
		}

		private static void ConfigureAuthentication(HttpRelayTransportBindingElement http, HttpRelayTransportSecurity transportSecurity)
		{
			transportSecurity.proxyCredentialType = Microsoft.ServiceBus.HttpProxyCredentialTypeHelper.MapToProxyCredentialType(http.ProxyAuthenticationScheme);
		}

		internal void ConfigureTransportAuthentication(HttpRelayTransportBindingElement http)
		{
			this.ConfigureAuthentication(http);
		}

		internal void ConfigureTransportProtectionAndAuthentication(HttpsRelayTransportBindingElement https)
		{
			this.ConfigureAuthentication(https);
		}

		internal static void ConfigureTransportProtectionAndAuthentication(HttpsRelayTransportBindingElement https, HttpRelayTransportSecurity transportSecurity)
		{
			HttpRelayTransportSecurity.ConfigureAuthentication(https, transportSecurity);
		}

		internal void ConfigureTransportProtectionOnly(HttpsRelayTransportBindingElement https)
		{
			HttpRelayTransportSecurity.DisableAuthentication(https);
		}

		private static void DisableAuthentication(HttpRelayTransportBindingElement http)
		{
			http.ProxyAuthenticationScheme = AuthenticationSchemes.Anonymous;
		}

		internal void DisableTransportAuthentication(HttpRelayTransportBindingElement http)
		{
			HttpRelayTransportSecurity.DisableAuthentication(http);
		}

		internal static bool IsConfiguredTransportAuthentication(HttpRelayTransportBindingElement http, HttpRelayTransportSecurity transportSecurity)
		{
			HttpRelayTransportSecurity.ConfigureAuthentication(http, transportSecurity);
			return true;
		}

		private static bool IsDisabledAuthentication(HttpRelayTransportBindingElement http)
		{
			return http.ProxyAuthenticationScheme == AuthenticationSchemes.Anonymous;
		}

		internal static bool IsDisabledTransportAuthentication(HttpRelayTransportBindingElement http)
		{
			return HttpRelayTransportSecurity.IsDisabledAuthentication(http);
		}
	}
}