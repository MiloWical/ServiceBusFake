using Microsoft.ServiceBus;
using System;
using System.Configuration;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;

namespace Microsoft.ServiceBus.Configuration
{
	public sealed class TcpRelayTransportElement : Microsoft.ServiceBus.Configuration.ConnectionOrientedTransportElement
	{
		private ConfigurationPropertyCollection properties;

		public override Type BindingElementType
		{
			get
			{
				return typeof(TcpRelayTransportBindingElement);
			}
		}

		[ConfigurationProperty("connectionMode", DefaultValue=TcpRelayConnectionMode.Relayed)]
		public TcpRelayConnectionMode ConnectionMode
		{
			get
			{
				return (TcpRelayConnectionMode)base["connectionMode"];
			}
			set
			{
				base["connectionMode"] = value;
			}
		}

		[ConfigurationProperty("connectionPoolSettings")]
		public SocketConnectionPoolSettingsElement ConnectionPoolSettings
		{
			get
			{
				return (SocketConnectionPoolSettingsElement)base["connectionPoolSettings"];
			}
			set
			{
				base["connectionPoolSettings"] = value;
			}
		}

		[ConfigurationProperty("isDynamic", DefaultValue=true)]
		public bool IsDynamic
		{
			get
			{
				return (bool)base["isDynamic"];
			}
			set
			{
				base["isDynamic"] = value;
			}
		}

		[ConfigurationProperty("listenBacklog", DefaultValue=10)]
		[IntegerValidator(MinValue=1)]
		public int ListenBacklog
		{
			get
			{
				return (int)base["listenBacklog"];
			}
			set
			{
				base["listenBacklog"] = value;
			}
		}

		protected override ConfigurationPropertyCollection Properties
		{
			get
			{
				if (this.properties == null)
				{
					ConfigurationPropertyCollection properties = base.Properties;
					properties.Add(new ConfigurationProperty("listenBacklog", typeof(int), (object)10, null, new IntegerValidator(1, 2147483647, false), ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("connectionMode", typeof(TcpRelayConnectionMode), (object)TcpRelayConnectionMode.Relayed, null, null, ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("relayClientAuthenticationType", typeof(Microsoft.ServiceBus.RelayClientAuthenticationType), (object)Microsoft.ServiceBus.RelayClientAuthenticationType.RelayAccessToken, null, new Microsoft.ServiceBus.Configuration.ServiceModelEnumValidator(typeof(RelayClientAuthenticationTypeHelper)), ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("connectionPoolSettings", typeof(SocketConnectionPoolSettingsElement), null, null, null, ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("isDynamic", typeof(bool), true, null, null, ConfigurationPropertyOptions.None));
					this.properties = properties;
				}
				return this.properties;
			}
		}

		[ConfigurationProperty("relayClientAuthenticationType", DefaultValue=Microsoft.ServiceBus.RelayClientAuthenticationType.RelayAccessToken)]
		[Microsoft.ServiceBus.Configuration.ServiceModelEnumValidator(typeof(RelayClientAuthenticationTypeHelper))]
		public Microsoft.ServiceBus.RelayClientAuthenticationType RelayClientAuthenticationType
		{
			get
			{
				return (Microsoft.ServiceBus.RelayClientAuthenticationType)base["relayClientAuthenticationType"];
			}
			set
			{
				base["relayClientAuthenticationType"] = value;
			}
		}

		public TcpRelayTransportElement()
		{
		}

		public override void ApplyConfiguration(BindingElement bindingElement)
		{
			base.ApplyConfiguration(bindingElement);
			TcpRelayTransportBindingElement listenBacklog = (TcpRelayTransportBindingElement)bindingElement;
			listenBacklog.ListenBacklog = this.ListenBacklog;
			listenBacklog.ConnectionMode = this.ConnectionMode;
			listenBacklog.RelayClientAuthenticationType = this.RelayClientAuthenticationType;
			this.ConnectionPoolSettings.ApplyConfiguration(listenBacklog.ConnectionPoolSettings);
			listenBacklog.IsDynamic = this.IsDynamic;
		}

		public override void CopyFrom(ServiceModelExtensionElement from)
		{
			base.CopyFrom(from);
			TcpRelayTransportElement tcpRelayTransportElement = (TcpRelayTransportElement)from;
			this.ListenBacklog = tcpRelayTransportElement.ListenBacklog;
			this.ConnectionMode = tcpRelayTransportElement.ConnectionMode;
			this.RelayClientAuthenticationType = tcpRelayTransportElement.RelayClientAuthenticationType;
			this.ConnectionPoolSettings.CopyFrom(tcpRelayTransportElement.ConnectionPoolSettings);
			this.IsDynamic = tcpRelayTransportElement.IsDynamic;
		}

		protected override TransportBindingElement CreateDefaultBindingElement()
		{
			return new TcpRelayTransportBindingElement();
		}

		protected override void InitializeFrom(BindingElement bindingElement)
		{
			base.InitializeFrom(bindingElement);
			TcpRelayTransportBindingElement tcpRelayTransportBindingElement = (TcpRelayTransportBindingElement)bindingElement;
			this.ListenBacklog = tcpRelayTransportBindingElement.ListenBacklog;
			this.ConnectionMode = tcpRelayTransportBindingElement.ConnectionMode;
			this.RelayClientAuthenticationType = tcpRelayTransportBindingElement.RelayClientAuthenticationType;
			this.ConnectionPoolSettings.InitializeFrom(tcpRelayTransportBindingElement.ConnectionPoolSettings);
			this.IsDynamic = tcpRelayTransportBindingElement.IsDynamic;
		}
	}
}