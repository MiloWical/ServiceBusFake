using Microsoft.ServiceBus;
using System;
using System.Configuration;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;

namespace Microsoft.ServiceBus.Configuration
{
	public sealed class RelayedOnewayTransportElement : Microsoft.ServiceBus.Configuration.ConnectionOrientedTransportElement
	{
		private ConfigurationPropertyCollection properties;

		public override Type BindingElementType
		{
			get
			{
				return typeof(RelayedOnewayTransportBindingElement);
			}
		}

		[ConfigurationProperty("connectionMode", DefaultValue=RelayedOnewayConnectionMode.Unicast)]
		public RelayedOnewayConnectionMode ConnectionMode
		{
			get
			{
				return (RelayedOnewayConnectionMode)base["connectionMode"];
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
					properties.Add(new ConfigurationProperty("connectionMode", typeof(RelayedOnewayConnectionMode), (object)RelayedOnewayConnectionMode.Unicast, null, null, ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("connectionPoolSettings", typeof(SocketConnectionPoolSettingsElement), null, null, null, ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("listenBacklog", typeof(int), (object)10, null, new IntegerValidator(1, 2147483647, false), ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("relayClientAuthenticationType", typeof(Microsoft.ServiceBus.RelayClientAuthenticationType), (object)Microsoft.ServiceBus.RelayClientAuthenticationType.RelayAccessToken, null, new Microsoft.ServiceBus.Configuration.ServiceModelEnumValidator(typeof(RelayClientAuthenticationTypeHelper)), ConfigurationPropertyOptions.None));
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

		public RelayedOnewayTransportElement()
		{
		}

		public override void ApplyConfiguration(BindingElement bindingElement)
		{
			base.ApplyConfiguration(bindingElement);
			RelayedOnewayTransportBindingElement connectionMode = (RelayedOnewayTransportBindingElement)bindingElement;
			connectionMode.ConnectionMode = this.ConnectionMode;
			connectionMode.ListenBacklog = this.ListenBacklog;
			connectionMode.MaxOutputDelay = base.MaxOutputDelay;
			connectionMode.RelayClientAuthenticationType = this.RelayClientAuthenticationType;
			this.ConnectionPoolSettings.ApplyConfiguration(connectionMode.ConnectionPoolSettings);
		}

		public override void CopyFrom(ServiceModelExtensionElement from)
		{
			base.CopyFrom(from);
			RelayedOnewayTransportElement relayedOnewayTransportElement = (RelayedOnewayTransportElement)from;
			this.ConnectionMode = relayedOnewayTransportElement.ConnectionMode;
			this.RelayClientAuthenticationType = relayedOnewayTransportElement.RelayClientAuthenticationType;
			this.ConnectionPoolSettings.CopyFrom(relayedOnewayTransportElement.ConnectionPoolSettings);
		}

		protected override TransportBindingElement CreateDefaultBindingElement()
		{
			return new RelayedOnewayTransportBindingElement();
		}

		protected override void InitializeFrom(BindingElement bindingElement)
		{
			base.InitializeFrom(bindingElement);
			RelayedOnewayTransportBindingElement relayedOnewayTransportBindingElement = (RelayedOnewayTransportBindingElement)bindingElement;
			this.ConnectionMode = relayedOnewayTransportBindingElement.ConnectionMode;
			this.RelayClientAuthenticationType = relayedOnewayTransportBindingElement.RelayClientAuthenticationType;
			this.ConnectionPoolSettings.InitializeFrom(relayedOnewayTransportBindingElement.ConnectionPoolSettings);
		}
	}
}