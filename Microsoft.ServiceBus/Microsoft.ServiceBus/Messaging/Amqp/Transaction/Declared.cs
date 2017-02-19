using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Amqp.Transaction
{
	internal sealed class Declared : Outcome
	{
		private const int Fields = 1;

		public readonly static string Name;

		public readonly static ulong Code;

		protected override int FieldCount
		{
			get
			{
				return 1;
			}
		}

		public ArraySegment<byte> TxnId
		{
			get;
			set;
		}

		static Declared()
		{
			Declared.Name = "amqp:declared:list";
			Declared.Code = (ulong)51;
		}

		public Declared() : base(Declared.Name, Declared.Code)
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
		}

		protected override void OnEncode(ByteBuffer buffer)
		{
			AmqpCodec.EncodeBinary(this.TxnId, buffer);
		}

		protected override int OnValueSize()
		{
			return 0 + AmqpCodec.GetBinaryEncodeSize(this.TxnId);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder("declared(");
			int num = 0;
			ArraySegment<byte> txnId = this.TxnId;
			base.AddFieldToString(txnId.Array != null, stringBuilder, "txn-id", this.TxnId, ref num);
			stringBuilder.Append(')');
			return stringBuilder.ToString();
		}
	}
}