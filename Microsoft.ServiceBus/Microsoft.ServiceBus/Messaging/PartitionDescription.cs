using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Name="PartitionDescription", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public sealed class PartitionDescription : EntityDescription, IResourceDescription
	{
		private string eventHubPath;

		private string partitionId;

		private string consumerGroupName;

		public long BeginSequenceNumber
		{
			get
			{
				long? internalBeginSequenceNumber = this.InternalBeginSequenceNumber;
				if (!internalBeginSequenceNumber.HasValue)
				{
					return (long)0;
				}
				return internalBeginSequenceNumber.GetValueOrDefault();
			}
		}

		public string ConsumerGroupName
		{
			get
			{
				return this.consumerGroupName;
			}
			internal set
			{
				base.ThrowIfReadOnly();
				if (string.IsNullOrWhiteSpace(value))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNullOrEmpty("ConsumerGroupName");
				}
				this.consumerGroupName = value;
			}
		}

		public long EndSequenceNumber
		{
			get
			{
				long? internalEndSequenceNumber = this.InternalEndSequenceNumber;
				if (!internalEndSequenceNumber.HasValue)
				{
					return (long)0;
				}
				return internalEndSequenceNumber.GetValueOrDefault();
			}
		}

		public string EventHubPath
		{
			get
			{
				return this.eventHubPath;
			}
			internal set
			{
				base.ThrowIfReadOnly();
				if (string.IsNullOrWhiteSpace(value))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNullOrEmpty("EventHubPath");
				}
				this.eventHubPath = value;
			}
		}

		public long IncomingBytesPerSecond
		{
			get
			{
				long? internalIncomingBytesPerSecond = this.InternalIncomingBytesPerSecond;
				if (!internalIncomingBytesPerSecond.HasValue)
				{
					return (long)0;
				}
				return internalIncomingBytesPerSecond.GetValueOrDefault();
			}
		}

		[DataMember(Name="BeginSequenceNumber", IsRequired=false, Order=1002, EmitDefaultValue=false)]
		internal long? InternalBeginSequenceNumber
		{
			get;
			set;
		}

		[DataMember(Name="EndSequenceNumber", IsRequired=false, Order=1003, EmitDefaultValue=false)]
		internal long? InternalEndSequenceNumber
		{
			get;
			set;
		}

		[DataMember(Name="IncomingBytesPerSecond", IsRequired=false, Order=1005, EmitDefaultValue=false)]
		internal long? InternalIncomingBytesPerSecond
		{
			get;
			set;
		}

		[DataMember(Name="LastCheckpoint", IsRequired=false, Order=1004, EmitDefaultValue=false)]
		internal string InternalLastCheckpoint
		{
			get;
			set;
		}

		[DataMember(Name="OutgoingBytesPerSecond", IsRequired=false, Order=1006, EmitDefaultValue=false)]
		internal long? InternalOutgoingBytesPerSecond
		{
			get;
			set;
		}

		[DataMember(Name="SizeInBytes", IsRequired=false, Order=1001, EmitDefaultValue=false)]
		internal long? InternalSizeInBytes
		{
			get;
			set;
		}

		internal string LastCheckpoint
		{
			get
			{
				return this.InternalLastCheckpoint;
			}
			set
			{
				base.ThrowIfReadOnly();
				if (string.IsNullOrWhiteSpace(value))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNullOrEmpty("LastCheckpoint");
				}
				this.InternalLastCheckpoint = value;
			}
		}

		string Microsoft.ServiceBus.Messaging.IResourceDescription.CollectionName
		{
			get
			{
				return "Partitions";
			}
		}

		public long OutgoingBytesPerSecond
		{
			get
			{
				long? internalOutgoingBytesPerSecond = this.InternalOutgoingBytesPerSecond;
				if (!internalOutgoingBytesPerSecond.HasValue)
				{
					return (long)0;
				}
				return internalOutgoingBytesPerSecond.GetValueOrDefault();
			}
		}

		public string PartitionId
		{
			get
			{
				return this.partitionId;
			}
			internal set
			{
				base.ThrowIfReadOnly();
				if (string.IsNullOrWhiteSpace(value))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNullOrEmpty("PartitionId");
				}
				this.partitionId = value;
			}
		}

		public long SizeInBytes
		{
			get
			{
				long? internalSizeInBytes = this.InternalSizeInBytes;
				if (!internalSizeInBytes.HasValue)
				{
					return (long)0;
				}
				return internalSizeInBytes.GetValueOrDefault();
			}
		}

		public PartitionDescription(string eventHubPath, string partitionId)
		{
			this.EventHubPath = eventHubPath;
			this.PartitionId = partitionId;
		}

		internal PartitionDescription()
		{
		}

		internal override bool IsValidForVersion(ApiVersion version)
		{
			if (!base.IsValidForVersion(version))
			{
				return false;
			}
			if (version < ApiVersion.Eight)
			{
				return false;
			}
			return true;
		}

		internal override void UpdateForVersion(ApiVersion version, EntityDescription existingDescription = null)
		{
			base.UpdateForVersion(version, existingDescription);
		}
	}
}