using System;
using System.Globalization;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Messaging.Channels
{
	internal static class TransportDefaults
	{
		public const string CloseTimeoutString = "00:01:00";

		public const string OpenTimeoutString = "00:01:00";

		public const string ReceiveTimeoutString = "00:10:00";

		public const string SendTimeoutString = "00:01:00";

		public const string SessionIdleTimeoutString = "00:01:00";

		public const string PrefetchCountString = "-1";

		public const bool ReceiveContextEnabled = false;

		public const long MaxBufferPoolSize = 524288L;

		public const long MaxReceivedMessageSize = 262144L;

		public const int MaxFrameSize = 65536;

		public const int PrefetchCount = -1;

		public readonly static TimeSpan CloseTimeout;

		public readonly static TimeSpan OpenTimeout;

		public readonly static TimeSpan ReceiveTimeout;

		public readonly static TimeSpan SendTimeout;

		public readonly static TimeSpan SessionIdleTimeout;

		static TransportDefaults()
		{
			Microsoft.ServiceBus.Messaging.Channels.TransportDefaults.CloseTimeout = TimeSpan.Parse("00:01:00", CultureInfo.InvariantCulture);
			Microsoft.ServiceBus.Messaging.Channels.TransportDefaults.OpenTimeout = TimeSpan.Parse("00:01:00", CultureInfo.InvariantCulture);
			Microsoft.ServiceBus.Messaging.Channels.TransportDefaults.ReceiveTimeout = TimeSpan.Parse("00:10:00", CultureInfo.InvariantCulture);
			Microsoft.ServiceBus.Messaging.Channels.TransportDefaults.SendTimeout = TimeSpan.Parse("00:01:00", CultureInfo.InvariantCulture);
			Microsoft.ServiceBus.Messaging.Channels.TransportDefaults.SessionIdleTimeout = TimeSpan.Parse("00:01:00", CultureInfo.InvariantCulture);
		}

		public static BinaryMessageEncodingBindingElement CreateDefaultEncoder()
		{
			return new BinaryMessageEncodingBindingElement();
		}
	}
}