using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	[DataContract(Name="UpdateMessageStateResponse", Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal class UpdateMessageStateResponseCommand : IExtensibleDataObject
	{
		private ExtensionDataObject extensionData;

		public ExtensionDataObject ExtensionData
		{
			get
			{
				return this.extensionData;
			}
			set
			{
				this.extensionData = value;
			}
		}

		public UpdateMessageStateResponseCommand()
		{
		}
	}
}