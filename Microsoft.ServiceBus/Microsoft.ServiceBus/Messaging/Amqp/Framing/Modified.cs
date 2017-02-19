using Microsoft.ServiceBus.Messaging.Amqp;
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class Modified : Outcome
	{
		private const int Fields = 3;

		public readonly static string Name;

		public readonly static ulong Code;

		public bool? DeliveryFailed
		{
			get;
			set;
		}

		protected override int FieldCount
		{
			get
			{
				return 3;
			}
		}

		public Microsoft.ServiceBus.Messaging.Amqp.Framing.Fields MessageAnnotations
		{
			get;
			set;
		}

		public bool? UndeliverableHere
		{
			get;
			set;
		}

		static Modified()
		{
			Modified.Name = "amqp:modified:list";
			Modified.Code = (ulong)39;
		}

		public Modified() : base(Modified.Name, Modified.Code)
		{
		}

		protected override void OnDecode(ByteBuffer buffer, int count)
		{
			int num = count;
			count = num - 1;
			if (num > 0)
			{
				this.DeliveryFailed = AmqpCodec.DecodeBoolean(buffer);
			}
			int num1 = count;
			count = num1 - 1;
			if (num1 > 0)
			{
				this.UndeliverableHere = AmqpCodec.DecodeBoolean(buffer);
			}
			int num2 = count;
			count = num2 - 1;
			if (num2 > 0)
			{
				this.MessageAnnotations = AmqpCodec.DecodeMap<Microsoft.ServiceBus.Messaging.Amqp.Framing.Fields>(buffer);
			}
		}

		protected override void OnEncode(ByteBuffer buffer)
		{
			AmqpCodec.EncodeBoolean(this.DeliveryFailed, buffer);
			AmqpCodec.EncodeBoolean(this.UndeliverableHere, buffer);
			AmqpCodec.EncodeMap(this.MessageAnnotations, buffer);
		}

		protected override int OnValueSize()
		{
			int booleanEncodeSize = 0 + AmqpCodec.GetBooleanEncodeSize(this.DeliveryFailed);
			booleanEncodeSize = booleanEncodeSize + AmqpCodec.GetBooleanEncodeSize(this.UndeliverableHere);
			booleanEncodeSize = booleanEncodeSize + AmqpCodec.GetMapEncodeSize(this.MessageAnnotations);
			return booleanEncodeSize;
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder("modified(");
			int num = 0;
			bool? deliveryFailed = this.DeliveryFailed;
			base.AddFieldToString(deliveryFailed.HasValue, stringBuilder, "delivery-failed", this.DeliveryFailed, ref num);
			bool? undeliverableHere = this.UndeliverableHere;
			base.AddFieldToString(undeliverableHere.HasValue, stringBuilder, "undeliverable-here", this.UndeliverableHere, ref num);
			base.AddFieldToString(this.MessageAnnotations != null, stringBuilder, "message-annotations", this.MessageAnnotations, ref num);
			stringBuilder.Append(')');
			return stringBuilder.ToString();
		}
	}
}