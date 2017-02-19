using System;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace Microsoft.ServiceBus
{
	[MessageContract(WrapperName="SwitchRoles", WrapperNamespace="http://schemas.microsoft.com/netservices/2009/05/servicebus/directconnect", IsWrapped=true)]
	internal class SwitchRolesMessage : IExtensibleDataObject
	{
		[MessageBodyMember(Order=0, Name="Id", Namespace="http://schemas.microsoft.com/netservices/2009/05/servicebus/directconnect")]
		private string id;

		private ExtensionDataObject extension;

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

		public SwitchRolesMessage()
		{
		}

		public SwitchRolesMessage(string id)
		{
			this.id = id;
		}
	}
}