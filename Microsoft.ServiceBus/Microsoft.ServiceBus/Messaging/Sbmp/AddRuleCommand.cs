using Microsoft.ServiceBus.Messaging;
using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	[DataContract(Name="AddRule", Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal class AddRuleCommand : IExtensibleDataObject
	{
		[DataMember(Name="ruleName", EmitDefaultValue=true, IsRequired=true, Order=65537)]
		private string ruleName;

		[DataMember(Name="ruleDescription", EmitDefaultValue=true, IsRequired=true, Order=65539)]
		private Microsoft.ServiceBus.Messaging.RuleDescription ruleDescription;

		[DataMember(Name="transactionId", EmitDefaultValue=false, IsRequired=false, Order=65540)]
		private string transactionId;

		[DataMember(Name="timeout", EmitDefaultValue=true, IsRequired=false, Order=65541)]
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

		public Microsoft.ServiceBus.Messaging.RuleDescription RuleDescription
		{
			get
			{
				return this.ruleDescription;
			}
			set
			{
				this.ruleDescription = value;
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

		public AddRuleCommand()
		{
		}
	}
}