using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Messaging.Channels;
using System;
using System.Configuration;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;

namespace Microsoft.ServiceBus.Messaging.Configuration
{
	public sealed class NetMessagingBindingExtensionElement : StandardBindingElement
	{
		private ConfigurationPropertyCollection properties;

		protected override Type BindingElementType
		{
			get
			{
				return typeof(NetMessagingBinding);
			}
		}

		[ConfigurationProperty("prefetchCount", DefaultValue="-1")]
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

		public NetMessagingBindingExtensionElement() : this(null)
		{
		}

		public NetMessagingBindingExtensionElement(string name) : base(name)
		{
		}

		protected override void InitializeFrom(Binding binding)
		{
			base.InitializeFrom(binding);
			NetMessagingBinding netMessagingBinding = (NetMessagingBinding)binding;
			this.PrefetchCount = netMessagingBinding.PrefetchCount;
			this.SessionIdleTimeout = netMessagingBinding.SessionIdleTimeout;
			this.TransportSettings.InitializeFrom(netMessagingBinding.TransportSettings);
		}

		protected override void OnApplyConfiguration(Binding binding)
		{
			NetMessagingBinding prefetchCount = (NetMessagingBinding)binding;
			prefetchCount.PrefetchCount = this.PrefetchCount;
			prefetchCount.SessionIdleTimeout = this.SessionIdleTimeout;
			this.TransportSettings.ApplyTo(prefetchCount.TransportSettings);
		}
	}
}