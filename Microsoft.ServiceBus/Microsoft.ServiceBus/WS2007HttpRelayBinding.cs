using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Configuration;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;

namespace Microsoft.ServiceBus
{
	public class WS2007HttpRelayBinding : WSHttpRelayBinding
	{
		private readonly static ReliableMessagingVersion WS2007ReliableMessagingVersion;

		private readonly static MessageSecurityVersion WS2007MessageSecurityVersion;

		static WS2007HttpRelayBinding()
		{
			WS2007HttpRelayBinding.WS2007ReliableMessagingVersion = ReliableMessagingVersion.WSReliableMessaging11;
			WS2007HttpRelayBinding.WS2007MessageSecurityVersion = MessageSecurityVersion.WSSecurity11WSTrust13WSSecureConversation13WSSecurityPolicy12BasicSecurityProfile10;
		}

		public WS2007HttpRelayBinding(string configName) : this()
		{
			this.ApplyConfiguration(configName);
		}

		public WS2007HttpRelayBinding()
		{
			base.ReliableSessionBindingElement.ReliableMessagingVersion = WS2007HttpRelayBinding.WS2007ReliableMessagingVersion;
			base.HttpsTransport.MessageSecurityVersion = WS2007HttpRelayBinding.WS2007MessageSecurityVersion;
		}

		public WS2007HttpRelayBinding(EndToEndSecurityMode securityMode, Microsoft.ServiceBus.RelayClientAuthenticationType relayClientAuthenticationType) : this(securityMode, relayClientAuthenticationType, false)
		{
		}

		public WS2007HttpRelayBinding(EndToEndSecurityMode securityMode, Microsoft.ServiceBus.RelayClientAuthenticationType relayClientAuthenticationType, bool reliableSessionEnabled) : base(securityMode, relayClientAuthenticationType, reliableSessionEnabled)
		{
			base.ReliableSessionBindingElement.ReliableMessagingVersion = WS2007HttpRelayBinding.WS2007ReliableMessagingVersion;
			base.HttpsTransport.MessageSecurityVersion = WS2007HttpRelayBinding.WS2007MessageSecurityVersion;
		}

		internal WS2007HttpRelayBinding(WSHttpRelaySecurity security, bool reliableSessionEnabled) : base(security, reliableSessionEnabled)
		{
			base.ReliableSessionBindingElement.ReliableMessagingVersion = WS2007HttpRelayBinding.WS2007ReliableMessagingVersion;
			base.HttpsTransport.MessageSecurityVersion = WS2007HttpRelayBinding.WS2007MessageSecurityVersion;
		}

		private void ApplyConfiguration(string configurationName)
		{
			WS2007HttpRelayBindingElement item = WS2007HttpRelayBindingCollectionElement.GetBindingCollectionElement().Bindings[configurationName];
			if (item == null)
			{
				ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
				string configInvalidBindingConfigurationName = Resources.ConfigInvalidBindingConfigurationName;
				object[] objArray = new object[] { configurationName, "ws2007HttpRelayBinding" };
				throw exceptionUtility.ThrowHelperError(new ConfigurationErrorsException(Microsoft.ServiceBus.SR.GetString(configInvalidBindingConfigurationName, objArray)));
			}
			item.ApplyConfiguration(this);
		}

		private static bool AreBindingsMatching(SecurityBindingElement sbe1, SecurityBindingElement sbe2)
		{
			Type type = typeof(SecurityElementBase);
			object[] objArray = new object[] { sbe1, sbe2 };
			return (bool)InvokeHelper.InvokeStaticMethod(type, "AreBindingsMatching", objArray);
		}

		protected override SecurityBindingElement CreateMessageSecurity()
		{
			return base.Security.CreateMessageSecurity(base.ReliableSession.Enabled, WS2007HttpRelayBinding.WS2007MessageSecurityVersion);
		}

		internal static bool TryCreate(SecurityBindingElement sbe, Microsoft.ServiceBus.RelayClientAuthenticationType relayClientAuthenticationType, TransportBindingElement transport, System.ServiceModel.Channels.ReliableSessionBindingElement rsbe, out Binding binding)
		{
			Microsoft.ServiceBus.UnifiedSecurityMode unifiedSecurityMode;
			WSHttpRelaySecurity wSHttpRelaySecurity;
			bool flag;
			bool flag1 = rsbe != null;
			binding = null;
			HttpRelayTransportSecurity defaultHttpTransportSecurity = WSHttpRelaySecurity.GetDefaultHttpTransportSecurity();
			if (!WSHttpRelayBinding.GetSecurityModeFromTransport(transport, defaultHttpTransportSecurity, out unifiedSecurityMode))
			{
				return false;
			}
			HttpsRelayTransportBindingElement httpsRelayTransportBindingElement = transport as HttpsRelayTransportBindingElement;
			if (httpsRelayTransportBindingElement != null && httpsRelayTransportBindingElement.MessageSecurityVersion != null && httpsRelayTransportBindingElement.MessageSecurityVersion.SecurityPolicyVersion != WS2007HttpRelayBinding.WS2007MessageSecurityVersion.SecurityPolicyVersion)
			{
				return false;
			}
			if (WS2007HttpRelayBinding.TryCreateSecurity(sbe, unifiedSecurityMode, relayClientAuthenticationType, defaultHttpTransportSecurity, flag1, out wSHttpRelaySecurity))
			{
				WS2007HttpRelayBinding wS2007HttpRelayBinding = new WS2007HttpRelayBinding(wSHttpRelaySecurity, flag1);
				if (!WSHttpRelayBinding.TryGetAllowCookiesFromTransport(transport, out flag))
				{
					return false;
				}
				wS2007HttpRelayBinding.AllowCookies = flag;
				binding = wS2007HttpRelayBinding;
			}
			if (rsbe != null && rsbe.ReliableMessagingVersion != ReliableMessagingVersion.WSReliableMessaging11)
			{
				return false;
			}
			return binding != null;
		}

		private static bool TryCreateSecurity(SecurityBindingElement sbe, Microsoft.ServiceBus.UnifiedSecurityMode mode, Microsoft.ServiceBus.RelayClientAuthenticationType relayClientAuthenticationType, HttpRelayTransportSecurity transportSecurity, bool isReliableSession, out WSHttpRelaySecurity security)
		{
			if (!WSHttpRelaySecurity.TryCreate(sbe, mode, transportSecurity, relayClientAuthenticationType, isReliableSession, out security))
			{
				return false;
			}
			if (sbe == null)
			{
				return true;
			}
			return WS2007HttpRelayBinding.AreBindingsMatching(security.CreateMessageSecurity(isReliableSession, WS2007HttpRelayBinding.WS2007MessageSecurityVersion), sbe);
		}
	}
}