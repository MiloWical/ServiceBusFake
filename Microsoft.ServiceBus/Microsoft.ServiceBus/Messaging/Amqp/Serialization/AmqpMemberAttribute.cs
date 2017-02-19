using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Amqp.Serialization
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple=false, Inherited=true)]
	internal sealed class AmqpMemberAttribute : Attribute
	{
		private int? order;

		private bool? mandatory;

		internal bool? InternalMandatory
		{
			get
			{
				return this.mandatory;
			}
		}

		internal int? InternalOrder
		{
			get
			{
				return this.order;
			}
		}

		public bool Mandatory
		{
			get
			{
				if (!this.mandatory.HasValue)
				{
					return false;
				}
				return this.mandatory.Value;
			}
			set
			{
				this.mandatory = new bool?(value);
			}
		}

		public string Name
		{
			get;
			set;
		}

		public int Order
		{
			get
			{
				if (!this.order.HasValue)
				{
					return 0;
				}
				return this.order.Value;
			}
			set
			{
				this.order = new int?(value);
			}
		}

		public AmqpMemberAttribute()
		{
		}
	}
}