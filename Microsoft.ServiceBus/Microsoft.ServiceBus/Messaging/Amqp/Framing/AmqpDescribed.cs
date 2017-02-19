using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal class AmqpDescribed : DescribedType, IAmqpSerializable
	{
		private AmqpSymbol name;

		private ulong code;

		public ulong DescriptorCode
		{
			get
			{
				return this.code;
			}
		}

		public AmqpSymbol DescriptorName
		{
			get
			{
				return this.name;
			}
		}

		public int EncodeSize
		{
			get
			{
				int encodeSize = 1 + ULongEncoding.GetEncodeSize(new ulong?(this.DescriptorCode));
				return encodeSize + this.GetValueEncodeSize();
			}
		}

		public long Length
		{
			get;
			set;
		}

		public long Offset
		{
			get;
			set;
		}

		public AmqpDescribed(AmqpSymbol name, ulong code) : base((name.Value == null ? code : name), null)
		{
			this.name = name;
			this.code = code;
		}

		protected void AddFieldToString(bool condition, StringBuilder sb, string fieldName, object value, ref int count)
		{
			if (condition)
			{
				if (count > 0)
				{
					sb.Append(',');
				}
				if (!(value is ArraySegment<byte>))
				{
					CultureInfo invariantCulture = CultureInfo.InvariantCulture;
					object[] objArray = new object[] { fieldName, value };
					sb.AppendFormat(invariantCulture, "{0}:{1}", objArray);
				}
				else
				{
					sb.Append(fieldName);
					sb.Append(':');
					ArraySegment<byte> nums = (ArraySegment<byte>)value;
					int num = Math.Min(nums.Count, 64);
					for (int i = 0; i < num; i++)
					{
						CultureInfo cultureInfo = CultureInfo.InvariantCulture;
						object[] array = new object[] { nums.Array[nums.Offset + i] };
						sb.AppendFormat(cultureInfo, "{0:X2}", array);
					}
				}
				count = count + 1;
			}
		}

		public void Decode(ByteBuffer buffer)
		{
			AmqpDescribed.DecodeDescriptor(buffer, out this.name, out this.code);
			this.DecodeValue(buffer);
		}

		public static void DecodeDescriptor(ByteBuffer buffer, out AmqpSymbol name, out ulong code)
		{
			name = new AmqpSymbol();
			code = (ulong)0;
			FormatCode formatCode = AmqpEncoding.ReadFormatCode(buffer);
			if (formatCode == 0)
			{
				formatCode = AmqpEncoding.ReadFormatCode(buffer);
			}
			if (formatCode == 163 || formatCode == 179)
			{
				name = SymbolEncoding.Decode(buffer, formatCode);
				return;
			}
			if (!(formatCode == 128) && !(formatCode == 68) && !(formatCode == 83))
			{
				throw AmqpEncoding.GetEncodingException(SRAmqp.AmqpInvalidFormatCode(formatCode, buffer.Offset));
			}
			code = ULongEncoding.Decode(buffer, formatCode).Value;
		}

		public virtual void DecodeValue(ByteBuffer buffer)
		{
			base.Value = AmqpEncoding.DecodeObject(buffer);
		}

		public void Encode(ByteBuffer buffer)
		{
			AmqpBitConverter.WriteUByte(buffer, 0);
			ULongEncoding.Encode(new ulong?(this.DescriptorCode), buffer);
			this.EncodeValue(buffer);
		}

		public virtual void EncodeValue(ByteBuffer buffer)
		{
			AmqpEncoding.EncodeObject(base.Value, buffer);
		}

		public virtual int GetValueEncodeSize()
		{
			return AmqpEncoding.GetObjectEncodeSize(base.Value);
		}

		public override string ToString()
		{
			return this.name.Value;
		}
	}
}