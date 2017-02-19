using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Amqp.Sasl
{
	internal sealed class SaslChallenge : Performative
	{
		private const int Fields = 1;

		public readonly static string Name;

		public readonly static ulong Code;

		public ArraySegment<byte> Challenge
		{
			get;
			set;
		}

		protected override int FieldCount
		{
			get
			{
				return 1;
			}
		}

		static SaslChallenge()
		{
			SaslChallenge.Name = "amqp:sasl-challenge:list";
			SaslChallenge.Code = (ulong)66;
		}

		public SaslChallenge() : base(SaslChallenge.Name, SaslChallenge.Code)
		{
		}

		protected override void EnsureRequired()
		{
			if (this.Challenge.Array == null)
			{
				throw new AmqpException(AmqpError.InvalidField, "sasl-challenge:challenge");
			}
		}

		protected override void OnDecode(ByteBuffer buffer, int count)
		{
			int num = count;
			count = num - 1;
			if (num > 0)
			{
				this.Challenge = AmqpCodec.DecodeBinary(buffer);
			}
		}

		protected override void OnEncode(ByteBuffer buffer)
		{
			AmqpCodec.EncodeBinary(this.Challenge, buffer);
		}

		protected override int OnValueSize()
		{
			return 0 + AmqpCodec.GetBinaryEncodeSize(this.Challenge);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder("sasl-challenge(");
			int num = 0;
			ArraySegment<byte> challenge = this.Challenge;
			base.AddFieldToString(challenge.Array != null, stringBuilder, "challenge", this.Challenge, ref num);
			stringBuilder.Append(')');
			return stringBuilder.ToString();
		}
	}
}