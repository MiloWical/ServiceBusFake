using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal abstract class DescribedList : AmqpDescribed
	{
		protected abstract int FieldCount
		{
			get;
		}

		public DescribedList(AmqpSymbol name, ulong code) : base(name, code)
		{
		}

		public override void DecodeValue(ByteBuffer buffer)
		{
			FormatCode formatCode = AmqpEncoding.ReadFormatCode(buffer);
			if (formatCode == 69)
			{
				return;
			}
			int num = 0;
			int num1 = 0;
			AmqpEncoding.ReadSizeAndCount(buffer, formatCode, 192, 208, out num, out num1);
			int offset = buffer.Offset;
			this.DecodeValue(buffer, num, num1);
			if (num1 - this.FieldCount > 0)
			{
				buffer.Complete(num - (buffer.Offset - offset) - (formatCode == 192 ? 1 : 4));
			}
		}

		public void DecodeValue(ByteBuffer buffer, int size, int count)
		{
			if (count > 0)
			{
				this.OnDecode(buffer, count);
				this.EnsureRequired();
			}
		}

		public override void EncodeValue(ByteBuffer buffer)
		{
			int length;
			if (this.FieldCount == 0)
			{
				AmqpBitConverter.WriteUByte(buffer, 69);
				return;
			}
			int num = this.OnValueSize();
			int encodeWidthByCountAndSize = AmqpEncoding.GetEncodeWidthByCountAndSize(this.FieldCount, num);
			if (encodeWidthByCountAndSize != 1)
			{
				AmqpBitConverter.WriteUByte(buffer, 208);
				length = buffer.Length;
				buffer.Append(4);
				AmqpBitConverter.WriteUInt(buffer, (uint)this.FieldCount);
			}
			else
			{
				AmqpBitConverter.WriteUByte(buffer, 192);
				length = buffer.Length;
				buffer.Append(1);
				AmqpBitConverter.WriteUByte(buffer, (byte)this.FieldCount);
			}
			this.OnEncode(buffer);
			int length1 = buffer.Length - length - encodeWidthByCountAndSize;
			if (encodeWidthByCountAndSize != 1)
			{
				AmqpBitConverter.WriteUInt(buffer.Buffer, length, (uint)length1);
				return;
			}
			AmqpBitConverter.WriteUByte(buffer.Buffer, length, (byte)length1);
		}

		protected virtual void EnsureRequired()
		{
		}

		public override int GetValueEncodeSize()
		{
			if (this.FieldCount == 0)
			{
				return 1;
			}
			int num = this.OnValueSize();
			int encodeWidthByCountAndSize = AmqpEncoding.GetEncodeWidthByCountAndSize(this.FieldCount, num);
			return 1 + encodeWidthByCountAndSize + encodeWidthByCountAndSize + num;
		}

		protected abstract void OnDecode(ByteBuffer buffer, int count);

		protected abstract void OnEncode(ByteBuffer buffer);

		protected abstract int OnValueSize();
	}
}