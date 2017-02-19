using Microsoft.ServiceBus.Diagnostics;
using System;
using System.ComponentModel;

namespace Microsoft.ServiceBus
{
	internal static class TokenScopeHelper
	{
		public static bool IsDefined(TokenScope v)
		{
			if (v == TokenScope.Entity)
			{
				return true;
			}
			return v == TokenScope.Namespace;
		}

		public static void Validate(TokenScope value)
		{
			if (!TokenScopeHelper.IsDefined(value))
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidEnumArgumentException("value", (int)value, typeof(TokenScope)));
			}
		}
	}
}