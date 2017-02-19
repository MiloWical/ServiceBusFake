using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class Detach : LinkPerformative
	{
		private const int Fields = 3;

		public readonly static string Name;

		public readonly static ulong Code;

		public bool? Closed
		{
			get;
			set;
		}

		public Microsoft.ServiceBus.Messaging.Amqp.Framing.Error Error
		{
			get;
			set;
		}

		protected override int FieldCount
		{
			get
			{
				return 3;
			}
		}

		static Detach()
		{
			Detach.Name = "amqp:detach:list";
			Detach.Code = (ulong)22;
		}

		public Detach() : base(Detach.Name, Detach.Code)
		{
		}

		protected override void EnsureRequired()
		{
			if (!base.Handle.HasValue)
			{
				throw AmqpEncoding.GetEncodingException(SRAmqp.AmqpRequiredFieldNotSet("handle", Detach.Name));
			}
		}

		protected override void OnDecode(ByteBuffer buffer, int count)
		{
			int num = count;
			count = num - 1;
			if (num > 0)
			{
				base.Handle = AmqpCodec.DecodeUInt(buffer);
			}
			int num1 = count;
			count = num1 - 1;
			if (num1 > 0)
			{
				this.Closed = AmqpCodec.DecodeBoolean(buffer);
			}
			int num2 = count;
			count = num2 - 1;
			if (num2 > 0)
			{
				this.Error = AmqpCodec.DecodeKnownType<Microsoft.ServiceBus.Messaging.Amqp.Framing.Error>(buffer);
			}
		}

		protected override void OnEncode(ByteBuffer buffer)
		{
			AmqpCodec.EncodeUInt(base.Handle, buffer);
			AmqpCodec.EncodeBoolean(this.Closed, buffer);
			AmqpCodec.EncodeSerializable(this.Error, buffer);
		}

		protected override int OnValueSize()
		{
			int uIntEncodeSize = 0 + AmqpCodec.GetUIntEncodeSize(base.Handle);
			uIntEncodeSize = uIntEncodeSize + AmqpCodec.GetBooleanEncodeSize(this.Closed);
			return uIntEncodeSize + AmqpCodec.GetObjectEncodeSize(this.Error);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder("detach(");
			int num = 0;
			uint? handle = base.Handle;
			base.AddFieldToString(handle.HasValue, stringBuilder, "handle", base.Handle, ref num);
			bool? closed = this.Closed;
			base.AddFieldToString(closed.HasValue, stringBuilder, "closed", this.Closed, ref num);
			base.AddFieldToString(this.Error != null, stringBuilder, "error", this.Error, ref num);
			stringBuilder.Append(')');
			return stringBuilder.ToString();
		}
	}
}