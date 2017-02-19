using System;

namespace Microsoft.ServiceBus.Security
{
	internal static class IntPtrHelper
	{
		internal static IntPtr Add(IntPtr a, int b)
		{
			return (IntPtr)((long)a + (long)b);
		}
	}
}