using Microsoft.ServiceBus.Messaging;
using System;
using System.Configuration;

namespace Microsoft.ServiceBus.Messaging.Configuration
{
	public sealed class NetMessagingTransportSettingsElement : ConfigurationElement
	{
		[ConfigurationProperty("batchFlushInterval", IsRequired=false, DefaultValue="0.00:00:00.20")]
		public TimeSpan BatchFlushInterval
		{
			get
			{
				return (TimeSpan)base["batchFlushInterval"];
			}
			set
			{
				base["batchFlushInterval"] = value;
			}
		}

		[ConfigurationProperty("enableRedirect", IsRequired=false)]
		public bool EnableRedirect
		{
			get
			{
				return (bool)base["enableRedirect"];
			}
			set
			{
				base["enableRedirect"] = value;
			}
		}

		public NetMessagingTransportSettingsElement()
		{
		}

		internal void ApplyTo(NetMessagingTransportSettings settings)
		{
			settings.BatchFlushInterval = this.BatchFlushInterval;
			settings.EnableRedirect = this.EnableRedirect;
		}

		internal void CopyFrom(NetMessagingTransportSettingsElement settingsElement)
		{
			this.BatchFlushInterval = settingsElement.BatchFlushInterval;
			this.EnableRedirect = settingsElement.EnableRedirect;
		}

		internal void InitializeFrom(NetMessagingTransportSettings settings)
		{
			this.BatchFlushInterval = settings.BatchFlushInterval;
			this.EnableRedirect = settings.EnableRedirect;
		}
	}
}