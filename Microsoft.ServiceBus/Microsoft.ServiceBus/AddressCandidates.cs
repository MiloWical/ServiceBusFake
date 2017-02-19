using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus
{
	[DataContract(Name="AddressCandidates", Namespace="http://schemas.microsoft.com/netservices/2009/05/servicebus/connect")]
	internal class AddressCandidates : IExtensibleDataObject
	{
		[DataMember(Name="Addresses", IsRequired=false, EmitDefaultValue=false, Order=0)]
		private Collection<AddressCandidate> addresses;

		private ExtensionDataObject extension;

		public Collection<AddressCandidate> Addresses
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

		public AddressCandidates()
		{
			this.addresses = new Collection<AddressCandidate>();
		}

		public void AddEndpoints(AddressType type, params IPEndPoint[] endpoints)
		{
			for (int i = 0; i < (int)endpoints.Length; i++)
			{
				this.addresses.Add(new AddressCandidate(type, endpoints[i].Address.GetRawIPv4Address(), endpoints[i].Port));
			}
		}

		public AddressCandidate GetAddress(AddressType type)
		{
			List<AddressCandidate> addresses = this.GetAddresses(type);
			if (addresses.Count != 1)
			{
				throw new InvalidOperationException(SRClient.MoreThanOneAddressCandidate);
			}
			return addresses[0];
		}

		public List<AddressCandidate> GetAddresses(AddressType type)
		{
			List<AddressCandidate> addressCandidates = new List<AddressCandidate>();
			for (int i = 0; i < this.addresses.Count; i++)
			{
				if (this.addresses[i].Type == type)
				{
					addressCandidates.Add(this.addresses[i]);
				}
			}
			return addressCandidates;
		}

		public IPEndPoint GetEndpoint(AddressType type)
		{
			List<IPEndPoint> endpoints = this.GetEndpoints(type);
			if (endpoints.Count != 1)
			{
				throw new InvalidOperationException(SRClient.MoreThanOneIPEndPoint);
			}
			return endpoints[0];
		}

		public List<IPEndPoint> GetEndpoints(AddressType type)
		{
			List<AddressCandidate> addresses = this.GetAddresses(type);
			List<IPEndPoint> pEndPoints = new List<IPEndPoint>();
			for (int i = 0; i < addresses.Count; i++)
			{
				long address = addresses[i].Address;
				AddressCandidate item = addresses[i];
				pEndPoints.Add(new IPEndPoint(address, item.Port));
			}
			return pEndPoints;
		}
	}
}