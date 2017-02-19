using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	[DataContract(Name="AutoRenewLockSessionForNamespaceResponse", Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal class ScaledReceiveHeaderInfo : IExtensibleDataObject
	{
		[DataMember(Name="sessionId", EmitDefaultValue=true, IsRequired=false, Order=65537)]
		private string sessionId;

		[DataMember(Name="LinkId", EmitDefaultValue=true, IsRequired=false, Order=65538)]
		private string linkId;

		[DataMember(Name="entityPath", EmitDefaultValue=true, IsRequired=false, Order=65539)]
		private string entityPath;

		[DataMember(Name="lockedUntilUtc", EmitDefaultValue=false, IsRequired=false, Order=65540)]
		private DateTime lockedUntilUtc;

		private ExtensionDataObject extensionData;

		public string EntityPath
		{
			get
			{
				return this.entityPath;
			}
			set
			{
				this.entityPath = value;
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

		public string LinkId
		{
			get
			{
				return this.linkId;
			}
			set
			{
				this.linkId = value;
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

		public ScaledReceiveHeaderInfo()
		{
		}
	}
}