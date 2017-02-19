using Microsoft.ServiceBus;
using System;
using System.Configuration;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;

namespace Microsoft.ServiceBus.Configuration
{
	public class NetEventRelayBindingElement : StandardBindingElement
	{
		private ConfigurationPropertyCollection properties;

		protected override Type BindingElementType
		{
			get
			{
				return typeof(NetEventRelayBinding);
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

		[ConfigurationProperty("maxBufferPoolSize", DefaultValue=524288L)]
		[LongValidator(MinValue=0L)]
		public long MaxBufferPoolSize
		{
			get
			{
				return (long)base["maxBufferPoolSize"];
			}
			set
			{
				base["maxBufferPoolSize"] = value;
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

		[ConfigurationProperty("maxConnections", DefaultValue=10)]
		[IntegerValidator(MinValue=1)]
		public int MaxConnections
		{
			get
			{
				return (int)base["maxConnections"];
			}
			set
			{
				base["maxConnections"] = value;
			}
		}

		[ConfigurationProperty("maxReceivedMessageSize", DefaultValue=65536L)]
		[LongValidator(MinValue=1L)]
		public long MaxReceivedMessageSize
		{
			get
			{
				return (long)base["maxReceivedMessageSize"];
			}
			set
			{
				base["maxReceivedMessageSize"] = value;
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
					properties.Add(new ConfigurationProperty("maxBufferPoolSize", typeof(long), (object)((long)524288), null, new LongValidator((long)0, 9223372036854775807L, false), ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("maxBufferSize", typeof(int), (object)65536, null, new IntegerValidator(1, 2147483647, false), ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("maxConnections", typeof(int), (object)10, null, new IntegerValidator(1, 2147483647, false), ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("maxReceivedMessageSize", typeof(long), (object)((long)65536), null, new LongValidator((long)1, 9223372036854775807L, false), ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("readerQuotas", typeof(Microsoft.ServiceBus.Configuration.XmlDictionaryReaderQuotasElement), null, null, null, ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("security", typeof(NetOnewayRelaySecurityElement), null, null, null, ConfigurationPropertyOptions.None));
					this.properties = properties;
				}
				return this.properties;
			}
		}

		[ConfigurationProperty("readerQuotas")]
		public Microsoft.ServiceBus.Configuration.XmlDictionaryReaderQuotasElement ReaderQuotas
		{
			get
			{
				return (Microsoft.ServiceBus.Configuration.XmlDictionaryReaderQuotasElement)base["readerQuotas"];
			}
		}

		[ConfigurationProperty("security")]
		public NetOnewayRelaySecurityElement Security
		{
			get
			{
				return (NetOnewayRelaySecurityElement)base["security"];
			}
		}

		public NetEventRelayBindingElement(string name) : base(name)
		{
		}

		public NetEventRelayBindingElement() : this(null)
		{
		}

		protected override void InitializeFrom(Binding binding)
		{
			base.InitializeFrom(binding);
			NetEventRelayBinding netEventRelayBinding = (NetEventRelayBinding)binding;
			this.MaxBufferPoolSize = netEventRelayBinding.MaxBufferPoolSize;
			this.MaxBufferSize = netEventRelayBinding.MaxBufferSize;
			this.MaxConnections = netEventRelayBinding.MaxConnections;
			this.MaxReceivedMessageSize = netEventRelayBinding.MaxReceivedMessageSize;
			this.ListenBacklog = netEventRelayBinding.ListenBacklog;
			this.Security.InitializeFrom(netEventRelayBinding.Security);
			this.ReaderQuotas.InitializeFrom(netEventRelayBinding.ReaderQuotas);
		}

		protected override void OnApplyConfiguration(Binding binding)
		{
			NetEventRelayBinding listenBacklog = (NetEventRelayBinding)binding;
			listenBacklog.ListenBacklog = this.ListenBacklog;
			listenBacklog.MaxBufferPoolSize = this.MaxBufferPoolSize;
			if (base.ElementInformation.Properties["maxBufferSize"].ValueOrigin != PropertyValueOrigin.Default)
			{
				listenBacklog.MaxBufferSize = this.MaxBufferSize;
			}
			listenBacklog.MaxConnections = this.MaxConnections;
			listenBacklog.MaxReceivedMessageSize = this.MaxReceivedMessageSize;
			this.Security.ApplyConfiguration(listenBacklog.Security);
			this.ReaderQuotas.ApplyConfiguration(listenBacklog.ReaderQuotas);
		}
	}
}