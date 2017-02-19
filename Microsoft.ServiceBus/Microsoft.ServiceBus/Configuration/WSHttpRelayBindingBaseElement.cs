using Microsoft.ServiceBus;
using System;
using System.ComponentModel;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.Text;

namespace Microsoft.ServiceBus.Configuration
{
	public abstract class WSHttpRelayBindingBaseElement : StandardBindingElement
	{
		private ConfigurationPropertyCollection properties;

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

		[ConfigurationProperty("messageEncoding", DefaultValue=WSMessageEncoding.Text)]
		[Microsoft.ServiceBus.Configuration.ServiceModelEnumValidator(typeof(Microsoft.ServiceBus.WSMessageEncodingHelper))]
		public WSMessageEncoding MessageEncoding
		{
			get
			{
				return (WSMessageEncoding)base["messageEncoding"];
			}
			set
			{
				base["messageEncoding"] = value;
			}
		}

		protected override ConfigurationPropertyCollection Properties
		{
			get
			{
				if (this.properties == null)
				{
					ConfigurationPropertyCollection properties = base.Properties;
					properties.Add(new ConfigurationProperty("bypassProxyOnLocal", typeof(bool), false, null, null, ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("maxBufferPoolSize", typeof(long), (object)((long)524288), null, new LongValidator((long)0, 9223372036854775807L, false), ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("maxReceivedMessageSize", typeof(long), (object)((long)65536), null, new LongValidator((long)1, 9223372036854775807L, false), ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("messageEncoding", typeof(WSMessageEncoding), (object)WSMessageEncoding.Text, null, new Microsoft.ServiceBus.Configuration.ServiceModelEnumValidator(typeof(Microsoft.ServiceBus.WSMessageEncodingHelper)), ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("proxyAddress", typeof(Uri), null, null, null, ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("readerQuotas", typeof(Microsoft.ServiceBus.Configuration.XmlDictionaryReaderQuotasElement), new Microsoft.ServiceBus.Configuration.XmlDictionaryReaderQuotasElement(), null, null, ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("reliableSession", typeof(StandardBindingOptionalReliableSessionElement), new StandardBindingOptionalReliableSessionElement(), null, null, ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("textEncoding", typeof(Encoding), "utf-8", new Microsoft.ServiceBus.Configuration.EncodingConverter(), null, ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("useDefaultWebProxy", typeof(bool), true, null, null, ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("isDynamic", typeof(bool), true, null, null, ConfigurationPropertyOptions.None));
					this.properties = properties;
				}
				return this.properties;
			}
		}

		[ConfigurationProperty("proxyAddress", DefaultValue=null)]
		public Uri ProxyAddress
		{
			get
			{
				return (Uri)base["proxyAddress"];
			}
			set
			{
				base["proxyAddress"] = value;
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

		[ConfigurationProperty("textEncoding", DefaultValue="utf-8")]
		[TypeConverter(typeof(Microsoft.ServiceBus.Configuration.EncodingConverter))]
		public Encoding TextEncoding
		{
			get
			{
				return (Encoding)base["textEncoding"];
			}
			set
			{
				base["textEncoding"] = value;
			}
		}

		[ConfigurationProperty("useDefaultWebProxy", DefaultValue=true)]
		public bool UseDefaultWebProxy
		{
			get
			{
				return (bool)base["useDefaultWebProxy"];
			}
			set
			{
				base["useDefaultWebProxy"] = value;
			}
		}

		protected WSHttpRelayBindingBaseElement(string name) : base(name)
		{
		}

		protected WSHttpRelayBindingBaseElement() : this(null)
		{
		}

		protected override void InitializeFrom(Binding binding)
		{
			base.InitializeFrom(binding);
			WSHttpRelayBindingBase wSHttpRelayBindingBase = (WSHttpRelayBindingBase)binding;
			this.MaxBufferPoolSize = wSHttpRelayBindingBase.MaxBufferPoolSize;
			this.MaxReceivedMessageSize = wSHttpRelayBindingBase.MaxReceivedMessageSize;
			this.MessageEncoding = wSHttpRelayBindingBase.MessageEncoding;
			if (wSHttpRelayBindingBase.ProxyAddress != null)
			{
				this.ProxyAddress = wSHttpRelayBindingBase.ProxyAddress;
			}
			this.TextEncoding = wSHttpRelayBindingBase.TextEncoding;
			this.UseDefaultWebProxy = wSHttpRelayBindingBase.UseDefaultWebProxy;
			this.IsDynamic = wSHttpRelayBindingBase.IsDynamic;
			this.ReaderQuotas.InitializeFrom(wSHttpRelayBindingBase.ReaderQuotas);
			this.ReliableSession.InitializeFrom(wSHttpRelayBindingBase.ReliableSession);
		}

		protected override void OnApplyConfiguration(Binding binding)
		{
			WSHttpRelayBindingBase maxBufferPoolSize = (WSHttpRelayBindingBase)binding;
			maxBufferPoolSize.MaxBufferPoolSize = this.MaxBufferPoolSize;
			maxBufferPoolSize.MaxReceivedMessageSize = this.MaxReceivedMessageSize;
			maxBufferPoolSize.MessageEncoding = this.MessageEncoding;
			if (this.ProxyAddress != null)
			{
				maxBufferPoolSize.ProxyAddress = this.ProxyAddress;
			}
			maxBufferPoolSize.TextEncoding = this.TextEncoding;
			maxBufferPoolSize.UseDefaultWebProxy = this.UseDefaultWebProxy;
			maxBufferPoolSize.IsDynamic = this.IsDynamic;
			this.ReaderQuotas.ApplyConfiguration(maxBufferPoolSize.ReaderQuotas);
			this.ReliableSession.ApplyConfiguration(maxBufferPoolSize.ReliableSession);
		}
	}
}