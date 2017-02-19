using System;

namespace Microsoft.ServiceBus.Channels
{
	internal class EncodedUpgrade : EncodedFramingRecord
	{
		public EncodedUpgrade(string contentType) : base(FramingRecordType.UpgradeRequest, contentType)
		{
		}
	}
}