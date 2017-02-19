using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Channels;
using System;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;

namespace Microsoft.ServiceBus.Configuration
{
	public class NetTcpRelayBindingElement : StandardBindingElement
	{
		private ConfigurationPropertyCollection properties;

		protected override Type BindingElementType
		{
			get
			{
				return typeof(NetTcpRelayBinding);
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
					properties.Add(new ConfigurationProperty("transferMode", typeof(System.ServiceModel.TransferMode), (object)System.ServiceModel.TransferMode.Buffered, null, new Microsoft.ServiceBus.Configuration.ServiceModelEnumValidator(typeof(Microsoft.ServiceBus.Channels.TransferModeHelper)), ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("connectionMode", typeof(TcpRelayConnectionMode), (object)TcpRelayConnectionMode.Relayed, null, null, ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("listenBacklog", typeof(int), (object)10, null, new IntegerValidator(1, 2147483647, false), ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("maxBufferPoolSize", typeof(long), (object)((long)524288), null, new LongValidator((long)0, 9223372036854775807L, false), ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("maxBufferSize", typeof(int), (object)65536, null, new IntegerValidator(1, 2147483647, false), ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("maxConnections", typeof(int), (object)10, null, new IntegerValidator(1, 2147483647, false), ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("maxReceivedMessageSize", typeof(long), (object)((long)65536), null, new LongValidator((long)1, 9223372036854775807L, false), ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("readerQuotas", typeof(Microsoft.ServiceBus.Configuration.XmlDictionaryReaderQuotasElement), null, null, null, ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("reliableSession", typeof(StandardBindingOptionalReliableSessionElement), null, null, null, ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("security", typeof(NetTcpRelaySecurityElement), null, null, null, ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("isDynamic", typeof(bool), true, null, null, ConfigurationPropertyOptions.None));
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

		[ConfigurationProperty("reliableSession")]
		public StandardBindingOptionalReliableSessionElement ReliableSession
		{
			get
			{
				return (StandardBindingOptionalReliableSessionElement)base["reliableSession"];
			}
		}

		[ConfigurationProperty("security")]
		public NetTcpRelaySecurityElement Security
		{
			get
			{
				return (NetTcpRelaySecurityElement)base["security"];
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

		public NetTcpRelayBindingElement(string name) : base(name)
		{
		}

		public NetTcpRelayBindingElement() : this(null)
		{
		}

		protected override void InitializeFrom(Binding binding)
		{
			base.InitializeFrom(binding);
			NetTcpRelayBinding netTcpRelayBinding = (NetTcpRelayBinding)binding;
			this.ConnectionMode = netTcpRelayBinding.ConnectionMode;
			this.TransferMode = netTcpRelayBinding.TransferMode;
			this.MaxBufferPoolSize = netTcpRelayBinding.MaxBufferPoolSize;
			this.MaxBufferSize = netTcpRelayBinding.MaxBufferSize;
			this.MaxConnections = netTcpRelayBinding.MaxConnections;
			this.MaxReceivedMessageSize = netTcpRelayBinding.MaxReceivedMessageSize;
			this.ListenBacklog = netTcpRelayBinding.ListenBacklog;
			this.ReliableSession.InitializeFrom(netTcpRelayBinding.ReliableSession);
			this.Security.InitializeFrom(netTcpRelayBinding.Security);
			this.ReaderQuotas.InitializeFrom(netTcpRelayBinding.ReaderQuotas);
			this.IsDynamic = netTcpRelayBinding.IsDynamic;
		}

		protected override void OnApplyConfiguration(Binding binding)
		{
			NetTcpRelayBinding transferMode = (NetTcpRelayBinding)binding;
			transferMode.TransferMode = this.TransferMode;
			transferMode.ConnectionMode = this.ConnectionMode;
			transferMode.ListenBacklog = this.ListenBacklog;
			transferMode.MaxBufferPoolSize = this.MaxBufferPoolSize;
			if (base.ElementInformation.Properties["maxBufferSize"].ValueOrigin != PropertyValueOrigin.Default)
			{
				transferMode.MaxBufferSize = this.MaxBufferSize;
			}
			transferMode.MaxConnections = this.MaxConnections;
			transferMode.MaxReceivedMessageSize = this.MaxReceivedMessageSize;
			this.ReliableSession.ApplyConfiguration(transferMode.ReliableSession);
			this.Security.ApplyConfiguration(transferMode.Security);
			this.ReaderQuotas.ApplyConfiguration(transferMode.ReaderQuotas);
			transferMode.IsDynamic = this.IsDynamic;
		}
	}
}