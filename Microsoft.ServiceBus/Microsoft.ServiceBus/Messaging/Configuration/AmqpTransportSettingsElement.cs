using Microsoft.ServiceBus.Messaging.Amqp;
using System;
using System.Configuration;

namespace Microsoft.ServiceBus.Messaging.Configuration
{
	internal sealed class AmqpTransportSettingsElement : ConfigurationElement
	{
		[ConfigurationProperty("maxFrameSize", IsRequired=false)]
		public int MaxFrameSize
		{
			get
			{
				return (int)base["maxFrameSize"];
			}
			set
			{
				base["maxFrameSize"] = value;
			}
		}

		[ConfigurationProperty("sslStreamUpgrade", IsRequired=false)]
		public bool SslStreamUpgrade
		{
			get
			{
				return (bool)base["sslStreamUpgrade"];
			}
			set
			{
				base["sslStreamUpgrade"] = value;
			}
		}

		[ConfigurationProperty("useSslStreamSecurity", IsRequired=false)]
		public bool UseSslStreamSecurity
		{
			get
			{
				return (bool)base["useSslStreamSecurity"];
			}
			set
			{
				base["useSslStreamSecurity"] = value;
			}
		}

		public AmqpTransportSettingsElement()
		{
		}

		internal void ApplyTo(AmqpTransportSettings settings)
		{
			settings.MaxFrameSize = this.MaxFrameSize;
			settings.SslStreamUpgrade = this.SslStreamUpgrade;
			settings.UseSslStreamSecurity = this.UseSslStreamSecurity;
		}

		internal void CopyFrom(AmqpTransportSettingsElement settingsElement)
		{
			this.MaxFrameSize = settingsElement.MaxFrameSize;
			this.SslStreamUpgrade = settingsElement.SslStreamUpgrade;
			this.UseSslStreamSecurity = settingsElement.UseSslStreamSecurity;
		}

		internal void InitializeFrom(AmqpTransportSettings settings)
		{
			this.MaxFrameSize = settings.MaxFrameSize;
			this.SslStreamUpgrade = settings.SslStreamUpgrade;
			this.UseSslStreamSecurity = settings.UseSslStreamSecurity;
		}
	}
}