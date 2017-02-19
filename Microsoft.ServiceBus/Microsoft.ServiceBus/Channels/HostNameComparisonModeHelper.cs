using Microsoft.ServiceBus.Diagnostics;
using System;
using System.ComponentModel;
using System.ServiceModel;

namespace Microsoft.ServiceBus.Channels
{
	internal static class HostNameComparisonModeHelper
	{
		internal static bool IsDefined(HostNameComparisonMode value)
		{
			if (value == HostNameComparisonMode.StrongWildcard || value == HostNameComparisonMode.Exact)
			{
				return true;
			}
			return value == HostNameComparisonMode.WeakWildcard;
		}

		public static void Validate(HostNameComparisonMode value)
		{
			if (!Microsoft.ServiceBus.Channels.HostNameComparisonModeHelper.IsDefined(value))
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidEnumArgumentException("value", (int)value, typeof(HostNameComparisonMode)));
			}
		}
	}
}