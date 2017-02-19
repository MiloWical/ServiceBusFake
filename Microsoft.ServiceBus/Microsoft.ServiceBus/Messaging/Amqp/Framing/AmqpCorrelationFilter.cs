using Microsoft.ServiceBus.Messaging.Amqp;
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class AmqpCorrelationFilter : AmqpFilter
	{
		private const int Fields = 1;

		public readonly static string Name;

		public readonly static ulong Code;

		public string CorrelationId
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

		static AmqpCorrelationFilter()
		{
			AmqpCorrelationFilter.Name = "com.microsoft:correlation-filter:list";
			AmqpCorrelationFilter.Code = 83483426825L;
		}

		public AmqpCorrelationFilter() : base(AmqpCorrelationFilter.Name, AmqpCorrelationFilter.Code)
		{
		}

		protected override void OnDecode(ByteBuffer buffer, int count)
		{
			int num = count;
			count = num - 1;
			if (num > 0)
			{
				this.CorrelationId = AmqpCodec.DecodeString(buffer);
			}
		}

		protected override void OnEncode(ByteBuffer buffer)
		{
			AmqpCodec.EncodeString(this.CorrelationId, buffer);
		}

		protected override int OnValueSize()
		{
			return AmqpCodec.GetStringEncodeSize(this.CorrelationId);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder("correlation(");
			int num = 0;
			base.AddFieldToString(this.CorrelationId != null, stringBuilder, "id", this.CorrelationId, ref num);
			stringBuilder.Append(')');
			return stringBuilder.ToString();
		}
	}
}