using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class Terminus
	{
		private Source source;

		private Target target;

		public Microsoft.ServiceBus.Messaging.Amqp.Framing.Address Address
		{
			get
			{
				if (this.source == null)
				{
					return this.target.Address;
				}
				return this.source.Address;
			}
		}

		public Multiple<AmqpSymbol> Capabilities
		{
			get
			{
				if (this.source == null)
				{
					return this.target.Capabilities;
				}
				return this.source.Capabilities;
			}
		}

		public TerminusDurability Durable
		{
			get
			{
				if (this.source != null)
				{
					if (!this.source.Durable.HasValue)
					{
						return TerminusDurability.None;
					}
					return (TerminusDurability)this.source.Durable.Value;
				}
				if (!this.target.Durable.HasValue)
				{
					return TerminusDurability.None;
				}
				return (TerminusDurability)this.target.Durable.Value;
			}
		}

		public bool? Dynamic
		{
			get
			{
				if (this.source == null)
				{
					return this.target.Dynamic;
				}
				return this.source.Dynamic;
			}
		}

		public AmqpMap DynamicNodeProperties
		{
			get
			{
				return (this.source != null ? this.source.DynamicNodeProperties : this.target.DynamicNodeProperties);
			}
		}

		public AmqpSymbol ExpiryPolicy
		{
			get
			{
				if (this.source == null)
				{
					return this.target.ExpiryPolicy;
				}
				return this.source.ExpiryPolicy;
			}
		}

		public uint? Timeout
		{
			get
			{
				if (this.source == null)
				{
					return this.target.Timeout;
				}
				return this.source.Timeout;
			}
		}

		public Terminus(Source source)
		{
			this.source = source;
		}

		public Terminus(Target target)
		{
			this.target = target;
		}
	}
}