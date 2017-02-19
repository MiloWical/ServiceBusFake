using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class Transfer : LinkPerformative
	{
		private const int Fields = 11;

		public readonly static string Name;

		public readonly static ulong Code;

		public bool? Aborted
		{
			get;
			set;
		}

		public bool? Batchable
		{
			get;
			set;
		}

		public uint? DeliveryId
		{
			get;
			set;
		}

		public ArraySegment<byte> DeliveryTag
		{
			get;
			set;
		}

		protected override int FieldCount
		{
			get
			{
				return 11;
			}
		}

		public uint? MessageFormat
		{
			get;
			set;
		}

		public bool? More
		{
			get;
			set;
		}

		public byte? RcvSettleMode
		{
			get;
			set;
		}

		public bool? Resume
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

		static Transfer()
		{
			Transfer.Name = "amqp:transfer:list";
			Transfer.Code = (ulong)20;
		}

		public Transfer() : base(Transfer.Name, Transfer.Code)
		{
		}

		protected override void EnsureRequired()
		{
			if (!base.Handle.HasValue)
			{
				throw AmqpEncoding.GetEncodingException(SRAmqp.AmqpRequiredFieldNotSet("handle", Transfer.Name));
			}
		}

		protected override void OnDecode(ByteBuffer buffer, int count)
		{
			int num = count;
			count = num - 1;
			if (num > 0)
			{
				base.Handle = AmqpCodec.DecodeUInt(buffer);
			}
			int num1 = count;
			count = num1 - 1;
			if (num1 > 0)
			{
				this.DeliveryId = AmqpCodec.DecodeUInt(buffer);
			}
			int num2 = count;
			count = num2 - 1;
			if (num2 > 0)
			{
				this.DeliveryTag = AmqpCodec.DecodeBinary(buffer);
			}
			int num3 = count;
			count = num3 - 1;
			if (num3 > 0)
			{
				this.MessageFormat = AmqpCodec.DecodeUInt(buffer);
			}
			int num4 = count;
			count = num4 - 1;
			if (num4 > 0)
			{
				this.Settled = AmqpCodec.DecodeBoolean(buffer);
			}
			int num5 = count;
			count = num5 - 1;
			if (num5 > 0)
			{
				this.More = AmqpCodec.DecodeBoolean(buffer);
			}
			int num6 = count;
			count = num6 - 1;
			if (num6 > 0)
			{
				this.RcvSettleMode = AmqpCodec.DecodeUByte(buffer);
			}
			int num7 = count;
			count = num7 - 1;
			if (num7 > 0)
			{
				this.State = (DeliveryState)AmqpCodec.DecodeAmqpDescribed(buffer);
			}
			int num8 = count;
			count = num8 - 1;
			if (num8 > 0)
			{
				this.Resume = AmqpCodec.DecodeBoolean(buffer);
			}
			int num9 = count;
			count = num9 - 1;
			if (num9 > 0)
			{
				this.Aborted = AmqpCodec.DecodeBoolean(buffer);
			}
			int num10 = count;
			count = num10 - 1;
			if (num10 > 0)
			{
				this.Batchable = AmqpCodec.DecodeBoolean(buffer);
			}
		}

		protected override void OnEncode(ByteBuffer buffer)
		{
			AmqpCodec.EncodeUInt(base.Handle, buffer);
			AmqpCodec.EncodeUInt(this.DeliveryId, buffer);
			AmqpCodec.EncodeBinary(this.DeliveryTag, buffer);
			AmqpCodec.EncodeUInt(this.MessageFormat, buffer);
			AmqpCodec.EncodeBoolean(this.Settled, buffer);
			AmqpCodec.EncodeBoolean(this.More, buffer);
			AmqpCodec.EncodeUByte(this.RcvSettleMode, buffer);
			AmqpCodec.EncodeSerializable(this.State, buffer);
			AmqpCodec.EncodeBoolean(this.Resume, buffer);
			AmqpCodec.EncodeBoolean(this.Aborted, buffer);
			AmqpCodec.EncodeBoolean(this.Batchable, buffer);
		}

		protected override int OnValueSize()
		{
			int uIntEncodeSize = 0 + AmqpCodec.GetUIntEncodeSize(base.Handle);
			uIntEncodeSize = uIntEncodeSize + AmqpCodec.GetUIntEncodeSize(this.DeliveryId);
			uIntEncodeSize = uIntEncodeSize + AmqpCodec.GetBinaryEncodeSize(this.DeliveryTag);
			uIntEncodeSize = uIntEncodeSize + AmqpCodec.GetUIntEncodeSize(this.MessageFormat);
			uIntEncodeSize = uIntEncodeSize + AmqpCodec.GetBooleanEncodeSize(this.Settled);
			uIntEncodeSize = uIntEncodeSize + AmqpCodec.GetBooleanEncodeSize(this.More);
			uIntEncodeSize = uIntEncodeSize + AmqpCodec.GetUByteEncodeSize(this.RcvSettleMode);
			uIntEncodeSize = uIntEncodeSize + AmqpCodec.GetSerializableEncodeSize(this.State);
			uIntEncodeSize = uIntEncodeSize + AmqpCodec.GetBooleanEncodeSize(this.Resume);
			uIntEncodeSize = uIntEncodeSize + AmqpCodec.GetBooleanEncodeSize(this.Aborted);
			return uIntEncodeSize + AmqpCodec.GetBooleanEncodeSize(this.Batchable);
		}

		public override string ToString()
		{
			int? nullable;
			StringBuilder stringBuilder = new StringBuilder("transfer(");
			int num = 0;
			uint? handle = base.Handle;
			base.AddFieldToString(handle.HasValue, stringBuilder, "handle", base.Handle, ref num);
			uint? deliveryId = this.DeliveryId;
			base.AddFieldToString(deliveryId.HasValue, stringBuilder, "delivery-id", this.DeliveryId, ref num);
			ArraySegment<byte> deliveryTag = this.DeliveryTag;
			base.AddFieldToString(deliveryTag.Array != null, stringBuilder, "delivery-tag", this.DeliveryTag, ref num);
			uint? messageFormat = this.MessageFormat;
			base.AddFieldToString(messageFormat.HasValue, stringBuilder, "message-format", this.MessageFormat, ref num);
			bool? settled = this.Settled;
			base.AddFieldToString(settled.HasValue, stringBuilder, "settled", this.Settled, ref num);
			bool? more = this.More;
			base.AddFieldToString(more.HasValue, stringBuilder, "more", this.More, ref num);
			byte? rcvSettleMode = this.RcvSettleMode;
			if (rcvSettleMode.HasValue)
			{
				nullable = new int?((int)rcvSettleMode.GetValueOrDefault());
			}
			else
			{
				nullable = null;
			}
			int? nullable1 = nullable;
			base.AddFieldToString(nullable1.HasValue, stringBuilder, "rcv-settle-mode", this.RcvSettleMode, ref num);
			base.AddFieldToString(this.State != null, stringBuilder, "state", this.State, ref num);
			bool? resume = this.Resume;
			base.AddFieldToString(resume.HasValue, stringBuilder, "resume", this.Resume, ref num);
			bool? aborted = this.Aborted;
			base.AddFieldToString(aborted.HasValue, stringBuilder, "aborted", this.Aborted, ref num);
			bool? batchable = this.Batchable;
			base.AddFieldToString(batchable.HasValue, stringBuilder, "batchable", this.Batchable, ref num);
			stringBuilder.Append(')');
			return stringBuilder.ToString();
		}
	}
}