using System;

namespace Microsoft.ServiceBus.Common
{
	internal static class HashCode
	{
		public static int CombineHashCodes(int h1, int h2)
		{
			return (h1 << 5) + h1 ^ h2;
		}

		public static int CombineHashCodes(int h1, int h2, int h3)
		{
			return HashCode.CombineHashCodes(HashCode.CombineHashCodes(h1, h2), h3);
		}

		public static int CombineHashCodes(int h1, int h2, int h3, int h4)
		{
			return HashCode.CombineHashCodes(HashCode.CombineHashCodes(h1, h2), HashCode.CombineHashCodes(h3, h4));
		}
	}
}