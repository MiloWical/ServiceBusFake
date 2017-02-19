using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
	internal static class FixedWidth
	{
		public const int FormatCode = 1;

		public const int Null = 0;

		public const int Boolean = 0;

		public const int BooleanVar = 1;

		public const int Zero = 0;

		public const int UByte = 1;

		public const int UShort = 2;

		public const int UInt = 4;

		public const int ULong = 8;

		public const int Byte = 1;

		public const int Short = 2;

		public const int Int = 4;

		public const int Long = 8;

		public const int Float = 4;

		public const int Double = 8;

		public const int Decimal32 = 4;

		public const int Decimal64 = 8;

		public const int Decimal128 = 16;

		public const int Char = 4;

		public const int TimeStamp = 8;

		public const int Uuid = 16;

		public const int NullEncoded = 1;

		public const int BooleanEncoded = 1;

		public const int BooleanVarEncoded = 2;

		public const int ZeroEncoded = 1;

		public const int UByteEncoded = 2;

		public const int UShortEncoded = 3;

		public const int UIntEncoded = 5;

		public const int ULongEncoded = 9;

		public const int ByteEncoded = 2;

		public const int ShortEncoded = 3;

		public const int IntEncoded = 5;

		public const int LongEncoded = 9;

		public const int FloatEncoded = 5;

		public const int DoubleEncoded = 9;

		public const int Decimal32Encoded = 5;

		public const int Decimal64Encoded = 9;

		public const int Decimal128Encoded = 17;

		public const int CharEncoded = 5;

		public const int TimeStampEncoded = 9;

		public const int UuidEncoded = 17;
	}
}