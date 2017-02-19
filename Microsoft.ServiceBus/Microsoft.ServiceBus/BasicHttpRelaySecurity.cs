using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Diagnostics;
using System;
using System.Globalization;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;

namespace Microsoft.ServiceBus
{
	public sealed class BasicHttpRelaySecurity
	{
		internal const EndToEndBasicHttpSecurityMode DefaultMode = EndToEndBasicHttpSecurityMode.Transport;

		internal const Microsoft.ServiceBus.RelayClientAuthenticationType DefaultRelayClientAuthenticationType = Microsoft.ServiceBus.RelayClientAuthenticationType.RelayAccessToken;

		private Microsoft.ServiceBus.RelayClientAuthenticationType relayClientAuthenticationType;

		private EndToEndBasicHttpSecurityMode mode;

		private HttpRelayTransportSecurity transportSecurity;

		private BasicHttpRelayMessageSecurity messageSecurity;

		public BasicHttpRelayMessageSecurity Message
		{
			get
			{
				return this.messageSecurity;
			}
		}

		public EndToEndBasicHttpSecurityMode Mode
		{
			get
			{
				return this.mode;
			}
			set
			{
				if (!EndToEndBasicHttpSecurityModeHelper.IsDefined(value))
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

		internal BasicHttpRelaySecurity() : this(EndToEndBasicHttpSecurityMode.Transport, Microsoft.ServiceBus.RelayClientAuthenticationType.RelayAccessToken, new HttpRelayTransportSecurity(), new BasicHttpRelayMessageSecurity())
		{
		}

		private BasicHttpRelaySecurity(EndToEndBasicHttpSecurityMode mode, Microsoft.ServiceBus.RelayClientAuthenticationType relayClientAuthenticationType, HttpRelayTransportSecurity transportSecurity, BasicHttpRelayMessageSecurity messageSecurity)
		{
			bool flag = EndToEndBasicHttpSecurityModeHelper.IsDefined(mode);
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] str = new object[] { mode.ToString() };
			DiagnosticUtility.DebugAssert(flag, string.Format(invariantCulture, "Invalid BasicHttpSecurityMode value: {0}.", str));
			this.Mode = mode;
			this.RelayClientAuthenticationType = relayClientAuthenticationType;
			this.transportSecurity = (transportSecurity == null ? new HttpRelayTransportSecurity() : transportSecurity);
			this.messageSecurity = (messageSecurity == null ? new BasicHttpRelayMessageSecurity() : messageSecurity);
		}

		internal SecurityBindingElement CreateMessageSecurity()
		{
			if (this.mode != EndToEndBasicHttpSecurityMode.Message && this.mode != EndToEndBasicHttpSecurityMode.TransportWithMessageCredential)
			{
				return null;
			}
			return this.messageSecurity.CreateMessageSecurity(this.Mode == EndToEndBasicHttpSecurityMode.TransportWithMessageCredential);
		}

		internal void DisableTransportAuthentication(HttpRelayTransportBindingElement http)
		{
			this.transportSecurity.DisableTransportAuthentication(http);
		}

		internal void EnableTransportAuthentication(HttpRelayTransportBindingElement http)
		{
			this.transportSecurity.ConfigureTransportAuthentication(http);
		}

		internal static void EnableTransportSecurity(HttpsRelayTransportBindingElement https, HttpRelayTransportSecurity transportSecurity)
		{
			HttpRelayTransportSecurity.ConfigureTransportProtectionAndAuthentication(https, transportSecurity);
		}

		internal void EnableTransportSecurity(HttpsRelayTransportBindingElement https)
		{
			if (this.mode == EndToEndBasicHttpSecurityMode.TransportWithMessageCredential)
			{
				this.transportSecurity.ConfigureTransportProtectionOnly(https);
				return;
			}
			this.transportSecurity.ConfigureTransportProtectionAndAuthentication(https);
		}

		internal static bool IsEnabledTransportAuthentication(HttpRelayTransportBindingElement http, HttpRelayTransportSecurity transportSecurity)
		{
			return HttpRelayTransportSecurity.IsConfiguredTransportAuthentication(http, transportSecurity);
		}

		internal static bool TryCreate(SecurityBindingElement sbe, Microsoft.ServiceBus.RelayClientAuthenticationType relayClientAuthenticationType, UnifiedSecurityMode mode, HttpRelayTransportSecurity transportSecurity, out BasicHttpRelaySecurity security)
		{
			bool flag;
			security = null;
			BasicHttpRelayMessageSecurity basicHttpRelayMessageSecurity = null;
			if (sbe == null)
			{
				mode = mode & (UnifiedSecurityMode.None | UnifiedSecurityMode.Transport | UnifiedSecurityMode.Both);
			}
			else
			{
				mode = mode & (UnifiedSecurityMode.Message | UnifiedSecurityMode.TransportWithMessageCredential);
				if (!BasicHttpRelayMessageSecurity.TryCreate(sbe, out basicHttpRelayMessageSecurity, out flag))
				{
					return false;
				}
			}
			EndToEndBasicHttpSecurityMode endToEndBasicHttpSecurityMode = EndToEndBasicHttpSecurityModeHelper.ToEndToEndBasicHttpSecurityMode(mode);
			bool flag1 = EndToEndBasicHttpSecurityModeHelper.IsDefined(endToEndBasicHttpSecurityMode);
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] str = new object[] { endToEndBasicHttpSecurityMode.ToString() };
			DiagnosticUtility.DebugAssert(flag1, string.Format(invariantCulture, "Invalid BasicHttpSecurityMode value: {0}.", str));
			security = new BasicHttpRelaySecurity(endToEndBasicHttpSecurityMode, relayClientAuthenticationType, transportSecurity, basicHttpRelayMessageSecurity);
			if (sbe == null)
			{
				return true;
			}
			Type type = typeof(SecurityElementBase);
			object[] objArray = new object[] { security.CreateMessageSecurity(), sbe };
			return (bool)InvokeHelper.InvokeStaticMethod(type, "AreBindingsMatching", objArray);
		}
	}
}