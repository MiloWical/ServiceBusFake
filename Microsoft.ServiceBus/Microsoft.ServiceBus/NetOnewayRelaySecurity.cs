using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Configuration;
using Microsoft.ServiceBus.Diagnostics;
using System;
using System.Globalization;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;

namespace Microsoft.ServiceBus
{
	public sealed class NetOnewayRelaySecurity
	{
		internal const EndToEndSecurityMode DefaultMode = EndToEndSecurityMode.Transport;

		internal const Microsoft.ServiceBus.RelayClientAuthenticationType DefaultRelayClientAuthenticationType = Microsoft.ServiceBus.RelayClientAuthenticationType.RelayAccessToken;

		private EndToEndSecurityMode mode;

		private Microsoft.ServiceBus.RelayClientAuthenticationType relayClientAuthenticationType;

		private RelayedOnewayTransportSecurity transportSecurity;

		private MessageSecurityOverRelayOneway messageSecurity;

		public MessageSecurityOverRelayOneway Message
		{
			get
			{
				if (this.messageSecurity == null)
				{
					this.messageSecurity = new MessageSecurityOverRelayOneway();
				}
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
				if (!EndToEndSecurityModeHelper.IsDefined(value))
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

		public RelayedOnewayTransportSecurity Transport
		{
			get
			{
				if (this.transportSecurity == null)
				{
					this.transportSecurity = new RelayedOnewayTransportSecurity();
				}
				return this.transportSecurity;
			}
		}

		internal NetOnewayRelaySecurity() : this(EndToEndSecurityMode.Transport, Microsoft.ServiceBus.RelayClientAuthenticationType.RelayAccessToken, null, null)
		{
		}

		internal NetOnewayRelaySecurity(EndToEndSecurityMode mode, Microsoft.ServiceBus.RelayClientAuthenticationType relayClientAuthenticationType) : this(mode, relayClientAuthenticationType, null, null)
		{
		}

		private NetOnewayRelaySecurity(EndToEndSecurityMode mode, Microsoft.ServiceBus.RelayClientAuthenticationType relayClientAuthenticationType, RelayedOnewayTransportSecurity transportSecurity, MessageSecurityOverRelayOneway messageSecurity)
		{
			bool flag = EndToEndSecurityModeHelper.IsDefined(mode);
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] str = new object[] { mode.ToString() };
			DiagnosticUtility.DebugAssert(flag, string.Format(invariantCulture, "Invalid RelaySecurityMode value: {0}.", str));
			this.mode = mode;
			this.relayClientAuthenticationType = relayClientAuthenticationType;
			this.transportSecurity = (transportSecurity == null ? new RelayedOnewayTransportSecurity() : transportSecurity);
			this.messageSecurity = (messageSecurity == null ? new MessageSecurityOverRelayOneway() : messageSecurity);
		}

		internal void ConfigureTransportSecurity(RelayedOnewayTransportBindingElement oneway)
		{
			if (this.mode != EndToEndSecurityMode.Transport && this.mode != EndToEndSecurityMode.TransportWithMessageCredential)
			{
				oneway.TransportProtectionEnabled = false;
				return;
			}
			oneway.TransportProtectionEnabled = true;
		}

		internal SecurityBindingElement CreateMessageSecurity()
		{
			return this.Message.CreateSecurityBindingElement();
		}

		internal static bool IsConfiguredTransportSecurity(NetOnewayRelayBindingElement oneway, out UnifiedSecurityMode mode)
		{
			if (oneway == null)
			{
				mode = UnifiedSecurityMode.None;
				return false;
			}
			if (oneway.Security.Mode == EndToEndSecurityMode.Transport || oneway.Security.Mode == EndToEndSecurityMode.TransportWithMessageCredential)
			{
				mode = UnifiedSecurityMode.Transport | UnifiedSecurityMode.TransportWithMessageCredential;
			}
			else
			{
				mode = UnifiedSecurityMode.None | UnifiedSecurityMode.Message;
			}
			return true;
		}

		internal static bool TryCreate(SecurityBindingElement sbe, EndToEndSecurityMode mode, Microsoft.ServiceBus.RelayClientAuthenticationType relayClientAuthenticationType, out NetOnewayRelaySecurity security)
		{
			MessageSecurityOverRelayOneway messageSecurityOverRelayOneway;
			security = null;
			if (!MessageSecurityOverRelayOneway.TryCreate(sbe, out messageSecurityOverRelayOneway))
			{
				messageSecurityOverRelayOneway = null;
			}
			security = new NetOnewayRelaySecurity(mode, relayClientAuthenticationType, null, messageSecurityOverRelayOneway);
			if (sbe == null)
			{
				return true;
			}
			Type type = typeof(SecurityElementBase);
			object[] objArray = new object[] { security.CreateMessageSecurity(), sbe, false };
			return (bool)InvokeHelper.InvokeStaticMethod(type, "AreBindingsMatching", objArray);
		}
	}
}