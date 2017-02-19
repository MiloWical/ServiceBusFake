using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	[DataContract(Name="SessionRenewLock", Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal class SessionRenewLockCommand : IExtensibleDataObject
	{
		[DataMember(Name="sessionId", EmitDefaultValue=true, IsRequired=true, Order=65537)]
		private string sessionId;

		[DataMember(Name="timeout", EmitDefaultValue=true, IsRequired=false, Order=65538)]
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

		public SessionRenewLockCommand()
		{
		}
	}
}