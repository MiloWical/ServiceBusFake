using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Amqp.Transaction
{
	internal sealed class TransactionalState : DeliveryState
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

		public Microsoft.ServiceBus.Messaging.Amqp.Framing.Outcome Outcome
		{
			get;
			set;
		}

		public ArraySegment<byte> TxnId
		{
			get;
			set;
		}

		static TransactionalState()
		{
			TransactionalState.Name = "amqp:transactional-state:list";
			TransactionalState.Code = (ulong)52;
		}

		public TransactionalState() : base(TransactionalState.Name, TransactionalState.Code)
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
				this.Outcome = (Microsoft.ServiceBus.Messaging.Amqp.Framing.Outcome)AmqpCodec.DecodeAmqpDescribed(buffer);
			}
		}

		protected override void OnEncode(ByteBuffer buffer)
		{
			AmqpCodec.EncodeBinary(this.TxnId, buffer);
			AmqpCodec.EncodeSerializable(this.Outcome, buffer);
		}

		protected override int OnValueSize()
		{
			int binaryEncodeSize = 0 + AmqpCodec.GetBinaryEncodeSize(this.TxnId);
			return binaryEncodeSize + AmqpCodec.GetSerializableEncodeSize(this.Outcome);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder("txn-state(");
			int num = 0;
			ArraySegment<byte> txnId = this.TxnId;
			base.AddFieldToString(txnId.Array != null, stringBuilder, "txn-id", this.TxnId, ref num);
			base.AddFieldToString(this.Outcome != null, stringBuilder, "outcome", this.Outcome, ref num);
			stringBuilder.Append(')');
			return stringBuilder.ToString();
		}
	}
}