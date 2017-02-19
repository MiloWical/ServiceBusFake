using System;

namespace Microsoft.ServiceBus.Security
{
	internal struct SecurityBufferStruct
	{
		public int count;

		public BufferType type;

		public IntPtr token;

		public readonly static int Size;

		static SecurityBufferStruct()
		{
			SecurityBufferStruct.Size = (int)sizeof(SecurityBufferStruct);
		}
	}
}