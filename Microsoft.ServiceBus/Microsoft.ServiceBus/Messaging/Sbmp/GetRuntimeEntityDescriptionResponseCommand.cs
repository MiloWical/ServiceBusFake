using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	[DataContract(Name="GetRuntimeEntityDescriptionResponse", Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal class GetRuntimeEntityDescriptionResponseCommand : IExtensibleDataObject
	{
		[DataMember(Name="EnablePartitioning", IsRequired=false, Order=65537, EmitDefaultValue=false)]
		private bool? internalEnablePartitioning;

		[DataMember(Name="RequiresDuplicateDetection", IsRequired=false, Order=65538, EmitDefaultValue=false)]
		private bool? internalRequiresDuplicateDetection;

		[DataMember(Name="PartitionCount", IsRequired=false, Order=65539, EmitDefaultValue=false)]
		private short? internalPartitionCount;

		[DataMember(Name="RequiresSession", IsRequired=false, Order=65540, EmitDefaultValue=false)]
		private bool? internalRequiresSession;

		[DataMember(Name="EnableSubscriptionPartitioning", IsRequired=false, Order=65541, EmitDefaultValue=false)]
		private bool? internalEnableSubscriptionPartitioning;

		private ExtensionDataObject extensionData;

		public bool EnablePartitioning
		{
			get
			{
				return this.internalEnablePartitioning.GetValueOrDefault();
			}
			set
			{
				this.internalEnablePartitioning = new bool?(value);
			}
		}

		public bool EnableSubscriptionPartitioning
		{
			get
			{
				return this.internalEnableSubscriptionPartitioning.GetValueOrDefault();
			}
			set
			{
				this.internalEnableSubscriptionPartitioning = new bool?(value);
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

		public short PartitionCount
		{
			get
			{
				return this.internalPartitionCount.GetValueOrDefault();
			}
			set
			{
				this.internalPartitionCount = new short?(value);
			}
		}

		public bool RequiresDuplicateDetection
		{
			get
			{
				return this.internalRequiresDuplicateDetection.GetValueOrDefault();
			}
			set
			{
				this.internalRequiresDuplicateDetection = new bool?(value);
			}
		}

		public bool RequiresSession
		{
			get
			{
				return this.internalRequiresSession.GetValueOrDefault();
			}
			set
			{
				this.internalRequiresSession = new bool?(value);
			}
		}

		public GetRuntimeEntityDescriptionResponseCommand()
		{
		}
	}
}