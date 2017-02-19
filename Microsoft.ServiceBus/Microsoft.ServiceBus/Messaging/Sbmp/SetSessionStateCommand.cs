using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	[DataContract(Name="SetSessionState", Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal class SetSessionStateCommand : IExtensibleDataObject
	{
		[DataMember(Name="sessionId", EmitDefaultValue=true, IsRequired=false, Order=65537)]
		private string sessionId;

		[DataMember(Name="sessionState", EmitDefaultValue=true, IsRequired=false, Order=65538)]
		private Microsoft.ServiceBus.Messaging.Sbmp.SessionState sessionState;

		[DataMember(Name="timeout", EmitDefaultValue=true, IsRequired=false, Order=65539)]
		private TimeSpan timeout;

		[DataMember(Name="transactionId", EmitDefaultValue=false, IsRequired=false, Order=65540)]
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

		public SetSessionStateCommand()
		{
		}
	}
}