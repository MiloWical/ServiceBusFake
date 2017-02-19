using Microsoft.ServiceBus;
using System;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
	internal sealed class TlsTransportSettings : TransportSettings
	{
		private readonly TransportSettings innerSettings;

		public X509Certificate2 Certificate
		{
			get;
			set;
		}

		public RemoteCertificateValidationCallback CertificateValidationCallback
		{
			get;
			set;
		}

		public TransportSettings InnerTransportSettings
		{
			get
			{
				return this.innerSettings;
			}
		}

		public bool IsInitiator
		{
			get;
			set;
		}

		public string TargetHost
		{
			get;
			set;
		}

		public TlsTransportSettings() : this(null, true)
		{
		}

		public TlsTransportSettings(TransportSettings innerSettings) : this(innerSettings, true)
		{
		}

		public TlsTransportSettings(TransportSettings innerSettings, bool isInitiator)
		{
			this.innerSettings = innerSettings;
			this.IsInitiator = isInitiator;
		}

		public override TransportInitiator CreateInitiator()
		{
			if (this.TargetHost == null)
			{
				throw new InvalidOperationException(SRClient.TargetHostNotSet);
			}
			return new TlsTransportInitiator(this);
		}

		public override TransportListener CreateListener()
		{
			if (this.Certificate == null)
			{
				throw new InvalidOperationException(SRClient.ServerCertificateNotSet);
			}
			return new TlsTransportListener(this);
		}

		public override string ToString()
		{
			return this.innerSettings.ToString();
		}
	}
}