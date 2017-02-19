using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Amqp.Serialization
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple=false, Inherited=true)]
	internal sealed class AmqpContractAttribute : Attribute
	{
		public long Code
		{
			get;
			set;
		}

		public EncodingType Encoding
		{
			get;
			set;
		}

		internal ulong? InternalCode
		{
			get
			{
				if (this.Code < (long)0)
				{
					return null;
				}
				return new ulong?((ulong)this.Code);
			}
		}

		public string Name
		{
			get;
			set;
		}

		public AmqpContractAttribute()
		{
			this.Encoding = EncodingType.List;
			this.Code = (long)-1;
		}
	}
}