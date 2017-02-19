using System;

namespace Microsoft.ServiceBus.Messaging
{
	internal enum PropertyValueType
	{
		Null,
		Byte,
		SByte,
		Char,
		Int16,
		UInt16,
		Int32,
		UInt32,
		Int64,
		UInt64,
		Single,
		Double,
		Decimal,
		Boolean,
		Guid,
		String,
		Uri,
		DateTime,
		DateTimeOffset,
		TimeSpan,
		Stream,
		Unknown
	}
}