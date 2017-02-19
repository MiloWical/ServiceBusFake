using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.ScaledEntity
{
	internal static class ScaledEntityPartitionResolver
	{
		private const int PartitionHighOrderByteIndexInLockToken = 14;

		private const int PartitionLowOrderByteIndexInLockToken = 15;

		public static string[] DefaultEventHubPartitionKeys
		{
			get;
			private set;
		}

		public static string[] DefaultPartitionKeys
		{
			get;
			private set;
		}

		public static void ComputeHash(byte[] data, uint seed1, uint seed2, out uint hash1, out uint hash2)
		{
			int i;
			uint length = (uint)((ulong)-559038737 + (long)((int)data.Length) + (ulong)seed1);
			uint num = length;
			uint num1 = length;
			uint num2 = length;
			num = num + seed2;
			int num3 = 0;
			for (i = (int)data.Length; i > 12; i = i - 12)
			{
				num2 = num2 + BitConverter.ToUInt32(data, num3);
				num1 = num1 + BitConverter.ToUInt32(data, num3 + 4);
				num = num + BitConverter.ToUInt32(data, num3 + 8);
				num2 = num2 - num;
				num2 = num2 ^ (num << 4 | num >> 28);
				num = num + num1;
				num1 = num1 - num2;
				num1 = num1 ^ (num2 << 6 | num2 >> 26);
				num2 = num2 + num;
				num = num - num1;
				num = num ^ (num1 << 8 | num1 >> 24);
				num1 = num1 + num2;
				num2 = num2 - num;
				num2 = num2 ^ (num << 16 | num >> 16);
				num = num + num1;
				num1 = num1 - num2;
				num1 = num1 ^ (num2 << 19 | num2 >> 13);
				num2 = num2 + num;
				num = num - num1;
				num = num ^ (num1 << 4 | num1 >> 28);
				num1 = num1 + num2;
				num3 = num3 + 12;
			}
			switch (i)
			{
				case 0:
				{
					hash1 = num;
					hash2 = num1;
					return;
				}
				case 1:
				{
					num2 = num2 + data[num3];
					break;
				}
				case 2:
				{
					num2 = num2 + (data[num3 + 1] << 8);
					goto case 1;
				}
				case 3:
				{
					num2 = num2 + (data[num3 + 2] << 16);
					goto case 2;
				}
				case 4:
				{
					num2 = num2 + BitConverter.ToUInt32(data, num3);
					break;
				}
				case 5:
				{
					num1 = num1 + data[num3 + 4];
					goto case 4;
				}
				case 6:
				{
					num1 = num1 + (data[num3 + 5] << 8);
					goto case 5;
				}
				case 7:
				{
					num1 = num1 + (data[num3 + 6] << 16);
					goto case 6;
				}
				case 8:
				{
					num1 = num1 + BitConverter.ToUInt32(data, num3 + 4);
					num2 = num2 + BitConverter.ToUInt32(data, num3);
					break;
				}
				case 9:
				{
					num = num + data[num3 + 8];
					goto case 8;
				}
				case 10:
				{
					num = num + (data[num3 + 9] << 8);
					goto case 9;
				}
				case 11:
				{
					num = num + (data[num3 + 10] << 16);
					goto case 10;
				}
				case 12:
				{
					num2 = num2 + BitConverter.ToUInt32(data, num3);
					num1 = num1 + BitConverter.ToUInt32(data, num3 + 4);
					num = num + BitConverter.ToUInt32(data, num3 + 8);
					break;
				}
			}
			num = num ^ num1;
			num = num - (num1 << 14 | num1 >> 18);
			num2 = num2 ^ num;
			num2 = num2 - (num << 11 | num >> 21);
			num1 = num1 ^ num2;
			num1 = num1 - (num2 << 25 | num2 >> 7);
			num = num ^ num1;
			num = num - (num1 << 16 | num1 >> 16);
			num2 = num2 ^ num;
			num2 = num2 - (num << 4 | num >> 28);
			num1 = num1 ^ num2;
			num1 = num1 - (num2 << 14 | num2 >> 18);
			num = num ^ num1;
			num = num - (num1 << 24 | num1 >> 8);
			hash1 = num;
			hash2 = num1;
		}

		public static string[] GeneratePartitionKeys(short partitionCount)
		{
			string[] strArrays = new string[partitionCount];
			int num = 0;
			int num1 = 0;
			while (num1 < partitionCount)
			{
				string str = num.ToString(CultureInfo.InvariantCulture);
				short entityLogicalPartition = ScaledEntityPartitionResolver.ResolveToEntityLogicalPartition(str, partitionCount);
				if (strArrays[entityLogicalPartition] == null)
				{
					strArrays[entityLogicalPartition] = str;
					num1++;
				}
				num++;
			}
			return strArrays;
		}

		public static void InitializeDefaultPartitionKeys(short partitionCount, short eventHubPartitionCount)
		{
			if (ScaledEntityPartitionResolver.DefaultPartitionKeys == null)
			{
				ScaledEntityPartitionResolver.DefaultPartitionKeys = ScaledEntityPartitionResolver.GeneratePartitionKeys(partitionCount);
			}
			if (ScaledEntityPartitionResolver.DefaultEventHubPartitionKeys == null)
			{
				ScaledEntityPartitionResolver.DefaultEventHubPartitionKeys = ScaledEntityPartitionResolver.GeneratePartitionKeys(eventHubPartitionCount);
			}
		}

		public static short LockTokenToEntityLogicalPartition(Guid lockToken)
		{
			byte[] byteArray = lockToken.ToByteArray();
			short num = (short)((byteArray[14] << 8) + byteArray[15]);
			return num;
		}

		public static bool MapsToSingleLogicalPartition(Guid[] lockTokens)
		{
			bool flag = true;
			if ((int)lockTokens.Length > 0)
			{
				short entityLogicalPartition = ScaledEntityPartitionResolver.LockTokenToEntityLogicalPartition(lockTokens[0]);
				int num = 1;
				while (num < (int)lockTokens.Length)
				{
					if (ScaledEntityPartitionResolver.LockTokenToEntityLogicalPartition(lockTokens[num]) == entityLogicalPartition)
					{
						num++;
					}
					else
					{
						flag = false;
						break;
					}
				}
			}
			return flag;
		}

		public static bool MapsToSingleLogicalPartition(long[] sequenceNumbers)
		{
			bool flag = true;
			if ((int)sequenceNumbers.Length > 0)
			{
				short entityLogicalPartition = ScaledEntityPartitionResolver.SequenceNumberToEntityLogicalPartition(sequenceNumbers[0]);
				int num = 1;
				while (num < (int)sequenceNumbers.Length)
				{
					if (ScaledEntityPartitionResolver.SequenceNumberToEntityLogicalPartition(sequenceNumbers[num]) == entityLogicalPartition)
					{
						num++;
					}
					else
					{
						flag = false;
						break;
					}
				}
			}
			return flag;
		}

		public static bool MapsToSingleLogicalPartition(string[] values, short partitionCount)
		{
			bool flag = true;
			if ((int)values.Length > 0)
			{
				short entityLogicalPartition = ScaledEntityPartitionResolver.ResolveToEntityLogicalPartition(values[0], partitionCount);
				int num = 1;
				while (num < (int)values.Length)
				{
					if (ScaledEntityPartitionResolver.ResolveToEntityLogicalPartition(values[num], partitionCount) == entityLogicalPartition)
					{
						num++;
					}
					else
					{
						flag = false;
						break;
					}
				}
			}
			return flag;
		}

		public static long RemovePartitionFromSequenceNumber(long stampedSequenceNumber)
		{
			return stampedSequenceNumber << 16 >> 16;
		}

		public static short ResolveToEntityLogicalPartition(string value, short entityPartitionCount)
		{
			uint num;
			uint num1;
			if (value == null)
			{
				return 0;
			}
			ScaledEntityPartitionResolver.ComputeHash(Encoding.ASCII.GetBytes(value.ToUpper(CultureInfo.InvariantCulture)), 0, 0, out num, out num1);
			long num2 = (long)(num ^ num1);
			return (short)Math.Abs(num2 % (long)entityPartitionCount);
		}

		public static short SequenceNumberToEntityLogicalPartition(long sequenceNumber)
		{
			return (short)(sequenceNumber >> 48);
		}

		public static Guid StampPartitionIntoLockToken(short partitionId, Guid lockToken)
		{
			byte num = (byte)(partitionId >> 8);
			byte num1 = (byte)partitionId;
			byte[] byteArray = lockToken.ToByteArray();
			byteArray[14] = num;
			byteArray[15] = num1;
			return new Guid(byteArray);
		}

		public static long StampPartitionIntoSequenceNumber(short partitionId, long sequenceNumber)
		{
			return ((long)partitionId << 48) + sequenceNumber;
		}
	}
}