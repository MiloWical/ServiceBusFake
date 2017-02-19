using System;

namespace Microsoft.ServiceBus.Security
{
	internal class SecurityBufferDescriptor
	{
		public readonly int Version;

		public readonly int Count;

		public unsafe void* UnmanagedPointer;

		public SecurityBufferDescriptor(int count)
		{
			unsafe
			{
				this.Version = 0;
				this.Count = count;
				this.UnmanagedPointer = (void*)0;
			}
		}
	}
}