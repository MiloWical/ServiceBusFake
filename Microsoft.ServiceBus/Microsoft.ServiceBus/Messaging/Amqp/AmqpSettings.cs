using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using Microsoft.ServiceBus.Messaging.Amqp.Transport;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal sealed class AmqpSettings
	{
		private List<TransportProvider> transportProviders;

		public bool AllowAnonymousConnection
		{
			get;
			set;
		}

		public bool AuthorizationDisabled
		{
			get;
			set;
		}

		public uint DefaultLinkCredit
		{
			get;
			set;
		}

		public int MaxConcurrentConnections
		{
			get;
			set;
		}

		public int MaxLinksPerSession
		{
			get;
			set;
		}

		public bool RequireSecureTransport
		{
			get;
			set;
		}

		public IRuntimeProvider RuntimeProvider
		{
			get;
			set;
		}

		public IList<TransportProvider> TransportProviders
		{
			get
			{
				if (this.transportProviders == null)
				{
					this.transportProviders = new List<TransportProvider>();
				}
				return this.transportProviders;
			}
		}

		public AmqpSettings()
		{
			this.MaxConcurrentConnections = 2147483647;
			this.MaxLinksPerSession = 2147483647;
			this.DefaultLinkCredit = 1000;
			this.AllowAnonymousConnection = true;
			this.AuthorizationDisabled = true;
		}

		public AmqpSettings Clone()
		{
			AmqpSettings amqpSetting = new AmqpSettings()
			{
				DefaultLinkCredit = this.DefaultLinkCredit,
				transportProviders = new List<TransportProvider>(this.TransportProviders),
				RuntimeProvider = this.RuntimeProvider,
				RequireSecureTransport = this.RequireSecureTransport,
				AllowAnonymousConnection = this.AllowAnonymousConnection,
				AuthorizationDisabled = this.AuthorizationDisabled
			};
			return amqpSetting;
		}

		public ProtocolHeader GetDefaultHeader()
		{
			TransportProvider defaultProvider = this.GetDefaultProvider();
			return new ProtocolHeader(defaultProvider.ProtocolId, defaultProvider.DefaultVersion);
		}

		private TransportProvider GetDefaultProvider()
		{
			TransportProvider transportProvider = null;
			if (this.RequireSecureTransport)
			{
				transportProvider = this.GetTransportProvider<TlsTransportProvider>();
			}
			else if (this.AllowAnonymousConnection)
			{
				transportProvider = this.GetTransportProvider<AmqpTransportProvider>();
			}
			else
			{
				transportProvider = this.GetTransportProvider<SaslTransportProvider>();
			}
			return transportProvider;
		}

		public ProtocolHeader GetSupportedHeader(ProtocolHeader requestedHeader)
		{
			AmqpVersion amqpVersion;
			TransportProvider transportProvider = null;
			if (!this.TryGetTransportProvider(requestedHeader, out transportProvider))
			{
				return this.GetDefaultHeader();
			}
			if (transportProvider.TryGetVersion(requestedHeader.Version, out amqpVersion))
			{
				return requestedHeader;
			}
			return new ProtocolHeader(transportProvider.ProtocolId, transportProvider.DefaultVersion);
		}

		public T GetTransportProvider<T>()
		where T : TransportProvider
		{
			T t;
			List<TransportProvider>.Enumerator enumerator = this.transportProviders.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					TransportProvider current = enumerator.Current;
					if (!(current is T))
					{
						continue;
					}
					t = (T)current;
					return t;
				}
				return default(T);
			}
			finally
			{
				((IDisposable)enumerator).Dispose();
			}
			return t;
		}

		public bool TryGetTransportProvider(ProtocolHeader header, out TransportProvider provider)
		{
			bool flag;
			if (this.TransportProviders.Count == 0)
			{
				throw new ArgumentException("TransportProviders");
			}
			provider = null;
			using (IEnumerator<TransportProvider> enumerator = this.TransportProviders.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					TransportProvider current = enumerator.Current;
					if (current.ProtocolId != header.ProtocolId)
					{
						continue;
					}
					provider = current;
					flag = true;
					return flag;
				}
				provider = this.GetDefaultProvider();
				return false;
			}
			return flag;
		}

		public void ValidateInitiatorSettings()
		{
			if (this.TransportProviders.Count == 0)
			{
				throw new ArgumentException("TransportProviders");
			}
		}

		public void ValidateListenerSettings()
		{
			if (this.TransportProviders.Count == 0)
			{
				throw new ArgumentException("TransportProviders");
			}
		}
	}
}