using Microsoft.ServiceBus.Configuration;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.Text;
using System.Xml;

namespace Microsoft.ServiceBus
{
	public class WebHttpRelayBinding : Binding, IBindingRuntimePreferences
	{
		private HttpsRelayTransportBindingElement httpsRelayTransportBindingElement;

		private HttpRelayTransportBindingElement httpRelayTransportBindingElement;

		private WebHttpRelaySecurity security = new WebHttpRelaySecurity();

		private WebMessageEncodingBindingElement webMessageEncodingBindingElement;

		public bool AllowCookies
		{
			get
			{
				return this.httpRelayTransportBindingElement.AllowCookies;
			}
			set
			{
				this.httpRelayTransportBindingElement.AllowCookies = value;
				this.httpsRelayTransportBindingElement.AllowCookies = value;
			}
		}

		public WebContentTypeMapper ContentTypeMapper
		{
			get
			{
				return this.webMessageEncodingBindingElement.ContentTypeMapper;
			}
			set
			{
				this.webMessageEncodingBindingElement.ContentTypeMapper = value;
			}
		}

		public System.ServiceModel.EnvelopeVersion EnvelopeVersion
		{
			get
			{
				return System.ServiceModel.EnvelopeVersion.None;
			}
		}

		public System.ServiceModel.HostNameComparisonMode HostNameComparisonMode
		{
			get
			{
				return this.httpRelayTransportBindingElement.HostNameComparisonMode;
			}
			set
			{
				this.httpRelayTransportBindingElement.HostNameComparisonMode = value;
				this.httpsRelayTransportBindingElement.HostNameComparisonMode = value;
			}
		}

		public bool IsDynamic
		{
			get
			{
				return this.httpRelayTransportBindingElement.IsDynamic;
			}
			set
			{
				this.httpRelayTransportBindingElement.IsDynamic = value;
				this.httpsRelayTransportBindingElement.IsDynamic = value;
			}
		}

		public long MaxBufferPoolSize
		{
			get
			{
				return this.httpRelayTransportBindingElement.MaxBufferPoolSize;
			}
			set
			{
				this.httpRelayTransportBindingElement.MaxBufferPoolSize = value;
				this.httpsRelayTransportBindingElement.MaxBufferPoolSize = value;
			}
		}

		public int MaxBufferSize
		{
			get
			{
				return this.httpRelayTransportBindingElement.MaxBufferSize;
			}
			set
			{
				this.httpRelayTransportBindingElement.MaxBufferSize = value;
				this.httpsRelayTransportBindingElement.MaxBufferSize = value;
			}
		}

		public long MaxReceivedMessageSize
		{
			get
			{
				return this.httpRelayTransportBindingElement.MaxReceivedMessageSize;
			}
			set
			{
				this.httpRelayTransportBindingElement.MaxReceivedMessageSize = value;
				this.httpsRelayTransportBindingElement.MaxReceivedMessageSize = value;
			}
		}

		public Uri ProxyAddress
		{
			get
			{
				return this.httpRelayTransportBindingElement.ProxyAddress;
			}
			set
			{
				this.httpRelayTransportBindingElement.ProxyAddress = value;
				this.httpsRelayTransportBindingElement.ProxyAddress = value;
			}
		}

		public XmlDictionaryReaderQuotas ReaderQuotas
		{
			get
			{
				return this.webMessageEncodingBindingElement.ReaderQuotas;
			}
			set
			{
				if (value == null)
				{
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("value");
				}
				value.CopyTo(this.webMessageEncodingBindingElement.ReaderQuotas);
			}
		}

		public override string Scheme
		{
			get
			{
				return this.GetTransport().Scheme;
			}
		}

		public WebHttpRelaySecurity Security
		{
			get
			{
				return this.security;
			}
		}

		bool System.ServiceModel.Channels.IBindingRuntimePreferences.ReceiveSynchronously
		{
			get
			{
				return false;
			}
		}

		public System.ServiceModel.TransferMode TransferMode
		{
			get
			{
				return this.httpRelayTransportBindingElement.TransferMode;
			}
			set
			{
				this.httpRelayTransportBindingElement.TransferMode = value;
				this.httpsRelayTransportBindingElement.TransferMode = value;
			}
		}

		public bool UseDefaultWebProxy
		{
			get
			{
				return this.httpRelayTransportBindingElement.UseDefaultWebProxy;
			}
			set
			{
				this.httpRelayTransportBindingElement.UseDefaultWebProxy = value;
				this.httpsRelayTransportBindingElement.UseDefaultWebProxy = value;
			}
		}

		public Encoding WriteEncoding
		{
			get
			{
				return this.webMessageEncodingBindingElement.WriteEncoding;
			}
			set
			{
				this.webMessageEncodingBindingElement.WriteEncoding = value;
			}
		}

		public WebHttpRelayBinding() : this(EndToEndWebHttpSecurityMode.Transport, RelayClientAuthenticationType.RelayAccessToken)
		{
		}

		public WebHttpRelayBinding(string configurationName) : this()
		{
			this.ApplyConfiguration(configurationName);
		}

		public WebHttpRelayBinding(EndToEndWebHttpSecurityMode securityMode, RelayClientAuthenticationType relayClientAuthenticationType)
		{
			this.Initialize();
			this.security.RelayClientAuthenticationType = relayClientAuthenticationType;
			this.security.Mode = securityMode;
		}

		private void ApplyConfiguration(string configurationName)
		{
			WebHttpRelayBindingElement item = WebHttpRelayBindingCollectionElement.GetBindingCollectionElement().Bindings[configurationName];
			if (item == null)
			{
				ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
				string configInvalidBindingConfigurationName = Resources.ConfigInvalidBindingConfigurationName;
				object[] objArray = new object[] { configurationName, "webHttpRelayBinding" };
				throw exceptionUtility.ThrowHelperError(new ConfigurationErrorsException(Microsoft.ServiceBus.SR.GetString(configInvalidBindingConfigurationName, objArray)));
			}
			item.ApplyConfiguration(this);
		}

		public override BindingElementCollection CreateBindingElements()
		{
			BindingElementCollection bindingElementCollection = new BindingElementCollection()
			{
				this.webMessageEncodingBindingElement,
				this.GetTransport()
			};
			return bindingElementCollection.Clone();
		}

		private TransportBindingElement GetTransport()
		{
			if (this.security.Mode == EndToEndWebHttpSecurityMode.Transport)
			{
				this.security.EnableTransportSecurity(this.httpsRelayTransportBindingElement);
				this.httpsRelayTransportBindingElement.RelayClientAuthenticationType = this.Security.RelayClientAuthenticationType;
				return this.httpsRelayTransportBindingElement;
			}
			this.security.DisableTransportAuthentication(this.httpRelayTransportBindingElement);
			this.httpRelayTransportBindingElement.RelayClientAuthenticationType = this.security.RelayClientAuthenticationType;
			return this.httpRelayTransportBindingElement;
		}

		private void Initialize()
		{
			this.httpRelayTransportBindingElement = new HttpRelayTransportBindingElement();
			this.httpsRelayTransportBindingElement = new HttpsRelayTransportBindingElement();
			this.httpRelayTransportBindingElement.ManualAddressing = true;
			this.httpsRelayTransportBindingElement.ManualAddressing = true;
			this.webMessageEncodingBindingElement = new WebMessageEncodingBindingElement()
			{
				MessageVersion = System.ServiceModel.Channels.MessageVersion.None
			};
		}

		internal static class WebHttpRelayBindingConfigurationStrings
		{
			internal const string WebHttpRelayBindingCollectionElementName = "webHttpRelayBinding";
		}
	}
}