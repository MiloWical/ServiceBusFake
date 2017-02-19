using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	[DataContract(Name="GetSessionState", Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal class GetSessionStateCommand : IExtensibleDataObject
	{
		[DataMember(Name="sessionId", EmitDefaultValue=true, IsRequired=true, Order=65537)]
		private string sessionId;

		[DataMember(Name="timeout", EmitDefaultValue=true, IsRequired=false, Order=65538)]
		private TimeSpan timeout;

		[DataMember(Name="transactionId", EmitDefaultValue=false, IsRequired=false, Order=65539)]
		private string transactionId;

		[DataMember(Name="isBrowseMode", EmitDefaultValue=false, IsRequired=false, Order=65540)]
		private bool isBrowseMode;

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

		public bool IsBrowseMode
		{
			get
			{
				return this.isBrowseMode;
			}
			set
			{
				this.isBrowseMode = value;
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

		public GetSessionStateCommand()
		{
		}
	}
}