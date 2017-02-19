using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging.Channels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Web.Services.Description;
using System.Xml;

namespace Microsoft.ServiceBus.Messaging
{
	public sealed class NetMessagingTransportBindingElement : TransportBindingElement, IPolicyExportExtension, IWsdlExportExtension
	{
		private NetMessagingTransportSettings transportSettings;

		private NetMessagingTransportBindingElement.ReceiveContextSettings receiveContextSettings;

		private int prefetchCount;

		private TimeSpan sessionIdleTimeout;

		[DefaultValue(-1)]
		public int PrefetchCount
		{
			get
			{
				return this.prefetchCount;
			}
			set
			{
				this.prefetchCount = value;
			}
		}

		internal bool ReceiveContextEnabled
		{
			get
			{
				return this.receiveContextSettings.Enabled;
			}
		}

		public override string Scheme
		{
			get
			{
				return "sb";
			}
		}

		[DefaultValue(typeof(TimeSpan), "00:01:00")]
		public TimeSpan SessionIdleTimeout
		{
			get
			{
				return this.sessionIdleTimeout;
			}
			set
			{
				TimeoutHelper.ThrowIfNonPositiveArgument(value, "SessionIdleTimeout");
				this.sessionIdleTimeout = value;
			}
		}

		public NetMessagingTransportSettings TransportSettings
		{
			get
			{
				return this.transportSettings;
			}
			set
			{
				if (value == null)
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("value");
				}
				this.transportSettings = value;
			}
		}

		public NetMessagingTransportBindingElement()
		{
			this.receiveContextSettings = new NetMessagingTransportBindingElement.ReceiveContextSettings();
			this.prefetchCount = -1;
			this.SessionIdleTimeout = Microsoft.ServiceBus.Messaging.Channels.TransportDefaults.SessionIdleTimeout;
			this.MaxReceivedMessageSize = (long)262144;
			this.transportSettings = new NetMessagingTransportSettings();
		}

		private NetMessagingTransportBindingElement(NetMessagingTransportBindingElement other) : base(other)
		{
			this.prefetchCount = other.prefetchCount;
			this.SessionIdleTimeout = other.SessionIdleTimeout;
			this.transportSettings = (NetMessagingTransportSettings)other.transportSettings.Clone();
			this.receiveContextSettings = other.receiveContextSettings;
		}

