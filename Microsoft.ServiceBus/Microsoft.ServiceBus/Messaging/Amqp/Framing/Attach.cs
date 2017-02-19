using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal class Attach : LinkPerformative
	{
		private const int Fields = 14;

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
				return 14;
			}
		}

		public bool? IncompleteUnsettled
		{
			get;
			set;
		}

		public uint? InitialDeliveryCount
		{
			get;
			set;
		}

		public string LinkName
		{
			get;
			set;
		}

		public ulong? MaxMessageSize
		{
			get;
			set;
		}

		public Multiple<AmqpSymbol> OfferedCapabilities
		{
			get;
			set;
		}

		public Microsoft.ServiceBus.Messaging.Amqp.Framing.Fields Properties
		{
			get;
			set;
		}

		public byte? RcvSettleMode
		{
			get;
			set;
		}

		public bool? Role
		{
			get;
			set;
		}

		public byte? SndSettleMode
		{
			get;
			set;
		}

		public object Source
		{
			get;
			set;
		}

		public object Target
		{
			get;
			set;
		}

		public AmqpMap Unsettled
		{
			get;
			set;
		}

		static Attach()
		{
			Attach.Name = "amqp:attach:list";
			Attach.Code = (ulong)18;
		}

		public Attach() : base(Attach.Name, Attach.Code)
		{
		}

		protected override void EnsureRequired()
		{
			if (this.LinkName == null)
			{
				throw AmqpEncoding.GetEncodingException(SRAmqp.AmqpRequiredFieldNotSet("name", Attach.Name));
			}
			if (!base.Handle.HasValue)
			{
				throw AmqpEncoding.GetEncodingException(SRAmqp.AmqpRequiredFieldNotSet("handle", Attach.Name));
			}
			if (!this.Role.HasValue)
			{
				throw AmqpEncoding.GetEncodingException(SRAmqp.AmqpRequiredFieldNotSet("role", Attach.Name));
			}
		}

		protected override void OnDecode(ByteBuffer buffer, int count)
		{
			int num = count;
			count = num - 1;
			if (num > 0)
			{
				this.LinkName = AmqpCodec.DecodeString(buffer);
			}
			int num1 = count;
			count = num1 - 1;
			if (num1 > 0)
			{
				base.Handle = AmqpCodec.DecodeUInt(buffer);
			}
			int num2 = count;
			count = num2 - 1;
			if (num2 > 0)
			{
				this.Role = AmqpCodec.DecodeBoolean(buffer);
			}
			int num3 = count;
			count = num3 - 1;
			if (num3 > 0)
			{
				this.SndSettleMode = AmqpCodec.DecodeUByte(buffer);
			}
			int num4 = count;
			count = num4 - 1;
			if (num4 > 0)
			{
				this.RcvSettleMode = AmqpCodec.DecodeUByte(buffer);
			}
			int num5 = count;
			count = num5 - 1;
			if (num5 > 0)
			{
				this.Source = AmqpCodec.DecodeObject(buffer);
			}
			int num6 = count;
			count = num6 - 1;
			if (num6 > 0)
			{
				this.Target = AmqpCodec.DecodeObject(buffer);
			}
			int num7 = count;
			count = num7 - 1;
			if (num7 > 0)
			{
				this.Unsettled = AmqpCodec.DecodeMap(buffer);
			}
			int num8 = count;
			count = num8 - 1;
			if (num8 > 0)
			{
				this.IncompleteUnsettled = AmqpCodec.DecodeBoolean(buffer);
			}
			int num9 = count;
			count = num9 - 1;
			if (num9 > 0)
			{
				this.InitialDeliveryCount = AmqpCodec.DecodeUInt(buffer);
			}
			int num10 = count;
			count = num10 - 1;
			if (num10 > 0)
			{
				this.MaxMessageSize = AmqpCodec.DecodeULong(buffer);
			}
			int num11 = count;
			count = num11 - 1;
			if (num11 > 0)
			{
				this.OfferedCapabilities = AmqpCodec.DecodeMultiple<AmqpSymbol>(buffer);
			}
			int num12 = count;
			count = num12 - 1;
			if (num12 > 0)
			{
				this.DesiredCapabilities = AmqpCodec.DecodeMultiple<AmqpSymbol>(buffer);
			}
			int num13 = count;
			count = num13 - 1;
			if (num13 > 0)
			{
				this.Properties = AmqpCodec.DecodeMap<Microsoft.ServiceBus.Messaging.Amqp.Framing.Fields>(buffer);
			}
		}

		protected override void OnEncode(ByteBuffer buffer)
		{
			AmqpCodec.EncodeString(this.LinkName, buffer);
			AmqpCodec.EncodeUInt(base.Handle, buffer);
			AmqpCodec.EncodeBoolean(this.Role, buffer);
			AmqpCodec.EncodeUByte(this.SndSettleMode, buffer);
			AmqpCodec.EncodeUByte(this.RcvSettleMode, buffer);
			AmqpCodec.EncodeObject(this.Source, buffer);
			AmqpCodec.EncodeObject(this.Target, buffer);
			AmqpCodec.EncodeMap(this.Unsettled, buffer);
			AmqpCodec.EncodeBoolean(this.IncompleteUnsettled, buffer);
			AmqpCodec.EncodeUInt(this.InitialDeliveryCount, buffer);
			AmqpCodec.EncodeULong(this.MaxMessageSize, buffer);
			AmqpCodec.EncodeMultiple<AmqpSymbol>(this.OfferedCapabilities, buffer);
			AmqpCodec.EncodeMultiple<AmqpSymbol>(this.DesiredCapabilities, buffer);
			AmqpCodec.EncodeMap(this.Properties, buffer);
		}

		protected override int OnValueSize()
		{
			int stringEncodeSize = 0 + AmqpCodec.GetStringEncodeSize(this.LinkName);
			stringEncodeSize = stringEncodeSize + AmqpCodec.GetUIntEncodeSize(base.Handle);
			stringEncodeSize = stringEncodeSize + AmqpCodec.GetBooleanEncodeSize(this.Role);
			stringEncodeSize = stringEncodeSize + AmqpCodec.GetUByteEncodeSize(this.SndSettleMode);
			stringEncodeSize = stringEncodeSize + AmqpCodec.GetUByteEncodeSize(this.RcvSettleMode);
			stringEncodeSize = stringEncodeSize + AmqpCodec.GetObjectEncodeSize(this.Source);
			stringEncodeSize = stringEncodeSize + AmqpCodec.GetObjectEncodeSize(this.Target);
			stringEncodeSize = stringEncodeSize + AmqpCodec.GetMapEncodeSize(this.Unsettled);
			stringEncodeSize = stringEncodeSize + AmqpCodec.GetBooleanEncodeSize(this.IncompleteUnsettled);
			stringEncodeSize = stringEncodeSize + AmqpCodec.GetUIntEncodeSize(this.InitialDeliveryCount);
			stringEncodeSize = stringEncodeSize + AmqpCodec.GetULongEncodeSize(this.MaxMessageSize);
			stringEncodeSize = stringEncodeSize + AmqpCodec.GetMultipleEncodeSize<AmqpSymbol>(this.OfferedCapabilities);
			stringEncodeSize = stringEncodeSize + AmqpCodec.GetMultipleEncodeSize<AmqpSymbol>(this.DesiredCapabilities);
			stringEncodeSize = stringEncodeSize + AmqpCodec.GetMapEncodeSize(this.Properties);
			return stringEncodeSize;
		}

		public override string ToString()
		{
			int? nullable;
			int? nullable1;
			StringBuilder stringBuilder = new StringBuilder("attach(");
			int num = 0;
			base.AddFieldToString(this.LinkName != null, stringBuilder, "name", this.LinkName, ref num);
			uint? handle = base.Handle;
			base.AddFieldToString(handle.HasValue, stringBuilder, "handle", base.Handle, ref num);
			bool? role = this.Role;
			base.AddFieldToString(role.HasValue, stringBuilder, "role", this.Role, ref num);
			byte? sndSettleMode = this.SndSettleMode;
			if (sndSettleMode.HasValue)
			{
				nullable = new int?((int)sndSettleMode.GetValueOrDefault());
			}
			else
			{
				nullable = null;
			}
			int? nullable2 = nullable;
			base.AddFieldToString(nullable2.HasValue, stringBuilder, "snd-settle-mode", this.SndSettleMode, ref num);
			byte? rcvSettleMode = this.RcvSettleMode;
			if (rcvSettleMode.HasValue)
			{
				nullable1 = new int?((int)rcvSettleMode.GetValueOrDefault());
			}
			else
			{
				nullable1 = null;
			}
			int? nullable3 = nullable1;
			base.AddFieldToString(nullable3.HasValue, stringBuilder, "rcv-settle-mode", this.RcvSettleMode, ref num);
			base.AddFieldToString(this.Source != null, stringBuilder, "source", this.Source, ref num);
			base.AddFieldToString(this.Target != null, stringBuilder, "target", this.Target, ref num);
			bool? incompleteUnsettled = this.IncompleteUnsettled;
			base.AddFieldToString(incompleteUnsettled.HasValue, stringBuilder, "incomplete-unsettled", this.IncompleteUnsettled, ref num);
			uint? initialDeliveryCount = this.InitialDeliveryCount;
			base.AddFieldToString(initialDeliveryCount.HasValue, stringBuilder, "initial-delivery-count", this.InitialDeliveryCount, ref num);
			ulong? maxMessageSize = this.MaxMessageSize;
			base.AddFieldToString(maxMessageSize.HasValue, stringBuilder, "max-message-size", this.MaxMessageSize, ref num);
			base.AddFieldToString(this.OfferedCapabilities != null, stringBuilder, "offered-capabilities", this.OfferedCapabilities, ref num);
			base.AddFieldToString(this.DesiredCapabilities != null, stringBuilder, "desired-capabilities", this.DesiredCapabilities, ref num);
			base.AddFieldToString(this.Properties != null, stringBuilder, "properties", this.Properties, ref num);
			stringBuilder.Append(')');
			return stringBuilder.ToString();
		}
	}
}