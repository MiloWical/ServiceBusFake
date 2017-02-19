using System;
using System.Runtime.Serialization;
using System.Transactions;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	[DataContract(Name="CompleteTransactionResponse", Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal class CompleteTransactionResponseCommand : IExtensibleDataObject
	{
		[DataMember(Name="commit", EmitDefaultValue=true, IsRequired=true, Order=65537)]
		private System.Transactions.TransactionStatus transactionStatus;

		[DataMember(Name="details", EmitDefaultValue=true, IsRequired=false, Order=65538)]
		private string details;

		private ExtensionDataObject extensionData;

		public string Details
		{
			get
			{
				return this.details;
			}
			set
			{
				this.details = value;
			}
		}

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

		public System.Transactions.TransactionStatus TransactionStatus
		{
			get
			{
				return this.transactionStatus;
			}
			set
			{
				this.transactionStatus = value;
			}
		}

		public CompleteTransactionResponseCommand()
		{
		}
	}
}