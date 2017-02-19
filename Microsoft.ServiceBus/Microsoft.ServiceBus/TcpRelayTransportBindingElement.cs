using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Xml;

namespace Microsoft.ServiceBus
{
	public class TcpRelayTransportBindingElement : Microsoft.ServiceBus.Channels.ConnectionOrientedTransportBindingElement, IPolicyExportExtension
	{
		private SocketConnectionPoolSettings connectionPoolSettings;

		private int listenBacklog;

		private bool transportProtectionEnabled;

		private Microsoft.ServiceBus.RelayClientAuthenticationType relayClientAuthenticationType;

		private TcpRelayConnectionMode relayedConnectionMode;

		public TcpRelayConnectionMode ConnectionMode
		{
			get
			{
				return this.relayedConnectionMode;
			}
			set
			{
				this.relayedConnectionMode = value;
			}
		}

		public SocketConnectionPoolSettings ConnectionPoolSettings
		{
			get
			{
				return this.connectionPoolSettings;
			}
		}

		public bool IsDynamic
		{
			get;
			set;
		}

		internal int ListenBacklog
		{
			get
			{
				return this.listenBacklog;
			}
			set
			{
				if (value <= 0)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value", Microsoft.ServiceBus.SR.GetString(Resources.ValueMustBePositive, new object[0])));
				}
				this.listenBacklog = value;
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
				this.relayClientAuthenticationType = value;
			}
		}

		public override string Scheme
		{
			get
			{
				return "sb";
			}
		}

		internal bool TransportProtectionEnabled
		{
			get
			{
				return this.transportProtectionEnabled;
			}
			set
			{
				this.transportProtectionEnabled = value;
			}
		}

		public TcpRelayTransportBindingElement() : this(Microsoft.ServiceBus.RelayClientAuthenticationType.RelayAccessToken)
		{
		}

		public TcpRelayTransportBindingElement(Microsoft.ServiceBus.RelayClientAuthenticationType relayClientAuthenticationType)
		{
			this.relayClientAuthenticationType = relayClientAuthenticationType;
			this.relayedConnectionMode = TcpRelayConnectionMode.Relayed;
			this.listenBacklog = 10;
			this.connectionPoolSettings = new SocketConnectionPoolSettings();
			this.IsDynamic = true;
		}

		protected TcpRelayTransportBindingElement(TcpRelayTransportBindingElement elementToBeCloned) : base(elementToBeCloned)
		{
			this.relayClientAuthenticationType = elementToBeCloned.relayClientAuthenticationType;
			this.relayedConnectionMode = elementToBeCloned.relayedConnectionMode;
			this.listenBacklog = elementToBeCloned.listenBacklog;
			this.transportProtectionEnabled = elementToBeCloned.transportProtectionEnabled;
			this.connectionPoolSettings = elementToBeCloned.connectionPoolSettings.Clone();
			this.IsDynamic = elementToBeCloned.IsDynamic;
		}

		public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
		{
			this.EnsureValid();
			return this.BuildInnerBindingElement(context).BuildChannelFactory<TChannel>(context);
		}

		public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
		where TChannel : class, IChannel
		{
			this.EnsureValid();
			return this.BuildInnerBindingElement(context).BuildChannelListener<TChannel>(context);
		}

