using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging.Amqp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
	internal abstract class TransportProvider
	{
		private List<AmqpVersion> versions;

		public AmqpVersion DefaultVersion
		{
			get
			{
				if (this.Versions.Count == 0)
				{
					throw new ArgumentException(SRAmqp.AmqpProtocolVersionNotSet(this));
				}
				return this.Versions[0];
			}
		}

		public Microsoft.ServiceBus.Messaging.Amqp.ProtocolId ProtocolId
		{
			get;
			protected set;
		}

		public IList<AmqpVersion> Versions
		{
			get
			{
				if (this.versions == null)
				{
					this.versions = new List<AmqpVersion>();
				}
				return this.versions;
			}
		}

		protected TransportProvider()
		{
		}

		public TransportBase CreateTransport(TransportBase innerTransport, bool isInitiator)
		{
			return this.OnCreateTransport(innerTransport, isInitiator);
		}

		protected abstract TransportBase OnCreateTransport(TransportBase innerTransport, bool isInitiator);

		public bool TryGetVersion(AmqpVersion requestedVersion, out AmqpVersion supportedVersion)
		{
			bool flag;
			supportedVersion = this.DefaultVersion;
			using (IEnumerator<AmqpVersion> enumerator = this.Versions.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (!enumerator.Current.Equals(requestedVersion))
					{
						continue;
					}
					supportedVersion = requestedVersion;
					flag = true;
					return flag;
				}
				return false;
			}
			return flag;
		}
	}
}