using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class Source : DescribedList
	{
		private const int Fields = 11;

		public readonly static string Name;

		public readonly static ulong Code;

		public Microsoft.ServiceBus.Messaging.Amqp.Framing.Address Address
		{
			get;
			set;
		}

		public Multiple<AmqpSymbol> Capabilities
		{
			get;
			set;
		}

		public Outcome DefaultOutcome
		{
			get;
			set;
		}

		public AmqpSymbol DistributionMode
		{
			get;
			set;
		}

		public uint? Durable
		{
			get;
			set;
		}

		public bool? Dynamic
		{
			get;
			set;
		}

		public Microsoft.ServiceBus.Messaging.Amqp.Framing.Fields DynamicNodeProperties
		{
			get;
			set;
		}

		public AmqpSymbol ExpiryPolicy
		{
			get;
			set;
		}

		protected override int FieldCount
		{
			get
			{
				return 11;
			}
		}

		public Microsoft.ServiceBus.Messaging.Amqp.Framing.FilterSet FilterSet
		{
			get;
			set;
		}

		public Multiple<AmqpSymbol> Outcomes
		{
			get;
			set;
		}

		public uint? Timeout
		{
			get;
			set;
		}

		static Source()
		{
			Source.Name = "amqp:source:list";
			Source.Code = (ulong)40;
		}

		public Source() : base(Source.Name, Source.Code)
		{
		}

		public Source(Uri uri) : this()
		{
			this.Address = uri.AbsoluteUri;
		}

		protected override void OnDecode(ByteBuffer buffer, int count)
		{
			int num = count;
			count = num - 1;
			if (num > 0)
			{
				this.Address = Microsoft.ServiceBus.Messaging.Amqp.Framing.Address.Decode(buffer);
			}
			int num1 = count;
			count = num1 - 1;
			if (num1 > 0)
			{
				this.Durable = AmqpCodec.DecodeUInt(buffer);
			}
			int num2 = count;
			count = num2 - 1;
			if (num2 > 0)
			{
				this.ExpiryPolicy = AmqpCodec.DecodeSymbol(buffer);
			}
			int num3 = count;
			count = num3 - 1;
			if (num3 > 0)
			{
				this.Timeout = AmqpCodec.DecodeUInt(buffer);
			}
			int num4 = count;
			count = num4 - 1;
			if (num4 > 0)
			{
				this.Dynamic = AmqpCodec.DecodeBoolean(buffer);
			}
			int num5 = count;
			count = num5 - 1;
			if (num5 > 0)
			{
				this.DynamicNodeProperties = AmqpCodec.DecodeMap<Microsoft.ServiceBus.Messaging.Amqp.Framing.Fields>(buffer);
			}
			int num6 = count;
			count = num6 - 1;
			if (num6 > 0)
			{
				this.DistributionMode = AmqpCodec.DecodeSymbol(buffer);
			}
			int num7 = count;
			count = num7 - 1;
			if (num7 > 0)
			{
				this.FilterSet = AmqpCodec.DecodeMap<Microsoft.ServiceBus.Messaging.Amqp.Framing.FilterSet>(buffer);
			}
			int num8 = count;
			count = num8 - 1;
			if (num8 > 0)
			{
				this.DefaultOutcome = (Outcome)AmqpCodec.DecodeAmqpDescribed(buffer);
			}
			int num9 = count;
			count = num9 - 1;
			if (num9 > 0)
			{
				this.Outcomes = AmqpCodec.DecodeMultiple<AmqpSymbol>(buffer);
			}
			int num10 = count;
			count = num10 - 1;
			if (num10 > 0)
			{
				this.Capabilities = AmqpCodec.DecodeMultiple<AmqpSymbol>(buffer);
			}
		}

		protected override void OnEncode(ByteBuffer buffer)
		{
			Microsoft.ServiceBus.Messaging.Amqp.Framing.Address.Encode(buffer, this.Address);
			AmqpCodec.EncodeUInt(this.Durable, buffer);
			AmqpCodec.EncodeSymbol(this.ExpiryPolicy, buffer);
			AmqpCodec.EncodeUInt(this.Timeout, buffer);
			AmqpCodec.EncodeBoolean(this.Dynamic, buffer);
			AmqpCodec.EncodeMap(this.DynamicNodeProperties, buffer);
			AmqpCodec.EncodeSymbol(this.DistributionMode, buffer);
			AmqpCodec.EncodeMap(this.FilterSet, buffer);
			AmqpCodec.EncodeSerializable(this.DefaultOutcome, buffer);
			AmqpCodec.EncodeMultiple<AmqpSymbol>(this.Outcomes, buffer);
			AmqpCodec.EncodeMultiple<AmqpSymbol>(this.Capabilities, buffer);
		}

		protected override int OnValueSize()
		{
			int encodeSize = 0 + Microsoft.ServiceBus.Messaging.Amqp.Framing.Address.GetEncodeSize(this.Address);
			encodeSize = encodeSize + AmqpCodec.GetUIntEncodeSize(this.Durable);
			encodeSize = encodeSize + AmqpCodec.GetSymbolEncodeSize(this.ExpiryPolicy);
			encodeSize = encodeSize + AmqpCodec.GetUIntEncodeSize(this.Timeout);
			encodeSize = encodeSize + AmqpCodec.GetBooleanEncodeSize(this.Dynamic);
			encodeSize = encodeSize + AmqpCodec.GetMapEncodeSize(this.DynamicNodeProperties);
			encodeSize = encodeSize + AmqpCodec.GetSymbolEncodeSize(this.DistributionMode);
			encodeSize = encodeSize + AmqpCodec.GetMapEncodeSize(this.FilterSet);
			encodeSize = encodeSize + AmqpCodec.GetSerializableEncodeSize(this.DefaultOutcome);
			encodeSize = encodeSize + AmqpCodec.GetMultipleEncodeSize<AmqpSymbol>(this.Outcomes);
			return encodeSize + AmqpCodec.GetMultipleEncodeSize<AmqpSymbol>(this.Capabilities);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder("source(");
			int num = 0;
			base.AddFieldToString(this.Address != null, stringBuilder, "address", this.Address, ref num);
			uint? durable = this.Durable;
			base.AddFieldToString(durable.HasValue, stringBuilder, "durable", this.Durable, ref num);
			AmqpSymbol expiryPolicy = this.ExpiryPolicy;
			base.AddFieldToString(expiryPolicy.Value != null, stringBuilder, "expiry-policy", this.ExpiryPolicy, ref num);
			uint? timeout = this.Timeout;
			base.AddFieldToString(timeout.HasValue, stringBuilder, "timeout", this.Timeout, ref num);
			bool? dynamic = this.Dynamic;
			base.AddFieldToString(dynamic.HasValue, stringBuilder, "dynamic", this.Dynamic, ref num);
			base.AddFieldToString(this.DynamicNodeProperties != null, stringBuilder, "dynamic-node-properties", this.DynamicNodeProperties, ref num);
			AmqpSymbol distributionMode = this.DistributionMode;
			base.AddFieldToString(distributionMode.Value != null, stringBuilder, "distribution-mode", this.DistributionMode, ref num);
			base.AddFieldToString(this.FilterSet != null, stringBuilder, "filter", this.FilterSet, ref num);
			base.AddFieldToString(this.DefaultOutcome != null, stringBuilder, "default-outcome", this.DefaultOutcome, ref num);
			base.AddFieldToString(this.Outcomes != null, stringBuilder, "outcomes", this.Outcomes, ref num);
			base.AddFieldToString(this.Capabilities != null, stringBuilder, "capabilities", this.Capabilities, ref num);
			stringBuilder.Append(')');
			return stringBuilder.ToString();
		}
	}
}