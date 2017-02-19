using Microsoft.ServiceBus.Messaging.Amqp;
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class Header : DescribedList
	{
		private const int Fields = 5;

		public readonly static string Name;

		public readonly static ulong Code;

		public uint? DeliveryCount
		{
			get;
			set;
		}

		public bool? Durable
		{
			get;
			set;
		}

		protected override int FieldCount
		{
			get
			{
				return 5;
			}
		}

		public bool? FirstAcquirer
		{
			get;
			set;
		}

		public byte? Priority
		{
			get;
			set;
		}

		public uint? Ttl
		{
			get;
			set;
		}

		static Header()
		{
			Header.Name = "amqp:header:list";
			Header.Code = (ulong)112;
		}

		public Header() : base(Header.Name, Header.Code)
		{
		}

		protected override void OnDecode(ByteBuffer buffer, int count)
		{
			int num = count;
			count = num - 1;
			if (num > 0)
			{
				this.Durable = AmqpCodec.DecodeBoolean(buffer);
			}
			int num1 = count;
			count = num1 - 1;
			if (num1 > 0)
			{
				this.Priority = AmqpCodec.DecodeUByte(buffer);
			}
			int num2 = count;
			count = num2 - 1;
			if (num2 > 0)
			{
				this.Ttl = AmqpCodec.DecodeUInt(buffer);
			}
			int num3 = count;
			count = num3 - 1;
			if (num3 > 0)
			{
				this.FirstAcquirer = AmqpCodec.DecodeBoolean(buffer);
			}
			int num4 = count;
			count = num4 - 1;
			if (num4 > 0)
			{
				this.DeliveryCount = AmqpCodec.DecodeUInt(buffer);
			}
		}

		protected override void OnEncode(ByteBuffer buffer)
		{
			AmqpCodec.EncodeBoolean(this.Durable, buffer);
			AmqpCodec.EncodeUByte(this.Priority, buffer);
			AmqpCodec.EncodeUInt(this.Ttl, buffer);
			AmqpCodec.EncodeBoolean(this.FirstAcquirer, buffer);
			AmqpCodec.EncodeUInt(this.DeliveryCount, buffer);
		}

		protected override int OnValueSize()
		{
			int booleanEncodeSize = 0;
			booleanEncodeSize = AmqpCodec.GetBooleanEncodeSize(this.Durable);
			booleanEncodeSize = booleanEncodeSize + AmqpCodec.GetUByteEncodeSize(this.Priority);
			booleanEncodeSize = booleanEncodeSize + AmqpCodec.GetUIntEncodeSize(this.Ttl);
			booleanEncodeSize = booleanEncodeSize + AmqpCodec.GetBooleanEncodeSize(this.FirstAcquirer);
			return booleanEncodeSize + AmqpCodec.GetUIntEncodeSize(this.DeliveryCount);
		}

		public override string ToString()
		{
			int? nullable;
			StringBuilder stringBuilder = new StringBuilder("header(");
			int num = 0;
			bool? durable = this.Durable;
			base.AddFieldToString(durable.HasValue, stringBuilder, "durable", this.Durable, ref num);
			byte? priority = this.Priority;
			if (priority.HasValue)
			{
				nullable = new int?((int)priority.GetValueOrDefault());
			}
			else
			{
				nullable = null;
			}
			int? nullable1 = nullable;
			base.AddFieldToString(nullable1.HasValue, stringBuilder, "priority", this.Priority, ref num);
			uint? ttl = this.Ttl;
			base.AddFieldToString(ttl.HasValue, stringBuilder, "ttl", this.Ttl, ref num);
			bool? firstAcquirer = this.FirstAcquirer;
			base.AddFieldToString(firstAcquirer.HasValue, stringBuilder, "first-acquirer", this.FirstAcquirer, ref num);
			uint? deliveryCount = this.DeliveryCount;
			base.AddFieldToString(deliveryCount.HasValue, stringBuilder, "delivery-count", this.DeliveryCount, ref num);
			stringBuilder.Append(')');
			return stringBuilder.ToString();
		}
	}
}