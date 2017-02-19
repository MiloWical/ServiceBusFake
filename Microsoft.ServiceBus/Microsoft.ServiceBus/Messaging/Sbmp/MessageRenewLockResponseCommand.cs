using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	[DataContract(Name="MessageRenewLockResponse", Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal class MessageRenewLockResponseCommand : IExtensibleDataObject
	{
		[DataMember(Name="lockedUntilUtc", EmitDefaultValue=true, IsRequired=false, Order=65537)]
		private IEnumerable<DateTime> lockedUntilUtcs;

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

		public IEnumerable<DateTime> LockedUntilUtcs
		{
			get
			{
				return this.lockedUntilUtcs;
			}
			set
			{
				this.lockedUntilUtcs = value;
			}
		}

		public MessageRenewLockResponseCommand()
		{
		}
	}
}