using System;

namespace Microsoft.ServiceBus.Common
{
	internal class ByteUtility
	{
		public ByteUtility()
		{
		}

		public static bool EndsWithTwoAsciiNewLines(byte[] buffer, int length)
		{
			if (length < 4)
			{
				return false;
			}
			if (buffer[length - 4] != 13 || buffer[length - 3] != 10 || buffer[length - 2] != 13)
			{
				return false;
			}
			return buffer[length - 1] == 10;
		}

		public static int IndexOfAsciiChar(byte[] array, int offset, int count, char asciiChar)
		{
			for (int i = offset; i < offset + count; i++)
			{
				if (array[i] == asciiChar)
				{
					return i;
				}
			}
			return -1;
		}

		public static int IndexOfAsciiChars(byte[] array, int offset, int count, char asciiChar1, char asciiChar2)
		{
			for (int i = offset; i < offset + count - 1; i++)
			{
				if (array[i] == asciiChar1 && array[i + 1] == asciiChar2)
				{
					return i;
				}
			}
			return -1;
		}
	}
}