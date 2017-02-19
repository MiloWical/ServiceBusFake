using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging.Channels
{
	[DataContract(Name="Ping", Namespace="http://schemas.microsoft.com/servicebus/2010/08/protocol/")]
	internal sealed class PingMessage : IExtensibleDataObject
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

		public PingMessage()
		{
		}
	}
}