using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Channels;
using System;
using System.ComponentModel;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.Text;

namespace Microsoft.ServiceBus.Configuration
{
	public class BasicHttpRelayBindingElement : StandardBindingElement
	{
		private ConfigurationPropertyCollection properties;

		[ConfigurationProperty("allowCookies", DefaultValue=false)]
		public bool AllowCookies
		{
			get
			{
				return (bool)base["allowCookies"];
			}
			set
			{
				base["allowCookies"] = value;
			}
		}

		protected override Type BindingElementType
		{
			get
			{
				return typeof(BasicHttpRelayBinding);
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
					properties.Add(new ConfigurationProperty("allowCookies", typeof(bool), false, null, null, ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("maxBufferSize", typeof(int), (object)65536, null, new IntegerValidator(1, 2147483647, false), ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("maxBufferPoolSize", typeof(long), (object)((long)524288), null, new LongValidator((long)0, 9223372036854775807L, false), ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("maxReceivedMessageSize", typeof(long), (object)((long)65536), null, new LongValidator((long)1, 9223372036854775807L, false), ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("messageEncoding", typeof(WSMessageEncoding), (object)WSMessageEncoding.Text, null, new Microsoft.ServiceBus.Configuration.ServiceModelEnumValidator(typeof(Microsoft.ServiceBus.WSMessageEncodingHelper)), ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("proxyAddress", typeof(Uri), null, null, null, ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("readerQuotas", typeof(Microsoft.ServiceBus.Configuration.XmlDictionaryReaderQuotasElement), null, null, null, ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("security", typeof(BasicHttpRelaySecurityElement), null, null, null, ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("textEncoding", typeof(Encoding), "utf-8", new Microsoft.ServiceBus.Configuration.EncodingConverter(), null, ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("transferMode", typeof(System.ServiceModel.TransferMode), (object)System.ServiceModel.TransferMode.Buffered, null, new Microsoft.ServiceBus.Configuration.ServiceModelEnumValidator(typeof(Microsoft.ServiceBus.Channels.TransferModeHelper)), ConfigurationPropertyOptions.None));
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

		[ConfigurationProperty("security")]
		public BasicHttpRelaySecurityElement Security
		{
			get
			{
				return (BasicHttpRelaySecurityElement)base["security"];
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

		public BasicHttpRelayBindingElement(string name) : base(name)
		{
		}

		public BasicHttpRelayBindingElement() : this(null)
		{
		}

		protected override void InitializeFrom(Binding binding)
		{
			base.InitializeFrom(binding);
			BasicHttpRelayBinding basicHttpRelayBinding = (BasicHttpRelayBinding)binding;
			this.MaxBufferSize = basicHttpRelayBinding.MaxBufferSize;
			this.MaxBufferPoolSize = basicHttpRelayBinding.MaxBufferPoolSize;
			this.MaxReceivedMessageSize = basicHttpRelayBinding.MaxReceivedMessageSize;
			this.MessageEncoding = basicHttpRelayBinding.MessageEncoding;
			if (basicHttpRelayBinding.ProxyAddress != null)
			{
				this.ProxyAddress = basicHttpRelayBinding.ProxyAddress;
			}
			this.TextEncoding = basicHttpRelayBinding.TextEncoding;
			this.TransferMode = basicHttpRelayBinding.TransferMode;
			this.UseDefaultWebProxy = basicHttpRelayBinding.UseDefaultWebProxy;
			this.IsDynamic = basicHttpRelayBinding.IsDynamic;
			this.AllowCookies = basicHttpRelayBinding.AllowCookies;
			this.Security.InitializeFrom(basicHttpRelayBinding.Security);
			this.ReaderQuotas.InitializeFrom(basicHttpRelayBinding.ReaderQuotas);
		}

		protected override void OnApplyConfiguration(Binding binding)
		{
			BasicHttpRelayBinding maxBufferPoolSize = (BasicHttpRelayBinding)binding;
			maxBufferPoolSize.MaxBufferPoolSize = this.MaxBufferPoolSize;
			maxBufferPoolSize.MaxReceivedMessageSize = this.MaxReceivedMessageSize;
			maxBufferPoolSize.MessageEncoding = this.MessageEncoding;
			maxBufferPoolSize.TextEncoding = this.TextEncoding;
			maxBufferPoolSize.TransferMode = this.TransferMode;
			maxBufferPoolSize.UseDefaultWebProxy = this.UseDefaultWebProxy;
			maxBufferPoolSize.IsDynamic = this.IsDynamic;
			maxBufferPoolSize.AllowCookies = this.AllowCookies;
			if (this.ProxyAddress != null)
			{
				maxBufferPoolSize.ProxyAddress = this.ProxyAddress;
			}
			if (base.ElementInformation.Properties["maxBufferSize"].ValueOrigin != PropertyValueOrigin.Default)
			{
				maxBufferPoolSize.MaxBufferSize = this.MaxBufferSize;
			}
			this.Security.ApplyConfiguration(maxBufferPoolSize.Security);
			this.ReaderQuotas.ApplyConfiguration(maxBufferPoolSize.ReaderQuotas);
		}
	}
}