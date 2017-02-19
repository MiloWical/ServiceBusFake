using Microsoft.ServiceBus.Messaging.Amqp;
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class Received : DeliveryState
	{
		private const int Fields = 2;

		public readonly static string Name;

		public readonly static ulong Code;

		protected override int FieldCount
		{
			get
			{
				return 2;
			}
		}

		public uint? SectionNumber
		{
			get;
			set;
		}

		public ulong? SectionOffset
		{
			get;
			set;
		}

		static Received()
		{
			Received.Name = "amqp:received:list";
			Received.Code = (ulong)35;
		}

		public Received() : base(Received.Name, Received.Code)
		{
		}

		protected override void OnDecode(ByteBuffer buffer, int count)
		{
			int num = count;
			count = num - 1;
			if (num > 0)
			{
				this.SectionNumber = AmqpCodec.DecodeUInt(buffer);
			}
			int num1 = count;
			count = num1 - 1;
			if (num1 > 0)
			{
				this.SectionOffset = AmqpCodec.DecodeULong(buffer);
			}
		}

		protected override void OnEncode(ByteBuffer buffer)
		{
			AmqpCodec.EncodeUInt(this.SectionNumber, buffer);
			AmqpCodec.EncodeULong(this.SectionOffset, buffer);
		}

		protected override int OnValueSize()
		{
			int uIntEncodeSize = 0 + AmqpCodec.GetUIntEncodeSize(this.SectionNumber);
			return uIntEncodeSize + AmqpCodec.GetULongEncodeSize(this.SectionOffset);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder("received(");
			int num = 0;
			uint? sectionNumber = this.SectionNumber;
			base.AddFieldToString(sectionNumber.HasValue, stringBuilder, "section-number", this.SectionNumber, ref num);
			ulong? sectionOffset = this.SectionOffset;
			base.AddFieldToString(sectionOffset.HasValue, stringBuilder, "section-offset", this.SectionOffset, ref num);
			stringBuilder.Append(')');
			return stringBuilder.ToString();
		}
	}
}