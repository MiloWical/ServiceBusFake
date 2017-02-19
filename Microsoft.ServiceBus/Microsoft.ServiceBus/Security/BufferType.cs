using System;

namespace Microsoft.ServiceBus.Security
{
	internal enum BufferType
	{
		Empty = 0,
		Data = 1,
		Token = 2,
		Parameters = 3,
		Missing = 4,
		Extra = 5,
		Trailer = 6,
		Header = 7,
		Padding = 9,
		Stream = 10,
		ChannelBindings = 14
	}
}