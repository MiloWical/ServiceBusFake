using Microsoft.ServiceBus.Channels;
using System;

namespace Microsoft.ServiceBus
{
	internal static class ServiceDefaults
	{
		internal const string CloseTimeoutString = "00:01:00";

		internal const string OpenTimeoutString = "00:01:00";

		internal const string InstanceContextIdleTimeoutString = "00:01:00";

		internal const string ReceiveTimeoutString = "00:01:00";

		internal const string SendTimeoutString = "00:01:00";

		internal const string TransactionTimeoutString = "00:00:00";

		public readonly static TimeSpan MaxClockSkew;

		internal static TimeSpan CloseTimeout
		{
			get
			{
				return TimeSpanHelper.FromMinutes(1, "00:01:00");
			}
		}

		internal static TimeSpan InstanceContextIdleTimeout
		{
			get
			{
				return TimeSpanHelper.FromMinutes(1, "00:01:00");
			}
		}

		internal static TimeSpan OpenTimeout
		{
			get
			{
				return TimeSpanHelper.FromMinutes(1, "00:01:00");
			}
		}

		internal static TimeSpan ReceiveTimeout
		{
			get
			{
				return TimeSpanHelper.FromMinutes(1, "00:01:00");
			}
		}

		internal static TimeSpan SendTimeout
		{
			get
			{
				return TimeSpanHelper.FromMinutes(1, "00:01:00");
			}
		}

		internal static TimeSpan TransactionTimeout
		{
			get
			{
				return TimeSpanHelper.FromMinutes(1, "00:00:00");
			}
		}

		static ServiceDefaults()
		{
			Microsoft.ServiceBus.ServiceDefaults.MaxClockSkew = new TimeSpan(0, 5, 0);
		}
	}
}