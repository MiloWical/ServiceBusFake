using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Configuration;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Globalization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.Xml;

namespace Microsoft.ServiceBus
{
	public class NetOnewayRelayBinding : Binding, IBindingRuntimePreferences
	{
		protected internal RelayedOnewayTransportBindingElement transport;

		protected internal BinaryMessageEncodingBindingElement encoding;

		internal NetOnewayRelaySecurity security = new NetOnewayRelaySecurity();

		public System.ServiceModel.EnvelopeVersion EnvelopeVersion
		{
			get
			{
				return System.ServiceModel.EnvelopeVersion.Soap12;
			}
		}

		public int ListenBacklog
		{
			get
			{
				return this.transport.ListenBacklog;
			}
			set
			{
				this.transport.ListenBacklog = value;
			}
		}

		public long MaxBufferPoolSize
		{
			get
			{
				return this.transport.MaxBufferPoolSize;
			}
			set
			{
				this.transport.MaxBufferPoolSize = value;
			}
		}

		public int MaxBufferSize
		{
			get
			{
				return this.transport.MaxBufferSize;
			}
			set
			{
				this.transport.MaxBufferSize = value;
			}
		}

		public int MaxConnections
		{
			get
			{
				return this.transport.MaxPendingConnections;
			}
			set
			{
				this.transport.MaxPendingConnections = value;
				this.transport.ConnectionPoolSettings.MaxOutboundConnectionsPerEndpoint = value;
			}
		}

		public long MaxReceivedMessageSize
		{
			get
			{
				return this.transport.MaxReceivedMessageSize;
			}
			set
			{
				this.transport.MaxReceivedMessageSize = value;
			}
		}

		protected internal System.ServiceModel.MessageSecurityVersion MessageSecurityVersion
		{
			get
			{
				return System.ServiceModel.MessageSecurityVersion.WSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11;
			}
		}

		public XmlDictionaryReaderQuotas ReaderQuotas
		{
			get
			{
				return this.encoding.ReaderQuotas;
			}
			set
			{
				if (value == null)
				{
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("value");
				}
				value.CopyTo(this.encoding.ReaderQuotas);
			}
		}

		public override string Scheme
		{
			get
			{
				return this.transport.Scheme;
			}
		}

		public NetOnewayRelaySecurity Security
		{
			get
			{
				return this.security;
			}
		}

		bool System.ServiceModel.Channels.IBindingRuntimePreferences.ReceiveSynchronously
		{
			get
			{
				return false;
			}
		}

		public NetOnewayRelayBinding()
		{
			this.Initialize();
		}

		public NetOnewayRelayBinding(EndToEndSecurityMode securityMode, RelayClientAuthenticationType relayClientAuthenticationType) : this()
		{
			this.security.RelayClientAuthenticationType = relayClientAuthenticationType;
			this.security.Mode = securityMode;
		}

		public NetOnewayRelayBinding(string configurationName) : this()
		{
			this.Initialize();
			this.ApplyConfiguration(configurationName);
		}

		protected NetOnewayRelayBinding(RelayedOnewayConnectionMode connectionMode, EndToEndSecurityMode securityMode, RelayClientAuthenticationType relayClientAuthenticationType) : this(securityMode, relayClientAuthenticationType)
		{
			this.transport.ConnectionMode = connectionMode;
		}

		protected NetOnewayRelayBinding(RelayedOnewayTransportBindingElement transport, BinaryMessageEncodingBindingElement encoding, NetOnewayRelaySecurity security)
		{
			this.transport = transport;
			this.encoding = encoding;
			this.security = security;
		}

		protected NetOnewayRelayBinding(NetOnewayRelaySecurity security)
		{
			this.security = security;
		}

		protected virtual void ApplyConfiguration(string configurationName)
		{
			NetOnewayRelayBindingElement item = NetOnewayRelayBindingCollectionElement.GetBindingCollectionElement().Bindings[configurationName];
			if (item == null)
			{
				ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
				string configInvalidBindingConfigurationName = Resources.ConfigInvalidBindingConfigurationName;
				object[] objArray = new object[] { configurationName, "netTcpRelayBinding" };
				throw exceptionUtility.ThrowHelperError(new ConfigurationErrorsException(Microsoft.ServiceBus.SR.GetString(configInvalidBindingConfigurationName, objArray)));
			}
			item.ApplyConfiguration(this);
		}

		public override BindingElementCollection CreateBindingElements()
		{
			BindingElementCollection bindingElementCollection = new BindingElementCollection();
			SecurityBindingElement securityBindingElement = this.CreateMessageSecurity();
			if (securityBindingElement != null)
			{
				bindingElementCollection.Add(securityBindingElement);
			}
			bindingElementCollection.Add(this.encoding);
			this.transport.RelayClientAuthenticationType = this.security.RelayClientAuthenticationType;
			this.transport.TransportProtectionEnabled = (this.security.Mode == EndToEndSecurityMode.Transport ? true : this.security.Mode == EndToEndSecurityMode.TransportWithMessageCredential);
			bindingElementCollection.Add(this.transport);
			return bindingElementCollection.Clone();
		}

		private SecurityBindingElement CreateMessageSecurity()
		{
			if (this.security.Mode != EndToEndSecurityMode.Message && this.security.Mode != EndToEndSecurityMode.TransportWithMessageCredential)
			{
				return null;
			}
			return this.security.CreateMessageSecurity();
		}

