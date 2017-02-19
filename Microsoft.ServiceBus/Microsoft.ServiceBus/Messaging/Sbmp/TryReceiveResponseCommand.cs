using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	[DataContract(Name="TryReceiveResponse", Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal class TryReceiveResponseCommand : IExtensibleDataObject
	{
		[DataMember(Name="TryReceiveResult", EmitDefaultValue=true, IsRequired=false, Order=65537)]
		private bool result;

		[DataMember(Name="messages", EmitDefaultValue=true, IsRequired=false, Order=65538)]
		private MessageCollection messages;

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

		public MessageCollection Messages
		{
			get
			{
				return this.messages;
			}
			set
			{
				this.messages = value;
			}
		}

		public bool Result
		{
			get
			{
				return this.result;
			}
			set
			{
				this.result = value;
			}
		}

		public TryReceiveResponseCommand()
		{
		}
	}
}