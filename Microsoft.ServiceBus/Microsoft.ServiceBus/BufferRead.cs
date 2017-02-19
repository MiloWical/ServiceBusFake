using System;

namespace Microsoft.ServiceBus
{
	internal delegate int BufferRead(byte[] buffer, int offset, int count);
}