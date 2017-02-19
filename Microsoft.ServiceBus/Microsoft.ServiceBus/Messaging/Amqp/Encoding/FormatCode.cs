using System;
using System.Globalization;

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
	internal struct FormatCode
	{
		public const byte Described = 0;

		public const byte Null = 64;

		public const byte Boolean = 86;

		public const byte BooleanTrue = 65;

		public const byte BooleanFalse = 66;

		public const byte UInt0 = 67;

		public const byte ULong0 = 68;

		public const byte UByte = 80;

		public const byte UShort = 96;

		public const byte UInt = 112;

		public const byte ULong = 128;

		public const byte Byte = 81;

		public const byte Short = 97;

		public const byte Int = 113;

		public const byte Long = 129;

		public const byte SmallUInt = 82;

		public const byte SmallULong = 83;

		public const byte SmallInt = 84;

		public const byte SmallLong = 85;

		public const byte Float = 114;

		public const byte Double = 130;

		public const byte Decimal32 = 116;

		public const byte Decimal64 = 132;

		public const byte Decimal128 = 148;

		public const byte Char = 115;

		public const byte TimeStamp = 131;

		public const byte Uuid = 152;

		public const byte Binary8 = 160;

		public const byte Binary32 = 176;

		public const byte String8Utf8 = 161;

		public const byte String32Utf8 = 177;

		public const byte Symbol8 = 163;

		public const byte Symbol32 = 179;

		public const byte List0 = 69;

		public const byte List8 = 192;

		public const byte List32 = 208;

		public const byte Map8 = 193;

		public const byte Map32 = 209;

		public const byte Array8 = 224;

		public const byte Array32 = 240;

		private byte type;

		private byte extType;

		public byte ExtType
		{
			get
			{
				return this.extType;
			}
		}

		public byte SubCategory
		{
			get
			{
				return (byte)((this.type & 240) >> 4);
			}
		}

		public byte SubType
		{
			get
			{
				return (byte)(this.type & 15);
			}
		}

		public byte Type
		{
			get
			{
				return this.type;
			}
		}

		public FormatCode(byte type) : this(type, 0)
		{
		}

		public FormatCode(byte type, byte extType)
		{
			this.type = type;
			this.extType = extType;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is FormatCode))
			{
				return false;
			}
			return this == (FormatCode)obj;
		}

		public override int GetHashCode()
		{
			return this.type.GetHashCode();
		}

		public static bool HasExtType(byte type)
		{
			return (type & 15) == 15;
		}

		public bool HasExtType()
		{
			return (this.type & 15) == 15;
		}

		public static bool operator ==(FormatCode fc1, FormatCode fc2)
		{
			return fc1.Type == fc2.Type;
		}

		public static implicit operator FormatCode(byte value)
		{
			return new FormatCode(value);
		}

		public static implicit operator Byte(FormatCode value)
		{
			return value.Type;
		}

		public static bool operator !=(FormatCode fc1, FormatCode fc2)
		{
			return fc1.Type != fc2.Type;
		}

		public override string ToString()
		{
			if (!this.HasExtType())
			{
				CultureInfo invariantCulture = CultureInfo.InvariantCulture;
				object[] type = new object[] { this.Type };
				return string.Format(invariantCulture, "0x{0:X2}", type);
			}
			CultureInfo cultureInfo = CultureInfo.InvariantCulture;
			object[] objArray = new object[] { this.Type, this.ExtType };
			return string.Format(cultureInfo, "0x{0:X2}.{1:X2}", objArray);
		}
	}
}