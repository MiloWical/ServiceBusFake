using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Channels.Security;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging.Amqp;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging
{
	public class MessagingFactorySettings
	{
		private TimeSpan operationTimeout;

		private Microsoft.ServiceBus.Messaging.NetMessagingTransportSettings netMessagingTransportSettings;

		private Microsoft.ServiceBus.Messaging.Amqp.AmqpTransportSettings amqpTransportSettings;

		public Microsoft.ServiceBus.Messaging.Amqp.AmqpTransportSettings AmqpTransportSettings
		{
			get
			{
				if (this.amqpTransportSettings == null)
				{
					this.amqpTransportSettings = new Microsoft.ServiceBus.Messaging.Amqp.AmqpTransportSettings();
				}
				return this.amqpTransportSettings;
			}
			set
			{
				if (value == null)
				{
					throw FxTrace.Exception.ArgumentNull("value");
				}
				this.amqpTransportSettings = value;
			}
		}

		public bool EnableAdditionalClientTimeout
		{
			get;
			set;
		}

		public Microsoft.ServiceBus.Messaging.NetMessagingTransportSettings NetMessagingTransportSettings
		{
			get
			{
				if (this.netMessagingTransportSettings == null)
				{
					this.netMessagingTransportSettings = new Microsoft.ServiceBus.Messaging.NetMessagingTransportSettings();
				}
				return this.netMessagingTransportSettings;
			}
			set
			{
				if (value == null)
				{
					throw FxTrace.Exception.ArgumentNull("value");
				}
				this.netMessagingTransportSettings = value;
			}
		}

		public TimeSpan OperationTimeout
		{
			get
			{
				return this.operationTimeout;
			}
			set
			{
				TimeoutHelper.ThrowIfNonPositiveArgument(value, "OperationTimeout");
				this.operationTimeout = value;
			}
		}

		public Microsoft.ServiceBus.TokenProvider TokenProvider
		{
			get;
			set;
		}

		public Microsoft.ServiceBus.Messaging.TransportType TransportType
		{
			get;
			set;
		}

		public MessagingFactorySettings()
		{
			this.operationTimeout = Constants.DefaultOperationTimeout;
			this.TokenProvider = null;
			this.TransportType = Microsoft.ServiceBus.Messaging.TransportType.NetMessaging;
			this.EnableAdditionalClientTimeout = true;
		}

		public MessagingFactorySettings(MessagingFactorySettings other)
		{
			this.operationTimeout = other.OperationTimeout;
			this.EnableAdditionalClientTimeout = other.EnableAdditionalClientTimeout;
			this.TokenProvider = other.TokenProvider;
			this.TransportType = other.TransportType;
			if (other.TransportType == Microsoft.ServiceBus.Messaging.TransportType.NetMessaging)
			{
				this.NetMessagingTransportSettings = (Microsoft.ServiceBus.Messaging.NetMessagingTransportSettings)other.NetMessagingTransportSettings.Clone();
				return;
			}
			this.AmqpTransportSettings = (Microsoft.ServiceBus.Messaging.Amqp.AmqpTransportSettings)other.AmqpTransportSettings.Clone();
		}

		internal IAsyncResult BeginCreateFactory(IEnumerable<Uri> uriAddresses, AsyncCallback callback, object state)
		{
			return this.OnBeginCreateFactory(uriAddresses, callback, state);
		}

		public virtual MessagingFactorySettings Clone()
		{
			return new MessagingFactorySettings(this);
		}

		internal MessagingFactory EndCreateFactory(IAsyncResult result)
		{
			return this.OnEndCreateFactory(result);
		}

		private ITransportSettings GetTransportSettings()
		{
			ITransportSettings amqpTransportSettings;
			if (this.TransportType != Microsoft.ServiceBus.Messaging.TransportType.NetMessaging)
			{
				amqpTransportSettings = this.AmqpTransportSettings;
			}
			else
			{
				amqpTransportSettings = this.NetMessagingTransportSettings;
			}
			return amqpTransportSettings;
		}

		protected virtual IAsyncResult OnBeginCreateFactory(Uri uri, AsyncCallback callback, object state)
		{
			return this.OnBeginCreateFactory(new List<Uri>()
			{
				uri
			}, callback, state);
		}

		protected virtual IAsyncResult OnBeginCreateFactory(IEnumerable<Uri> uriAddresses, AsyncCallback callback, object state)
		{
			ITransportSettings transportSettings = this.GetTransportSettings();
			IServiceBusSecuritySettings tokenProvider = transportSettings as IServiceBusSecuritySettings;
			if (tokenProvider != null)
			{
				tokenProvider.TokenProvider = this.TokenProvider;
			}
			return transportSettings.BeginCreateFactory(uriAddresses, callback, state);
		}

		protected virtual MessagingFactory OnEndCreateFactory(IAsyncResult result)
		{
			return this.GetTransportSettings().EndCreateFactory(result);
		}
	}
}