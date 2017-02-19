using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using System;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;

namespace Microsoft.ServiceBus.Configuration
{
	public sealed class SocketConnectionPoolSettingsElement : ConfigurationElement
	{
		private ConfigurationPropertyCollection properties;

		[ConfigurationProperty("groupName", DefaultValue="default")]
		[StringValidator(MinLength=0)]
		public string GroupName
		{
			get
			{
				return (string)base["groupName"];
			}
			set
			{
				if (string.IsNullOrEmpty(value))
				{
					value = string.Empty;
				}
				base["groupName"] = value;
			}
		}

		[ConfigurationProperty("idleTimeout", DefaultValue="00:02:00")]
		[ServiceModelTimeSpanValidator(MinValueString="00:00:00")]
		[TypeConverter(typeof(TimeSpanOrInfiniteConverter))]
		public TimeSpan IdleTimeout
		{
			get
			{
				return (TimeSpan)base["idleTimeout"];
			}
			set
			{
				base["idleTimeout"] = value;
			}
		}

		[ConfigurationProperty("leaseTimeout", DefaultValue="00:05:00")]
		[ServiceModelTimeSpanValidator(MinValueString="00:00:00")]
		[TypeConverter(typeof(TimeSpanOrInfiniteConverter))]
		public TimeSpan LeaseTimeout
		{
			get
			{
				return (TimeSpan)base["leaseTimeout"];
			}
			set
			{
				base["leaseTimeout"] = value;
			}
		}

		[ConfigurationProperty("maxOutboundConnectionsPerEndpoint", DefaultValue=10)]
		[IntegerValidator(MinValue=0)]
		public int MaxOutboundConnectionsPerEndpoint
		{
			get
			{
				return (int)base["maxOutboundConnectionsPerEndpoint"];
			}
			set
			{
				base["maxOutboundConnectionsPerEndpoint"] = value;
			}
		}

		protected override ConfigurationPropertyCollection Properties
		{
			get
			{
				if (this.properties == null)
				{
					ConfigurationPropertyCollection configurationPropertyCollections = new ConfigurationPropertyCollection()
					{
						new ConfigurationProperty("groupName", typeof(string), "default", null, new StringValidator(0, 2147483647, null), ConfigurationPropertyOptions.None),
						new ConfigurationProperty("leaseTimeout", typeof(TimeSpan), (object)TimeSpan.Parse("00:05:00", CultureInfo.InvariantCulture), new TimeSpanOrInfiniteConverter(), new TimeSpanOrInfiniteValidator(TimeSpan.Parse("00:00:00", CultureInfo.InvariantCulture), TimeSpan.Parse("24.20:31:23.6470000", CultureInfo.InvariantCulture)), ConfigurationPropertyOptions.None),
						new ConfigurationProperty("idleTimeout", typeof(TimeSpan), (object)TimeSpan.Parse("00:02:00", CultureInfo.InvariantCulture), new TimeSpanOrInfiniteConverter(), new TimeSpanOrInfiniteValidator(TimeSpan.Parse("00:00:00", CultureInfo.InvariantCulture), TimeSpan.Parse("24.20:31:23.6470000", CultureInfo.InvariantCulture)), ConfigurationPropertyOptions.None),
						new ConfigurationProperty("maxOutboundConnectionsPerEndpoint", typeof(int), (object)10, null, new IntegerValidator(0, 2147483647, false), ConfigurationPropertyOptions.None)
					};
					this.properties = configurationPropertyCollections;
				}
				return this.properties;
			}
		}

		public SocketConnectionPoolSettingsElement()
		{
		}

		internal void ApplyConfiguration(SocketConnectionPoolSettings settings)
		{
			if (settings == null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("settings");
			}
			settings.GroupName = this.GroupName;
			settings.IdleTimeout = this.IdleTimeout;
			settings.LeaseTimeout = this.LeaseTimeout;
			settings.MaxOutboundConnectionsPerEndpoint = this.MaxOutboundConnectionsPerEndpoint;
		}

		internal void CopyFrom(SocketConnectionPoolSettingsElement source)
		{
			if (source == null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("source");
			}
			this.GroupName = source.GroupName;
			this.IdleTimeout = source.IdleTimeout;
			this.LeaseTimeout = source.LeaseTimeout;
			this.MaxOutboundConnectionsPerEndpoint = source.MaxOutboundConnectionsPerEndpoint;
		}

		internal void InitializeFrom(SocketConnectionPoolSettings settings)
		{
			if (settings == null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("settings");
			}
			this.GroupName = settings.GroupName;
			this.IdleTimeout = settings.IdleTimeout;
			this.LeaseTimeout = settings.LeaseTimeout;
			this.MaxOutboundConnectionsPerEndpoint = settings.MaxOutboundConnectionsPerEndpoint;
		}
	}
}