using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	[DataContract(Name="CompleteTransaction", Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal class CompleteTransactionCommand : IExtensibleDataObject
	{
		[DataMember(Name="transactionId", EmitDefaultValue=true, IsRequired=true, Order=65537)]
		private string transactionId;

		[DataMember(Name="commit", EmitDefaultValue=true, IsRequired=true, Order=65538)]
		private bool commit;

		[DataMember(Name="timeout", EmitDefaultValue=true, IsRequired=false, Order=65539)]
		private TimeSpan timeout;

		private ExtensionDataObject extensionData;

		public bool Commit
		{
			get
			{
				return this.commit;
			}
			set
			{
				this.commit = value;
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

		public CompleteTransactionCommand()
		{
		}
	}
}