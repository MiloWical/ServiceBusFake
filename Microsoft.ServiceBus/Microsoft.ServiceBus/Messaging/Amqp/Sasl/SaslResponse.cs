using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Amqp.Sasl
{
	internal sealed class SaslResponse : Performative
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

		public ArraySegment<byte> Response
		{
			get;
			set;
		}

		static SaslResponse()
		{
			SaslResponse.Name = "amqp:sasl-response:list";
			SaslResponse.Code = (ulong)67;
		}

		public SaslResponse() : base(SaslResponse.Name, SaslResponse.Code)
		{
		}

		protected override void EnsureRequired()
		{
			if (this.Response.Array == null)
			{
				throw new AmqpException(AmqpError.InvalidField, "sasl-response:response");
			}
		}

		protected override void OnDecode(ByteBuffer buffer, int count)
		{
			int num = count;
			count = num - 1;
			if (num > 0)
			{
				this.Response = AmqpCodec.DecodeBinary(buffer);
			}
		}

		protected override void OnEncode(ByteBuffer buffer)
		{
			AmqpCodec.EncodeBinary(this.Response, buffer);
		}

		protected override int OnValueSize()
		{
			return 0 + AmqpCodec.GetBinaryEncodeSize(this.Response);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder("sasl-response(");
			int num = 0;
			ArraySegment<byte> response = this.Response;
			base.AddFieldToString(response.Array != null, stringBuilder, "response", this.Response, ref num);
			stringBuilder.Append(')');
			return stringBuilder.ToString();
		}
	}
}