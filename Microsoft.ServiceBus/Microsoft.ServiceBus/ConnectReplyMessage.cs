using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus
{
	[DataContract(Name="ConnectReply", Namespace="http://schemas.microsoft.com/netservices/2009/05/servicebus/connect")]
	internal class ConnectReplyMessage : IExtensibleDataObject
	{
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

		public ConnectReplyMessage()
		{
		}
	}
}