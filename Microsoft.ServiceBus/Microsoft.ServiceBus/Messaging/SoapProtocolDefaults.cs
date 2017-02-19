using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Configuration;
using System.Net.Security;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using System.ServiceModel.Security.Tokens;
using System.Xml;

namespace Microsoft.ServiceBus.Messaging
{
	internal static class SoapProtocolDefaults
	{
		public const bool DefaultPortSharingEnabled = false;

		public const ushort DefaultPort = 9093;

		public const string UriScheme = "sb";

		public const string TransportUriScheme = "net.tcp";

		private readonly static TimeSpan ClientToBrokerTimeout;

		static SoapProtocolDefaults()
		{
			SoapProtocolDefaults.ClientToBrokerTimeout = TimeSpan.FromSeconds(10);
		}

		internal static TimeSpan BufferTimeout(TimeSpan timeout)
		{
			return TimeoutHelper.Add(timeout, SoapProtocolDefaults.ClientToBrokerTimeout);
		}

		public static CustomBinding CreateBinding(bool portSharingEnabled, int maxReceivedMessageSize, long maxBufferPoolSize, bool useSslStreamSecurity, bool clientCertificateAuthEnabled, DnsEndpointIdentity endpointIdentity, IssuedSecurityTokenParameters issuedTokenParameters)
		{
			TransactionFlowBindingElement transactionFlowBindingElement = new TransactionFlowBindingElement();
			BinaryMessageEncodingBindingElement binaryMessageEncodingBindingElement = new BinaryMessageEncodingBindingElement();
			binaryMessageEncodingBindingElement.ReaderQuotas.MaxStringContentLength = maxReceivedMessageSize;
			TcpTransportBindingElement tcpTransportBindingElement = new TcpTransportBindingElement()
			{
				PortSharingEnabled = portSharingEnabled,
				MaxReceivedMessageSize = (long)maxReceivedMessageSize,
				MaxBufferPoolSize = maxBufferPoolSize
			};
			CustomBinding customBinding = new CustomBinding();
			customBinding.Elements.Add(transactionFlowBindingElement);
			if (useSslStreamSecurity)
			{
				SslStreamSecurityBindingElement sslStreamSecurityBindingElement = new SslStreamSecurityBindingElement();
				if (endpointIdentity != null)
				{
					sslStreamSecurityBindingElement.IdentityVerifier = new LenientDnsIdentityVerifier(endpointIdentity);
				}
				sslStreamSecurityBindingElement.RequireClientCertificate = clientCertificateAuthEnabled;
				customBinding.Elements.Add(sslStreamSecurityBindingElement);
			}
			customBinding.Elements.Add(binaryMessageEncodingBindingElement);
			customBinding.Elements.Add(tcpTransportBindingElement);
			return customBinding;
		}

		public static CustomBinding CreateBinding(bool portSharingEnabled, int maxReceivedMessageSize, long maxBufferPoolSize, IssuedSecurityTokenParameters issuedTokenParameters)
		{
			return SoapProtocolDefaults.CreateBinding(portSharingEnabled, maxReceivedMessageSize, maxBufferPoolSize, false, false, null, issuedTokenParameters);
		}

		public static CustomBinding CreateSslBinding(bool portSharingEnabled, int maxReceivedMessageSize, long maxBufferPoolSize, bool clientCertificateAuthEnabled, DnsEndpointIdentity endpointIdentity, IssuedSecurityTokenParameters issuedTokenParameters)
		{
			return SoapProtocolDefaults.CreateBinding(portSharingEnabled, maxReceivedMessageSize, maxBufferPoolSize, true, clientCertificateAuthEnabled, endpointIdentity, issuedTokenParameters);
		}

		public static CustomBinding CreateSslBinding(bool portSharingEnabled, int maxReceivedMessageSize, long maxBufferPoolSize, bool clientCertificateAuthEnabled, IssuedSecurityTokenParameters issuedTokenParameters)
		{
			return SoapProtocolDefaults.CreateBinding(portSharingEnabled, maxReceivedMessageSize, maxBufferPoolSize, true, clientCertificateAuthEnabled, null, issuedTokenParameters);
		}

		public static ChannelProtectionRequirements GetChannelProtectionRequirements(ProtectionLevel protectionLevel)
		{
			MessagePartSpecification messagePartSpecification = new MessagePartSpecification(true);
			MessagePartSpecification messagePartSpecification1 = new MessagePartSpecification();
			ChannelProtectionRequirements channelProtectionRequirement = new ChannelProtectionRequirements();
			switch (protectionLevel)
			{
				case ProtectionLevel.None:
				{
					channelProtectionRequirement.IncomingSignatureParts.AddParts(messagePartSpecification1, "*");
					channelProtectionRequirement.OutgoingSignatureParts.AddParts(messagePartSpecification1, "*");
					channelProtectionRequirement.IncomingEncryptionParts.AddParts(messagePartSpecification1, "*");
					channelProtectionRequirement.OutgoingEncryptionParts.AddParts(messagePartSpecification1, "*");
					break;
				}
				case ProtectionLevel.Sign:
				{
					channelProtectionRequirement.IncomingSignatureParts.AddParts(messagePartSpecification, "*");
					channelProtectionRequirement.OutgoingSignatureParts.AddParts(messagePartSpecification, "*");
					channelProtectionRequirement.IncomingEncryptionParts.AddParts(messagePartSpecification1, "*");
					channelProtectionRequirement.OutgoingEncryptionParts.AddParts(messagePartSpecification1, "*");
					break;
				}
				case ProtectionLevel.EncryptAndSign:
				{
					channelProtectionRequirement.IncomingSignatureParts.AddParts(messagePartSpecification, "*");
					channelProtectionRequirement.OutgoingSignatureParts.AddParts(messagePartSpecification, "*");
					channelProtectionRequirement.IncomingEncryptionParts.AddParts(messagePartSpecification, "*");
					channelProtectionRequirement.OutgoingEncryptionParts.AddParts(messagePartSpecification, "*");
					break;
				}
			}
			return channelProtectionRequirement;
		}

		public static EndpointIdentity GetEndpointIdentity(Uri address)
		{
			return new DnsEndpointIdentity(address.Host);
		}

		public static bool IsAvailableClientCertificateThumbprint(out string thumbprint)
		{
			bool flag;
			try
			{
				thumbprint = ConfigurationManager.AppSettings["certificateThumbprintAsClient"];
				flag = (!string.IsNullOrEmpty(thumbprint) ? true : false);
			}
			catch (ConfigurationErrorsException configurationErrorsException)
			{
				thumbprint = null;
				return false;
			}
			return flag;
		}
	}
}