using Microsoft.ServiceBus.Messaging.Amqp;
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class End : Performative
	{
		private const int Fields = 1;

		public readonly static string Name;

		public readonly static ulong Code;

		public Microsoft.ServiceBus.Messaging.Amqp.Framing.Error Error
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

		static End()
		{
			End.Name = "amqp:end:list";
			End.Code = (ulong)23;
		}

		public End() : base(End.Name, End.Code)
		{
		}

		protected override void OnDecode(ByteBuffer buffer, int count)
		{
			int num = count;
			count = num - 1;
			if (num > 0)
			{
				this.Error = AmqpCodec.DecodeKnownType<Microsoft.ServiceBus.Messaging.Amqp.Framing.Error>(buffer);
			}
		}

		protected override void OnEncode(ByteBuffer buffer)
		{
			AmqpCodec.EncodeSerializable(this.Error, buffer);
		}

		protected override int OnValueSize()
		{
			return 0 + AmqpCodec.GetSerializableEncodeSize(this.Error);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder("end(");
			int num = 0;
			base.AddFieldToString(this.Error != null, stringBuilder, "error", this.Error, ref num);
			stringBuilder.Append(')');
			return stringBuilder.ToString();
		}
	}
}