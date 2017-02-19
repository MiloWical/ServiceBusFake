using System;

namespace Microsoft.ServiceBus.Messaging.Configuration
{
	internal static class ConfigurationStrings
	{
		public const string BatchFlushInterval = "batchFlushInterval";

		public const string EnableRedirect = "enableRedirect";

		public const string MaxReceivedMessageSize = "maxReceivedMessageSize";

		public const string NetMessagingBinding = "netMessagingBinding";

		public const string SessionIdleTimeout = "sessionIdleTimeout";

		public const string ServiceModelBindings = "system.serviceModel/bindings";

		public const string TransportSettings = "transportSettings";

		public const string PrefetchCount = "prefetchCount";

		public const string NetTransportSettings = "nettransportSettings";

		public const string AmqpTransportSettings = "amqptransportSettings";

		public const string SslStreamUpgrade = "sslStreamUpgrade";

		public const string UseSslStreamSecurity = "useSslStreamSecurity";

		public const string MaxFrameSize = "maxFrameSize";
	}
}