		public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
		{
			if (!this.CanBuildChannelFactory<TChannel>(context))
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.Argument("TChannel", SRClient.ChannelTypeNotSupported(typeof(TChannel)));
			}
			return (IChannelFactory<TChannel>)(new ServiceBusChannelFactory(context, this));
		}

		public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
		where TChannel : class, IChannel
		{
			if (!this.CanBuildChannelListener<TChannel>(context))
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.Argument("TChannel", SRClient.ChannelTypeNotSupported(typeof(TChannel)));
			}
			if (typeof(TChannel) == typeof(IInputChannel))
			{
				return (IChannelListener<TChannel>)(new ServiceBusInputChannelListener(context, this));
			}
			return (IChannelListener<TChannel>)(new ServiceBusInputSessionChannelListener(context, this));
		}

		public override bool CanBuildChannelFactory<T>(BindingContext context)
		{
			if (context == null)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("context");
			}
			return typeof(T) == typeof(IOutputChannel);
		}

		public override bool CanBuildChannelListener<T>(BindingContext context)
		where T : class, IChannel
		{
			if (context == null)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("context");
			}
			if (typeof(T) == typeof(IInputChannel))
			{
				return true;
			}
			return typeof(T) == typeof(IInputSessionChannel);
		}

		public override BindingElement Clone()
		{
			return new NetMessagingTransportBindingElement(this);
		}

		internal MessagingFactorySettings CreateMessagingFactorySettings(BindingContext context)
		{
			MessagingFactorySettings messagingFactorySetting = new MessagingFactorySettings()
			{
				NetMessagingTransportSettings = (NetMessagingTransportSettings)this.TransportSettings.Clone()
			};
			TransportClientEndpointBehavior transportClientEndpointBehavior = context.BindingParameters.Find<TransportClientEndpointBehavior>();
			if (transportClientEndpointBehavior != null)
			{
				messagingFactorySetting.TokenProvider = transportClientEndpointBehavior.TokenProvider;
			}
			return messagingFactorySetting;
		}

		public override T GetProperty<T>(BindingContext context)
		where T : class
		{
			if (context == null)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("context");
			}
			if (typeof(T) == typeof(IReceiveContextSettings))
			{
				return (T)this.receiveContextSettings;
			}
			return base.GetProperty<T>(context);
		}

		private static SoapBinding GetSoapBinding(WsdlEndpointConversionContext endpointContext, WsdlExporter exporter)
		{
			EnvelopeVersion item = null;
			SoapBinding soapBinding = null;
			object obj = null;
			object obj1 = new object();
			if (exporter.State.TryGetValue(obj1, out obj))
			{
				Dictionary<System.Web.Services.Description.Binding, EnvelopeVersion> bindings = obj as Dictionary<System.Web.Services.Description.Binding, EnvelopeVersion>;
				if (bindings != null && bindings.ContainsKey(endpointContext.WsdlBinding))
				{
					item = bindings[endpointContext.WsdlBinding];
				}
			}
			if (item == EnvelopeVersion.None)
			{
				return null;
			}
			foreach (object extension in endpointContext.WsdlBinding.Extensions)
			{
				SoapBinding soapBinding1 = extension as SoapBinding;
				if (soapBinding1 == null)
				{
					continue;
				}
				soapBinding = soapBinding1;
			}
			return soapBinding;
		}

		void System.ServiceModel.Description.IPolicyExportExtension.ExportPolicy(MetadataExporter exporter, PolicyConversionContext context)
		{
			if (exporter == null || context == null)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull((exporter == null ? "exporter" : "context"));
			}
			ICollection<XmlElement> bindingAssertions = context.GetBindingAssertions();
			bindingAssertions.Add((new XmlDocument()).CreateElement("sb", "netMessaging", "http://sample.schemas.microsoft.com/policy/netMessaging"));
			MessageEncodingBindingElement messageEncodingBindingElement = context.BindingElements.Find<MessageEncodingBindingElement>();
			if (messageEncodingBindingElement == null)
			{
				messageEncodingBindingElement = Microsoft.ServiceBus.Messaging.Channels.TransportDefaults.CreateDefaultEncoder();
				IPolicyExportExtension policyExportExtension = messageEncodingBindingElement as IPolicyExportExtension;
				if (policyExportExtension != null)
				{
					policyExportExtension.ExportPolicy(exporter, context);
				}
			}
			WSAddressingHelper.AddWSAddressingAssertion(exporter, context, messageEncodingBindingElement.MessageVersion.Addressing);
		}

		void System.ServiceModel.Description.IWsdlExportExtension.ExportContract(WsdlExporter exporter, WsdlContractConversionContext context)
		{
		}

		void System.ServiceModel.Description.IWsdlExportExtension.ExportEndpoint(WsdlExporter exporter, WsdlEndpointConversionContext context)
		{
			MessageEncodingBindingElement messageEncodingBindingElement = context.Endpoint.Binding.CreateBindingElements().Find<MessageEncodingBindingElement>();
			if (messageEncodingBindingElement == null)
			{
				messageEncodingBindingElement = Microsoft.ServiceBus.Messaging.Channels.TransportDefaults.CreateDefaultEncoder();
			}
			SoapBinding soapBinding = NetMessagingTransportBindingElement.GetSoapBinding(context, exporter);
			if (soapBinding != null)
			{
				soapBinding.Transport = "http://sample.schemas.microsoft.com/policy/netMessaging";
			}
			if (context.WsdlPort != null)
			{
				WSAddressingHelper.AddAddressToWsdlPort(context.WsdlPort, context.Endpoint.Address, messageEncodingBindingElement.MessageVersion.Addressing);
			}
		}

		private sealed class ReceiveContextSettings : IReceiveContextSettings
		{
			public bool Enabled
			{
				get;
				set;
			}

			public TimeSpan ValidityDuration
			{
				get
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new NotSupportedException(), null);
				}
			}

			public ReceiveContextSettings()
			{
				this.Enabled = false;
			}
		}
	}
}