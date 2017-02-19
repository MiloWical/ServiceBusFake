using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Xml;

namespace Microsoft.ServiceBus
{
	public class HttpRelayTransportBindingElement : TransportBindingElement, IPolicyExportExtension, IWsdlExportExtension, ITransportTokenAssertionProvider
	{
		private bool allowCookies;

		private bool keepAliveEnabled;

		private int maxBufferSize;

		private bool maxBufferSizeInitialized;

		private Uri proxyAddress;

		private AuthenticationSchemes proxyAuthenticationScheme;

		private System.ServiceModel.TransferMode transferMode;

		private bool useDefaultWebProxy;

		private Microsoft.ServiceBus.RelayClientAuthenticationType relayClientAuthenticationType;

		public bool AllowCookies
		{
			get
			{
				return this.allowCookies;
			}
			set
			{
				this.allowCookies = value;
			}
		}

		public System.ServiceModel.HostNameComparisonMode HostNameComparisonMode
		{
			get;
			set;
		}

		public bool IsDynamic
		{
			get;
			set;
		}

		public bool KeepAliveEnabled
		{
			get
			{
				return this.keepAliveEnabled;
			}
			set
			{
				this.keepAliveEnabled = value;
			}
		}

		public int MaxBufferSize
		{
			get
			{
				if (this.maxBufferSizeInitialized || this.TransferMode != System.ServiceModel.TransferMode.Buffered)
				{
					return this.maxBufferSize;
				}
				long maxReceivedMessageSize = this.MaxReceivedMessageSize;
				if (maxReceivedMessageSize > (long)2147483647)
				{
					return 2147483647;
				}
				return (int)maxReceivedMessageSize;
			}
			set
			{
				if (value <= 0)
				{
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value", (object)value, Microsoft.ServiceBus.SR.GetString(Resources.ValueMustBePositive, new object[0])));
				}
				this.maxBufferSizeInitialized = true;
				this.maxBufferSize = value;
			}
		}

		public Uri ProxyAddress
		{
			get
			{
				return this.proxyAddress;
			}
			set
			{
				this.proxyAddress = value;
			}
		}

		public AuthenticationSchemes ProxyAuthenticationScheme
		{
			get
			{
				return this.proxyAuthenticationScheme;
			}
			set
			{
				if (!Microsoft.ServiceBus.Channels.AuthenticationSchemesHelper.IsSingleton(value))
				{
					ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
					string httpProxyRequiresSingleAuthScheme = Resources.HttpProxyRequiresSingleAuthScheme;
					object[] objArray = new object[] { value };
					throw exceptionUtility.ThrowHelperArgument("value", Microsoft.ServiceBus.SR.GetString(httpProxyRequiresSingleAuthScheme, objArray));
				}
				this.proxyAuthenticationScheme = value;
			}
		}

		public Microsoft.ServiceBus.RelayClientAuthenticationType RelayClientAuthenticationType
		{
			get
			{
				return this.relayClientAuthenticationType;
			}
			set
			{
				this.relayClientAuthenticationType = value;
			}
		}

		public override string Scheme
		{
			get
			{
				return "http";
			}
		}

		public System.ServiceModel.TransferMode TransferMode
		{
			get
			{
				return this.transferMode;
			}
			set
			{
				Microsoft.ServiceBus.Channels.TransferModeHelper.Validate(value);
				this.transferMode = value;
			}
		}

		public bool UseDefaultWebProxy
		{
			get
			{
				return this.useDefaultWebProxy;
			}
			set
			{
				this.useDefaultWebProxy = value;
			}
		}

		public HttpRelayTransportBindingElement() : this(Microsoft.ServiceBus.RelayClientAuthenticationType.RelayAccessToken)
		{
		}

