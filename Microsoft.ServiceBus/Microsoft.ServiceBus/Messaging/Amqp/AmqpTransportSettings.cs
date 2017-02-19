using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Channels.Security;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Messaging.Amqp.Sasl;
using Microsoft.ServiceBus.Messaging.Amqp.Transport;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	public sealed class AmqpTransportSettings : ITransportSettings, IServiceBusSecuritySettings
	{
		private int maxFrameSize;

		private bool sslStreamUpgrade;

		private TimeSpan batchFlushInterval;

		internal Version AmqpVersion
		{
			get;
			set;
		}

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
					throw FxTrace.Exception.ArgumentOutOfRange("BatchFlushInterval", value, SRClient.InvalidBatchFlushInterval);
				}
				this.batchFlushInterval = value;
			}
		}

		internal RemoteCertificateValidationCallback CertificateValidationCallback
		{
			get;
			set;
		}

		internal int DefaultPort
		{
			get
			{
				if (this.UseSslStreamSecurity && !this.SslStreamUpgrade)
				{
					return 5671;
				}
				return 5672;
			}
		}

		internal bool DirectMode
		{
			get;
			set;
		}

		public int MaxFrameSize
		{
			get
			{
				return this.maxFrameSize;
			}
			set
			{
				if (value < 512)
				{
					throw new ArgumentOutOfRangeException();
				}
				this.maxFrameSize = value;
			}
		}

		Microsoft.ServiceBus.TokenProvider Microsoft.ServiceBus.Channels.Security.IServiceBusSecuritySettings.TokenProvider
		{
			get;
			set;
		}

		internal System.Net.NetworkCredential NetworkCredential
		{
			get;
			set;
		}

		internal string OpenHostName
		{
			get;
			set;
		}

		internal string SslHostName
		{
			get;
			set;
		}

		internal bool SslStreamUpgrade
		{
			get
			{
				return this.sslStreamUpgrade;
			}
			set
			{
				this.sslStreamUpgrade = value;
				if (this.sslStreamUpgrade)
				{
					this.UseSslStreamSecurity = true;
				}
			}
		}

		public bool UseSslStreamSecurity
		{
			get;
			set;
		}

		public AmqpTransportSettings()
		{
			this.AmqpVersion = new Version(1, 0, 0, 0);
			this.UseSslStreamSecurity = true;
			this.MaxFrameSize = 65536;
			this.batchFlushInterval = Constants.DefaultBatchFlushInterval;
		}

		public object Clone()
		{
			AmqpTransportSettings amqpTransportSetting = new AmqpTransportSettings()
			{
				BatchFlushInterval = this.batchFlushInterval
			};
			((IServiceBusSecuritySettings)amqpTransportSetting).TokenProvider = ((IServiceBusSecuritySettings)this).TokenProvider;
			amqpTransportSetting.UseSslStreamSecurity = this.UseSslStreamSecurity;
			amqpTransportSetting.sslStreamUpgrade = this.sslStreamUpgrade;
			amqpTransportSetting.NetworkCredential = this.NetworkCredential;
			amqpTransportSetting.CertificateValidationCallback = this.CertificateValidationCallback;
			amqpTransportSetting.AmqpVersion = this.AmqpVersion;
			amqpTransportSetting.MaxFrameSize = this.MaxFrameSize;
			amqpTransportSetting.DirectMode = this.DirectMode;
			amqpTransportSetting.OpenHostName = this.OpenHostName;
			this.SslHostName = this.SslHostName;
			return amqpTransportSetting;
		}

		internal AmqpSettings CreateAmqpSettings(string sslHostName)
		{
			AmqpSettings amqpSetting = new AmqpSettings();
			if (this.SslStreamUpgrade)
			{
				TlsTransportSettings tlsTransportSetting = new TlsTransportSettings()
				{
					CertificateValidationCallback = this.CertificateValidationCallback,
					TargetHost = sslHostName
				};
				TlsTransportProvider tlsTransportProvider = new TlsTransportProvider(tlsTransportSetting);
				tlsTransportProvider.Versions.Add(new Microsoft.ServiceBus.Messaging.Amqp.AmqpVersion(1, 0, 0));
				amqpSetting.TransportProviders.Add(tlsTransportProvider);
			}
			if (this.TokenProvider != null || this.NetworkCredential != null)
			{
				SaslTransportProvider saslTransportProvider = new SaslTransportProvider();
				saslTransportProvider.Versions.Add(new Microsoft.ServiceBus.Messaging.Amqp.AmqpVersion(1, 0, 0));
				amqpSetting.TransportProviders.Add(saslTransportProvider);
				if (this.NetworkCredential == null)
				{
					saslTransportProvider.AddHandler(new SaslExternalHandler());
				}
				else
				{
					SaslPlainHandler saslPlainHandler = new SaslPlainHandler()
					{
						AuthenticationIdentity = this.NetworkCredential.UserName,
						Password = this.NetworkCredential.Password
					};
					saslTransportProvider.AddHandler(saslPlainHandler);
				}
			}
			AmqpTransportProvider amqpTransportProvider = new AmqpTransportProvider();
			amqpTransportProvider.Versions.Add(new Microsoft.ServiceBus.Messaging.Amqp.AmqpVersion(this.AmqpVersion));
			amqpSetting.TransportProviders.Add(amqpTransportProvider);
			return amqpSetting;
		}

		IAsyncResult Microsoft.ServiceBus.Messaging.ITransportSettings.BeginCreateFactory(IEnumerable<Uri> physicalUriAddresses, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult<MessagingFactory>(new AmqpMessagingFactory(physicalUriAddresses, this), callback, state);
		}

		MessagingFactory Microsoft.ServiceBus.Messaging.ITransportSettings.EndCreateFactory(IAsyncResult result)
		{
			return CompletedAsyncResult<MessagingFactory>.End(result);
		}
	}
}