using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using System;
using System.Collections.ObjectModel;
using System.Net.Security;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using System.Xml;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	internal static class SbmpProtocolDefaults
	{
		public const int DefaultMaxFaultSize = 65536;

		public const bool DefaultPortSharingEnabled = false;

		public const string UriScheme = "sb";

		internal const double DefaultClientToBrokerTimeout = 10;

		internal static double InternalClientToBrokerTimeout;

		public readonly static string TransportUriScheme;

		private readonly static TimeSpan ClientToBrokerTimeout;

		static SbmpProtocolDefaults()
		{
			SbmpProtocolDefaults.InternalClientToBrokerTimeout = 10;
			SbmpProtocolDefaults.TransportUriScheme = Uri.UriSchemeNetTcp;
			SbmpProtocolDefaults.ClientToBrokerTimeout = TimeSpan.FromSeconds(10);
		}

		internal static TimeSpan BufferTimeout(TimeSpan timeout, bool enableAdditionalClientTimeout)
		{
			TimeSpan timeSpan = (timeout < TimeSpan.Zero ? TimeSpan.Zero : timeout);
			if (!enableAdditionalClientTimeout)
			{
				return timeSpan;
			}
			if (SbmpProtocolDefaults.InternalClientToBrokerTimeout == 10)
			{
				return TimeoutHelper.Add(timeSpan, SbmpProtocolDefaults.ClientToBrokerTimeout);
			}
			return TimeoutHelper.Add(timeSpan, TimeSpan.FromSeconds(SbmpProtocolDefaults.InternalClientToBrokerTimeout));
		}

		public static CustomBinding CreateBinding(bool portSharingEnabled, bool useWebStream, int maxReceivedMessageSize, bool useSslStreamSecurity, DnsEndpointIdentity endpointIdentity)
		{
			return SbmpProtocolDefaults.CreateBinding(portSharingEnabled, useWebStream, false, maxReceivedMessageSize, useSslStreamSecurity, endpointIdentity);
		}

		public static CustomBinding CreateBinding(bool portSharingEnabled, bool useWebStream, bool useHttpsWebStream, int maxReceivedMessageSize, bool useSslStreamSecurity, DnsEndpointIdentity endpointIdentity)
		{
			TransportBindingElement tcpTransportBindingElement;
			BinaryMessageEncodingBindingElement binaryMessageEncodingBindingElement = new BinaryMessageEncodingBindingElement();
			binaryMessageEncodingBindingElement.ReaderQuotas.MaxStringContentLength = 50000;
			bool flag = (useWebStream ? false : useSslStreamSecurity);
			if (useWebStream)
			{
				flag = (useHttpsWebStream ? false : useSslStreamSecurity);
			}
			if (!useWebStream)
			{
				tcpTransportBindingElement = new TcpTransportBindingElement()
				{
					PortSharingEnabled = portSharingEnabled
				};
			}
			else
			{
				tcpTransportBindingElement = new SocketConnectionBindingElement(new WebStreamOnewayClientConnectionElement((flag ? SocketSecurityRole.SslClient : SocketSecurityRole.None), "messaging", useHttpsWebStream), false);
			}
			tcpTransportBindingElement.MaxReceivedMessageSize = (long)maxReceivedMessageSize;
			tcpTransportBindingElement.ManualAddressing = true;
			CustomBinding customBinding = new CustomBinding();
			if (flag)
			{
				BindingElementCollection elements = customBinding.Elements;
				SslStreamSecurityBindingElement sslStreamSecurityBindingElement = new SslStreamSecurityBindingElement()
				{
					IdentityVerifier = new LenientDnsIdentityVerifier(endpointIdentity)
				};
				elements.Add(sslStreamSecurityBindingElement);
			}
			customBinding.Elements.Add(binaryMessageEncodingBindingElement);
			customBinding.Elements.Add(tcpTransportBindingElement);
			return customBinding;
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
	}
}