using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Diagnostics;
using System;
using System.Globalization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;

namespace Microsoft.ServiceBus
{
	public sealed class NetTcpRelaySecurity
	{
		internal const EndToEndSecurityMode DefaultMode = EndToEndSecurityMode.Transport;

		internal const Microsoft.ServiceBus.RelayClientAuthenticationType DefaultRelayClientAuthenticationType = Microsoft.ServiceBus.RelayClientAuthenticationType.RelayAccessToken;

		private Microsoft.ServiceBus.RelayClientAuthenticationType relayClientAuthenticationType;

		private EndToEndSecurityMode mode;

		private TcpRelayTransportSecurity transportSecurity;

		private MessageSecurityOverRelayConnection messageSecurity;

		public MessageSecurityOverRelayConnection Message
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
				if (!EndToEndSecurityModeHelper.IsDefined(value))
				{
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value"));
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
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value"));
				}
				this.relayClientAuthenticationType = value;
			}
		}

		public TcpRelayTransportSecurity Transport
		{
			get
			{
				return this.transportSecurity;
			}
		}

		internal NetTcpRelaySecurity() : this(EndToEndSecurityMode.Transport, Microsoft.ServiceBus.RelayClientAuthenticationType.RelayAccessToken, new TcpRelayTransportSecurity(), new MessageSecurityOverRelayConnection())
		{
		}

		private NetTcpRelaySecurity(EndToEndSecurityMode mode, Microsoft.ServiceBus.RelayClientAuthenticationType relayClientAuthenticationType, TcpRelayTransportSecurity transportSecurity, MessageSecurityOverRelayConnection messageSecurity)
		{
			bool flag = EndToEndSecurityModeHelper.IsDefined(mode);
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] str = new object[] { mode.ToString() };
			Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DebugAssert(flag, string.Format(invariantCulture, "Invalid RelaySecurityMode value: {0}.", str));
			this.mode = mode;
			this.relayClientAuthenticationType = relayClientAuthenticationType;
			this.transportSecurity = (transportSecurity == null ? new TcpRelayTransportSecurity() : transportSecurity);
			this.messageSecurity = (messageSecurity == null ? new MessageSecurityOverRelayConnection() : messageSecurity);
		}

		internal SecurityBindingElement CreateMessageSecurity(bool isReliableSessionEnabled, MessageSecurityVersion version)
		{
			if (this.mode != EndToEndSecurityMode.Message && this.mode != EndToEndSecurityMode.TransportWithMessageCredential)
			{
				return null;
			}
			return this.messageSecurity.CreateSecurityBindingElement(this.mode == EndToEndSecurityMode.TransportWithMessageCredential, isReliableSessionEnabled);
		}

		internal static bool TryCreate(SecurityBindingElement wsSecurity, EndToEndSecurityMode mode, Microsoft.ServiceBus.RelayClientAuthenticationType relayClientAuthenticationType, bool isReliableSessionEnabled, TcpRelayTransportSecurity tcpTransportSecurity, out NetTcpRelaySecurity security)
		{
			security = null;
			MessageSecurityOverRelayConnection messageSecurityOverRelayConnection = null;
			if ((mode == EndToEndSecurityMode.Message || mode == EndToEndSecurityMode.TransportWithMessageCredential) && !MessageSecurityOverRelayConnection.TryCreate(wsSecurity, isReliableSessionEnabled, out messageSecurityOverRelayConnection))
			{
				return false;
			}
			security = new NetTcpRelaySecurity(mode, relayClientAuthenticationType, tcpTransportSecurity, messageSecurityOverRelayConnection);
			if (wsSecurity == null)
			{
				return true;
			}
			Type type = typeof(SecurityElementBase);
			object[] objArray = new object[] { security.CreateMessageSecurity(isReliableSessionEnabled, wsSecurity.MessageSecurityVersion), wsSecurity, false };
			return (bool)InvokeHelper.InvokeStaticMethod(type, "AreBindingsMatching", objArray);
		}
	}
}