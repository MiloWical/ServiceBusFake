using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	[DataContract(Name="SessionRenewLockResponse", Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal class SessionRenewLockResponseCommand : IExtensibleDataObject
	{
		[DataMember(Name="lockedUntilUtc", EmitDefaultValue=true, IsRequired=false, Order=65537)]
		private DateTime lockedUntilUtc;

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

		public DateTime LockedUntilUtc
		{
			get
			{
				return this.lockedUntilUtc;
			}
			set
			{
				this.lockedUntilUtc = value;
			}
		}

		public SessionRenewLockResponseCommand()
		{
		}
	}
}