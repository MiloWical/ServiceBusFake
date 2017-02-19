using System;

namespace Microsoft.ServiceBus
{
	internal class ConnectConstants
	{
		public const string Connect = "Connect";

		public const string ConnectContract = "ConnectContract";

		public const int ConnectionPingTimeout = 30;

		public const string ConnectReply = "ConnectReply";

		public const string DnsName = "DNS Name";

		public const int DefaultConnectionPort = 9352;

		public const int DefaultOnewayConnectionPort = 9350;

		public const int DefaultSecureOnewayConnectionPort = 9351;

		public const int EncoderMaxArrayLength = 61440;

		public const int EncoderMaxStringContentLength = 61440;

		public const int EncoderMaxDepth = 32;

		public const int EncoderReadPoolSize = 128;

		public const int EncoderWritePoolSize = 128;

		public const int EncoderMaxSizeOfHeaders = 65536;

		public const string EnvelopeNone = "EnvelopeNone";

		public const string EnvelopeSoap11 = "EnvelopeSoap11";

		public const string EnvelopeSoap12 = "EnvelopeSoap12";

		public const string HttpRequestContract = "HttpRequest";

		public const string ServiceBusAuthorizationHeaderName = "ServiceBusAuthorization";

		public const string SubjectAlternativeNameOid = "2.5.29.17";

		public const string Listen = "Listen";

		public const string ListenReply = "ListenReply";

		public const long MaxBufferPoolSize = 67108864L;

		public const int MaxMessageSize = 65536;

		public const string Namespace = "http://schemas.microsoft.com/netservices/2009/05/servicebus/connect";

		public const string Manage = "Manage";

		public const string MulticastElement = "Multicast";

		public const string Ping = "Ping";

		public const string OnewayPing = "OnewayPing";

		public const string Prefix = "rel";

		public const string Redirect = "Redirect";

		public const string RelayedAccept = "RelayedAccept";

		public const string RelayedAcceptReply = "RelayedAcceptReply";

		public const string RelayedConnect = "RelayedConnect";

		public const string RelayedConnectReply = "RelayedConnectReply";

		public const string RelayedOnewayElement = "RelayedOneway";

		public const string RouteAddressKey = "RouteAddress";

		public const int RoutePingTimeout = 30;

		public const int MaxGetTokenRetry = 3;

		public const string Scheme = "sb";

		public const string ServiceBusWebSocketSecureScheme = "sbwss";

		public const string SecureTransportNamespace = "http://schemas.microsoft.com/ws/2006/05/framing/policy";

		public const string Send = "Send";

		public const string SendContract = "SendContract";

		public const string SendReply = "SendReply";

		public const string ViaHeaderName = "RelayVia";

		public const string RelayAccessTokenHeaderName = "RelayAccessToken";

		public const string ProcessAtHeaderName = "ProcessAt";

		public const string ProcessAtRoleAttributeName = "role";

		public const string ProcessAtRoleAttributeValueDefault = "http://schemas.microsoft.com/netservices/2009/05/servicebus/connect/roles/relay";

		public const string XProcessAtHttpHeader = "X-PROCESS-AT";

		public const string XHttpMethodEquivHttpHeader = "X-HTTP-METHOD-EQUIV";

		public const string XHttpMethodOverrideHttpHeader = "X-HTTP-METHOD-OVERRIDE";

		public const string PolicyAssertionSenderRelayCredential = "SenderRelayCredential";

		public const string PolicyAssertionListenerRelayCredential = "ListenerRelayCredential";

		public const string PolicyAssertionRelaySocketConnection = "RelaySocketConnection";

		public const string PolicyAssertionHybridSocketConnection = "HybridSocketConnection";

		public const string PolicyAssertionSslTransportSecurity = "SslTransportSecurity";

		public const string TracingActivityIdHeaderName = "TracingAcitivityId";

		public const string MaximumListenersPerEndpointQuotaName = "MaximumListenersPerEndpoint";

		public const string AmqpMessagePropertyName = "AmqpMessageProperty";

		public readonly static TimeSpan ConnectionInitiateTimeout;

		public readonly static TimeSpan ConnectOperationTimeout;

		public readonly static string ConnectType;

		public readonly static int[] DefaultProbePorts;

		public readonly static string ProbeType;

		public readonly static TimeSpan RouteIdleTimeout;

		public readonly static TimeSpan RelayedOnewaySendTimeout;

		public readonly static byte[] NoSsl;

		public readonly static byte[] UseSsl;

		static ConnectConstants()
		{
			ConnectConstants.ConnectionInitiateTimeout = TimeSpan.FromSeconds(60);
			ConnectConstants.ConnectOperationTimeout = TimeSpan.FromSeconds(10);
			ConnectConstants.ConnectType = "connect";
			ConnectConstants.DefaultProbePorts = new int[] { 9352, 9353 };
			ConnectConstants.ProbeType = "probe";
			ConnectConstants.RouteIdleTimeout = TimeSpan.FromSeconds(90);
			ConnectConstants.RelayedOnewaySendTimeout = TimeSpan.FromSeconds(60);
			ConnectConstants.NoSsl = new byte[1];
			ConnectConstants.UseSsl = new byte[] { 1 };
		}

		public ConnectConstants()
		{
		}

		internal static class Actions
		{
			public const string ConnectRequest = "http://schemas.microsoft.com/netservices/2009/05/servicebus/connect/Connect";

			public const string ListenRequest = "http://schemas.microsoft.com/netservices/2009/05/servicebus/connect/Listen";

			public const string OnewayPingRequest = "http://schemas.microsoft.com/netservices/2009/05/servicebus/connect/OnewayPing";
		}

		internal static class Amqp
		{
			public const string HttpPrefix = "Http";

			public const string HttpHeaderName = "Header";

			public const string HttpMethodName = "Method";

			public const string HttpStatusCodeName = "StatusCode";

			public const string HttpStatusDescriptionName = "StatusDescription";

			public readonly static char[] PropertySeparator;

			public readonly static string HttpMethodFullName;

			public readonly static string HttpStatusCodeFullName;

			public readonly static string HttpStatusDescriptionFullName;

			public readonly static string HttpHeaderPrefix;

			static Amqp()
			{
				ConnectConstants.Amqp.PropertySeparator = new char[] { ':' };
				ConnectConstants.Amqp.HttpMethodFullName = string.Concat("Http", ConnectConstants.Amqp.PropertySeparator[0], "Method");
				ConnectConstants.Amqp.HttpStatusCodeFullName = string.Concat("Http", ConnectConstants.Amqp.PropertySeparator[0], "StatusCode");
				ConnectConstants.Amqp.HttpStatusDescriptionFullName = string.Concat("Http", ConnectConstants.Amqp.PropertySeparator[0], "StatusDescription");
				object[] propertySeparator = new object[] { "Http", ConnectConstants.Amqp.PropertySeparator[0], "Header", ConnectConstants.Amqp.PropertySeparator[0] };
				ConnectConstants.Amqp.HttpHeaderPrefix = string.Concat(propertySeparator);
			}
		}
	}
}