using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal class Begin : Performative
	{
		private const int Fields = 8;

		public readonly static string Name;

		public readonly static ulong Code;

		public Multiple<AmqpSymbol> DesiredCapabilities
		{
			get;
			set;
		}

		protected override int FieldCount
		{
			get
			{
				return 8;
			}
		}

		public uint? HandleMax
		{
			get;
			set;
		}

		public uint? IncomingWindow
		{
			get;
			set;
		}

		public uint? NextOutgoingId
		{
			get;
			set;
		}

		public Multiple<AmqpSymbol> OfferedCapabilities
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

		public ushort? RemoteChannel
		{
			get;
			set;
		}

		static Begin()
		{
			Begin.Name = "amqp:begin:list";
			Begin.Code = (ulong)17;
		}

		public Begin() : base(Begin.Name, Begin.Code)
		{
		}

		protected override void EnsureRequired()
		{
			if (!this.NextOutgoingId.HasValue)
			{
				throw new AmqpException(AmqpError.InvalidField, "begin.next-outgoing-id");
			}
			if (!this.IncomingWindow.HasValue)
			{
				throw new AmqpException(AmqpError.InvalidField, "begin.incoming-window");
			}
			if (!this.OutgoingWindow.HasValue)
			{
				throw new AmqpException(AmqpError.InvalidField, "begin.outgoing-window");
			}
		}

		protected override void OnDecode(ByteBuffer buffer, int count)
		{
			int num = count;
			count = num - 1;
			if (num > 0)
			{
				this.RemoteChannel = AmqpCodec.DecodeUShort(buffer);
			}
			int num1 = count;
			count = num1 - 1;
			if (num1 > 0)
			{
				this.NextOutgoingId = AmqpCodec.DecodeUInt(buffer);
			}
			int num2 = count;
			count = num2 - 1;
			if (num2 > 0)
			{
				this.IncomingWindow = AmqpCodec.DecodeUInt(buffer);
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
				this.HandleMax = AmqpCodec.DecodeUInt(buffer);
			}
			int num5 = count;
			count = num5 - 1;
			if (num5 > 0)
			{
				this.OfferedCapabilities = AmqpCodec.DecodeMultiple<AmqpSymbol>(buffer);
			}
			int num6 = count;
			count = num6 - 1;
			if (num6 > 0)
			{
				this.DesiredCapabilities = AmqpCodec.DecodeMultiple<AmqpSymbol>(buffer);
			}
			int num7 = count;
			count = num7 - 1;
			if (num7 > 0)
			{
				this.Properties = AmqpCodec.DecodeMap<Microsoft.ServiceBus.Messaging.Amqp.Framing.Fields>(buffer);
			}
		}

		protected override void OnEncode(ByteBuffer buffer)
		{
			AmqpCodec.EncodeUShort(this.RemoteChannel, buffer);
			AmqpCodec.EncodeUInt(this.NextOutgoingId, buffer);
			AmqpCodec.EncodeUInt(this.IncomingWindow, buffer);
			AmqpCodec.EncodeUInt(this.OutgoingWindow, buffer);
			AmqpCodec.EncodeUInt(this.HandleMax, buffer);
			AmqpCodec.EncodeMultiple<AmqpSymbol>(this.OfferedCapabilities, buffer);
			AmqpCodec.EncodeMultiple<AmqpSymbol>(this.DesiredCapabilities, buffer);
			AmqpCodec.EncodeMap(this.Properties, buffer);
		}

		protected override int OnValueSize()
		{
			int uShortEncodeSize = 0 + AmqpCodec.GetUShortEncodeSize(this.RemoteChannel);
			uShortEncodeSize = uShortEncodeSize + AmqpCodec.GetUIntEncodeSize(this.NextOutgoingId);
			uShortEncodeSize = uShortEncodeSize + AmqpCodec.GetUIntEncodeSize(this.IncomingWindow);
			uShortEncodeSize = uShortEncodeSize + AmqpCodec.GetUIntEncodeSize(this.OutgoingWindow);
			uShortEncodeSize = uShortEncodeSize + AmqpCodec.GetUIntEncodeSize(this.HandleMax);
			uShortEncodeSize = uShortEncodeSize + AmqpCodec.GetMultipleEncodeSize<AmqpSymbol>(this.OfferedCapabilities);
			uShortEncodeSize = uShortEncodeSize + AmqpCodec.GetMultipleEncodeSize<AmqpSymbol>(this.DesiredCapabilities);
			uShortEncodeSize = uShortEncodeSize + AmqpCodec.GetMapEncodeSize(this.Properties);
			return uShortEncodeSize;
		}

		public override string ToString()
		{
			int? nullable;
			StringBuilder stringBuilder = new StringBuilder("begin(");
			int num = 0;
			ushort? remoteChannel = this.RemoteChannel;
			if (remoteChannel.HasValue)
			{
				nullable = new int?((int)remoteChannel.GetValueOrDefault());
			}
			else
			{
				nullable = null;
			}
			int? nullable1 = nullable;
			base.AddFieldToString(nullable1.HasValue, stringBuilder, "remote-channel", this.RemoteChannel, ref num);
			uint? nextOutgoingId = this.NextOutgoingId;
			base.AddFieldToString(nextOutgoingId.HasValue, stringBuilder, "next-outgoing-id", this.NextOutgoingId, ref num);
			uint? incomingWindow = this.IncomingWindow;
			base.AddFieldToString(incomingWindow.HasValue, stringBuilder, "incoming-window", this.IncomingWindow, ref num);
			uint? outgoingWindow = this.OutgoingWindow;
			base.AddFieldToString(outgoingWindow.HasValue, stringBuilder, "outgoing-window", this.OutgoingWindow, ref num);
			uint? handleMax = this.HandleMax;
			base.AddFieldToString(handleMax.HasValue, stringBuilder, "handle-max", this.HandleMax, ref num);
			base.AddFieldToString(this.OfferedCapabilities != null, stringBuilder, "offered-capabilities", this.OfferedCapabilities, ref num);
			base.AddFieldToString(this.DesiredCapabilities != null, stringBuilder, "desired-capabilities", this.DesiredCapabilities, ref num);
			base.AddFieldToString(this.Properties != null, stringBuilder, "properties", this.Properties, ref num);
			stringBuilder.Append(')');
			return stringBuilder.ToString();
		}
	}
}