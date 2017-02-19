using Microsoft.ServiceBus.Diagnostics;
using System;

namespace Microsoft.ServiceBus
{
	public sealed class WebHttpRelaySecurity
	{
		internal const EndToEndWebHttpSecurityMode DefaultMode = EndToEndWebHttpSecurityMode.Transport;

		internal const Microsoft.ServiceBus.RelayClientAuthenticationType DefaultRelayClientAuthenticationType = Microsoft.ServiceBus.RelayClientAuthenticationType.RelayAccessToken;

		private Microsoft.ServiceBus.RelayClientAuthenticationType relayClientAuthenticationType;

		private EndToEndWebHttpSecurityMode mode;

		private HttpRelayTransportSecurity transportSecurity;

		public EndToEndWebHttpSecurityMode Mode
		{
			get
			{
				return this.mode;
			}
			set
			{
				if (!EndToEndWebHttpSecurityModeHelper.IsDefined(value))
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value"));
				}
				this.mode = value;
			}
		}

		public Microsoft.ServiceBus.RelayClientAuthenticationType RelayClientAuthenticationType
		{
			get
			{
				return this.relayClientAuthenticationType;
			}
			set
			{
				if (!RelayClientAuthenticationTypeHelper.IsDefined(value))
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value"));
				}
				this.relayClientAuthenticationType = value;
			}
		}

		public HttpRelayTransportSecurity Transport
		{
			get
			{
				return this.transportSecurity;
			}
		}

		internal WebHttpRelaySecurity() : this(EndToEndWebHttpSecurityMode.Transport, Microsoft.ServiceBus.RelayClientAuthenticationType.RelayAccessToken, new HttpRelayTransportSecurity())
		{
		}

		private WebHttpRelaySecurity(EndToEndWebHttpSecurityMode mode, Microsoft.ServiceBus.RelayClientAuthenticationType relayClientAuthenticationType, HttpRelayTransportSecurity transportSecurity)
		{
			this.Mode = mode;
			this.RelayClientAuthenticationType = relayClientAuthenticationType;
			this.transportSecurity = (transportSecurity == null ? new HttpRelayTransportSecurity() : transportSecurity);
		}

		internal void DisableTransportAuthentication(HttpRelayTransportBindingElement http)
		{
			this.transportSecurity.DisableTransportAuthentication(http);
		}

		internal void EnableTransportAuthentication(HttpRelayTransportBindingElement http)
		{
			this.transportSecurity.ConfigureTransportAuthentication(http);
		}

		internal void EnableTransportSecurity(HttpsRelayTransportBindingElement https)
		{
			this.transportSecurity.ConfigureTransportProtectionAndAuthentication(https);
		}
	}
}