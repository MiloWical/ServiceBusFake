using Microsoft.ServiceBus.Messaging;
using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	[DataContract(Name="Peek", Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal sealed class PeekCommand : IExtensibleDataObject
	{
		[DataMember(Name="fromSequenceNumber", EmitDefaultValue=true, IsRequired=false, Order=65537)]
		private long fromSequenceNumber;

		[DataMember(Name="messageCount", EmitDefaultValue=true, IsRequired=false, Order=65538)]
		private int messageCount;

		[DataMember(Name="sessionId", EmitDefaultValue=true, IsRequired=false, Order=65539)]
		private string sessionId;

		[DataMember(Name="timeout", EmitDefaultValue=true, IsRequired=false, Order=65540)]
		private TimeSpan timeout;

		[DataMember(Name="messageVersion", EmitDefaultValue=false, IsRequired=false, Order=65541)]
		private int messageVersion;

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

		public long FromSequenceNumber
		{
			get
			{
				return this.fromSequenceNumber;
			}
			set
			{
				this.fromSequenceNumber = value;
			}
		}

		public int MessageCount
		{
			get
			{
				return this.messageCount;
			}
			set
			{
				this.messageCount = value;
			}
		}

		public int MessageVersion
		{
			get
			{
				if (this.messageVersion == 0)
				{
					return BrokeredMessage.MessageVersion1;
				}
				return this.messageVersion;
			}
			set
			{
				this.messageVersion = value;
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

		public TimeSpan Timeout
		{
			get
			{
				return this.timeout;
			}
			set
			{
				this.timeout = value;
			}
		}

		public PeekCommand()
		{
		}
	}
}