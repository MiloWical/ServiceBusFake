using System;

namespace Microsoft.ServiceBus.Channels
{
	internal class EncodedFault : EncodedFramingRecord
	{
		public EncodedFault(string fault) : base(FramingRecordType.Fault, fault)
		{
		}
	}
}