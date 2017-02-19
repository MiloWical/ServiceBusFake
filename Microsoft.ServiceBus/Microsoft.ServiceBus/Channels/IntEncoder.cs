using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;

namespace Microsoft.ServiceBus.Channels
{
	internal static class IntEncoder
	{
		public const int MaxEncodedSize = 5;

		public static int Encode(int value, byte[] bytes, int offset)
		{
			int num = 1;
			while (((long)value & (ulong)-128) != (long)0)
			{
				int num1 = offset;
				offset = num1 + 1;
				bytes[num1] = (byte)(value & 127 | 128);
				num++;
				value = value >> 7;
			}
			bytes[offset] = (byte)value;
			return num;
		}

		public static int GetEncodedSize(int value)
		{
			if (value < 0)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value", (object)value, Microsoft.ServiceBus.SR.GetString(Resources.ValueMustBeNonNegative, new object[0])));
			}
			int num = 1;
			while (((long)value & (ulong)-128) != (long)0)
			{
				num++;
				value = value >> 7;
			}
			return num;
		}
	}
}