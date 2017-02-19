using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal class Open : Performative
	{
		private const int Fields = 10;

		public readonly static string Name;

		public readonly static ulong Code;

		public ushort? ChannelMax
		{
			get;
			set;
		}

		public string ContainerId
		{
			get;
			set;
		}

		public Multiple<AmqpSymbol> DesiredCapabilities
		{
			get;
			set;
		}

		protected override int FieldCount
		{
			get
			{
				return 10;
			}
		}

		public string HostName
		{
			get;
			set;
		}

		public uint? IdleTimeOut
		{
			get;
			set;
		}

		public Multiple<AmqpSymbol> IncomingLocales
		{
			get;
			set;
		}

		public uint? MaxFrameSize
		{
			get;
			set;
		}

		public Multiple<AmqpSymbol> OfferedCapabilities
		{
			get;
			set;
		}

		public Multiple<AmqpSymbol> OutgoingLocales
		{
			get;
			set;
		}

		public Microsoft.ServiceBus.Messaging.Amqp.Framing.Fields Properties
		{
			get;
			set;
		}

		static Open()
		{
			Open.Name = "amqp:open:list";
			Open.Code = (ulong)16;
		}

		public Open() : base(Open.Name, Open.Code)
		{
		}

		protected override void EnsureRequired()
		{
			if (this.ContainerId == null)
			{
				throw AmqpEncoding.GetEncodingException(SRAmqp.AmqpRequiredFieldNotSet("container-id", Open.Name));
			}
		}

		protected override void OnDecode(ByteBuffer buffer, int count)
		{
			int num = count;
			count = num - 1;
			if (num > 0)
			{
				this.ContainerId = AmqpCodec.DecodeString(buffer);
			}
			int num1 = count;
			count = num1 - 1;
			if (num1 > 0)
			{
				this.HostName = AmqpCodec.DecodeString(buffer);
			}
			int num2 = count;
			count = num2 - 1;
			if (num2 > 0)
			{
				this.MaxFrameSize = AmqpCodec.DecodeUInt(buffer);
			}
			int num3 = count;
			count = num3 - 1;
			if (num3 > 0)
			{
				this.ChannelMax = AmqpCodec.DecodeUShort(buffer);
			}
			int num4 = count;
			count = num4 - 1;
			if (num4 > 0)
			{
				this.IdleTimeOut = AmqpCodec.DecodeUInt(buffer);
			}
			int num5 = count;
			count = num5 - 1;
			if (num5 > 0)
			{
				this.OutgoingLocales = AmqpCodec.DecodeMultiple<AmqpSymbol>(buffer);
			}
			int num6 = count;
			count = num6 - 1;
			if (num6 > 0)
			{
				this.IncomingLocales = AmqpCodec.DecodeMultiple<AmqpSymbol>(buffer);
			}
			int num7 = count;
			count = num7 - 1;
			if (num7 > 0)
			{
				this.OfferedCapabilities = AmqpCodec.DecodeMultiple<AmqpSymbol>(buffer);
			}
			int num8 = count;
			count = num8 - 1;
			if (num8 > 0)
			{
				this.DesiredCapabilities = AmqpCodec.DecodeMultiple<AmqpSymbol>(buffer);
			}
			int num9 = count;
			count = num9 - 1;
			if (num9 > 0)
			{
				this.Properties = AmqpCodec.DecodeMap<Microsoft.ServiceBus.Messaging.Amqp.Framing.Fields>(buffer);
			}
		}

		protected override void OnEncode(ByteBuffer buffer)
		{
			AmqpCodec.EncodeString(this.ContainerId, buffer);
			AmqpCodec.EncodeString(this.HostName, buffer);
			AmqpCodec.EncodeUInt(this.MaxFrameSize, buffer);
			AmqpCodec.EncodeUShort(this.ChannelMax, buffer);
			AmqpCodec.EncodeUInt(this.IdleTimeOut, buffer);
			AmqpCodec.EncodeMultiple<AmqpSymbol>(this.OutgoingLocales, buffer);
			AmqpCodec.EncodeMultiple<AmqpSymbol>(this.IncomingLocales, buffer);
			AmqpCodec.EncodeMultiple<AmqpSymbol>(this.OfferedCapabilities, buffer);
			AmqpCodec.EncodeMultiple<AmqpSymbol>(this.DesiredCapabilities, buffer);
			AmqpCodec.EncodeMap(this.Properties, buffer);
		}

		protected override int OnValueSize()
		{
			int stringEncodeSize = 0 + AmqpCodec.GetStringEncodeSize(this.ContainerId);
			stringEncodeSize = stringEncodeSize + AmqpCodec.GetStringEncodeSize(this.HostName);
			stringEncodeSize = stringEncodeSize + AmqpCodec.GetUIntEncodeSize(this.MaxFrameSize);
			stringEncodeSize = stringEncodeSize + AmqpCodec.GetUShortEncodeSize(this.ChannelMax);
			stringEncodeSize = stringEncodeSize + AmqpCodec.GetUIntEncodeSize(this.IdleTimeOut);
			stringEncodeSize = stringEncodeSize + AmqpCodec.GetMultipleEncodeSize<AmqpSymbol>(this.OutgoingLocales);
			stringEncodeSize = stringEncodeSize + AmqpCodec.GetMultipleEncodeSize<AmqpSymbol>(this.IncomingLocales);
			stringEncodeSize = stringEncodeSize + AmqpCodec.GetMultipleEncodeSize<AmqpSymbol>(this.OfferedCapabilities);
			stringEncodeSize = stringEncodeSize + AmqpCodec.GetMultipleEncodeSize<AmqpSymbol>(this.DesiredCapabilities);
			stringEncodeSize = stringEncodeSize + AmqpCodec.GetMapEncodeSize(this.Properties);
			return stringEncodeSize;
		}

		public override string ToString()
		{
			int? nullable;
			StringBuilder stringBuilder = new StringBuilder("open(");
			int num = 0;
			base.AddFieldToString(this.ContainerId != null, stringBuilder, "container-id", this.ContainerId, ref num);
			base.AddFieldToString(this.HostName != null, stringBuilder, "host-name", this.HostName, ref num);
			uint? maxFrameSize = this.MaxFrameSize;
			base.AddFieldToString(maxFrameSize.HasValue, stringBuilder, "max-frame-size", this.MaxFrameSize, ref num);
			ushort? channelMax = this.ChannelMax;
			if (channelMax.HasValue)
			{
				nullable = new int?((int)channelMax.GetValueOrDefault());
			}
			else
			{
				nullable = null;
			}
			int? nullable1 = nullable;
			base.AddFieldToString(nullable1.HasValue, stringBuilder, "channel-max", this.ChannelMax, ref num);
			uint? idleTimeOut = this.IdleTimeOut;
			base.AddFieldToString(idleTimeOut.HasValue, stringBuilder, "idle-time-out", this.IdleTimeOut, ref num);
			base.AddFieldToString(this.OutgoingLocales != null, stringBuilder, "outgoing-locales", this.OutgoingLocales, ref num);
			base.AddFieldToString(this.IncomingLocales != null, stringBuilder, "incoming-locales", this.IncomingLocales, ref num);
			base.AddFieldToString(this.OfferedCapabilities != null, stringBuilder, "offered-capabilities", this.OfferedCapabilities, ref num);
			base.AddFieldToString(this.DesiredCapabilities != null, stringBuilder, "desired-capabilities", this.DesiredCapabilities, ref num);
			base.AddFieldToString(this.Properties != null, stringBuilder, "properties", this.Properties, ref num);
			stringBuilder.Append(')');
			return stringBuilder.ToString();
		}
	}
}