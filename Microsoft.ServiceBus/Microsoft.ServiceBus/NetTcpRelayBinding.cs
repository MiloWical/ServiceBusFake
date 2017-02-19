using Microsoft.ServiceBus.Channels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus
{
	public class NetTcpRelayBinding : NetTcpRelayBindingBase, IBindingRuntimePreferences
	{
		private OptionalReliableSession reliableSession;

		private ReliableSessionBindingElement session;

		public OptionalReliableSession ReliableSession
		{
			get
			{
				return this.reliableSession;
			}
		}

		public NetTcpRelayBinding()
		{
			this.Initialize();
		}

		public NetTcpRelayBinding(EndToEndSecurityMode securityMode, RelayClientAuthenticationType relayClientAuthenticationType) : base(securityMode, relayClientAuthenticationType)
		{
			this.Initialize();
		}

		public NetTcpRelayBinding(EndToEndSecurityMode securityMode, RelayClientAuthenticationType relayClientAuthenticationType, bool reliableSessionEnabled) : this(securityMode, relayClientAuthenticationType)
		{
			this.ReliableSession.Enabled = reliableSessionEnabled;
		}

		public NetTcpRelayBinding(string configurationName) : base(configurationName)
		{
		}

		protected NetTcpRelayBinding(TcpRelayTransportBindingElement transport, BinaryMessageEncodingBindingElement encoding, ReliableSessionBindingElement session, NetTcpRelaySecurity security) : base(transport, encoding, security)
		{
			this.Initialize();
			this.ReliableSession.Enabled = session != null;
			this.InitializeFrom(session);
		}

		protected override void ApplyConfiguration(string configurationName)
		{
			this.Initialize();
			base.ApplyConfiguration(configurationName);
		}

		public override BindingElementCollection CreateBindingElements()
		{
			BindingElementCollection bindingElementCollection = base.CreateBindingElements();
			if (this.reliableSession.Enabled)
			{
				bindingElementCollection.Insert(0, this.session);
			}
			return bindingElementCollection.Clone();
		}

		protected internal override SecurityBindingElement CreateMessageSecurity()
		{
			if (this.security.Mode != EndToEndSecurityMode.Message && this.security.Mode != EndToEndSecurityMode.TransportWithMessageCredential)
			{
				return null;
			}
			return this.security.CreateMessageSecurity(this.ReliableSession.Enabled, base.MessageSecurityVersion);
		}

		private void Initialize()
		{
			this.session = new ReliableSessionBindingElement();
			this.reliableSession = new OptionalReliableSession(this.session);
		}

		private void InitializeFrom(ReliableSessionBindingElement session)
		{
			if (session != null)
			{
				this.session.InactivityTimeout = session.InactivityTimeout;
				this.session.Ordered = session.Ordered;
			}
		}

		protected bool IsBindingElementsMatch(TcpRelayTransportBindingElement transport, BinaryMessageEncodingBindingElement encoding, ReliableSessionBindingElement session)
		{
			if (!base.IsBindingElementsMatch(transport, encoding))
			{
				return false;
			}
			if (this.reliableSession.Enabled)
			{
				Type type = typeof(BindingElement);
				ReliableSessionBindingElement reliableSessionBindingElement = this.session;
				object[] objArray = new object[] { session };
				if (!(bool)InvokeHelper.InvokeInstanceMethod(type, reliableSessionBindingElement, "IsMatch", objArray))
				{
					return false;
				}
			}
			else if (session != null)
			{
				return false;
			}
			return true;
		}

		internal static bool TryCreate(BindingElementCollection elements, out Binding binding)
		{
			Microsoft.ServiceBus.UnifiedSecurityMode unifiedSecurityMode;
			NetTcpRelaySecurity netTcpRelaySecurity;
			binding = null;
			if (elements.Count > 5)
			{
				return false;
			}
			TcpRelayTransportBindingElement tcpRelayTransportBindingElement = null;
			BinaryMessageEncodingBindingElement binaryMessageEncodingBindingElement = null;
			ReliableSessionBindingElement reliableSessionBindingElement = null;
			SecurityBindingElement securityBindingElement = null;
			foreach (BindingElement element in elements)
			{
				if (element is SecurityBindingElement)
				{
					securityBindingElement = element as SecurityBindingElement;
				}
				else if (element is TransportBindingElement)
				{
					tcpRelayTransportBindingElement = element as TcpRelayTransportBindingElement;
				}
				else if (!(element is MessageEncodingBindingElement))
				{
					if (!(element is ReliableSessionBindingElement))
					{
						continue;
					}
					reliableSessionBindingElement = element as ReliableSessionBindingElement;
				}
				else
				{
					binaryMessageEncodingBindingElement = element as BinaryMessageEncodingBindingElement;
				}
			}
			if (tcpRelayTransportBindingElement == null)
			{
				return false;
			}
			if (binaryMessageEncodingBindingElement == null)
			{
				return false;
			}
			TcpRelayTransportSecurity tcpRelayTransportSecurity = new TcpRelayTransportSecurity();
			unifiedSecurityMode = (!tcpRelayTransportBindingElement.TransportProtectionEnabled ? Microsoft.ServiceBus.UnifiedSecurityMode.None | Microsoft.ServiceBus.UnifiedSecurityMode.Message : Microsoft.ServiceBus.UnifiedSecurityMode.Transport | Microsoft.ServiceBus.UnifiedSecurityMode.TransportWithMessageCredential);
			if (!NetTcpRelayBindingBase.TryCreateSecurity(securityBindingElement, tcpRelayTransportBindingElement.RelayClientAuthenticationType, unifiedSecurityMode, reliableSessionBindingElement != null, tcpRelayTransportSecurity, out netTcpRelaySecurity))
			{
				return false;
			}
			NetTcpRelayBinding netTcpRelayBinding = new NetTcpRelayBinding(tcpRelayTransportBindingElement, binaryMessageEncodingBindingElement, reliableSessionBindingElement, netTcpRelaySecurity);
			if (!netTcpRelayBinding.IsBindingElementsMatch(tcpRelayTransportBindingElement, binaryMessageEncodingBindingElement, reliableSessionBindingElement))
			{
				return false;
			}
			binding = netTcpRelayBinding;
			return true;
		}
	}
}