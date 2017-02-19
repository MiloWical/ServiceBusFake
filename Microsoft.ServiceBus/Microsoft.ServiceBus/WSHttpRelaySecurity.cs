using Microsoft.ServiceBus.Diagnostics;
using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus
{
	public sealed class WSHttpRelaySecurity
	{
		internal const EndToEndSecurityMode DefaultMode = EndToEndSecurityMode.Transport;

		internal const Microsoft.ServiceBus.RelayClientAuthenticationType DefaultRelayClientAuthenticationType = Microsoft.ServiceBus.RelayClientAuthenticationType.RelayAccessToken;

		private Microsoft.ServiceBus.RelayClientAuthenticationType relayClientAuthenticationType;

		private EndToEndSecurityMode mode;

		private HttpRelayTransportSecurity transportSecurity;

		private NonDualMessageSecurityOverRelayHttp messageSecurity;

		public NonDualMessageSecurityOverRelayHttp Message
		{
			get
			{
				return this.messageSecurity;
			}
		}

		public EndToEndSecurityMode Mode
		{
			get
			{
				return this.mode;
			}
			set
			{
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
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value"));
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

		internal WSHttpRelaySecurity() : this(EndToEndSecurityMode.Transport, Microsoft.ServiceBus.RelayClientAuthenticationType.RelayAccessToken, WSHttpRelaySecurity.GetDefaultHttpTransportSecurity(), new NonDualMessageSecurityOverRelayHttp())
		{
		}

		internal WSHttpRelaySecurity(EndToEndSecurityMode mode, Microsoft.ServiceBus.RelayClientAuthenticationType relayClientAuthenticationType, HttpRelayTransportSecurity transportSecurity, NonDualMessageSecurityOverRelayHttp messageSecurity)
		{
			this.mode = mode;
			this.relayClientAuthenticationType = relayClientAuthenticationType;
			this.transportSecurity = (transportSecurity == null ? WSHttpRelaySecurity.GetDefaultHttpTransportSecurity() : transportSecurity);
			this.messageSecurity = (messageSecurity == null ? new NonDualMessageSecurityOverRelayHttp() : messageSecurity);
		}

		internal static void ApplyTransportSecurity(HttpsRelayTransportBindingElement transport, HttpRelayTransportSecurity transportSecurity)
		{
			HttpRelayTransportSecurity.ConfigureTransportProtectionAndAuthentication(transport, transportSecurity);
		}

		internal void ApplyTransportSecurity(HttpsRelayTransportBindingElement https)
		{
			if (this.mode == EndToEndSecurityMode.TransportWithMessageCredential || this.mode == EndToEndSecurityMode.Transport)
			{
				this.transportSecurity.ConfigureTransportProtectionOnly(https);
			}
		}

		internal SecurityBindingElement CreateMessageSecurity(bool isReliableSessionEnabled, MessageSecurityVersion version)
		{
			if (this.mode != EndToEndSecurityMode.Message && this.mode != EndToEndSecurityMode.TransportWithMessageCredential)
			{
				return null;
			}
			return this.messageSecurity.CreateSecurityBindingElement(this.Mode == EndToEndSecurityMode.TransportWithMessageCredential, isReliableSessionEnabled, version);
		}

		internal static HttpRelayTransportSecurity GetDefaultHttpTransportSecurity()
		{
			return new HttpRelayTransportSecurity();
		}

		internal static bool TryCreate(SecurityBindingElement sbe, Microsoft.ServiceBus.UnifiedSecurityMode mode, HttpRelayTransportSecurity transportSecurity, Microsoft.ServiceBus.RelayClientAuthenticationType relayClientAuthenticationType, bool isReliableSessionEnabled, out WSHttpRelaySecurity security)
		{
			security = null;
			NonDualMessageSecurityOverRelayHttp nonDualMessageSecurityOverRelayHttp = null;
			EndToEndSecurityMode relaySecurityMode = EndToEndSecurityMode.None;
			if (sbe == null)
			{
				mode = mode & (Microsoft.ServiceBus.UnifiedSecurityMode.None | Microsoft.ServiceBus.UnifiedSecurityMode.Transport | Microsoft.ServiceBus.UnifiedSecurityMode.Both);
				relaySecurityMode = EndToEndSecurityModeHelper.ToRelaySecurityMode(mode);
			}
			else
			{
				mode = mode & (Microsoft.ServiceBus.UnifiedSecurityMode.Message | Microsoft.ServiceBus.UnifiedSecurityMode.TransportWithMessageCredential);
				relaySecurityMode = EndToEndSecurityModeHelper.ToRelaySecurityMode(mode);
				if (!MessageSecurityOverRelayHttp.TryCreate<NonDualMessageSecurityOverRelayHttp>(sbe, relaySecurityMode == EndToEndSecurityMode.TransportWithMessageCredential, isReliableSessionEnabled, out nonDualMessageSecurityOverRelayHttp))
				{
					return false;
				}
			}
			security = new WSHttpRelaySecurity(relaySecurityMode, relayClientAuthenticationType, transportSecurity, nonDualMessageSecurityOverRelayHttp);
			return true;
		}
	}
}