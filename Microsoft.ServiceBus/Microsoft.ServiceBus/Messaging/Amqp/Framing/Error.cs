using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	[Serializable]
	internal sealed class Error : DescribedList, ISerializable
	{
		private const int Fields = 3;

		public readonly static string Name;

		public readonly static ulong Code;

		public AmqpSymbol Condition
		{
			get;
			set;
		}

		public string Description
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

		public Microsoft.ServiceBus.Messaging.Amqp.Framing.Fields Info
		{
			get;
			set;
		}

		static Error()
		{
			Error.Name = "amqp:error:list";
			Error.Code = (ulong)29;
		}

		public Error() : base(Error.Name, Error.Code)
		{
		}

		private Error(SerializationInfo info, StreamingContext context) : base(Error.Name, Error.Code)
		{
			this.Condition = (string)info.GetValue("Condition", typeof(string));
			this.Description = (string)info.GetValue("Description", typeof(string));
		}

		protected override void EnsureRequired()
		{
			if (this.Condition.Value == null)
			{
				throw AmqpEncoding.GetEncodingException(SRAmqp.AmqpRequiredFieldNotSet("condition", Error.Name));
			}
		}

		protected override void OnDecode(ByteBuffer buffer, int count)
		{
			int num = count;
			count = num - 1;
			if (num > 0)
			{
				this.Condition = AmqpCodec.DecodeSymbol(buffer);
			}
			int num1 = count;
			count = num1 - 1;
			if (num1 > 0)
			{
				this.Description = AmqpCodec.DecodeString(buffer);
			}
			int num2 = count;
			count = num2 - 1;
			if (num2 > 0)
			{
				this.Info = AmqpCodec.DecodeMap<Microsoft.ServiceBus.Messaging.Amqp.Framing.Fields>(buffer);
			}
		}

		protected override void OnEncode(ByteBuffer buffer)
		{
			AmqpCodec.EncodeSymbol(this.Condition, buffer);
			AmqpCodec.EncodeString(this.Description, buffer);
			AmqpCodec.EncodeMap(this.Info, buffer);
		}

		protected override int OnValueSize()
		{
			int symbolEncodeSize = 0;
			symbolEncodeSize = AmqpCodec.GetSymbolEncodeSize(this.Condition);
			symbolEncodeSize = symbolEncodeSize + AmqpCodec.GetStringEncodeSize(this.Description);
			symbolEncodeSize = symbolEncodeSize + AmqpCodec.GetMapEncodeSize(this.Info);
			return symbolEncodeSize;
		}

		void System.Runtime.Serialization.ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("Condition", this.Condition.Value);
			info.AddValue("Description", this.Description);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder("error(");
			int num = 0;
			AmqpSymbol condition = this.Condition;
			base.AddFieldToString(condition.Value != null, stringBuilder, "condition", this.Condition, ref num);
			base.AddFieldToString(this.Description != null, stringBuilder, "description", this.Description, ref num);
			base.AddFieldToString(this.Info != null, stringBuilder, "info", this.Info, ref num);
			stringBuilder.Append(')');
			return stringBuilder.ToString();
		}
	}
}