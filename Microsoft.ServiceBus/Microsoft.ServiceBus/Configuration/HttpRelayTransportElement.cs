using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Channels;
using System;
using System.Configuration;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;

namespace Microsoft.ServiceBus.Configuration
{
	public class HttpRelayTransportElement : TransportElement
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

		public override Type BindingElementType
		{
			get
			{
				return typeof(HttpRelayTransportBindingElement);
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

		[ConfigurationProperty("keepAliveEnabled", DefaultValue=true)]
		public bool KeepAliveEnabled
		{
			get
			{
				return (bool)base["keepAliveEnabled"];
			}
			set
			{
				base["keepAliveEnabled"] = value;
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

		protected override ConfigurationPropertyCollection Properties
		{
			get
			{
				if (this.properties == null)
				{
					ConfigurationPropertyCollection properties = base.Properties;
					properties.Add(new ConfigurationProperty("allowCookies", typeof(bool), false, null, null, ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("relayClientAuthenticationType", typeof(Microsoft.ServiceBus.RelayClientAuthenticationType), (object)Microsoft.ServiceBus.RelayClientAuthenticationType.RelayAccessToken, null, new Microsoft.ServiceBus.Configuration.ServiceModelEnumValidator(typeof(RelayClientAuthenticationTypeHelper)), ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("keepAliveEnabled", typeof(bool), true, null, null, ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("maxBufferSize", typeof(int), (object)65536, null, new IntegerValidator(1, 2147483647, false), ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("proxyAddress", typeof(Uri), null, null, null, ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("proxyAuthenticationScheme", typeof(AuthenticationSchemes), (object)AuthenticationSchemes.Anonymous, null, null, ConfigurationPropertyOptions.None));
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

		[ConfigurationProperty("proxyAuthenticationScheme", DefaultValue=AuthenticationSchemes.Anonymous)]
		public AuthenticationSchemes ProxyAuthenticationScheme
		{
			get
			{
				return (AuthenticationSchemes)base["proxyAuthenticationScheme"];
			}
			set
			{
				base["proxyAuthenticationScheme"] = value;
			}
		}

		[ConfigurationProperty("relayClientAuthenticationType", DefaultValue=Microsoft.ServiceBus.RelayClientAuthenticationType.RelayAccessToken)]
		[Microsoft.ServiceBus.Configuration.ServiceModelEnumValidator(typeof(RelayClientAuthenticationTypeHelper))]
		public Microsoft.ServiceBus.RelayClientAuthenticationType RelayClientAuthenticationType
		{
			get
			{
				return (Microsoft.ServiceBus.RelayClientAuthenticationType)base["relayClientAuthenticationType"];
			}
			set
			{
				base["relayClientAuthenticationType"] = value;
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

		public HttpRelayTransportElement()
		{
		}

		public override void ApplyConfiguration(BindingElement bindingElement)
		{
			base.ApplyConfiguration(bindingElement);
			HttpRelayTransportBindingElement allowCookies = (HttpRelayTransportBindingElement)bindingElement;
			allowCookies.AllowCookies = this.AllowCookies;
			allowCookies.RelayClientAuthenticationType = this.RelayClientAuthenticationType;
			allowCookies.KeepAliveEnabled = this.KeepAliveEnabled;
			if (base.ElementInformation.Properties["maxBufferSize"].ValueOrigin != PropertyValueOrigin.Default)
			{
				allowCookies.MaxBufferSize = this.MaxBufferSize;
			}
			allowCookies.ProxyAddress = this.ProxyAddress;
			allowCookies.ProxyAuthenticationScheme = this.ProxyAuthenticationScheme;
			allowCookies.RelayClientAuthenticationType = this.RelayClientAuthenticationType;
			allowCookies.TransferMode = this.TransferMode;
			allowCookies.UseDefaultWebProxy = this.UseDefaultWebProxy;
			allowCookies.IsDynamic = this.IsDynamic;
		}

		public override void CopyFrom(ServiceModelExtensionElement from)
		{
			base.CopyFrom(from);
			HttpRelayTransportElement httpRelayTransportElement = (HttpRelayTransportElement)from;
			this.AllowCookies = httpRelayTransportElement.AllowCookies;
			this.RelayClientAuthenticationType = httpRelayTransportElement.RelayClientAuthenticationType;
			this.KeepAliveEnabled = httpRelayTransportElement.KeepAliveEnabled;
			this.MaxBufferSize = httpRelayTransportElement.MaxBufferSize;
			this.ProxyAddress = httpRelayTransportElement.ProxyAddress;
			this.ProxyAuthenticationScheme = httpRelayTransportElement.ProxyAuthenticationScheme;
			this.RelayClientAuthenticationType = httpRelayTransportElement.RelayClientAuthenticationType;
			this.TransferMode = httpRelayTransportElement.TransferMode;
			this.UseDefaultWebProxy = httpRelayTransportElement.UseDefaultWebProxy;
			this.IsDynamic = httpRelayTransportElement.IsDynamic;
		}

		protected override TransportBindingElement CreateDefaultBindingElement()
		{
			return new HttpRelayTransportBindingElement();
		}

		protected override void InitializeFrom(BindingElement bindingElement)
		{
			base.InitializeFrom(bindingElement);
			HttpRelayTransportBindingElement httpRelayTransportBindingElement = (HttpRelayTransportBindingElement)bindingElement;
			this.AllowCookies = httpRelayTransportBindingElement.AllowCookies;
			this.RelayClientAuthenticationType = httpRelayTransportBindingElement.RelayClientAuthenticationType;
			this.KeepAliveEnabled = httpRelayTransportBindingElement.KeepAliveEnabled;
			this.MaxBufferSize = httpRelayTransportBindingElement.MaxBufferSize;
			this.ProxyAddress = httpRelayTransportBindingElement.ProxyAddress;
			this.ProxyAuthenticationScheme = httpRelayTransportBindingElement.ProxyAuthenticationScheme;
			this.RelayClientAuthenticationType = httpRelayTransportBindingElement.RelayClientAuthenticationType;
			this.TransferMode = httpRelayTransportBindingElement.TransferMode;
			this.UseDefaultWebProxy = httpRelayTransportBindingElement.UseDefaultWebProxy;
			this.IsDynamic = httpRelayTransportBindingElement.IsDynamic;
		}
	}
}