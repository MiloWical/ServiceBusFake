using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.Net.Security;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus
{
	internal class SocketConnectionBindingElement : Microsoft.ServiceBus.Channels.ConnectionOrientedTransportBindingElement
	{
		private IConnectionElement connectionElement;

		private SocketConnectionPoolSettings connectionPoolSettings;

		private int listenBacklog;

		private bool teredoEnabled;

		private bool enableKeepAlive;

		internal IConnectionElement ConnectionElement
		{
			get
			{
				return this.connectionElement;
			}
			set
			{
				this.connectionElement = value;
			}
		}

		internal SocketConnectionPoolSettings ConnectionPoolSettings
		{
			get
			{
				return this.connectionPoolSettings;
			}
		}

		internal int ListenBacklog
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

		public override string Scheme
		{
			get
			{
				return "sb";
			}
		}

		public Microsoft.ServiceBus.SocketSecurityRole SocketSecurityRole
		{
			get
			{
				if (!(this.connectionElement is ISecureableConnectionElement))
				{
					return Microsoft.ServiceBus.SocketSecurityRole.None;
				}
				return ((ISecureableConnectionElement)this.connectionElement).SecurityMode;
			}
		}

		internal bool TeredoEnabled
		{
			get
			{
				return this.teredoEnabled;
			}
			set
			{
				this.teredoEnabled = value;
			}
		}

		public SocketConnectionBindingElement(IConnectionElement connectionElement)
		{
			this.connectionElement = connectionElement;
			this.enableKeepAlive = true;
			this.listenBacklog = 10;
			this.connectionPoolSettings = new SocketConnectionPoolSettings();
		}

		public SocketConnectionBindingElement(IConnectionElement connectionElement, bool enableKeepAlive) : this(connectionElement)
		{
			this.enableKeepAlive = enableKeepAlive;
		}

		protected SocketConnectionBindingElement(SocketConnectionBindingElement elementToBeCloned) : base(elementToBeCloned)
		{
			this.connectionElement = elementToBeCloned.ConnectionElement;
			this.enableKeepAlive = elementToBeCloned.enableKeepAlive;
			this.listenBacklog = elementToBeCloned.listenBacklog;
			this.teredoEnabled = elementToBeCloned.teredoEnabled;
			this.connectionPoolSettings = elementToBeCloned.connectionPoolSettings.Clone();
		}

		public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
		{
			if (context == null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("context");
			}
			if (!this.CanBuildChannelFactory<TChannel>(context))
			{
				ExceptionUtility exceptionUtility = DiagnosticUtility.ExceptionUtility;
				string channelTypeNotSupported = Resources.ChannelTypeNotSupported;
				object[] objArray = new object[] { typeof(TChannel) };
				throw exceptionUtility.ThrowHelperArgument("TChannel", Microsoft.ServiceBus.SR.GetString(channelTypeNotSupported, objArray));
			}
			return (IChannelFactory<TChannel>)(new SocketConnectionChannelFactory<TChannel>(this, context, this.enableKeepAlive));
		}

		public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
		where TChannel : class, IChannel
		{
			SocketConnectionChannelListener socketConnectionDuplexChannelListener;
			if (context == null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("context");
			}
			if (!this.CanBuildChannelListener<TChannel>(context))
			{
				ExceptionUtility exceptionUtility = DiagnosticUtility.ExceptionUtility;
				string channelTypeNotSupported = Resources.ChannelTypeNotSupported;
				object[] objArray = new object[] { typeof(TChannel) };
				throw exceptionUtility.ThrowHelperArgument("TChannel", Microsoft.ServiceBus.SR.GetString(channelTypeNotSupported, objArray));
			}
			if (typeof(TChannel) != typeof(IReplyChannel))
			{
				if (typeof(TChannel) != typeof(IDuplexSessionChannel))
				{
					ExceptionUtility exceptionUtility1 = DiagnosticUtility.ExceptionUtility;
					string str = Resources.ChannelTypeNotSupported;
					object[] objArray1 = new object[] { typeof(TChannel) };
					throw exceptionUtility1.ThrowHelperArgument("TChannel", Microsoft.ServiceBus.SR.GetString(str, objArray1));
				}
				socketConnectionDuplexChannelListener = new SocketConnectionDuplexChannelListener(this, context);
			}
			else
			{
				socketConnectionDuplexChannelListener = new SocketConnectionReplyChannelListener(this, context);
			}
			return (IChannelListener<TChannel>)socketConnectionDuplexChannelListener;
		}

		public override BindingElement Clone()
		{
			return new SocketConnectionBindingElement(this);
		}

		public override T GetProperty<T>(BindingContext context)
		where T : class
		{
			if (context == null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("context");
			}
			if (typeof(T) == typeof(IBindingDeliveryCapabilities))
			{
				return (T)(new SocketConnectionBindingElement.BindingDeliveryCapabilitiesHelper());
			}
			if (typeof(T) != typeof(ISecurityCapabilities))
			{
				return base.GetProperty<T>(context);
			}
			return (T)(new SocketConnectionBindingElement.SecurityCapabilitiesHelper(this.SocketSecurityRole));
		}

		internal override bool IsMatch(BindingElement b)
		{
			if (!base.IsMatch(b))
			{
				return false;
			}
			SocketConnectionBindingElement socketConnectionBindingElement = b as SocketConnectionBindingElement;
			if (socketConnectionBindingElement == null)
			{
				return false;
			}
			if (this.listenBacklog != socketConnectionBindingElement.listenBacklog)
			{
				return false;
			}
			if (this.teredoEnabled != socketConnectionBindingElement.teredoEnabled)
			{
				return false;
			}
			if (this.connectionElement != socketConnectionBindingElement.ConnectionElement)
			{
				return false;
			}
			if (this.SocketSecurityRole != socketConnectionBindingElement.SocketSecurityRole)
			{
				return false;
			}
			if (!this.connectionPoolSettings.IsMatch(socketConnectionBindingElement.connectionPoolSettings))
			{
				return false;
			}
			return true;
		}

		private class BindingDeliveryCapabilitiesHelper : IBindingDeliveryCapabilities
		{
			bool System.ServiceModel.Channels.IBindingDeliveryCapabilities.AssuresOrderedDelivery
			{
				get
				{
					return true;
				}
			}

			bool System.ServiceModel.Channels.IBindingDeliveryCapabilities.QueuedDelivery
			{
				get
				{
					return false;
				}
			}

			internal BindingDeliveryCapabilitiesHelper()
			{
			}
		}

		private class SecurityCapabilitiesHelper : ISecurityCapabilities
		{
			private Microsoft.ServiceBus.SocketSecurityRole socketSecurityRole;

			ProtectionLevel System.ServiceModel.Channels.ISecurityCapabilities.SupportedRequestProtectionLevel
			{
				get
				{
					if (this.socketSecurityRole == Microsoft.ServiceBus.SocketSecurityRole.None)
					{
						return ProtectionLevel.None;
					}
					return ProtectionLevel.EncryptAndSign;
				}
			}

			ProtectionLevel System.ServiceModel.Channels.ISecurityCapabilities.SupportedResponseProtectionLevel
			{
				get
				{
					if (this.socketSecurityRole == Microsoft.ServiceBus.SocketSecurityRole.None)
					{
						return ProtectionLevel.None;
					}
					return ProtectionLevel.EncryptAndSign;
				}
			}

			bool System.ServiceModel.Channels.ISecurityCapabilities.SupportsClientAuthentication
			{
				get
				{
					return false;
				}
			}

			bool System.ServiceModel.Channels.ISecurityCapabilities.SupportsClientWindowsIdentity
			{
				get
				{
					return false;
				}
			}

			bool System.ServiceModel.Channels.ISecurityCapabilities.SupportsServerAuthentication
			{
				get
				{
					return this.socketSecurityRole != Microsoft.ServiceBus.SocketSecurityRole.None;
				}
			}

			internal SecurityCapabilitiesHelper(Microsoft.ServiceBus.SocketSecurityRole securityRole)
			{
				this.socketSecurityRole = securityRole;
			}
		}
	}
}