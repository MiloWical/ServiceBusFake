using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	[DataContract(Name="MessageRenewLock", Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal class MessageRenewLockCommand : IExtensibleDataObject
	{
		[DataMember(Name="lockTokens", EmitDefaultValue=true, IsRequired=true, Order=65537)]
		private IEnumerable<Guid> lockTokens;

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

		public MessageRenewLockCommand()
		{
		}
	}
}