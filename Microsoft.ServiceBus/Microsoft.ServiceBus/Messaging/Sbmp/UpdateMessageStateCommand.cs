using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	[DataContract(Name="UpdateMessageState", Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal class UpdateMessageStateCommand : IExtensibleDataObject
	{
		[DataMember(Name="lockTokens", EmitDefaultValue=true, IsRequired=true, Order=65537)]
		private IEnumerable<Guid> lockTokens;

		[DataMember(Name="timeout", EmitDefaultValue=true, IsRequired=false, Order=65539)]
		private TimeSpan timeout;

		[DataMember(Name="transactionId", EmitDefaultValue=false, IsRequired=false, Order=65540)]
		private string transactionId;

		[DataMember(Name="deadLetterInfo", EmitDefaultValue=true, IsRequired=false, Order=65541)]
		private Microsoft.ServiceBus.Messaging.DeadLetterInfo deadLetterInfo;

		[DataMember(Name="propertiesToModify", EmitDefaultValue=true, IsRequired=false, Order=65542)]
		private IDictionary<string, object> propertiesToModify;

		private ExtensionDataObject extensionData;

		public Microsoft.ServiceBus.Messaging.DeadLetterInfo DeadLetterInfo
		{
			get
			{
				return this.deadLetterInfo;
			}
			set
			{
				this.deadLetterInfo = value;
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

		public IEnumerable<Guid> LockTokens
		{
			get
			{
				return this.lockTokens;
			}
			set
			{
				this.lockTokens = value;
			}
		}

		[DataMember(Name="messageDisposition", EmitDefaultValue=true, IsRequired=true, Order=65538)]
		private DispositionStatus messageDisposition
		{
			get;
			set;
		}

		public DispositionStatus MessageDisposition
		{
			get
			{
				return this.messageDisposition;
			}
			set
			{
				this.messageDisposition = value;
			}
		}

		public IDictionary<string, object> PropertiesToModify
		{
			get
			{
				return this.propertiesToModify;
			}
			set
			{
				this.propertiesToModify = value;
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

		public UpdateMessageStateCommand()
		{
		}
	}
}