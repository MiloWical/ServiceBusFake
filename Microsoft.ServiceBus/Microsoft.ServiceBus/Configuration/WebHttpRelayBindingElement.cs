using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Diagnostics;
using System;
using System.ComponentModel;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.Text;
using System.Xml;

namespace Microsoft.ServiceBus.Configuration
{
	public class WebHttpRelayBindingElement : StandardBindingElement
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
				return typeof(WebHttpRelayBinding);
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
					properties.Add(new ConfigurationProperty("proxyAddress", typeof(Uri), null, null, null, ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("readerQuotas", typeof(Microsoft.ServiceBus.Configuration.XmlDictionaryReaderQuotasElement), null, null, null, ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("security", typeof(WebHttpRelaySecurityElement), null, null, null, ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("writeEncoding", typeof(Encoding), "utf-8", new Microsoft.ServiceBus.Configuration.EncodingConverter(), null, ConfigurationPropertyOptions.None));
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
		public WebHttpRelaySecurityElement Security
		{
			get
			{
				return (WebHttpRelaySecurityElement)base["security"];
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

		[ConfigurationProperty("writeEncoding", DefaultValue="utf-8")]
		[TypeConverter(typeof(Microsoft.ServiceBus.Configuration.EncodingConverter))]
		[Microsoft.ServiceBus.Configuration.WebEncodingValidator]
		public Encoding WriteEncoding
		{
			get
			{
				return (Encoding)base["writeEncoding"];
			}
			set
			{
				base["writeEncoding"] = value;
			}
		}

		public WebHttpRelayBindingElement(string name) : base(name)
		{
		}

		public WebHttpRelayBindingElement() : this(null)
		{
		}

		private void ApplyReaderQuotasConfiguration(XmlDictionaryReaderQuotas readerQuotas)
		{
			if (readerQuotas == null)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("readerQuotas");
			}
			if (this.ReaderQuotas.MaxDepth != 0)
			{
				readerQuotas.MaxDepth = this.ReaderQuotas.MaxDepth;
			}
			if (this.ReaderQuotas.MaxStringContentLength != 0)
			{
				readerQuotas.MaxStringContentLength = this.ReaderQuotas.MaxStringContentLength;
			}
			if (this.ReaderQuotas.MaxArrayLength != 0)
			{
				readerQuotas.MaxArrayLength = this.ReaderQuotas.MaxArrayLength;
			}
			if (this.ReaderQuotas.MaxBytesPerRead != 0)
			{
				readerQuotas.MaxBytesPerRead = this.ReaderQuotas.MaxBytesPerRead;
			}
			if (this.ReaderQuotas.MaxNameTableCharCount != 0)
			{
				readerQuotas.MaxNameTableCharCount = this.ReaderQuotas.MaxNameTableCharCount;
			}
		}

		protected override void InitializeFrom(Binding binding)
		{
			base.InitializeFrom(binding);
			WebHttpRelayBinding webHttpRelayBinding = (WebHttpRelayBinding)binding;
			this.MaxBufferSize = webHttpRelayBinding.MaxBufferSize;
			this.MaxBufferPoolSize = webHttpRelayBinding.MaxBufferPoolSize;
			this.MaxReceivedMessageSize = webHttpRelayBinding.MaxReceivedMessageSize;
			if (webHttpRelayBinding.ProxyAddress != null)
			{
				this.ProxyAddress = webHttpRelayBinding.ProxyAddress;
			}
			this.WriteEncoding = webHttpRelayBinding.WriteEncoding;
			this.TransferMode = webHttpRelayBinding.TransferMode;
			this.UseDefaultWebProxy = webHttpRelayBinding.UseDefaultWebProxy;
			this.AllowCookies = webHttpRelayBinding.AllowCookies;
			this.Security.InitializeFrom(webHttpRelayBinding.Security);
			this.InitializeReaderQuotas(webHttpRelayBinding.ReaderQuotas);
			this.IsDynamic = webHttpRelayBinding.IsDynamic;
		}

		internal void InitializeReaderQuotas(XmlDictionaryReaderQuotas readerQuotas)
		{
			if (readerQuotas == null)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("readerQuotas");
			}
			this.ReaderQuotas.MaxDepth = readerQuotas.MaxDepth;
			this.ReaderQuotas.MaxStringContentLength = readerQuotas.MaxStringContentLength;
			this.ReaderQuotas.MaxArrayLength = readerQuotas.MaxArrayLength;
			this.ReaderQuotas.MaxBytesPerRead = readerQuotas.MaxBytesPerRead;
			this.ReaderQuotas.MaxNameTableCharCount = readerQuotas.MaxNameTableCharCount;
		}

		protected override void OnApplyConfiguration(Binding binding)
		{
			WebHttpRelayBinding maxBufferPoolSize = (WebHttpRelayBinding)binding;
			maxBufferPoolSize.MaxBufferPoolSize = this.MaxBufferPoolSize;
			maxBufferPoolSize.MaxReceivedMessageSize = this.MaxReceivedMessageSize;
			maxBufferPoolSize.WriteEncoding = this.WriteEncoding;
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
			this.ApplyReaderQuotasConfiguration(maxBufferPoolSize.ReaderQuotas);
		}
	}
}