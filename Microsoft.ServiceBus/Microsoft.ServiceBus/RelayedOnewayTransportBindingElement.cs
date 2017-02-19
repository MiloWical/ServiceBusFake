using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Security;
using System.Xml;

namespace Microsoft.ServiceBus
{
	public class RelayedOnewayTransportBindingElement : TransportBindingElement, IPolicyExportExtension
	{
		private Microsoft.ServiceBus.RelayClientAuthenticationType relayClientAuthenticationType;

		private RelayedOnewayConnectionMode connectionMode;

		private SocketConnectionPoolSettings connectionPoolSettings;

		private int listenBacklog;

		private int connectionBufferSize;

		private TimeSpan channelInitializationTimeout;

		private int maxBufferSize;

		private bool maxBufferSizeInitialized;

		private int maxPendingConnections;

		private TimeSpan maxOutputDelay;

		private int maxPendingAccepts;

		private bool transportProtectionEnabled;

		public TimeSpan ChannelInitializationTimeout
		{
			get
			{
				return this.channelInitializationTimeout;
			}
			set
			{
				if (value <= TimeSpan.Zero)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value", (object)value, Microsoft.ServiceBus.SR.GetString(Resources.TimeSpanMustbeGreaterThanTimeSpanZero, new object[0])));
				}
				if (TimeoutHelper.IsTooLarge(value))
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value", (object)value, Microsoft.ServiceBus.SR.GetString(Resources.SFxTimeoutOutOfRangeTooBig, new object[0])));
				}
				this.channelInitializationTimeout = value;
			}
		}

		public int ConnectionBufferSize
		{
			get
			{
				return this.connectionBufferSize;
			}
			set
			{
				if (value < 0)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value", (object)value, Microsoft.ServiceBus.SR.GetString(Resources.ValueMustBeNonNegative, new object[0])));
				}
				this.connectionBufferSize = value;
			}
		}

		public RelayedOnewayConnectionMode ConnectionMode
		{
			get
			{
				return this.connectionMode;
			}
			set
			{
				this.connectionMode = value;
			}
		}

		public SocketConnectionPoolSettings ConnectionPoolSettings
		{
			get
			{
				return this.connectionPoolSettings;
			}
		}

		public int ListenBacklog
		{
			get
			{
				return this.listenBacklog;
			}
			set
			{
				if (value <= 0)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value", Microsoft.ServiceBus.SR.GetString(Resources.ValueMustBePositive, new object[0])));
				}
				this.listenBacklog = value;
			}
		}

		public int MaxBufferSize
		{
			get
			{
				if (this.maxBufferSizeInitialized)
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
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value", (object)value, Microsoft.ServiceBus.SR.GetString(Resources.ValueMustBePositive, new object[0])));
				}
				this.maxBufferSizeInitialized = true;
				this.maxBufferSize = value;
			}
		}

		public TimeSpan MaxOutputDelay
		{
			get
			{
				return this.maxOutputDelay;
			}
			set
			{
				if (value < TimeSpan.Zero)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value", (object)value, Microsoft.ServiceBus.SR.GetString(Resources.SFxTimeoutOutOfRange0, new object[0])));
				}
				if (TimeoutHelper.IsTooLarge(value))
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value", (object)value, Microsoft.ServiceBus.SR.GetString(Resources.SFxTimeoutOutOfRangeTooBig, new object[0])));
				}
				this.maxOutputDelay = value;
			}
		}

		public int MaxPendingAccepts
		{
			get
			{
				return this.maxPendingAccepts;
			}
			set
			{
				if (value <= 0)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value", (object)value, Microsoft.ServiceBus.SR.GetString(Resources.ValueMustBePositive, new object[0])));
				}
				this.maxPendingAccepts = value;
			}
		}

		public int MaxPendingConnections
		{
			get
			{
				return this.maxPendingConnections;
			}
			set
			{
				if (value <= 0)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value", (object)value, Microsoft.ServiceBus.SR.GetString(Resources.ValueMustBePositive, new object[0])));
				}
				this.maxPendingConnections = value;
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
				return "sb";
			}
		}

		internal bool TransportProtectionEnabled
		{
			get
			{
				return this.transportProtectionEnabled;
			}
			set
			{
				this.transportProtectionEnabled = value;
			}
		}

		public RelayedOnewayTransportBindingElement() : this(Microsoft.ServiceBus.RelayClientAuthenticationType.RelayAccessToken, RelayedOnewayConnectionMode.Unicast)
		{
		}

		public RelayedOnewayTransportBindingElement(Microsoft.ServiceBus.RelayClientAuthenticationType relayClientAuthenticationType) : this(relayClientAuthenticationType, RelayedOnewayConnectionMode.Unicast)
		{
		}

		public RelayedOnewayTransportBindingElement(Microsoft.ServiceBus.RelayClientAuthenticationType relayClientAuthenticationType, RelayedOnewayConnectionMode connectionMode)
		{
			this.relayClientAuthenticationType = relayClientAuthenticationType;
			this.connectionMode = connectionMode;
			this.listenBacklog = 10;
			this.connectionPoolSettings = new SocketConnectionPoolSettings();
			this.connectionBufferSize = 65536;
			this.channelInitializationTimeout = Microsoft.ServiceBus.Channels.ConnectionOrientedTransportDefaults.ChannelInitializationTimeout;
			this.maxBufferSize = 65536;
			this.maxPendingConnections = 10;
			this.maxOutputDelay = Microsoft.ServiceBus.Channels.ConnectionOrientedTransportDefaults.MaxOutputDelay;
			this.maxPendingAccepts = 1;
		}

		public RelayedOnewayTransportBindingElement(RelayedOnewayTransportBindingElement elementToClone)
		{
			this.channelInitializationTimeout = elementToClone.channelInitializationTimeout;
			this.connectionBufferSize = elementToClone.connectionBufferSize;
			this.connectionMode = elementToClone.connectionMode;
			this.connectionPoolSettings = elementToClone.connectionPoolSettings.Clone();
			this.listenBacklog = elementToClone.listenBacklog;
			base.ManualAddressing = elementToClone.ManualAddressing;
			this.MaxBufferPoolSize = elementToClone.MaxBufferPoolSize;
			this.maxBufferSize = elementToClone.maxBufferSize;
			this.maxBufferSizeInitialized = elementToClone.maxBufferSizeInitialized;
			this.maxOutputDelay = elementToClone.maxOutputDelay;
			this.maxPendingAccepts = elementToClone.maxPendingAccepts;
			this.maxPendingConnections = elementToClone.maxPendingConnections;
			this.MaxReceivedMessageSize = elementToClone.MaxReceivedMessageSize;
			this.relayClientAuthenticationType = elementToClone.relayClientAuthenticationType;
			this.transportProtectionEnabled = elementToClone.transportProtectionEnabled;
		}

		public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
		{
			if (context == null)
			{
				throw new ArgumentNullException("context");
			}
			if (typeof(TChannel) != typeof(IOutputChannel))
			{
				throw new NotImplementedException(SRClient.UnsupportedChannelType(typeof(TChannel)));
			}
			NameSettings nameSetting = context.BindingParameters.Find<NameSettings>();
			if (nameSetting == null)
			{
				nameSetting = new NameSettings();
				context.BindingParameters.Add(nameSetting);
			}
			if (nameSetting.ServiceSettings.ListenerType == ListenerType.None)
			{
				nameSetting.ServiceSettings.ListenerType = (this.ConnectionMode == RelayedOnewayConnectionMode.Unicast ? ListenerType.Unicast : ListenerType.Multicast);
				nameSetting.ServiceSettings.TransportProtection = (this.transportProtectionEnabled ? RelayTransportProtectionMode.EndToEnd : RelayTransportProtectionMode.None);
				nameSetting.ServiceSettings.RelayClientAuthenticationType = this.relayClientAuthenticationType;
			}
			return (IChannelFactory<TChannel>)(new RelayedOnewayChannelFactory(context, this));
		}

		public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
		where TChannel : class, IChannel
		{
			if (context == null)
			{
				throw new ArgumentNullException("context");
			}
			if (typeof(TChannel) != typeof(IInputChannel))
			{
				throw new NotImplementedException(SRClient.UnsupportedChannelType(typeof(TChannel)));
			}
			NameSettings nameSetting = context.BindingParameters.Find<NameSettings>();
			if (nameSetting == null)
			{
				nameSetting = new NameSettings();
				context.BindingParameters.Add(nameSetting);
			}
			if (nameSetting.ServiceSettings.ListenerType == ListenerType.None)
			{
				nameSetting.ServiceSettings.ListenerType = (this.ConnectionMode == RelayedOnewayConnectionMode.Unicast ? ListenerType.Unicast : ListenerType.Multicast);
				nameSetting.ServiceSettings.TransportProtection = (this.transportProtectionEnabled ? RelayTransportProtectionMode.EndToEnd : RelayTransportProtectionMode.None);
				nameSetting.ServiceSettings.RelayClientAuthenticationType = this.relayClientAuthenticationType;
			}
			return (IChannelListener<TChannel>)(new RelayedOnewayChannelListener(context, this));
		}

		public override bool CanBuildChannelFactory<TChannel>(BindingContext context)
		{
			if (context == null)
			{
				throw new ArgumentNullException("context");
			}
			return typeof(TChannel) == typeof(IOutputChannel);
		}

		public override bool CanBuildChannelListener<TChannel>(BindingContext context)
		where TChannel : class, IChannel
		{
			if (context == null)
			{
				throw new ArgumentNullException("context");
			}
			return typeof(TChannel) == typeof(IInputChannel);
		}

		public override BindingElement Clone()
		{
			return new RelayedOnewayTransportBindingElement(this);
		}

		public override T GetProperty<T>(BindingContext context)
		where T : class
		{
			if (context == null)
			{
				throw new ArgumentNullException("context");
			}
			if (typeof(T) != typeof(ChannelProtectionRequirements))
			{
				return base.GetProperty<T>(context);
			}
			ChannelProtectionRequirements protectionRequirements = RelayedOnewayTransportBindingElement.GetProtectionRequirements(context);
			protectionRequirements.Add(context.GetInnerProperty<ChannelProtectionRequirements>() ?? new ChannelProtectionRequirements());
			return (T)protectionRequirements;
		}

		internal static new ChannelProtectionRequirements GetProtectionRequirements(BindingContext context)
		{
			AddressingVersion wSAddressing10 = AddressingVersion.WSAddressing10;
			MessageEncodingBindingElement messageEncodingBindingElement = ClientMessageUtility.CreateInnerEncodingBindingElement(context);
			if (messageEncodingBindingElement != null)
			{
				wSAddressing10 = messageEncodingBindingElement.MessageVersion.Addressing;
			}
			return RelayedOnewayTransportBindingElement.GetProtectionRequirements(wSAddressing10);
		}

		private static ChannelProtectionRequirements GetProtectionRequirements(AddressingVersion addressingVersion)
		{
			PropertyInfo property = addressingVersion.GetType().GetProperty("SignedMessageParts", BindingFlags.Instance | BindingFlags.NonPublic);
			MessagePartSpecification value = (MessagePartSpecification)property.GetValue(addressingVersion, new object[0]);
			ChannelProtectionRequirements channelProtectionRequirement = new ChannelProtectionRequirements();
			channelProtectionRequirement.IncomingSignatureParts.AddParts(value);
			channelProtectionRequirement.OutgoingSignatureParts.AddParts(value);
			return channelProtectionRequirement;
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
			XmlDocument xmlDocument = new XmlDocument();
			XmlElement xmlElement = xmlDocument.CreateElement("rel", "RelayedOneway", "http://schemas.microsoft.com/netservices/2009/05/servicebus/connect");
			if (this.connectionMode == RelayedOnewayConnectionMode.Multicast)
			{
				XmlElement xmlElement1 = xmlDocument.CreateElement("rel", "Multicast", "http://schemas.microsoft.com/netservices/2009/05/servicebus/connect");
				xmlElement.AppendChild(xmlElement1);
			}
			context.GetBindingAssertions().Add(xmlElement);
		}
	}
}