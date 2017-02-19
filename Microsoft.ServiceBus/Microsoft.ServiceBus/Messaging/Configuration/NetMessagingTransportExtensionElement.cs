using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Messaging.Channels;
using System;
using System.Configuration;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;

namespace Microsoft.ServiceBus.Messaging.Configuration
{
	public sealed class NetMessagingTransportExtensionElement : TransportElement
	{
		private ConfigurationPropertyCollection properties;

		public override Type BindingElementType
		{
			get
			{
				return typeof(NetMessagingTransportBindingElement);
			}
		}

		[ConfigurationProperty("prefetchCount", DefaultValue=-1)]
		public int PrefetchCount
		{
			get
			{
				return (int)base["prefetchCount"];
			}
			set
			{
				base["prefetchCount"] = value;
			}
		}

		protected override ConfigurationPropertyCollection Properties
		{
			get
			{
				if (this.properties == null)
				{
					ConfigurationPropertyCollection properties = base.Properties;
					if (properties.Remove("maxReceivedMessageSize"))
					{
						properties.Add(new ConfigurationProperty("maxReceivedMessageSize", typeof(long), (object)((long)262144), null, new LongValidator((long)1, 9223372036854775807L, false), ConfigurationPropertyOptions.None));
					}
					properties.Add(new ConfigurationProperty("prefetchCount", typeof(int), (object)-1));
					properties.Add(new ConfigurationProperty("sessionIdleTimeout", typeof(TimeSpan), (object)Microsoft.ServiceBus.Messaging.Channels.TransportDefaults.SessionIdleTimeout, new InfiniteTimeSpanConverter(), new PositiveTimeSpanValidator(), ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("transportSettings", typeof(NetMessagingTransportSettingsElement)));
					this.properties = properties;
				}
				return this.properties;
			}
		}

		[ConfigurationProperty("sessionIdleTimeout", DefaultValue="00:01:00")]
		[PositiveTimeSpanValidator]
		public TimeSpan SessionIdleTimeout
		{
			get
			{
				return (TimeSpan)base["sessionIdleTimeout"];
			}
			set
			{
				TimeoutHelper.ThrowIfNonPositiveArgument(value, "sessionIdleTimeout");
				base["sessionIdleTimeout"] = value;
			}
		}

		[ConfigurationProperty("transportSettings", IsRequired=false)]
		public NetMessagingTransportSettingsElement TransportSettings
		{
			get
			{
				return (NetMessagingTransportSettingsElement)base["transportSettings"];
			}
		}

		public NetMessagingTransportExtensionElement()
		{
		}

		public override void ApplyConfiguration(BindingElement bindingElement)
		{
			base.ApplyConfiguration(bindingElement);
			NetMessagingTransportBindingElement prefetchCount = (NetMessagingTransportBindingElement)bindingElement;
			prefetchCount.PrefetchCount = this.PrefetchCount;
			prefetchCount.SessionIdleTimeout = this.SessionIdleTimeout;
			this.TransportSettings.ApplyTo(prefetchCount.TransportSettings);
		}

		public override void CopyFrom(ServiceModelExtensionElement from)
		{
			base.CopyFrom(from);
			NetMessagingTransportExtensionElement netMessagingTransportExtensionElement = (NetMessagingTransportExtensionElement)from;
			this.PrefetchCount = netMessagingTransportExtensionElement.PrefetchCount;
			this.SessionIdleTimeout = netMessagingTransportExtensionElement.SessionIdleTimeout;
			this.TransportSettings.CopyFrom(netMessagingTransportExtensionElement.TransportSettings);
		}

		protected override TransportBindingElement CreateDefaultBindingElement()
		{
			return new NetMessagingTransportBindingElement();
		}

		protected override void InitializeFrom(BindingElement bindingElement)
		{
			base.InitializeFrom(bindingElement);
			NetMessagingTransportBindingElement netMessagingTransportBindingElement = (NetMessagingTransportBindingElement)bindingElement;
			this.PrefetchCount = netMessagingTransportBindingElement.PrefetchCount;
			this.SessionIdleTimeout = netMessagingTransportBindingElement.SessionIdleTimeout;
			this.TransportSettings.InitializeFrom(netMessagingTransportBindingElement.TransportSettings);
		}
	}
}