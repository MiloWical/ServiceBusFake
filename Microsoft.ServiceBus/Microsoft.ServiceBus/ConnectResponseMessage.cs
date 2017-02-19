using System;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace Microsoft.ServiceBus
{
	[MessageContract(WrapperName="ConnectResponse", WrapperNamespace="http://schemas.microsoft.com/netservices/2009/05/servicebus/directconnect", IsWrapped=true)]
	internal class ConnectResponseMessage : IExtensibleDataObject
	{
		[MessageBodyMember(Order=0, Name="Id", Namespace="http://schemas.microsoft.com/netservices/2009/05/servicebus/directconnect")]
		private string id;

		[MessageBodyMember(Order=1, Name="Addresses", Namespace="http://schemas.microsoft.com/netservices/2009/05/servicebus/directconnect")]
		private AddressCandidates addresses;

		private ExtensionDataObject extension;

		internal AddressCandidates Addresses
		{
			get
			{
				return this.addresses;
			}
			set
			{
				this.addresses = value;
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

		public ConnectResponseMessage()
		{
		}

		public ConnectResponseMessage(string id, AddressCandidates addresses)
		{
			this.id = id;
			this.addresses = addresses;
		}
	}
}