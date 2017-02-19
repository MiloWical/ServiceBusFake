using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Channels;
using System;
using System.ComponentModel;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;

namespace Microsoft.ServiceBus.Configuration
{
	public abstract class ConnectionOrientedTransportElement : TransportElement
	{
		private ConfigurationPropertyCollection properties;

		[ConfigurationProperty("channelInitializationTimeout", DefaultValue="00:01:00")]
		[Microsoft.ServiceBus.Configuration.ServiceModelTimeSpanValidator(MinValueString="00:00:00.0000001")]
		[TypeConverter(typeof(Microsoft.ServiceBus.TimeSpanOrInfiniteConverter))]
		public TimeSpan ChannelInitializationTimeout
		{
			get
			{
				return (TimeSpan)base["channelInitializationTimeout"];
			}
			set
			{
				base["channelInitializationTimeout"] = value;
			}
		}

		[ConfigurationProperty("connectionBufferSize", DefaultValue=65536)]
		[IntegerValidator(MinValue=1)]
		public int ConnectionBufferSize
		{
			get
			{
				return (int)base["connectionBufferSize"];
			}
			set
			{
				base["connectionBufferSize"] = value;
			}
		}

		[ConfigurationProperty("hostNameComparisonMode", DefaultValue=System.ServiceModel.HostNameComparisonMode.StrongWildcard)]
		[Microsoft.ServiceBus.Configuration.ServiceModelEnumValidator(typeof(Microsoft.ServiceBus.Channels.HostNameComparisonModeHelper))]
		public System.ServiceModel.HostNameComparisonMode HostNameComparisonMode
		{
			get
			{
				return (System.ServiceModel.HostNameComparisonMode)base["hostNameComparisonMode"];
			}
			set
			{
				base["hostNameComparisonMode"] = value;
			}
		}

		[ConfigurationProperty("maxBufferSize", DefaultValue=65536)]
		[IntegerValidator(MinValue=1)]
		public int MaxBufferSize
		{
			get
			{
				return (int)base["maxBufferSize"];
			}
			set
			{
				base["maxBufferSize"] = value;
			}
		}

		[ConfigurationProperty("maxOutputDelay", DefaultValue="00:00:00.2")]
		[Microsoft.ServiceBus.Configuration.ServiceModelTimeSpanValidator(MinValueString="00:00:00")]
		[TypeConverter(typeof(Microsoft.ServiceBus.TimeSpanOrInfiniteConverter))]
		public TimeSpan MaxOutputDelay
		{
			get
			{
				return (TimeSpan)base["maxOutputDelay"];
			}
			set
			{
				base["maxOutputDelay"] = value;
			}
		}

		[ConfigurationProperty("maxPendingAccepts", DefaultValue=1)]
		[IntegerValidator(MinValue=1)]
		public int MaxPendingAccepts
		{
			get
			{
				return (int)base["maxPendingAccepts"];
			}
			set
			{
				base["maxPendingAccepts"] = value;
			}
		}

		[ConfigurationProperty("maxPendingConnections", DefaultValue=10)]
		[IntegerValidator(MinValue=1)]
		public int MaxPendingConnections
		{
			get
			{
				return (int)base["maxPendingConnections"];
			}
			set
			{
				base["maxPendingConnections"] = value;
			}
		}

