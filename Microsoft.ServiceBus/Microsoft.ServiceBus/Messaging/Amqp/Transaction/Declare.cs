using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Amqp.Transaction
{
	internal sealed class Declare : Performative
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

		public object GlobalId
		{
			get;
			set;
		}

		static Declare()
		{
			Declare.Name = "amqp:declare:list";
			Declare.Code = (ulong)49;
		}

		public Declare() : base(Declare.Name, Declare.Code)
		{
		}

		protected override void OnDecode(ByteBuffer buffer, int count)
		{
			int num = count;
			count = num - 1;
			if (num > 0)
			{
				this.GlobalId = AmqpEncoding.DecodeObject(buffer);
			}
		}

		protected override void OnEncode(ByteBuffer buffer)
		{
			AmqpCodec.EncodeObject(this.GlobalId, buffer);
		}

		protected override int OnValueSize()
		{
			return 0 + AmqpCodec.GetObjectEncodeSize(this.GlobalId);
		}

		public override string ToString()
		{
			return "declare()";
		}
	}
}