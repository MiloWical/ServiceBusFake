using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	[DataContract(Name="DeleteRule", Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal class DeleteRuleCommand : IExtensibleDataObject
	{
		[DataMember(Name="ruleName", EmitDefaultValue=true, IsRequired=true, Order=65537)]
		private string ruleName;

		[DataMember(Name="transactionId", EmitDefaultValue=false, IsRequired=false, Order=65538)]
		private string transactionId;

		[DataMember(Name="timeout", EmitDefaultValue=true, IsRequired=false, Order=65539)]
		private TimeSpan timeout;

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

		public string RuleName
		{
			get
			{
				return this.ruleName;
			}
			set
			{
				this.ruleName = value;
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

		public DeleteRuleCommand()
		{
		}
	}
}