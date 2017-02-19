using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus
{
	public abstract class WSHttpRelayBinding : WSHttpRelayBindingBase
	{
		private readonly static MessageSecurityVersion WSMessageSecurityVersion;

		private WSHttpRelaySecurity security = new WSHttpRelaySecurity();

		public bool AllowCookies
		{
			get
			{
				return base.HttpTransport.AllowCookies;
			}
			set
			{
				base.HttpTransport.AllowCookies = value;
				base.HttpsTransport.AllowCookies = value;
			}
		}

		internal override Microsoft.ServiceBus.RelayClientAuthenticationType RelayClientAuthenticationType
		{
			get
			{
				return this.Security.RelayClientAuthenticationType;
			}
		}

		public WSHttpRelaySecurity Security
		{
			get
			{
				return this.security;
			}
		}

		static WSHttpRelayBinding()
		{
			WSHttpRelayBinding.WSMessageSecurityVersion = MessageSecurityVersion.WSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11BasicSecurityProfile10;
		}

		internal WSHttpRelayBinding()
		{
		}

		internal WSHttpRelayBinding(EndToEndSecurityMode securityMode, Microsoft.ServiceBus.RelayClientAuthenticationType relayClientAuthenticationType, bool reliableSessionEnabled) : base(reliableSessionEnabled)
		{
			this.security.Mode = securityMode;
			this.security.RelayClientAuthenticationType = relayClientAuthenticationType;
		}

		internal WSHttpRelayBinding(WSHttpRelaySecurity security, bool reliableSessionEnabled) : base(reliableSessionEnabled)
		{
			this.security = security ?? new WSHttpRelaySecurity();
		}

		protected override SecurityBindingElement CreateMessageSecurity()
		{
			return this.security.CreateMessageSecurity(base.ReliableSession.Enabled, WSHttpRelayBinding.WSMessageSecurityVersion);
		}

		internal static bool GetSecurityModeFromTransport(TransportBindingElement transport, HttpRelayTransportSecurity transportSecurity, out Microsoft.ServiceBus.UnifiedSecurityMode mode)
		{
			mode = Microsoft.ServiceBus.UnifiedSecurityMode.None;
			if (!(transport is HttpsRelayTransportBindingElement))
			{
				if (!(transport is HttpRelayTransportBindingElement))
				{
					return false;
				}
				mode = Microsoft.ServiceBus.UnifiedSecurityMode.None | Microsoft.ServiceBus.UnifiedSecurityMode.Message;
			}
			else
			{
				mode = Microsoft.ServiceBus.UnifiedSecurityMode.Transport | Microsoft.ServiceBus.UnifiedSecurityMode.TransportWithMessageCredential;
				WSHttpRelaySecurity.ApplyTransportSecurity((HttpsRelayTransportBindingElement)transport, transportSecurity);
			}
			return true;
		}

		protected override TransportBindingElement GetTransport()
		{
			if (this.security.Mode == EndToEndSecurityMode.None || this.security.Mode == EndToEndSecurityMode.Message && this.security.RelayClientAuthenticationType == Microsoft.ServiceBus.RelayClientAuthenticationType.None)
			{
				base.HttpTransport.RelayClientAuthenticationType = this.RelayClientAuthenticationType;
				return base.HttpTransport;
			}
			this.security.ApplyTransportSecurity(base.HttpsTransport);
			base.HttpsTransport.RelayClientAuthenticationType = this.RelayClientAuthenticationType;
			return base.HttpsTransport;
		}

		internal static bool TryGetAllowCookiesFromTransport(TransportBindingElement transport, out bool allowCookies)
		{
			HttpRelayTransportBindingElement httpRelayTransportBindingElement = transport as HttpRelayTransportBindingElement;
			if (httpRelayTransportBindingElement == null)
			{
				allowCookies = false;
				return false;
			}
			allowCookies = httpRelayTransportBindingElement.AllowCookies;
			return true;
		}
	}
}