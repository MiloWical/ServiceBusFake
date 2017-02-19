using Microsoft.ServiceBus.Messaging.Amqp;
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class AmqpSqlFilter : AmqpFilter
	{
		private const int Fields = 2;

		public readonly static string Name;

		public readonly static ulong Code;

		public int? CompatibilityLevel
		{
			get;
			set;
		}

		public string Expression
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

		static AmqpSqlFilter()
		{
			AmqpSqlFilter.Name = "com.microsoft:sql-filter:list";
			AmqpSqlFilter.Code = 83483426822L;
		}

		public AmqpSqlFilter() : base(AmqpSqlFilter.Name, AmqpSqlFilter.Code)
		{
		}

		protected override void OnDecode(ByteBuffer buffer, int count)
		{
			int num = count;
			count = num - 1;
			if (num > 0)
			{
				this.Expression = AmqpCodec.DecodeString(buffer);
			}
			int num1 = count;
			count = num1 - 1;
			if (num1 > 0)
			{
				this.CompatibilityLevel = AmqpCodec.DecodeInt(buffer);
			}
		}

		protected override void OnEncode(ByteBuffer buffer)
		{
			AmqpCodec.EncodeString(this.Expression, buffer);
			AmqpCodec.EncodeInt(this.CompatibilityLevel, buffer);
		}

		protected override int OnValueSize()
		{
			int stringEncodeSize = 0;
			stringEncodeSize = AmqpCodec.GetStringEncodeSize(this.Expression);
			return stringEncodeSize + AmqpCodec.GetIntEncodeSize(this.CompatibilityLevel);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder("sql(");
			int num = 0;
			base.AddFieldToString(this.Expression != null, stringBuilder, "expression", this.Expression, ref num);
			int? compatibilityLevel = this.CompatibilityLevel;
			base.AddFieldToString(compatibilityLevel.HasValue, stringBuilder, "level", this.CompatibilityLevel, ref num);
			stringBuilder.Append(')');
			return stringBuilder.ToString();
		}
	}
}