using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Channels
{
	public abstract class ConnectionOrientedTransportBindingElement : TransportBindingElement
	{
		private int connectionBufferSize;

		private bool exposeConnectionProperty;

		private System.ServiceModel.HostNameComparisonMode hostNameComparisonMode;

		private bool inheritBaseAddressSettings;

		private TimeSpan channelInitializationTimeout;

		private int maxBufferSize;

		private bool maxBufferSizeInitialized;

		private int maxPendingConnections;

		private TimeSpan maxOutputDelay;

		private int maxPendingAccepts;

		private System.ServiceModel.TransferMode transferMode;

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
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value", (object)value, Microsoft.ServiceBus.SR.GetString(Resources.TimeSpanMustbeGreaterThanTimeSpanZero, new object[0])));
				}
				if (TimeoutHelper.IsTooLarge(value))
				{
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value", (object)value, Microsoft.ServiceBus.SR.GetString(Resources.SFxTimeoutOutOfRangeTooBig, new object[0])));
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
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value", (object)value, Microsoft.ServiceBus.SR.GetString(Resources.ValueMustBeNonNegative, new object[0])));
				}
				this.connectionBufferSize = value;
			}
		}

		internal bool ExposeConnectionProperty
		{
			get
			{
				return this.exposeConnectionProperty;
			}
			set
			{
				this.exposeConnectionProperty = value;
			}
		}

		public System.ServiceModel.HostNameComparisonMode HostNameComparisonMode
		{
			get
			{
				return this.hostNameComparisonMode;
			}
			set
			{
				Microsoft.ServiceBus.Channels.HostNameComparisonModeHelper.Validate(value);
				this.hostNameComparisonMode = value;
			}
		}

		internal bool InheritBaseAddressSettings
		{
			get
			{
				return this.inheritBaseAddressSettings;
			}
			set
			{
				this.inheritBaseAddressSettings = value;
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
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value", (object)value, Microsoft.ServiceBus.SR.GetString(Resources.SFxTimeoutOutOfRange0, new object[0])));
				}
				if (TimeoutHelper.IsTooLarge(value))
				{
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value", (object)value, Microsoft.ServiceBus.SR.GetString(Resources.SFxTimeoutOutOfRangeTooBig, new object[0])));
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
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value", (object)value, Microsoft.ServiceBus.SR.GetString(Resources.ValueMustBePositive, new object[0])));
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
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value", (object)value, Microsoft.ServiceBus.SR.GetString(Resources.ValueMustBePositive, new object[0])));
				}
				this.maxPendingConnections = value;
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

		internal ConnectionOrientedTransportBindingElement()
		{
			this.connectionBufferSize = 65536;
			this.hostNameComparisonMode = System.ServiceModel.HostNameComparisonMode.StrongWildcard;
			this.channelInitializationTimeout = Microsoft.ServiceBus.Channels.ConnectionOrientedTransportDefaults.ChannelInitializationTimeout;
			this.maxBufferSize = 65536;
			this.maxPendingConnections = 10;
			this.maxOutputDelay = Microsoft.ServiceBus.Channels.ConnectionOrientedTransportDefaults.MaxOutputDelay;
			this.maxPendingAccepts = 1;
			this.transferMode = System.ServiceModel.TransferMode.Buffered;
		}

		internal ConnectionOrientedTransportBindingElement(Microsoft.ServiceBus.Channels.ConnectionOrientedTransportBindingElement elementToBeCloned) : base(elementToBeCloned)
		{
			this.connectionBufferSize = elementToBeCloned.connectionBufferSize;
			this.exposeConnectionProperty = elementToBeCloned.exposeConnectionProperty;
			this.hostNameComparisonMode = elementToBeCloned.hostNameComparisonMode;
			this.inheritBaseAddressSettings = elementToBeCloned.InheritBaseAddressSettings;
			this.channelInitializationTimeout = elementToBeCloned.ChannelInitializationTimeout;
			this.maxBufferSize = elementToBeCloned.maxBufferSize;
			this.maxBufferSizeInitialized = elementToBeCloned.maxBufferSizeInitialized;
			this.maxPendingConnections = elementToBeCloned.maxPendingConnections;
			this.maxOutputDelay = elementToBeCloned.maxOutputDelay;
			this.maxPendingAccepts = elementToBeCloned.maxPendingAccepts;
			this.transferMode = elementToBeCloned.transferMode;
		}

		public override bool CanBuildChannelFactory<TChannel>(BindingContext context)
		{
			if (context == null)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("context");
			}
			if (this.TransferMode == System.ServiceModel.TransferMode.Buffered)
			{
				return typeof(TChannel) == typeof(IDuplexSessionChannel);
			}
			return typeof(TChannel) == typeof(IRequestChannel);
		}

		public override bool CanBuildChannelListener<TChannel>(BindingContext context)
		where TChannel : class, IChannel
		{
			if (context == null)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("context");
			}
			if (this.TransferMode == System.ServiceModel.TransferMode.Buffered)
			{
				return typeof(TChannel) == typeof(IDuplexSessionChannel);
			}
			return typeof(TChannel) == typeof(IReplyChannel);
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

		internal virtual new bool IsMatch(BindingElement b)
		{
			if (b == null)
			{
				return false;
			}
			TransportBindingElement transportBindingElement = b as TransportBindingElement;
			if (transportBindingElement == null)
			{
				return false;
			}
			if (this.MaxBufferPoolSize != transportBindingElement.MaxBufferPoolSize)
			{
				return false;
			}
			if (this.MaxReceivedMessageSize != transportBindingElement.MaxReceivedMessageSize)
			{
				return false;
			}
			Microsoft.ServiceBus.Channels.ConnectionOrientedTransportBindingElement connectionOrientedTransportBindingElement = b as Microsoft.ServiceBus.Channels.ConnectionOrientedTransportBindingElement;
			if (connectionOrientedTransportBindingElement == null)
			{
				return false;
			}
			if (this.connectionBufferSize != connectionOrientedTransportBindingElement.connectionBufferSize)
			{
				return false;
			}
			if (this.hostNameComparisonMode != connectionOrientedTransportBindingElement.hostNameComparisonMode)
			{
				return false;
			}
			if (this.inheritBaseAddressSettings != connectionOrientedTransportBindingElement.inheritBaseAddressSettings)
			{
				return false;
			}
			if (this.channelInitializationTimeout != connectionOrientedTransportBindingElement.channelInitializationTimeout)
			{
				return false;
			}
			if (this.maxBufferSize != connectionOrientedTransportBindingElement.maxBufferSize)
			{
				return false;
			}
			if (this.maxPendingConnections != connectionOrientedTransportBindingElement.maxPendingConnections)
			{
				return false;
			}
			if (this.maxOutputDelay != connectionOrientedTransportBindingElement.maxOutputDelay)
			{
				return false;
			}
			if (this.maxPendingAccepts != connectionOrientedTransportBindingElement.maxPendingAccepts)
			{
				return false;
			}
			if (this.transferMode != connectionOrientedTransportBindingElement.transferMode)
			{
				return false;
			}
			return true;
		}
	}
}