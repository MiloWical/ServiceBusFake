using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	[DataContract(Name="PartitionedEntitySessionInfo", Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal class PartitionedEntitySessionInfo
	{
		[DataMember(Name="sessionId", EmitDefaultValue=false, IsRequired=false, Order=65537)]
		private string sessionId;

		[DataMember(Name="sessionState", EmitDefaultValue=false, IsRequired=false, Order=65538)]
		private Microsoft.ServiceBus.Messaging.Sbmp.SessionState sessionState;

		[DataMember(Name="partitionId", EmitDefaultValue=false, IsRequired=false, Order=65539)]
		private short partitionId;

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

		public short PartitionId
		{
			get
			{
				return this.partitionId;
			}
			set
			{
				this.partitionId = value;
			}
		}

		public string SessionId
		{
			get
			{
				return this.sessionId;
			}
			set
			{
				this.sessionId = value;
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

		public PartitionedEntitySessionInfo()
		{
		}
	}
}