		public HttpRelayTransportBindingElement(Microsoft.ServiceBus.RelayClientAuthenticationType relayClientAuthenticationType)
		{
			HttpTransportBindingElement httpTransportBindingElement = new HttpTransportBindingElement();
			this.allowCookies = httpTransportBindingElement.AllowCookies;
			this.keepAliveEnabled = httpTransportBindingElement.KeepAliveEnabled;
			this.maxBufferSize = httpTransportBindingElement.MaxBufferSize;
			this.proxyAuthenticationScheme = httpTransportBindingElement.ProxyAuthenticationScheme;
			this.proxyAddress = httpTransportBindingElement.ProxyAddress;
			this.transferMode = httpTransportBindingElement.TransferMode;
			this.useDefaultWebProxy = httpTransportBindingElement.UseDefaultWebProxy;
			this.relayClientAuthenticationType = relayClientAuthenticationType;
			this.IsDynamic = true;
		}

		protected HttpRelayTransportBindingElement(HttpRelayTransportBindingElement elementToBeCloned) : base(elementToBeCloned)
		{
			this.allowCookies = elementToBeCloned.allowCookies;
			this.HostNameComparisonMode = elementToBeCloned.HostNameComparisonMode;
			this.keepAliveEnabled = elementToBeCloned.keepAliveEnabled;
			this.maxBufferSize = elementToBeCloned.maxBufferSize;
			this.maxBufferSizeInitialized = elementToBeCloned.maxBufferSizeInitialized;
			this.proxyAddress = elementToBeCloned.proxyAddress;
			this.proxyAuthenticationScheme = elementToBeCloned.proxyAuthenticationScheme;
			this.transferMode = elementToBeCloned.transferMode;
			this.useDefaultWebProxy = elementToBeCloned.useDefaultWebProxy;
			this.relayClientAuthenticationType = elementToBeCloned.relayClientAuthenticationType;
			this.IsDynamic = elementToBeCloned.IsDynamic;
		}

		public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
		{
			if (typeof(TChannel) != typeof(IRequestChannel))
			{
				ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
				string channelTypeNotSupported = Resources.ChannelTypeNotSupported;
				object[] objArray = new object[] { typeof(TChannel) };
				throw exceptionUtility.ThrowHelperArgument("TChannel", Microsoft.ServiceBus.SR.GetString(channelTypeNotSupported, objArray));
			}
			NameSettings nameSetting = context.BindingParameters.Find<NameSettings>();
			if (nameSetting == null)
			{
				nameSetting = new NameSettings();
				context.BindingParameters.Add(nameSetting);
			}
			nameSetting.ServiceSettings.ListenerType = ListenerType.RelayedHttp;
			nameSetting.ServiceSettings.TransportProtection = (this is HttpsRelayTransportBindingElement ? RelayTransportProtectionMode.EndToEnd : RelayTransportProtectionMode.None);
			nameSetting.ServiceSettings.RelayClientAuthenticationType = this.relayClientAuthenticationType;
			nameSetting.ServiceSettings.IsDynamic = this.IsDynamic;
			BindingContext bindingContext = this.CreateInnerChannelBindingContext(context);
			return (IChannelFactory<TChannel>)(new RelayHttpTransportChannelFactory(context, bindingContext.BuildInnerChannelFactory<IRequestChannel>()));
		}

		public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
		where TChannel : class, IChannel
		{
			if (typeof(TChannel) != typeof(IReplyChannel))
			{
				ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
				string channelTypeNotSupported = Resources.ChannelTypeNotSupported;
				object[] objArray = new object[] { typeof(TChannel) };
				throw exceptionUtility.ThrowHelperArgument("TChannel", Microsoft.ServiceBus.SR.GetString(channelTypeNotSupported, objArray));
			}
			NameSettings nameSetting = context.BindingParameters.Find<NameSettings>();
			if (nameSetting == null)
			{
				nameSetting = new NameSettings();
				context.BindingParameters.Add(nameSetting);
			}
			nameSetting.ServiceSettings.ListenerType = ListenerType.RelayedHttp;
			nameSetting.ServiceSettings.TransportProtection = (this is HttpsRelayTransportBindingElement ? RelayTransportProtectionMode.EndToEnd : RelayTransportProtectionMode.None);
			nameSetting.ServiceSettings.RelayClientAuthenticationType = this.relayClientAuthenticationType;
			nameSetting.ServiceSettings.IsDynamic = this.IsDynamic;
			MessageEncodingBindingElement messageEncodingBindingElement = context.BindingParameters.Find<MessageEncodingBindingElement>();
			BindingContext bindingContext = this.CreateInnerListenerBindingContext(context);
			if (messageEncodingBindingElement != null)
			{
				context.BindingParameters.Remove<MessageEncodingBindingElement>();
			}
			return (IChannelListener<TChannel>)(new RelayHttpTransportChannelListener(context, messageEncodingBindingElement, bindingContext.BuildInnerChannelListener<IReplyChannel>()));
		}

