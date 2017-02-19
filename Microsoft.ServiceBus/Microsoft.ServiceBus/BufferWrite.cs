using System;

namespace Microsoft.ServiceBus
{
	internal delegate void BufferWrite(byte[] buffer, int offset, int count);
}