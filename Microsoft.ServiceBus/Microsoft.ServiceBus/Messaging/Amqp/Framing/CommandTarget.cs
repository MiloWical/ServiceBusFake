using Microsoft.ServiceBus.Messaging.Amqp;
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class CommandTarget : DescribedList
	{
		private const int Fields = 2;

		public readonly static string Name;

		public readonly static ulong Code;

		public string Entity
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

		public Microsoft.ServiceBus.Messaging.Amqp.Framing.Fields Properties
		{
			get;
			set;
		}

		static CommandTarget()
		{
			CommandTarget.Name = "com.microsoft:command-target:list";
			CommandTarget.Code = 1335734829057L;
		}

		public CommandTarget() : base(CommandTarget.Name, CommandTarget.Code)
		{
		}

		protected override void EnsureRequired()
		{
			if (this.Entity == null)
			{
				throw new AmqpException(AmqpError.InvalidField, "command-target.entity");
			}
		}

		protected override void OnDecode(ByteBuffer buffer, int count)
		{
			int num = count;
			count = num - 1;
			if (num > 0)
			{
				this.Entity = AmqpCodec.DecodeString(buffer);
			}
			int num1 = count;
			count = num1 - 1;
			if (num1 > 0)
			{
				this.Properties = AmqpCodec.DecodeMap<Microsoft.ServiceBus.Messaging.Amqp.Framing.Fields>(buffer);
			}
		}

		protected override void OnEncode(ByteBuffer buffer)
		{
			AmqpCodec.EncodeString(this.Entity, buffer);
			AmqpCodec.EncodeMap(this.Properties, buffer);
		}

		protected override int OnValueSize()
		{
			int stringEncodeSize = 0 + AmqpCodec.GetStringEncodeSize(this.Entity);
			stringEncodeSize = stringEncodeSize + AmqpCodec.GetMapEncodeSize(this.Properties);
			return stringEncodeSize;
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder("cmdtarget(");
			int num = 0;
			base.AddFieldToString(this.Entity != null, stringBuilder, "entity", this.Entity, ref num);
			base.AddFieldToString(this.Properties != null, stringBuilder, "properties", this.Properties, ref num);
			stringBuilder.Append(')');
			return stringBuilder.ToString();
		}
	}
}