		public override bool CanBuildChannelFactory<TChannel>(BindingContext context)
		{
			return typeof(TChannel) == typeof(IRequestChannel);
		}

		public override bool CanBuildChannelListener<TChannel>(BindingContext context)
		where TChannel : class, IChannel
		{
			return typeof(TChannel) == typeof(IReplyChannel);
		}

		public override BindingElement Clone()
		{
			return new HttpRelayTransportBindingElement(this);
		}

		private BindingContext CreateInnerChannelBindingContext(BindingContext context)
		{
			HttpTransportBindingElement httpTransportBindingElement = this.CreateInnerChannelBindingElement();
			this.InitializeInnerChannelBindingElement(httpTransportBindingElement);
			BindingElement[] bindingElementArray = new BindingElement[] { httpTransportBindingElement };
			CustomBinding customBinding = new CustomBinding(bindingElementArray)
			{
				CloseTimeout = context.Binding.CloseTimeout,
				Name = context.Binding.Name,
				Namespace = context.Binding.Namespace,
				OpenTimeout = context.Binding.OpenTimeout,
				ReceiveTimeout = context.Binding.ReceiveTimeout,
				SendTimeout = context.Binding.SendTimeout
			};
			BindingParameterCollection bindingParameterCollection = new BindingParameterCollection();
			foreach (object bindingParameter in context.BindingParameters)
			{
				bindingParameterCollection.Add(bindingParameter);
			}
			return new BindingContext(customBinding, bindingParameterCollection, context.ListenUriBaseAddress, context.ListenUriRelativeAddress, context.ListenUriMode);
		}

		protected virtual HttpTransportBindingElement CreateInnerChannelBindingElement()
		{
			return new HttpTransportBindingElement();
		}

		private BindingContext CreateInnerListenerBindingContext(BindingContext context)
		{
			BinaryMessageEncodingBindingElement binaryMessageEncodingBindingElement = ClientMessageUtility.CreateInnerEncodingBindingElement(context);
			NameSettings nameSetting = context.BindingParameters.Find<NameSettings>();
			TcpRelayTransportBindingElement tcpRelayTransportBindingElement = new TcpRelayTransportBindingElement(this.RelayClientAuthenticationType)
			{
				ManualAddressing = base.ManualAddressing,
				HostNameComparisonMode = this.HostNameComparisonMode,
				MaxBufferPoolSize = this.MaxBufferPoolSize,
				MaxBufferSize = this.MaxBufferSize,
				MaxReceivedMessageSize = this.MaxReceivedMessageSize,
				TransferMode = System.ServiceModel.TransferMode.Streamed,
				TransportProtectionEnabled = nameSetting.ServiceSettings.TransportProtection != RelayTransportProtectionMode.None
			};
			tcpRelayTransportBindingElement.ConnectionPoolSettings.MaxOutboundConnectionsPerEndpoint = 100;
			tcpRelayTransportBindingElement.IsDynamic = nameSetting.ServiceSettings.IsDynamic;
			BindingElement[] bindingElementArray = new BindingElement[] { binaryMessageEncodingBindingElement, tcpRelayTransportBindingElement };
			CustomBinding customBinding = new CustomBinding(bindingElementArray)
			{
				CloseTimeout = context.Binding.CloseTimeout,
				Name = context.Binding.Name,
				Namespace = context.Binding.Namespace,
				OpenTimeout = context.Binding.OpenTimeout,
				ReceiveTimeout = context.Binding.ReceiveTimeout,
				SendTimeout = context.Binding.SendTimeout
			};
			BindingParameterCollection bindingParameterCollection = new BindingParameterCollection();
			foreach (object bindingParameter in context.BindingParameters)
			{
				if (bindingParameter is MessageEncodingBindingElement)
				{
					continue;
				}
				bindingParameterCollection.Add(bindingParameter);
			}
			Uri sbUri = RelayedHttpUtility.ConvertToSbUri(context.ListenUriBaseAddress);
			return new BindingContext(customBinding, bindingParameterCollection, sbUri, context.ListenUriRelativeAddress, context.ListenUriMode);
		}

