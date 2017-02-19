using Microsoft.ServiceBus.Diagnostics;
using System;
using System.ComponentModel;
using System.Net.Security;

namespace Microsoft.ServiceBus
{
	internal static class ProtectionLevelHelper
	{
		internal static int GetOrdinal(ProtectionLevel? p)
		{
			if (!p.HasValue)
			{
				return 1;
			}
			switch (p.Value)
			{
				case ProtectionLevel.None:
				{
					return 2;
				}
				case ProtectionLevel.Sign:
				{
					return 3;
				}
				case ProtectionLevel.EncryptAndSign:
				{
					return 4;
				}
				default:
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidEnumArgumentException("p", (int)p.Value, typeof(ProtectionLevel)));
				}
			}
		}

		internal static bool IsDefined(ProtectionLevel value)
		{
			if (value == ProtectionLevel.None || value == ProtectionLevel.Sign)
			{
				return true;
			}
			return value == ProtectionLevel.EncryptAndSign;
		}

		internal static bool IsStronger(ProtectionLevel v1, ProtectionLevel v2)
		{
			if (v1 == ProtectionLevel.EncryptAndSign && v2 != ProtectionLevel.EncryptAndSign)
			{
				return true;
			}
			if (v1 != ProtectionLevel.Sign)
			{
				return false;
			}
			return v2 == ProtectionLevel.None;
		}

		internal static bool IsStrongerOrEqual(ProtectionLevel v1, ProtectionLevel v2)
		{
			if (v1 == ProtectionLevel.EncryptAndSign)
			{
				return true;
			}
			if (v1 != ProtectionLevel.Sign)
			{
				return false;
			}
			return v2 != ProtectionLevel.EncryptAndSign;
		}

		internal static ProtectionLevel Max(ProtectionLevel v1, ProtectionLevel v2)
		{
			if (!ProtectionLevelHelper.IsStronger(v1, v2))
			{
				return v2;
			}
			return v1;
		}

		internal static void Validate(ProtectionLevel value)
		{
			if (!ProtectionLevelHelper.IsDefined(value))
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidEnumArgumentException("value", (int)value, typeof(ProtectionLevel)));
			}
		}
	}
}