		protected override ConfigurationPropertyCollection Properties
		{
			get
			{
				if (this.properties == null)
				{
					ConfigurationPropertyCollection properties = base.Properties;
					properties.Add(new ConfigurationProperty("channelInitializationTimeout", typeof(TimeSpan), (object)Microsoft.ServiceBus.Channels.ConnectionOrientedTransportDefaults.ChannelInitializationTimeout, null, new Microsoft.ServiceBus.Configuration.TimeSpanOrInfiniteValidator(TimeSpan.Zero, TimeSpan.MaxValue), ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("connectionBufferSize", typeof(int), (object)65536, null, new IntegerValidator(1, 2147483647, false), ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("hostNameComparisonMode", typeof(System.ServiceModel.HostNameComparisonMode), (object)System.ServiceModel.HostNameComparisonMode.StrongWildcard, null, new Microsoft.ServiceBus.Configuration.ServiceModelEnumValidator(typeof(Microsoft.ServiceBus.Channels.HostNameComparisonModeHelper)), ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("maxOutputDelay", typeof(TimeSpan), (object)Microsoft.ServiceBus.Channels.ConnectionOrientedTransportDefaults.MaxOutputDelay, null, new Microsoft.ServiceBus.Configuration.TimeSpanOrInfiniteValidator(TimeSpan.Zero, TimeSpan.MaxValue), ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("maxBufferSize", typeof(int), (object)65536, null, new IntegerValidator(1, 2147483647, false), ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("maxPendingAccepts", typeof(int), (object)65536, null, new IntegerValidator(1, 2147483647, false), ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("maxPendingConnections", typeof(int), (object)65536, null, new IntegerValidator(1, 2147483647, false), ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("maxBufferSize", typeof(int), (object)65536, null, new IntegerValidator(1, 2147483647, false), ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("transferMode", typeof(System.ServiceModel.TransferMode), (object)System.ServiceModel.TransferMode.Buffered, null, new Microsoft.ServiceBus.Configuration.ServiceModelEnumValidator(typeof(Microsoft.ServiceBus.Channels.TransferModeHelper)), ConfigurationPropertyOptions.None));
					this.properties = properties;
				}
				return this.properties;
			}
		}

		[ConfigurationProperty("transferMode", DefaultValue=System.ServiceModel.TransferMode.Buffered)]
		[Microsoft.ServiceBus.Configuration.ServiceModelEnumValidator(typeof(Microsoft.ServiceBus.Channels.TransferModeHelper))]
		public System.ServiceModel.TransferMode TransferMode
		{
			get
			{
				return (System.ServiceModel.TransferMode)base["transferMode"];
			}
			set
			{
				base["transferMode"] = value;
			}
		}

		internal ConnectionOrientedTransportElement()
		{
		}

		public override void ApplyConfiguration(BindingElement bindingElement)
		{
			base.ApplyConfiguration(bindingElement);
			Microsoft.ServiceBus.Channels.ConnectionOrientedTransportBindingElement channelInitializationTimeout = bindingElement as Microsoft.ServiceBus.Channels.ConnectionOrientedTransportBindingElement;
			int? nullable = null;
			if (base.ElementInformation.Properties["maxBufferSize"].ValueOrigin != PropertyValueOrigin.Default)
			{
				nullable = new int?(this.MaxBufferSize);
			}
			if (channelInitializationTimeout == null)
			{
				RelayedOnewayTransportBindingElement connectionBufferSize = bindingElement as RelayedOnewayTransportBindingElement;
				if (connectionBufferSize == null)
				{
					throw new InvalidCastException(SRClient.ExpectedTypeInvalidCastException(typeof(Microsoft.ServiceBus.Channels.ConnectionOrientedTransportBindingElement).ToString(), typeof(RelayedOnewayTransportBindingElement).ToString(), bindingElement.GetType().ToString()));
				}
				connectionBufferSize.ChannelInitializationTimeout = this.ChannelInitializationTimeout;
				connectionBufferSize.ConnectionBufferSize = this.ConnectionBufferSize;
				connectionBufferSize.MaxPendingAccepts = this.MaxPendingAccepts;
				connectionBufferSize.MaxPendingConnections = this.MaxPendingConnections;
				if (nullable.HasValue)
				{
					connectionBufferSize.MaxBufferSize = nullable.Value;
					return;
				}
			}
			else
			{
				channelInitializationTimeout.ChannelInitializationTimeout = this.ChannelInitializationTimeout;
				channelInitializationTimeout.ConnectionBufferSize = this.ConnectionBufferSize;
				channelInitializationTimeout.HostNameComparisonMode = this.HostNameComparisonMode;
				channelInitializationTimeout.MaxPendingConnections = this.MaxPendingConnections;
				channelInitializationTimeout.MaxOutputDelay = this.MaxOutputDelay;
				channelInitializationTimeout.MaxPendingAccepts = this.MaxPendingAccepts;
				channelInitializationTimeout.TransferMode = this.TransferMode;
				if (nullable.HasValue)
				{
					channelInitializationTimeout.MaxBufferSize = nullable.Value;
					return;
				}
			}
		}

		public override void CopyFrom(ServiceModelExtensionElement from)
		{
			base.CopyFrom(from);
			Microsoft.ServiceBus.Configuration.ConnectionOrientedTransportElement connectionOrientedTransportElement = (Microsoft.ServiceBus.Configuration.ConnectionOrientedTransportElement)from;
			this.ConnectionBufferSize = connectionOrientedTransportElement.ConnectionBufferSize;
			this.MaxBufferSize = connectionOrientedTransportElement.MaxBufferSize;
			this.TransferMode = connectionOrientedTransportElement.TransferMode;
		}
	}
}