		private HttpTransportBindingElement CreateMetadataTemplateBindingElement()
		{
			HttpTransportBindingElement httpTransportBindingElement = new HttpTransportBindingElement()
			{
				AllowCookies = this.AllowCookies,
				AuthenticationScheme = AuthenticationSchemes.Anonymous,
				KeepAliveEnabled = this.KeepAliveEnabled,
				ManualAddressing = base.ManualAddressing,
				MaxBufferPoolSize = this.MaxBufferPoolSize,
				MaxBufferSize = this.MaxBufferSize,
				MaxReceivedMessageSize = this.MaxReceivedMessageSize,
				ProxyAddress = this.ProxyAddress,
				ProxyAuthenticationScheme = this.ProxyAuthenticationScheme,
				TransferMode = this.TransferMode,
				UseDefaultWebProxy = this.UseDefaultWebProxy
			};
			return httpTransportBindingElement;
		}

		public override T GetProperty<T>(BindingContext context)
		where T : class
		{
			if (context == null)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("context");
			}
			if (typeof(T) != typeof(System.ServiceModel.TransferMode))
			{
				return base.GetProperty<T>(context);
			}
			return (T)(object)this.TransferMode;
		}

		public XmlElement GetTransportTokenAssertion()
		{
			return (new SslStreamSecurityBindingElement()).GetTransportTokenAssertion();
		}

		protected virtual void InitializeInnerChannelBindingElement(HttpTransportBindingElement httpTransportElement)
		{
			httpTransportElement.AllowCookies = this.allowCookies;
			httpTransportElement.AuthenticationScheme = AuthenticationSchemes.Anonymous;
			httpTransportElement.BypassProxyOnLocal = false;
			httpTransportElement.HostNameComparisonMode = this.HostNameComparisonMode;
			httpTransportElement.KeepAliveEnabled = this.keepAliveEnabled;
			httpTransportElement.ManualAddressing = base.ManualAddressing;
			httpTransportElement.MaxBufferPoolSize = this.MaxBufferPoolSize;
			httpTransportElement.MaxBufferSize = this.MaxBufferSize;
			httpTransportElement.MaxReceivedMessageSize = this.MaxReceivedMessageSize;
			httpTransportElement.ProxyAddress = this.proxyAddress;
			httpTransportElement.ProxyAuthenticationScheme = this.proxyAuthenticationScheme;
			httpTransportElement.TransferMode = this.transferMode;
			httpTransportElement.UseDefaultWebProxy = this.useDefaultWebProxy;
		}

		void System.ServiceModel.Description.IPolicyExportExtension.ExportPolicy(MetadataExporter exporter, PolicyConversionContext context)
		{
			if (exporter == null)
			{
				throw new ArgumentNullException("exporter");
			}
			if (context == null)
			{
				throw new ArgumentNullException("context");
			}
			this.CreateMetadataTemplateBindingElement().ExportPolicy(exporter, context);
			XmlDocument xmlDocument = new XmlDocument();
			if (this.RelayClientAuthenticationType == Microsoft.ServiceBus.RelayClientAuthenticationType.RelayAccessToken)
			{
				XmlElement xmlElement = xmlDocument.CreateElement("rel", "SenderRelayCredential", "http://schemas.microsoft.com/netservices/2009/05/servicebus/connect");
				context.GetBindingAssertions().Add(xmlElement);
			}
		}

		void System.ServiceModel.Description.IWsdlExportExtension.ExportContract(WsdlExporter exporter, WsdlContractConversionContext context)
		{
			((IWsdlExportExtension)this.CreateMetadataTemplateBindingElement()).ExportContract(exporter, context);
		}

		void System.ServiceModel.Description.IWsdlExportExtension.ExportEndpoint(WsdlExporter exporter, WsdlEndpointConversionContext context)
		{
			((IWsdlExportExtension)this.CreateMetadataTemplateBindingElement()).ExportEndpoint(exporter, context);
		}
	}
}