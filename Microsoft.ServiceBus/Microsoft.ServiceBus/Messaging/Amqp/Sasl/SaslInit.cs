using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Amqp.Sasl
{
	internal sealed class SaslInit : Performative
	{
		private const int Fields = 3;

		public readonly static string Name;

		public readonly static ulong Code;

		protected override int FieldCount
		{
			get
			{
				return 3;
			}
		}

		public string HostName
		{
			get;
			set;
		}

		public ArraySegment<byte> InitialResponse
		{
			get;
			set;
		}

		public AmqpSymbol Mechanism
		{
			get;
			set;
		}

		static SaslInit()
		{
			SaslInit.Name = "amqp:sasl-init:list";
			SaslInit.Code = (ulong)65;
		}

		public SaslInit() : base(SaslInit.Name, SaslInit.Code)
		{
		}

		protected override void EnsureRequired()
		{
			if (this.Mechanism.Value == null)
			{
				throw new AmqpException(AmqpError.InvalidField, "mechanism");
			}
		}

		protected override void OnDecode(ByteBuffer buffer, int count)
		{
			int num = count;
			count = num - 1;
			if (num > 0)
			{
				this.Mechanism = AmqpCodec.DecodeSymbol(buffer);
			}
			int num1 = count;
			count = num1 - 1;
			if (num1 > 0)
			{
				this.InitialResponse = AmqpCodec.DecodeBinary(buffer);
			}
			int num2 = count;
			count = num2 - 1;
			if (num2 > 0)
			{
				this.HostName = AmqpCodec.DecodeString(buffer);
			}
		}

		protected override void OnEncode(ByteBuffer buffer)
		{
			AmqpCodec.EncodeSymbol(this.Mechanism, buffer);
			AmqpCodec.EncodeBinary(this.InitialResponse, buffer);
			AmqpCodec.EncodeString(this.HostName, buffer);
		}

		protected override int OnValueSize()
		{
			int symbolEncodeSize = 0 + AmqpCodec.GetSymbolEncodeSize(this.Mechanism);
			symbolEncodeSize = symbolEncodeSize + AmqpCodec.GetBinaryEncodeSize(this.InitialResponse);
			return symbolEncodeSize + AmqpCodec.GetStringEncodeSize(this.HostName);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder("sasl-init(");
			int num = 0;
			AmqpSymbol mechanism = this.Mechanism;
			base.AddFieldToString(mechanism.Value != null, stringBuilder, "mechanism", this.Mechanism, ref num);
			ArraySegment<byte> initialResponse = this.InitialResponse;
			base.AddFieldToString(initialResponse.Array != null, stringBuilder, "initial-response", this.InitialResponse, ref num);
			base.AddFieldToString(this.HostName != null, stringBuilder, "host-name", this.HostName, ref num);
			stringBuilder.Append(')');
			return stringBuilder.ToString();
		}
	}
}