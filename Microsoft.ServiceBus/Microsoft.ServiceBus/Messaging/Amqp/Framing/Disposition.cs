using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class Disposition : Performative
	{
		private const int Fields = 6;

		public readonly static string Name;

		public readonly static ulong Code;

		public bool? Batchable
		{
			get;
			set;
		}

		protected override int FieldCount
		{
			get
			{
				return 6;
			}
		}

		public uint? First
		{
			get;
			set;
		}

		public uint? Last
		{
			get;
			set;
		}

		public bool? Role
		{
			get;
			set;
		}

		public bool? Settled
		{
			get;
			set;
		}

		public DeliveryState State
		{
			get;
			set;
		}

		static Disposition()
		{
			Disposition.Name = "amqp:disposition:list";
			Disposition.Code = (ulong)21;
		}

		public Disposition() : base(Disposition.Name, Disposition.Code)
		{
		}

		protected override void EnsureRequired()
		{
			if (!this.Role.HasValue)
			{
				throw AmqpEncoding.GetEncodingException(SRAmqp.AmqpRequiredFieldNotSet("role", Disposition.Name));
			}
			if (!this.First.HasValue)
			{
				throw AmqpEncoding.GetEncodingException(SRAmqp.AmqpRequiredFieldNotSet("first", Disposition.Name));
			}
		}

		protected override void OnDecode(ByteBuffer buffer, int count)
		{
			int num = count;
			count = num - 1;
			if (num > 0)
			{
				this.Role = AmqpCodec.DecodeBoolean(buffer);
			}
			int num1 = count;
			count = num1 - 1;
			if (num1 > 0)
			{
				this.First = AmqpCodec.DecodeUInt(buffer);
			}
			int num2 = count;
			count = num2 - 1;
			if (num2 > 0)
			{
				this.Last = AmqpCodec.DecodeUInt(buffer);
			}
			int num3 = count;
			count = num3 - 1;
			if (num3 > 0)
			{
				this.Settled = AmqpCodec.DecodeBoolean(buffer);
			}
			int num4 = count;
			count = num4 - 1;
			if (num4 > 0)
			{
				this.State = (DeliveryState)AmqpCodec.DecodeAmqpDescribed(buffer);
			}
			int num5 = count;
			count = num5 - 1;
			if (num5 > 0)
			{
				this.Batchable = AmqpCodec.DecodeBoolean(buffer);
			}
		}

		protected override void OnEncode(ByteBuffer buffer)
		{
			AmqpCodec.EncodeBoolean(this.Role, buffer);
			AmqpCodec.EncodeUInt(this.First, buffer);
			AmqpCodec.EncodeUInt(this.Last, buffer);
			AmqpCodec.EncodeBoolean(this.Settled, buffer);
			AmqpCodec.EncodeSerializable(this.State, buffer);
			AmqpCodec.EncodeBoolean(this.Batchable, buffer);
		}

		protected override int OnValueSize()
		{
			int booleanEncodeSize = 0 + AmqpCodec.GetBooleanEncodeSize(this.Role);
			booleanEncodeSize = booleanEncodeSize + AmqpCodec.GetUIntEncodeSize(this.First);
			booleanEncodeSize = booleanEncodeSize + AmqpCodec.GetUIntEncodeSize(this.Last);
			booleanEncodeSize = booleanEncodeSize + AmqpCodec.GetBooleanEncodeSize(this.Settled);
			booleanEncodeSize = booleanEncodeSize + AmqpCodec.GetSerializableEncodeSize(this.State);
			return booleanEncodeSize + AmqpCodec.GetBooleanEncodeSize(this.Batchable);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder("disposition(");
			int num = 0;
			bool? role = this.Role;
			base.AddFieldToString(role.HasValue, stringBuilder, "role", this.Role, ref num);
			uint? first = this.First;
			base.AddFieldToString(first.HasValue, stringBuilder, "first", this.First, ref num);
			uint? last = this.Last;
			base.AddFieldToString(last.HasValue, stringBuilder, "last", this.Last, ref num);
			bool? settled = this.Settled;
			base.AddFieldToString(settled.HasValue, stringBuilder, "settled", this.Settled, ref num);
			base.AddFieldToString(this.State != null, stringBuilder, "state", this.State, ref num);
			bool? batchable = this.Batchable;
			base.AddFieldToString(batchable.HasValue, stringBuilder, "batchable", this.Batchable, ref num);
			stringBuilder.Append(')');
			return stringBuilder.ToString();
		}
	}
}