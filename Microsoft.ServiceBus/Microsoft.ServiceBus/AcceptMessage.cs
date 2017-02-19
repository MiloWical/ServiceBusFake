using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus
{
	[DataContract(Name="RelayedAccept", Namespace="http://schemas.microsoft.com/netservices/2009/05/servicebus/connect")]
	internal class AcceptMessage : IExtensibleDataObject
	{
		[DataMember(Name="Id", IsRequired=false, EmitDefaultValue=false, Order=0)]
		private string id;

		private ExtensionDataObject extensionDataObject;

		public ExtensionDataObject ExtensionData
		{
			get
			{
				return this.extensionDataObject;
			}
			set
			{
				this.extensionDataObject = value;
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

		public AcceptMessage(string id)
		{
			this.id = id;
		}
	}
}