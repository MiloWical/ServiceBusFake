using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Amqp.Sasl
{
	internal sealed class SaslOutcome : Performative
	{
		private const int Fields = 2;

		public readonly static string Name;

		public readonly static ulong Code;

		public ArraySegment<byte> AdditionalData
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

		public SaslCode? OutcomeCode
		{
			get;
			set;
		}

		static SaslOutcome()
		{
			SaslOutcome.Name = "amqp:sasl-outcome:list";
			SaslOutcome.Code = (ulong)68;
		}

		public SaslOutcome() : base(SaslOutcome.Name, SaslOutcome.Code)
		{
		}

		protected override void EnsureRequired()
		{
			if (!this.OutcomeCode.HasValue)
			{
				throw new AmqpException(AmqpError.InvalidField, "sasl-outcome:code");
			}
		}

		protected override void OnDecode(ByteBuffer buffer, int count)
		{
			SaslCode? nullable;
			int num = count;
			count = num - 1;
			if (num > 0)
			{
				byte? nullable1 = AmqpCodec.DecodeUByte(buffer);
				if (nullable1.HasValue)
				{
					nullable = new SaslCode?((SaslCode)nullable1.GetValueOrDefault());
				}
				else
				{
					nullable = null;
				}
				this.OutcomeCode = nullable;
			}
			int num1 = count;
			count = num1 - 1;
			if (num1 > 0)
			{
				this.AdditionalData = AmqpCodec.DecodeBinary(buffer);
			}
		}

		protected override void OnEncode(ByteBuffer buffer)
		{
			byte? nullable;
			SaslCode? outcomeCode = this.OutcomeCode;
			if (outcomeCode.HasValue)
			{
				nullable = new byte?(outcomeCode.GetValueOrDefault());
			}
			else
			{
				nullable = null;
			}
			AmqpCodec.EncodeUByte(nullable, buffer);
			AmqpCodec.EncodeBinary(this.AdditionalData, buffer);
		}

		protected override int OnValueSize()
		{
			byte? nullable;
			int num = 0;
			SaslCode? outcomeCode = this.OutcomeCode;
			if (outcomeCode.HasValue)
			{
				nullable = new byte?(outcomeCode.GetValueOrDefault());
			}
			else
			{
				nullable = null;
			}
			int uByteEncodeSize = num + AmqpCodec.GetUByteEncodeSize(nullable);
			return uByteEncodeSize + AmqpCodec.GetBinaryEncodeSize(this.AdditionalData);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder("sasl-outcome(");
			int num = 0;
			SaslCode? outcomeCode = this.OutcomeCode;
			base.AddFieldToString(outcomeCode.HasValue, stringBuilder, "code", this.OutcomeCode, ref num);
			ArraySegment<byte> additionalData = this.AdditionalData;
			base.AddFieldToString(additionalData.Array != null, stringBuilder, "additional-data", this.AdditionalData, ref num);
			stringBuilder.Append(')');
			return stringBuilder.ToString();
		}
	}
}