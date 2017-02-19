using Microsoft.ServiceBus.Diagnostics;
using System;
using System.Globalization;

namespace Microsoft.ServiceBus.Channels
{
	internal static class TimeSpanHelper
	{
		public static TimeSpan FromDays(int days, string configValue)
		{
			TimeSpan timeSpan = TimeSpan.FromTicks(864000000000L * (long)days);
			DiagnosticUtility.DebugAssert(timeSpan == TimeSpan.Parse(configValue, CultureInfo.InvariantCulture), "");
			return timeSpan;
		}

		public static TimeSpan FromMilliseconds(int ms, string configValue)
		{
			TimeSpan timeSpan = TimeSpan.FromTicks((long)10000 * (long)ms);
			DiagnosticUtility.DebugAssert(timeSpan == TimeSpan.Parse(configValue, CultureInfo.InvariantCulture), "");
			return timeSpan;
		}

		public static TimeSpan FromMinutes(int minutes, string configValue)
		{
			TimeSpan timeSpan = TimeSpan.FromTicks((long)600000000 * (long)minutes);
			DiagnosticUtility.DebugAssert(timeSpan == TimeSpan.Parse(configValue, CultureInfo.InvariantCulture), "");
			return timeSpan;
		}

		public static TimeSpan FromSeconds(int seconds, string configValue)
		{
			TimeSpan timeSpan = TimeSpan.FromTicks((long)10000000 * (long)seconds);
			DiagnosticUtility.DebugAssert(timeSpan == TimeSpan.Parse(configValue, CultureInfo.InvariantCulture), "");
			return timeSpan;
		}
	}
}