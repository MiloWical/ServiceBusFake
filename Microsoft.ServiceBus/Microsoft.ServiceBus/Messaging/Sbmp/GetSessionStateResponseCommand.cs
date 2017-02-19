using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	[DataContract(Name="GetSessionStateResponse", Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal class GetSessionStateResponseCommand : IExtensibleDataObject
	{
		[DataMember(Name="sessionState", EmitDefaultValue=true, IsRequired=false, Order=65537)]
		private Microsoft.ServiceBus.Messaging.Sbmp.SessionState sessionState;

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

		public Microsoft.ServiceBus.Messaging.Sbmp.SessionState SessionState
		{
			get
			{
				return this.sessionState;
			}
			set
			{
				this.sessionState = value;
			}
		}

		public GetSessionStateResponseCommand()
		{
		}
	}
}