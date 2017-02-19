using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Amqp.Sasl
{
	internal sealed class SaslMechanisms : Performative
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

		public Multiple<AmqpSymbol> SaslServerMechanisms
		{
			get;
			set;
		}

		static SaslMechanisms()
		{
			SaslMechanisms.Name = "amqp:sasl-mechanisms:list";
			SaslMechanisms.Code = (ulong)64;
		}

		public SaslMechanisms() : base(SaslMechanisms.Name, SaslMechanisms.Code)
		{
		}

		protected override void EnsureRequired()
		{
			if (this.SaslServerMechanisms == null)
			{
				throw new AmqpException(AmqpError.InvalidField, "sasl-mechanisms:sasl-server-mechanisms");
			}
		}

		protected override void OnDecode(ByteBuffer buffer, int count)
		{
			int num = count;
			count = num - 1;
			if (num > 0)
			{
				this.SaslServerMechanisms = AmqpCodec.DecodeMultiple<AmqpSymbol>(buffer);
			}
		}

		protected override void OnEncode(ByteBuffer buffer)
		{
			AmqpCodec.EncodeMultiple<AmqpSymbol>(this.SaslServerMechanisms, buffer);
		}

		protected override int OnValueSize()
		{
			return 0 + AmqpCodec.GetMultipleEncodeSize<AmqpSymbol>(this.SaslServerMechanisms);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder("sasl-mechanisms(");
			int num = 0;
			base.AddFieldToString(this.SaslServerMechanisms != null, stringBuilder, "sasl-server-mechanisms", this.SaslServerMechanisms, ref num);
			stringBuilder.Append(')');
			return stringBuilder.ToString();
		}
	}
}