using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	[DataContract(Name="ScheduleMessage", Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal class ScheduleMessageCommand
	{
		[DataMember(Name="messages", EmitDefaultValue=true, IsRequired=true, Order=65537)]
		private MessageCollection messages;

		[DataMember(Name="timeout", EmitDefaultValue=true, IsRequired=false, Order=65538)]
		private TimeSpan timeout;

		[DataMember(Name="transactionId", EmitDefaultValue=false, IsRequired=false, Order=65539)]
		private string transactionId;

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

		public string TransactionId
		{
			get
			{
				return this.transactionId;
			}
			set
			{
				this.transactionId = value;
			}
		}

		public ScheduleMessageCommand()
		{
		}
	}
}