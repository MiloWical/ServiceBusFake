using Microsoft.ServiceBus.Messaging.Amqp;
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class Flow : LinkPerformative
	{
		private const int Fields = 11;

		public readonly static string Name;

		public readonly static ulong Code;

		public uint? Available
		{
			get;
			set;
		}

		public uint? DeliveryCount
		{
			get;
			set;
		}

		public bool? Drain
		{
			get;
			set;
		}

		public bool? Echo
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

		public uint? IncomingWindow
		{
			get;
			set;
		}

		public uint? LinkCredit
		{
			get;
			set;
		}

		public uint? NextIncomingId
		{
			get;
			set;
		}

		public uint? NextOutgoingId
		{
			get;
			set;
		}

		public uint? OutgoingWindow
		{
			get;
			set;
		}

		public Microsoft.ServiceBus.Messaging.Amqp.Framing.Fields Properties
		{
			get;
			set;
		}

		static Flow()
		{
			Flow.Name = "amqp:flow:list";
			Flow.Code = (ulong)19;
		}

		public Flow() : base(Flow.Name, Flow.Code)
		{
		}

		protected override void EnsureRequired()
		{
			if (!this.IncomingWindow.HasValue)
			{
				throw new AmqpException(AmqpError.InvalidField, "flow.incoming-window");
			}
			if (!this.NextOutgoingId.HasValue)
			{
				throw new AmqpException(AmqpError.InvalidField, "flow.next-outgoing-id");
			}
			if (!this.OutgoingWindow.HasValue)
			{
				throw new AmqpException(AmqpError.InvalidField, "flow.outgoing-window");
			}
		}

		protected override void OnDecode(ByteBuffer buffer, int count)
		{
			int num = count;
			count = num - 1;
			if (num > 0)
			{
				this.NextIncomingId = AmqpCodec.DecodeUInt(buffer);
			}
			int num1 = count;
			count = num1 - 1;
			if (num1 > 0)
			{
				this.IncomingWindow = AmqpCodec.DecodeUInt(buffer);
			}
			int num2 = count;
			count = num2 - 1;
			if (num2 > 0)
			{
				this.NextOutgoingId = AmqpCodec.DecodeUInt(buffer);
			}
			int num3 = count;
			count = num3 - 1;
			if (num3 > 0)
			{
				this.OutgoingWindow = AmqpCodec.DecodeUInt(buffer);
			}
			int num4 = count;
			count = num4 - 1;
			if (num4 > 0)
			{
				base.Handle = AmqpCodec.DecodeUInt(buffer);
			}
			int num5 = count;
			count = num5 - 1;
			if (num5 > 0)
			{
				this.DeliveryCount = AmqpCodec.DecodeUInt(buffer);
			}
			int num6 = count;
			count = num6 - 1;
			if (num6 > 0)
			{
				this.LinkCredit = AmqpCodec.DecodeUInt(buffer);
			}
			int num7 = count;
			count = num7 - 1;
			if (num7 > 0)
			{
				this.Available = AmqpCodec.DecodeUInt(buffer);
			}
			int num8 = count;
			count = num8 - 1;
			if (num8 > 0)
			{
				this.Drain = AmqpCodec.DecodeBoolean(buffer);
			}
			int num9 = count;
			count = num9 - 1;
			if (num9 > 0)
			{
				this.Echo = AmqpCodec.DecodeBoolean(buffer);
			}
			int num10 = count;
			count = num10 - 1;
			if (num10 > 0)
			{
				this.Properties = AmqpCodec.DecodeMap<Microsoft.ServiceBus.Messaging.Amqp.Framing.Fields>(buffer);
			}
		}

		protected override void OnEncode(ByteBuffer buffer)
		{
			AmqpCodec.EncodeUInt(this.NextIncomingId, buffer);
			AmqpCodec.EncodeUInt(this.IncomingWindow, buffer);
			AmqpCodec.EncodeUInt(this.NextOutgoingId, buffer);
			AmqpCodec.EncodeUInt(this.OutgoingWindow, buffer);
			AmqpCodec.EncodeUInt(base.Handle, buffer);
			AmqpCodec.EncodeUInt(this.DeliveryCount, buffer);
			AmqpCodec.EncodeUInt(this.LinkCredit, buffer);
			AmqpCodec.EncodeUInt(this.Available, buffer);
			AmqpCodec.EncodeBoolean(this.Drain, buffer);
			AmqpCodec.EncodeBoolean(this.Echo, buffer);
			AmqpCodec.EncodeMap(this.Properties, buffer);
		}

		protected override int OnValueSize()
		{
			int uIntEncodeSize = 0 + AmqpCodec.GetUIntEncodeSize(this.NextIncomingId);
			uIntEncodeSize = uIntEncodeSize + AmqpCodec.GetUIntEncodeSize(this.IncomingWindow);
			uIntEncodeSize = uIntEncodeSize + AmqpCodec.GetUIntEncodeSize(this.NextOutgoingId);
			uIntEncodeSize = uIntEncodeSize + AmqpCodec.GetUIntEncodeSize(this.OutgoingWindow);
			uIntEncodeSize = uIntEncodeSize + AmqpCodec.GetUIntEncodeSize(base.Handle);
			uIntEncodeSize = uIntEncodeSize + AmqpCodec.GetUIntEncodeSize(this.DeliveryCount);
			uIntEncodeSize = uIntEncodeSize + AmqpCodec.GetUIntEncodeSize(this.LinkCredit);
			uIntEncodeSize = uIntEncodeSize + AmqpCodec.GetUIntEncodeSize(this.Available);
			uIntEncodeSize = uIntEncodeSize + AmqpCodec.GetBooleanEncodeSize(this.Drain);
			uIntEncodeSize = uIntEncodeSize + AmqpCodec.GetBooleanEncodeSize(this.Echo);
			uIntEncodeSize = uIntEncodeSize + AmqpCodec.GetMapEncodeSize(this.Properties);
			return uIntEncodeSize;
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder("flow(");
			int num = 0;
			uint? nextIncomingId = this.NextIncomingId;
			base.AddFieldToString(nextIncomingId.HasValue, stringBuilder, "next-in-id", this.NextIncomingId, ref num);
			uint? incomingWindow = this.IncomingWindow;
			base.AddFieldToString(incomingWindow.HasValue, stringBuilder, "in-window", this.IncomingWindow, ref num);
			uint? nextOutgoingId = this.NextOutgoingId;
			base.AddFieldToString(nextOutgoingId.HasValue, stringBuilder, "next-out-id", this.NextOutgoingId, ref num);
			uint? outgoingWindow = this.OutgoingWindow;
			base.AddFieldToString(outgoingWindow.HasValue, stringBuilder, "out-window", this.OutgoingWindow, ref num);
			uint? handle = base.Handle;
			base.AddFieldToString(handle.HasValue, stringBuilder, "handle", base.Handle, ref num);
			uint? linkCredit = this.LinkCredit;
			base.AddFieldToString(linkCredit.HasValue, stringBuilder, "link-credit", this.LinkCredit, ref num);
			uint? deliveryCount = this.DeliveryCount;
			base.AddFieldToString(deliveryCount.HasValue, stringBuilder, "delivery-count", this.DeliveryCount, ref num);
			uint? available = this.Available;
			base.AddFieldToString(available.HasValue, stringBuilder, "available", this.Available, ref num);
			bool? drain = this.Drain;
			base.AddFieldToString(drain.HasValue, stringBuilder, "drain", this.Drain, ref num);
			bool? echo = this.Echo;
			base.AddFieldToString(echo.HasValue, stringBuilder, "echo", this.Echo, ref num);
			base.AddFieldToString(this.Properties != null, stringBuilder, "properties", this.Properties, ref num);
			stringBuilder.Append(')');
			return stringBuilder.ToString();
		}
	}
}