using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Name="ConsumerGroupDescription", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public sealed class ConsumerGroupDescription : EntityDescription, IResourceDescription
	{
		private string eventHubPath;

		private string name;

		public DateTime CreatedAt
		{
			get
			{
				DateTime? internalCreatedAt = this.InternalCreatedAt;
				if (!internalCreatedAt.HasValue)
				{
					return DateTime.MinValue;
				}
				return internalCreatedAt.GetValueOrDefault();
			}
		}

		internal bool EnableCheckpoint
		{
			get
			{
				bool? internalEnableCheckpoint = this.InternalEnableCheckpoint;
				if (!internalEnableCheckpoint.HasValue)
				{
					return false;
				}
				return internalEnableCheckpoint.GetValueOrDefault();
			}
			set
			{
				base.ThrowIfReadOnly();
				this.InternalEnableCheckpoint = new bool?(value);
			}
		}

		public string EventHubPath
		{
			get
			{
				return this.eventHubPath;
			}
			set
			{
				base.ThrowIfReadOnly();
				if (string.IsNullOrWhiteSpace(value))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNullOrEmpty("EventHubPath");
				}
				this.eventHubPath = value;
			}
		}

		[DataMember(Name="CreatedAt", IsRequired=false, Order=1001, EmitDefaultValue=false)]
		internal DateTime? InternalCreatedAt
		{
			get;
			set;
		}

		[DataMember(Name="EnableCheckpoint", IsRequired=false, Order=1003, EmitDefaultValue=false)]
		internal bool? InternalEnableCheckpoint
		{
			get;
			set;
		}

		[DataMember(Name="UpdatedAt", IsRequired=false, Order=1002, EmitDefaultValue=false)]
		internal DateTime? InternalUpdatedAt
		{
			get;
			set;
		}

		[DataMember(Name="UserMetadata", IsRequired=false, Order=1006, EmitDefaultValue=false)]
		internal string InternalUserMetadata
		{
			get;
			set;
		}

		string Microsoft.ServiceBus.Messaging.IResourceDescription.CollectionName
		{
			get
			{
				return "ConsumerGroups";
			}
		}

		public string Name
		{
			get
			{
				return this.name;
			}
			set
			{
				base.ThrowIfReadOnly();
				if (string.IsNullOrWhiteSpace(value))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNullOrEmpty("Name");
				}
				this.name = value;
			}
		}

		public DateTime UpdatedAt
		{
			get
			{
				DateTime? internalUpdatedAt = this.InternalUpdatedAt;
				if (!internalUpdatedAt.HasValue)
				{
					return DateTime.MinValue;
				}
				return internalUpdatedAt.GetValueOrDefault();
			}
		}

		public string UserMetadata
		{
			get
			{
				return this.InternalUserMetadata;
			}
			set
			{
				base.ThrowIfReadOnly();
				if (string.IsNullOrWhiteSpace(value))
				{
					this.InternalUserMetadata = null;
					return;
				}
				if (value.Length > 1024)
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentOutOfRange("UserMetadata", value.Length, SRClient.ArgumentOutOfRange(0, 1024));
				}
				this.InternalUserMetadata = value;
			}
		}

		public ConsumerGroupDescription(string eventHubPath, string consumerGroupName)
		{
			this.EventHubPath = eventHubPath;
			this.Name = consumerGroupName;
		}

		internal ConsumerGroupDescription()
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