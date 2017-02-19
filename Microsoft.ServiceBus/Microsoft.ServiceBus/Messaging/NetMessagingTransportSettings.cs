using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Channels.Security;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging.Sbmp;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.ServiceModel;

namespace Microsoft.ServiceBus.Messaging
{
	public sealed class NetMessagingTransportSettings : ITransportSettings, IServiceBusSecuritySettings
	{
		private TimeSpan batchFlushInterval;

		public TimeSpan BatchFlushInterval
		{
			get
			{
				return this.batchFlushInterval;
			}
			set
			{
				if (value < TimeSpan.Zero)
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentOutOfRange("BatchFlushInterval", value, SRClient.InvalidBatchFlushInterval);
				}
				this.batchFlushInterval = value;
			}
		}

		public bool EnableRedirect
		{
			get;
			set;
		}

		internal DnsEndpointIdentity EndpointIdentity
		{
			get;
			set;
		}

		internal bool GatewayMode
		{
			get;
			set;
		}

		Microsoft.ServiceBus.TokenProvider Microsoft.ServiceBus.Channels.Security.IServiceBusSecuritySettings.TokenProvider
		{
			get;
			set;
		}

		internal bool UseSslStreamSecurity
		{
			get;
			set;
		}

		public NetMessagingTransportSettings()
		{
			this.batchFlushInterval = Constants.DefaultBatchFlushInterval;
			this.UseSslStreamSecurity = true;
			this.EnableRedirect = false;
			this.GatewayMode = false;
			this.EndpointIdentity = null;
		}

		public object Clone()
		{
			NetMessagingTransportSettings netMessagingTransportSetting = new NetMessagingTransportSettings()
			{
				BatchFlushInterval = this.BatchFlushInterval
			};
			((IServiceBusSecuritySettings)netMessagingTransportSetting).TokenProvider = ((IServiceBusSecuritySettings)this).TokenProvider;
			netMessagingTransportSetting.UseSslStreamSecurity = this.UseSslStreamSecurity;
			netMessagingTransportSetting.EnableRedirect = this.EnableRedirect;
			netMessagingTransportSetting.GatewayMode = this.GatewayMode;
			netMessagingTransportSetting.EndpointIdentity = this.EndpointIdentity;
			return netMessagingTransportSetting;
		}

		IAsyncResult Microsoft.ServiceBus.Messaging.ITransportSettings.BeginCreateFactory(IEnumerable<Uri> physicalUriAddresses, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult<MessagingFactory>(new SbmpMessagingFactory(physicalUriAddresses, this), callback, state);
		}

		MessagingFactory Microsoft.ServiceBus.Messaging.ITransportSettings.EndCreateFactory(IAsyncResult result)
		{
			return CompletedAsyncResult<MessagingFactory>.End(result);
		}
	}
}