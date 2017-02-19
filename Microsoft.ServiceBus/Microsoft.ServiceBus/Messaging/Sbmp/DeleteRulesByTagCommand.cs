using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	[DataContract(Name="DeleteRulesByTag", Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal class DeleteRulesByTagCommand : IExtensibleDataObject
	{
		ExtensionDataObject System.Runtime.Serialization.IExtensibleDataObject.ExtensionData
		{
			get;
			set;
		}

		[DataMember(Name="tag", EmitDefaultValue=false, IsRequired=true, Order=65537)]
		public string Tag
		{
			get;
			set;
		}

		[DataMember(Name="timeout", EmitDefaultValue=true, IsRequired=false, Order=65539)]
		public TimeSpan Timeout
		{
			get;
			set;
		}

		[DataMember(Name="transactionId", EmitDefaultValue=false, IsRequired=false, Order=65538)]
		public string TransactionId
		{
			get;
			set;
		}

		public DeleteRulesByTagCommand()
		{
		}
	}
}