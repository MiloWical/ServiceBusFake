using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Amqp.Transaction
{
	internal sealed class Discharge : Performative
	{
		private const int Fields = 2;

		public readonly static string Name;

		public readonly static ulong Code;

		public bool? Fail
		{
			get;
			set;
		}

		protected override int FieldCount
		{
			get
			{
				return 2;
			}
		}

		public ArraySegment<byte> TxnId
		{
			get;
			set;
		}

		static Discharge()
		{
			Discharge.Name = "amqp:discharge:list";
			Discharge.Code = (ulong)50;
		}

		public Discharge() : base(Discharge.Name, Discharge.Code)
		{
		}

		protected override void OnDecode(ByteBuffer buffer, int count)
		{
			int num = count;
			count = num - 1;
			if (num > 0)
			{
				this.TxnId = AmqpCodec.DecodeBinary(buffer);
			}
			int num1 = count;
			count = num1 - 1;
			if (num1 > 0)
			{
				this.Fail = AmqpCodec.DecodeBoolean(buffer);
			}
		}

		protected override void OnEncode(ByteBuffer buffer)
		{
			AmqpCodec.EncodeBinary(this.TxnId, buffer);
			AmqpCodec.EncodeBoolean(this.Fail, buffer);
		}

		protected override int OnValueSize()
		{
			int binaryEncodeSize = 0;
			binaryEncodeSize = AmqpCodec.GetBinaryEncodeSize(this.TxnId);
			return binaryEncodeSize + AmqpCodec.GetBooleanEncodeSize(this.Fail);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder("discharge(");
			int num = 0;
			ArraySegment<byte> txnId = this.TxnId;
			base.AddFieldToString(txnId.Array != null, stringBuilder, "txn-id", this.TxnId, ref num);
			bool? fail = this.Fail;
			base.AddFieldToString(fail.HasValue, stringBuilder, "fail", this.Fail, ref num);
			stringBuilder.Append(')');
			return stringBuilder.ToString();
		}
	}
}