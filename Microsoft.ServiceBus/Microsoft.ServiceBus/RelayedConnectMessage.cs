using System;
using System.Net;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace Microsoft.ServiceBus
{
	[MessageContract(IsWrapped=true, WrapperName="RelayedConnect", WrapperNamespace="http://schemas.microsoft.com/netservices/2009/05/servicebus/relayedconnect")]
	internal class RelayedConnectMessage : IExtensibleDataObject
	{
		internal readonly static TypedMessageConverter MessageConverter;

		[MessageBodyMember(Order=0, Name="Id", Namespace="http://schemas.microsoft.com/netservices/2009/05/servicebus/relayedconnect")]
		private string id;

		[MessageBodyMember(Order=1, Name="IpAddress", Namespace="http://schemas.microsoft.com/netservices/2009/05/servicebus/relayedconnect")]
		private long? ipAddress;

		[MessageBodyMember(Order=2, Name="IpPort", Namespace="http://schemas.microsoft.com/netservices/2009/05/servicebus/relayedconnect")]
		private int? ipPort;

		[MessageBodyMember(Order=3, Name="HttpAddress", Namespace="http://schemas.microsoft.com/netservices/2009/05/servicebus/relayedconnect")]
		private long? httpAddress;

		[MessageBodyMember(Order=4, Name="HttpPort", Namespace="http://schemas.microsoft.com/netservices/2009/05/servicebus/relayedconnect")]
		private int? httpPort;

		[MessageBodyMember(Order=6, Name="HttpsAddress", Namespace="http://schemas.microsoft.com/netservices/2009/05/servicebus/relayedconnect")]
		private long? httpsAddress;

		[MessageBodyMember(Order=7, Name="HttpsPort", Namespace="http://schemas.microsoft.com/netservices/2009/05/servicebus/relayedconnect")]
		private int? httpsPort;

		private ExtensionDataObject extension;

		public IPEndPoint HttpEndpoint
		{
			get
			{
				if (!this.httpAddress.HasValue || !this.httpPort.HasValue)
				{
					return null;
				}
				return new IPEndPoint(this.httpAddress.Value, this.httpPort.Value);
			}
			set
			{
				if (value == null)
				{
					this.httpAddress = null;
					return;
				}
				this.httpAddress = new long?(value.Address.GetRawIPv4Address());
				this.httpPort = new int?(value.Port);
			}
		}

		public IPEndPoint HttpsEndpoint
		{
			get
			{
				if (!this.httpsAddress.HasValue || !this.httpsPort.HasValue)
				{
					return null;
				}
				return new IPEndPoint(this.httpsAddress.Value, this.httpsPort.Value);
			}
			set
			{
				if (value == null)
				{
					this.httpsAddress = null;
					return;
				}
				this.httpsAddress = new long?(value.Address.GetRawIPv4Address());
				this.httpsPort = new int?(value.Port);
			}
		}

		public string Id
		{
			get
			{
				return this.id;
			}
			set
			{
				this.id = value;
			}
		}

		public IPEndPoint IpEndpoint
		{
			get
			{
				if (!this.ipAddress.HasValue || !this.ipPort.HasValue)
				{
					return null;
				}
				return new IPEndPoint(this.ipAddress.Value, this.ipPort.Value);
			}
			set
			{
				if (value == null)
				{
					this.ipAddress = null;
					return;
				}
				this.ipAddress = new long?(value.Address.GetRawIPv4Address());
				this.ipPort = new int?(value.Port);
			}
		}

		ExtensionDataObject System.Runtime.Serialization.IExtensibleDataObject.ExtensionData
		{
			get
			{
				return this.extension;
			}
			set
			{
				this.extension = value;
			}
		}

		static RelayedConnectMessage()
		{
			RelayedConnectMessage.MessageConverter = TypedMessageConverter.Create(typeof(RelayedConnectMessage), "http://schemas.microsoft.com/netservices/2009/05/servicebus/relayedconnect/RelayedConnect");
		}

		public RelayedConnectMessage()
		{
		}

		public RelayedConnectMessage(string id, IPEndPoint ipEndPoint, IPEndPoint httpEndPoint, IPEndPoint httpsEndPoint)
		{
			this.id = id;
			this.ipAddress = new long?(ipEndPoint.Address.GetRawIPv4Address());
			this.ipPort = new int?(ipEndPoint.Port);
			this.httpAddress = new long?(httpEndPoint.Address.GetRawIPv4Address());
			this.httpPort = new int?(httpEndPoint.Port);
			this.httpsAddress = new long?(httpsEndPoint.Address.GetRawIPv4Address());
			this.httpsPort = new int?(httpsEndPoint.Port);
		}
	}
}