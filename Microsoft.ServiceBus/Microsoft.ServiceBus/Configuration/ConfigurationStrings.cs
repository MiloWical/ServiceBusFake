using System;

namespace Microsoft.ServiceBus.Configuration
{
	internal class ConfigurationStrings
	{
		internal const string ChannelInitializationTimeout = "channelInitializationTimeout";

		internal const string ConnectionBufferSize = "connectionBufferSize";

		internal const string ConnectionMode = "connectionMode";

		internal const string HostNameComparisonMode = "hostNameComparisonMode";

		internal const string MaxBufferPoolSize = "maxBufferPoolSize";

		internal const string MaxBufferSize = "maxBufferSize";

		internal const string MaxConnections = "maxConnections";

		internal const string MaxOutputDelay = "maxOutputDelay";

		internal const string MaxPendingAccepts = "maxPendingAccepts";

		internal const string MaxPendingConnections = "maxPendingConnections";

		internal const string MaxReceivedMessageSize = "maxReceivedMessageSize";

		internal const string ReaderQuotas = "readerQuotas";

		internal const string Security = "security";

		internal const string TransferMode = "transferMode";

		internal const string IsDynamic = "isDynamic";

		internal const string TokenProvider = "tokenProvider";

		internal const string TokenScope = "tokenScope";

		internal const string IssuerAddress = "issuerAddress";

		internal const string IssuerName = "issuerName";

		internal const string IssuerSecret = "issuerSecret";

		internal const string SharedSecret = "sharedSecret";

		internal const string WindowsAuthentication = "windowsAuthentication";

		internal const string SharedAccessSignature = "sharedAccessSignature";

		internal const string KeyName = "keyName";

		internal const string Key = "key";

		internal const string Name = "name";

		internal const string Address = "address";

		internal const string AddressValue = "value";

		public const string UseDefaultCredentials = "useDefaultCredentials";

		public const string UserName = "userName";

		public const string Password = "password";

		public const string Domain = "domain";

		internal const string StsUri = "stsUri";

		internal const string StsUriValue = "value";

		internal const string StsUris = "stsUris";

		internal const string BasicHttpRelayBindingCollectionElementName = "basicHttpRelayBinding";

		internal const string NetTcpRelayBindingCollectionElementName = "netTcpRelayBinding";

		internal const string NetOnewayRelayBindingCollectionElementName = "netOnewayRelayBinding";

		internal const string NetEventRelayBindingCollectionElementName = "netEventRelayBinding";

		internal const string WebHttpRelayBindingCollectionElementName = "webHttpRelayBinding";

		internal const string WS2007HttpRelayBindingCollectionElementName = "ws2007HttpRelayBinding";

		internal const string EstablishSecurityContext = "establishSecurityContext";

		internal const string Default = "Default";

		internal const string AlgorithmSuite = "algorithmSuite";

		internal const string NegotiateServiceCredential = "negotiateServiceCredential";

		internal const string ClientCredentialType = "clientCredentialType";

		internal const string ListenBacklog = "listenBacklog";

		internal const string ProxyCredentialType = "proxyCredentialType";

		internal const string ProxyAuthenticationScheme = "proxyAuthenticationScheme";

		internal const string ProxyAddress = "proxyAddress";

		internal const string KeepAliveEnabled = "keepAliveEnabled";

		internal const string ProtectionLevel = "protectionLevel";

		internal const string ConnectionPoolSettings = "connectionPoolSettings";

		internal const string GroupName = "groupName";

		internal const string LeaseTimeout = "leaseTimeout";

		internal const string IdleTimeout = "idleTimeout";

		internal const string MaxOutboundConnectionsPerEndpoint = "maxOutboundConnectionsPerEndpoint";

		internal const string TimeSpanZero = "00:00:00";

		internal const string TimeSpanOneTick = "00:00:00.0000001";

		internal const string Message = "message";

		internal const string Transport = "transport";

		internal const string Mode = "mode";

		internal const string RelayClientAuthenticationType = "relayClientAuthenticationType";

		internal const string Basic128 = "Basic128";

		internal const string Basic192 = "Basic192";

		internal const string Basic256 = "Basic256";

		internal const string Basic128Rsa15 = "Basic128Rsa15";

		internal const string Basic192Rsa15 = "Basic192Rsa15";

		internal const string Basic256Rsa15 = "Basic256Rsa15";

		internal const string Basic128Sha256 = "Basic128Sha256";

		internal const string Basic192Sha256 = "Basic192Sha256";

		internal const string Basic256Sha256 = "Basic256Sha256";

		internal const string Basic128Sha256Rsa15 = "Basic128Sha256Rsa15";

		internal const string Basic192Sha256Rsa15 = "Basic192Sha256Rsa15";

		internal const string Basic256Sha256Rsa15 = "Basic256Sha256Rsa15";

		internal const string TripleDes = "TripleDes";

		internal const string TripleDesRsa15 = "TripleDesRsa15";

		internal const string TripleDesSha256 = "TripleDesSha256";

		internal const string TripleDesSha256Rsa15 = "TripleDesSha256Rsa15";

		internal const string AllowCookies = "allowCookies";

		internal const string MessageEncoding = "messageEncoding";

		internal const string TextEncoding = "textEncoding";

		internal const string WriteEncoding = "writeEncoding";

		internal const string ReliableSession = "reliableSession";

		internal const string UseDefaultWebProxy = "useDefaultWebProxy";

		internal const string MaxArrayLength = "maxArrayLength";

		internal const string MaxBytesPerRead = "maxBytesPerRead";

		internal const string MaxDepth = "maxDepth";

		internal const string MaxNameTableCharCount = "maxNameTableCharCount";

		internal const string MaxStringContentLength = "maxStringContentLength";

		internal const string SectionGroupName = "system.serviceModel";

		internal const string BindingsSectionGroupName = "bindings";

		internal const string ServiceBusX509RevocationModeName = "Microsoft.ServiceBus.X509RevocationMode";

		internal const string ServiceBusOverrideAutoDetectModeName = "Microsoft.ServiceBus.OverrideAutoDetectMode";

		internal static string BindingsSectionGroupPath
		{
			get
			{
				return ConfigurationHelpers.GetSectionPath("bindings");
			}
		}

		public ConfigurationStrings()
		{
		}
	}
}