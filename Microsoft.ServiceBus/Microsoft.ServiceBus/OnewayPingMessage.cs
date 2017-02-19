using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus
{
	[DataContract(Name="OnewayPing", Namespace="http://schemas.microsoft.com/netservices/2009/05/servicebus/connect")]
	internal class OnewayPingMessage : IExtensibleDataObject
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

		public OnewayPingMessage()
		{
		}
	}
}