		private RelayedOnewayTransportBindingElement GetTransport()
		{
			this.security.ConfigureTransportSecurity(this.transport);
			return this.transport;
		}

		private void Initialize()
		{
			this.transport = new RelayedOnewayTransportBindingElement(this.security.RelayClientAuthenticationType, RelayedOnewayConnectionMode.Unicast);
			this.encoding = new BinaryMessageEncodingBindingElement();
		}

		private static void InitializeFrom(RelayedOnewayTransportBindingElement transport, BinaryMessageEncodingBindingElement encoding)
		{
			throw new NotImplementedException();
		}

		private bool IsBindingElementsMatch(RelayedOnewayTransportBindingElement transport, MessageEncodingBindingElement encoding)
		{
			Type type = typeof(BindingElement);
			RelayedOnewayTransportBindingElement relayedOnewayTransportBindingElement = this.GetTransport();
			object[] objArray = new object[] { transport };
			if ((bool)InvokeHelper.InvokeInstanceMethod(type, relayedOnewayTransportBindingElement, "IsMatch", objArray))
			{
				return false;
			}
			Type type1 = typeof(BindingElement);
			BinaryMessageEncodingBindingElement binaryMessageEncodingBindingElement = this.encoding;
			object[] objArray1 = new object[] { encoding };
			if ((bool)InvokeHelper.InvokeInstanceMethod(type1, binaryMessageEncodingBindingElement, "IsMatch", objArray1))
			{
				return false;
			}
			return true;
		}

		internal static bool TryCreate(BindingElementCollection elements, out Binding binding)
		{
			Microsoft.ServiceBus.UnifiedSecurityMode unifiedSecurityMode;
			NetOnewayRelaySecurity netOnewayRelaySecurity;
			bool flag;
			binding = null;
			if (elements.Count > 3)
			{
				return false;
			}
			SecurityBindingElement securityBindingElement = null;
			BinaryMessageEncodingBindingElement binaryMessageEncodingBindingElement = null;
			RelayedOnewayTransportBindingElement relayedOnewayTransportBindingElement = null;
			using (IEnumerator<BindingElement> enumerator = elements.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					BindingElement current = enumerator.Current;
					if (current is SecurityBindingElement)
					{
						securityBindingElement = current as SecurityBindingElement;
					}
					else if (current is TransportBindingElement)
					{
						relayedOnewayTransportBindingElement = current as RelayedOnewayTransportBindingElement;
					}
					else if (!(current is MessageEncodingBindingElement))
					{
						flag = false;
						return flag;
					}
					else
					{
						binaryMessageEncodingBindingElement = current as BinaryMessageEncodingBindingElement;
					}
				}
				unifiedSecurityMode = (!relayedOnewayTransportBindingElement.TransportProtectionEnabled ? Microsoft.ServiceBus.UnifiedSecurityMode.None | Microsoft.ServiceBus.UnifiedSecurityMode.Message : Microsoft.ServiceBus.UnifiedSecurityMode.Transport | Microsoft.ServiceBus.UnifiedSecurityMode.TransportWithMessageCredential);
				if (binaryMessageEncodingBindingElement == null)
				{
					return false;
				}
				if (!NetOnewayRelayBinding.TryCreateSecurity(securityBindingElement, relayedOnewayTransportBindingElement.RelayClientAuthenticationType, unifiedSecurityMode, out netOnewayRelaySecurity))
				{
					return false;
				}
				NetOnewayRelayBinding netOnewayRelayBinding = new NetOnewayRelayBinding(netOnewayRelaySecurity);
				NetOnewayRelayBinding.InitializeFrom(relayedOnewayTransportBindingElement, binaryMessageEncodingBindingElement);
				if (!netOnewayRelayBinding.IsBindingElementsMatch(relayedOnewayTransportBindingElement, binaryMessageEncodingBindingElement))
				{
					return false;
				}
				binding = netOnewayRelayBinding;
				return true;
			}
			return flag;
		}

		private static bool TryCreateSecurity(SecurityBindingElement sbe, RelayClientAuthenticationType relayClientAuthenticationType, Microsoft.ServiceBus.UnifiedSecurityMode mode, out NetOnewayRelaySecurity security)
		{
			if (sbe == null)
			{
				mode = mode & (Microsoft.ServiceBus.UnifiedSecurityMode.None | Microsoft.ServiceBus.UnifiedSecurityMode.Transport | Microsoft.ServiceBus.UnifiedSecurityMode.TransportWithMessageCredential);
			}
			else
			{
				mode = mode & (Microsoft.ServiceBus.UnifiedSecurityMode.Message | Microsoft.ServiceBus.UnifiedSecurityMode.Both);
			}
			EndToEndSecurityMode relaySecurityMode = EndToEndSecurityModeHelper.ToRelaySecurityMode(mode);
			bool flag = EndToEndSecurityModeHelper.IsDefined(relaySecurityMode);
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] str = new object[] { relaySecurityMode.ToString() };
			Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DebugAssert(flag, string.Format(invariantCulture, "Invalid RelaySecurityMode value: {0}.", str));
			if (NetOnewayRelaySecurity.TryCreate(sbe, relaySecurityMode, relayClientAuthenticationType, out security))
			{
				return true;
			}
			return false;
		}
	}
}