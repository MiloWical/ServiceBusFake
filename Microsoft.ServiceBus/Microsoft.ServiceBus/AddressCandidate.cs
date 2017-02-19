using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus
{
	[DataContract(Name="AddressCandidate", Namespace="http://schemas.microsoft.com/netservices/2009/05/servicebus/connect")]
	internal struct AddressCandidate : IExtensibleDataObject
	{
		[DataMember(Name="Address", IsRequired=false, EmitDefaultValue=false, Order=0)]
		private long? address;

		[DataMember(Name="Port", IsRequired=false, EmitDefaultValue=false, Order=1)]
		private int? port;

		[DataMember(Name="Type", IsRequired=false, EmitDefaultValue=false, Order=2)]
		private AddressType? type;

		private ExtensionDataObject extension;

		public long Address
		{
			get
			{
				if (!this.address.HasValue)
				{
					return (long)0;
				}
				return this.address.Value;
			}
			set
			{
				this.address = new long?(value);
			}
		}

		public int Port
		{
			get
			{
				if (!this.port.HasValue)
				{
					return 0;
				}
				return this.port.Value;
			}
			set
			{
				this.port = new int?(value);
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

		public AddressType Type
		{
			get
			{
				if (!this.type.HasValue)
				{
					return AddressType.Local;
				}
				return this.type.Value;
			}
			set
			{
				this.type = new AddressType?(value);
			}
		}

		public AddressCandidate(AddressType type, long address, int port)
		{
			this.type = new AddressType?(type);
			this.address = new long?(address);
			this.port = new int?(port);
			this.extension = null;
		}
	}
}