		private BindingElement BuildInnerBindingElement(BindingContext context)
		{
			SocketConnectionBindingElement socketConnectionBindingElement = null;
			NameSettings nameSetting = context.BindingParameters.Find<NameSettings>();
			NameSettings relayClientAuthenticationType = nameSetting;
			if (nameSetting == null)
			{
				relayClientAuthenticationType = new NameSettings();
				context.BindingParameters.Add(relayClientAuthenticationType);
			}
			relayClientAuthenticationType.ServiceSettings.TransportProtection = (this.transportProtectionEnabled ? RelayTransportProtectionMode.EndToEnd : RelayTransportProtectionMode.None);
			relayClientAuthenticationType.ServiceSettings.RelayClientAuthenticationType = this.RelayClientAuthenticationType;
			relayClientAuthenticationType.ServiceSettings.IsDynamic = this.IsDynamic;
			TokenProvider tokenProvider = TokenProviderUtility.CreateTokenProvider(context);
			switch (this.relayedConnectionMode)
			{
				case TcpRelayConnectionMode.Relayed:
				{
					if (relayClientAuthenticationType.ServiceSettings.ListenerType != ListenerType.RelayedHttp)
					{
						relayClientAuthenticationType.ServiceSettings.ListenerType = ListenerType.RelayedConnection;
					}
					ConnectivitySettings connectivitySetting = context.BindingParameters.Find<ConnectivitySettings>();
					HttpConnectivitySettings httpConnectivitySetting = context.BindingParameters.Find<HttpConnectivitySettings>();
					socketConnectionBindingElement = new SocketConnectionBindingElement(new ConnectivityModeConnectionElement(tokenProvider, (this.transportProtectionEnabled ? SocketSecurityRole.SslClient : SocketSecurityRole.None), context, relayClientAuthenticationType, connectivitySetting, httpConnectivitySetting));
					break;
				}
				case TcpRelayConnectionMode.Hybrid:
				{
					if (base.ChannelInitializationTimeout < TimeSpan.FromSeconds(60))
					{
						base.ChannelInitializationTimeout = TimeSpan.FromSeconds(60);
					}
					relayClientAuthenticationType.ServiceSettings.ListenerType = ListenerType.DirectConnection;
					socketConnectionBindingElement = new SocketConnectionBindingElement(new HybridConnectionElement(context, this, relayClientAuthenticationType, tokenProvider));
					break;
				}
				default:
				{
					goto case TcpRelayConnectionMode.Relayed;
				}
			}
			socketConnectionBindingElement.ChannelInitializationTimeout = base.ChannelInitializationTimeout;
			socketConnectionBindingElement.ConnectionBufferSize = base.ConnectionBufferSize;
			socketConnectionBindingElement.ConnectionPoolSettings.GroupName = this.ConnectionPoolSettings.GroupName;
			socketConnectionBindingElement.ConnectionPoolSettings.IdleTimeout = this.ConnectionPoolSettings.IdleTimeout;
			socketConnectionBindingElement.ConnectionPoolSettings.LeaseTimeout = this.ConnectionPoolSettings.LeaseTimeout;
			socketConnectionBindingElement.ConnectionPoolSettings.MaxOutboundConnectionsPerEndpoint = this.ConnectionPoolSettings.MaxOutboundConnectionsPerEndpoint;
			socketConnectionBindingElement.ExposeConnectionProperty = base.ExposeConnectionProperty;
			socketConnectionBindingElement.HostNameComparisonMode = base.HostNameComparisonMode;
			socketConnectionBindingElement.InheritBaseAddressSettings = base.InheritBaseAddressSettings;
			socketConnectionBindingElement.ListenBacklog = this.ListenBacklog;
			socketConnectionBindingElement.ManualAddressing = base.ManualAddressing;
			socketConnectionBindingElement.MaxBufferPoolSize = this.MaxBufferPoolSize;
			socketConnectionBindingElement.MaxBufferSize = base.MaxBufferSize;
			socketConnectionBindingElement.MaxOutputDelay = base.MaxOutputDelay;
			socketConnectionBindingElement.MaxPendingAccepts = base.MaxPendingAccepts;
			socketConnectionBindingElement.MaxPendingConnections = base.MaxPendingConnections;
			socketConnectionBindingElement.MaxReceivedMessageSize = this.MaxReceivedMessageSize;
			socketConnectionBindingElement.TransferMode = base.TransferMode;
			socketConnectionBindingElement.TeredoEnabled = false;
			return socketConnectionBindingElement;
		}

		public override BindingElement Clone()
		{
			return new TcpRelayTransportBindingElement(this);
		}

