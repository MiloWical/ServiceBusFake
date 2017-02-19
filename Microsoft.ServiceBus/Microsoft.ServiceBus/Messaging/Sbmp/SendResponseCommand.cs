using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	[DataContract(Name="SendResponse", Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal class SendResponseCommand : IExtensibleDataObject
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

		public SendResponseCommand()
		{
		}
	}
}