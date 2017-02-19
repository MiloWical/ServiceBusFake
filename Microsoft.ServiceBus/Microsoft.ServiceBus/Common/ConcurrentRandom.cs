using System;

namespace Microsoft.ServiceBus.Common
{
	internal static class ConcurrentRandom
	{
		private readonly static Random seedGenerator;

		[ThreadStatic]
		private static Random threadLocalRandom;

		static ConcurrentRandom()
		{
			ConcurrentRandom.seedGenerator = new Random();
		}

		private static Random GetThreadLocalRandom()
		{
			int num;
			if (ConcurrentRandom.threadLocalRandom == null)
			{
				lock (ConcurrentRandom.seedGenerator)
				{
					num = ConcurrentRandom.seedGenerator.Next();
				}
				ConcurrentRandom.threadLocalRandom = new Random(num);
			}
			return ConcurrentRandom.threadLocalRandom;
		}

		public static int Next(int minValue, int maxValue)
		{
			return ConcurrentRandom.GetThreadLocalRandom().Next(minValue, maxValue);
		}

		public static long NextPositiveLong()
		{
			byte[] numArray = new byte[8];
			ConcurrentRandom.GetThreadLocalRandom().NextBytes(numArray);
			return Math.Abs((long)BitConverter.ToUInt64(numArray, 0));
		}
	}
}