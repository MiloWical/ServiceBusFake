using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Configuration;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Globalization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.Xml;

namespace Microsoft.ServiceBus
{
	public abstract class NetTcpRelayBindingBase : Binding, IBindingRuntimePreferences
	{
		protected internal TcpRelayTransportBindingElement transport;

		protected internal BinaryMessageEncodingBindingElement encoding;

		internal NetTcpRelaySecurity security = new NetTcpRelaySecurity();

		public TcpRelayConnectionMode ConnectionMode
		{
			get
			{
				return this.transport.ConnectionMode;
			}
			set
			{
				this.transport.ConnectionMode = value;
			}
		}

		public System.ServiceModel.EnvelopeVersion EnvelopeVersion
		{
			get
			{
				return System.ServiceModel.EnvelopeVersion.Soap12;
			}
		}

		public System.ServiceModel.HostNameComparisonMode HostNameComparisonMode
		{
			get
			{
				return this.transport.HostNameComparisonMode;
			}
			set
			{
				this.transport.HostNameComparisonMode = value;
			}
		}

		public bool IsDynamic
		{
			get
			{
				return this.transport.IsDynamic;
			}
			set
			{
				this.transport.IsDynamic = value;
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

		public NetTcpRelaySecurity Security
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

		public System.ServiceModel.TransferMode TransferMode
		{
			get
			{
				return this.transport.TransferMode;
			}
			set
			{
				this.transport.TransferMode = value;
			}
		}

		protected NetTcpRelayBindingBase()
		{
			this.Initialize();
		}

		protected NetTcpRelayBindingBase(EndToEndSecurityMode securityMode, RelayClientAuthenticationType relayClientAuthenticationType) : this()
		{
			this.security.Mode = securityMode;
			this.security.RelayClientAuthenticationType = relayClientAuthenticationType;
		}

		protected NetTcpRelayBindingBase(string configurationName) : this()
		{
			this.ApplyConfiguration(configurationName);
		}

		protected NetTcpRelayBindingBase(TcpRelayTransportBindingElement transport, BinaryMessageEncodingBindingElement encoding, NetTcpRelaySecurity security) : this()
		{
			this.security = security;
			this.InitializeFrom(transport, encoding);
		}

		protected virtual void ApplyConfiguration(string configurationName)
		{
			NetTcpRelayBindingElement item = NetTcpRelayBindingCollectionElement.GetBindingCollectionElement().Bindings[configurationName];
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
			bool flag = (this.Security.Mode == EndToEndSecurityMode.TransportWithMessageCredential ? true : this.Security.Mode == EndToEndSecurityMode.Transport);
			if (flag)
			{
				bindingElementCollection.Add(new TcpClientTransportTokenAssertionProviderBindingElement());
			}
			bindingElementCollection.Add(this.encoding);
			this.transport.RelayClientAuthenticationType = this.security.RelayClientAuthenticationType;
			this.transport.TransportProtectionEnabled = flag;
			bindingElementCollection.Add(this.transport);
			return bindingElementCollection.Clone();
		}

		protected internal abstract SecurityBindingElement CreateMessageSecurity();

		private void Initialize()
		{
			this.transport = new TcpRelayTransportBindingElement();
			this.encoding = new BinaryMessageEncodingBindingElement();
		}

		private void InitializeFrom(TcpRelayTransportBindingElement transport, BinaryMessageEncodingBindingElement encoding)
		{
			Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DebugAssert(transport != null, "Invalid (null) transport value.");
			Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DebugAssert(encoding != null, "Invalid (null) encoding value.");
			this.MaxBufferPoolSize = transport.MaxBufferPoolSize;
			this.MaxBufferSize = transport.MaxBufferSize;
			this.MaxConnections = transport.MaxPendingConnections;
			this.ListenBacklog = transport.ListenBacklog;
			this.MaxReceivedMessageSize = transport.MaxReceivedMessageSize;
			this.TransferMode = transport.TransferMode;
			this.transport.TransportProtectionEnabled = transport.TransportProtectionEnabled;
			this.ConnectionMode = transport.ConnectionMode;
			this.ReaderQuotas = encoding.ReaderQuotas;
		}

		protected bool IsBindingElementsMatch(TcpRelayTransportBindingElement transport, BinaryMessageEncodingBindingElement encoding)
		{
			if (!this.transport.IsMatch(transport))
			{
				return false;
			}
			Type type = typeof(BindingElement);
			BinaryMessageEncodingBindingElement binaryMessageEncodingBindingElement = this.encoding;
			object[] objArray = new object[] { encoding };
			if (!(bool)InvokeHelper.InvokeInstanceMethod(type, binaryMessageEncodingBindingElement, "IsMatch", objArray))
			{
				return false;
			}
			return true;
		}

		internal static bool TryCreateSecurity(SecurityBindingElement sbe, RelayClientAuthenticationType relayClientAuthenticationType, Microsoft.ServiceBus.UnifiedSecurityMode mode, bool isReliableSession, TcpRelayTransportSecurity tcpTransportSecurity, out NetTcpRelaySecurity security)
		{
			if (sbe == null)
			{
				mode = mode & (Microsoft.ServiceBus.UnifiedSecurityMode.None | Microsoft.ServiceBus.UnifiedSecurityMode.Transport | Microsoft.ServiceBus.UnifiedSecurityMode.Both);
			}
			else
			{
				mode = mode & (Microsoft.ServiceBus.UnifiedSecurityMode.Message | Microsoft.ServiceBus.UnifiedSecurityMode.TransportWithMessageCredential);
			}
			EndToEndSecurityMode relaySecurityMode = EndToEndSecurityModeHelper.ToRelaySecurityMode(mode);
			bool flag = EndToEndSecurityModeHelper.IsDefined(relaySecurityMode);
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] str = new object[] { relaySecurityMode.ToString() };
			Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DebugAssert(flag, string.Format(invariantCulture, "Invalid RelaySecurityMode value: {0}.", str));
			if (NetTcpRelaySecurity.TryCreate(sbe, relaySecurityMode, relayClientAuthenticationType, isReliableSession, tcpTransportSecurity, out security))
			{
				return true;
			}
			return false;
		}
	}
}