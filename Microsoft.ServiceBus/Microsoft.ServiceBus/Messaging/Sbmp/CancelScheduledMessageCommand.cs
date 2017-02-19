using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	[DataContract(Name="CancelScheduledMessage", Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal class CancelScheduledMessageCommand
	{
		[DataMember(Name="SequenceNumbers", EmitDefaultValue=true, IsRequired=true, Order=65537)]
		private IEnumerable<long> sequenceNumbers;

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

		public IEnumerable<long> SequenceNumbers
		{
			get
			{
				return this.sequenceNumbers;
			}
			set
			{
				this.sequenceNumbers = value;
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

		public CancelScheduledMessageCommand()
		{
		}
	}
}