		private void EnsureValid()
		{
			if (this.TransportProtectionEnabled && this.ConnectionMode == TcpRelayConnectionMode.Hybrid)
			{
				throw new InvalidOperationException(SRClient.InvalidConfiguration);
			}
		}

		public override T GetProperty<T>(BindingContext context)
		where T : class
		{
			if (context == null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("context");
			}
			BindingElement bindingElement = this.BuildInnerBindingElement(context);
			if (typeof(T) == typeof(IBindingDeliveryCapabilities))
			{
				return (T)(new TcpRelayTransportBindingElement.BindingDeliveryCapabilitiesHelper());
			}
			return bindingElement.GetProperty<T>(context);
		}

		internal override bool IsMatch(BindingElement b)
		{
			if (!base.IsMatch(b))
			{
				return false;
			}
			TcpRelayTransportBindingElement tcpRelayTransportBindingElement = b as TcpRelayTransportBindingElement;
			if (tcpRelayTransportBindingElement == null)
			{
				return false;
			}
			if (this.listenBacklog != tcpRelayTransportBindingElement.listenBacklog)
			{
				return false;
			}
			if (this.transportProtectionEnabled != tcpRelayTransportBindingElement.transportProtectionEnabled)
			{
				return false;
			}
			if (!this.connectionPoolSettings.IsMatch(tcpRelayTransportBindingElement.connectionPoolSettings))
			{
				return false;
			}
			return true;
		}

		void System.ServiceModel.Description.IPolicyExportExtension.ExportPolicy(MetadataExporter exporter, PolicyConversionContext context)
		{
			if (exporter == null)
			{
				throw new ArgumentNullException("exporter");
			}
			if (context == null)
			{
				throw new ArgumentNullException("context");
			}
			XmlDocument xmlDocument = new XmlDocument();
			XmlElement xmlElement = null;
			if (this.TransportProtectionEnabled)
			{
				SslStreamSecurityBindingElement sslStreamSecurityBindingElement = new SslStreamSecurityBindingElement();
				if (!context.BindingElements.Contains(typeof(SecurityBindingElement)))
				{
					context.BindingElements.Add(sslStreamSecurityBindingElement);
				}
				((IPolicyExportExtension)sslStreamSecurityBindingElement).ExportPolicy(exporter, context);
			}
			switch (this.relayedConnectionMode)
			{
				case TcpRelayConnectionMode.Hybrid:
				{
					xmlElement = xmlDocument.CreateElement("rel", "HybridSocketConnection", "http://schemas.microsoft.com/netservices/2009/05/servicebus/connect");
					break;
				}
				default:
				{
					xmlElement = xmlDocument.CreateElement("rel", "RelaySocketConnection", "http://schemas.microsoft.com/netservices/2009/05/servicebus/connect");
					break;
				}
			}
			context.GetBindingAssertions().Add(xmlElement);
			XmlElement xmlElement1 = xmlDocument.CreateElement("rel", "ListenerRelayCredential", "http://schemas.microsoft.com/netservices/2009/05/servicebus/connect");
			xmlElement1.SetAttribute("Optional", exporter.PolicyVersion.Namespace, "true");
			context.GetBindingAssertions().Add(xmlElement1);
			if (this.RelayClientAuthenticationType == Microsoft.ServiceBus.RelayClientAuthenticationType.RelayAccessToken)
			{
				XmlElement xmlElement2 = xmlDocument.CreateElement("rel", "SenderRelayCredential", "http://schemas.microsoft.com/netservices/2009/05/servicebus/connect");
				context.GetBindingAssertions().Add(xmlElement2);
			}
		}

		private class BindingDeliveryCapabilitiesHelper : IBindingDeliveryCapabilities
		{
			bool System.ServiceModel.Channels.IBindingDeliveryCapabilities.AssuresOrderedDelivery
			{
				get
				{
					return true;
				}
			}

			bool System.ServiceModel.Channels.IBindingDeliveryCapabilities.QueuedDelivery
			{
				get
				{
					return false;
				}
			}

			internal BindingDeliveryCapabilitiesHelper()
			{
			}
		}
	}
}