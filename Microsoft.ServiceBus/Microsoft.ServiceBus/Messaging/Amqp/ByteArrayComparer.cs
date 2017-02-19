using System;
using System.Collections.Generic;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal sealed class ByteArrayComparer : IEqualityComparer<ArraySegment<byte>>
	{
		private static ByteArrayComparer instance;

		public static ByteArrayComparer Instance
		{
			get
			{
				return ByteArrayComparer.instance;
			}
		}

		static ByteArrayComparer()
		{
			ByteArrayComparer.instance = new ByteArrayComparer();
		}

		private ByteArrayComparer()
		{
		}

		public static bool AreEqual(ArraySegment<byte> x, ArraySegment<byte> y)
		{
			if (x.Array == null || y.Array == null)
			{
				if (x.Array != null)
				{
					return false;
				}
				return null == y.Array;
			}
			if (x.Count != y.Count)
			{
				return false;
			}
			for (int i = 0; i < x.Count; i++)
			{
				if (x.Array[i + x.Offset] != y.Array[i + y.Offset])
				{
					return false;
				}
			}
			return true;
		}

		public bool Equals(ArraySegment<byte> x, ArraySegment<byte> y)
		{
			return ByteArrayComparer.AreEqual(x, y);
		}

		public int GetHashCode(ArraySegment<byte> obj)
		{
			int count = obj.Count;
			for (int i = 0; i < obj.Count; i++)
			{
				count = (count << 4) - count ^ obj.Array[i + obj.Offset];
			}
			return count;
		}
	}
}