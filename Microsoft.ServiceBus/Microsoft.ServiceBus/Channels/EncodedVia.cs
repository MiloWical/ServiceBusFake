using System;

namespace Microsoft.ServiceBus.Channels
{
	internal class EncodedVia : EncodedFramingRecord
	{
		public EncodedVia(string via) : base(FramingRecordType.Via, via)
		{
		}
	}
}