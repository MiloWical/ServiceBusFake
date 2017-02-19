using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Name="EventHubDescription", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public sealed class EventHubDescription : EntityDescription, IResourceDescription
	{
		private string path;

		public AuthorizationRules Authorization
		{
			get
			{
				if (this.InternalAuthorization == null)
				{
					this.InternalAuthorization = new AuthorizationRules();
				}
				return this.InternalAuthorization;
			}
		}

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

		[DataMember(Name="AuthorizationRules", IsRequired=false, Order=1005, EmitDefaultValue=false)]
		internal AuthorizationRules InternalAuthorization
		{
			get;
			set;
		}

		[DataMember(Name="CreatedAt", IsRequired=false, Order=1009, EmitDefaultValue=false)]
		internal DateTime? InternalCreatedAt
		{
			get;
			set;
		}

		[DataMember(Name="MessageRetentionInDays", IsRequired=false, Order=1003, EmitDefaultValue=false)]
		internal long? InternalMessageRetentionInDays
		{
			get;
			set;
		}

		[DataMember(Name="PartitionCount", IsRequired=false, Order=1015, EmitDefaultValue=false)]
		internal int? InternalPartitionCount
		{
			get;
			set;
		}

		[DataMember(Name="Status", IsRequired=false, Order=1007, EmitDefaultValue=false)]
		internal EntityStatus? InternalStatus
		{
			get;
			set;
		}

		[DataMember(Name="UpdatedAt", IsRequired=false, Order=1010, EmitDefaultValue=false)]
		internal DateTime? InternalUpdatedAt
		{
			get;
			set;
		}

		[DataMember(Name="UserMetadata", IsRequired=false, Order=1012, EmitDefaultValue=false)]
		internal string InternalUserMetadata
		{
			get;
			set;
		}

		public long MessageRetentionInDays
		{
			get
			{
				long? internalMessageRetentionInDays = this.InternalMessageRetentionInDays;
				if (!internalMessageRetentionInDays.HasValue)
				{
					return (long)7;
				}
				return internalMessageRetentionInDays.GetValueOrDefault();
			}
			set
			{
				base.ThrowIfReadOnly();
				this.InternalMessageRetentionInDays = new long?(value);
			}
		}

		string Microsoft.ServiceBus.Messaging.IResourceDescription.CollectionName
		{
			get
			{
				return "EventHubs";
			}
		}

		public int PartitionCount
		{
			get
			{
				int? internalPartitionCount = this.InternalPartitionCount;
				if (!internalPartitionCount.HasValue)
				{
					return 16;
				}
				return internalPartitionCount.GetValueOrDefault();
			}
			set
			{
				base.ThrowIfReadOnly();
				if (value < 1)
				{
					throw Fx.Exception.ArgumentOutOfRange("PartitionCount", value, Resources.ValueMustBePositive);
				}
				this.InternalPartitionCount = new int?(value);
			}
		}

		public string[] PartitionIds
		{
			get
			{
				List<string> strs = new List<string>();
				for (int i = 0; i < this.PartitionCount; i++)
				{
					strs.Add(i.ToString(NumberFormatInfo.InvariantInfo));
				}
				return strs.ToArray();
			}
		}

		public string Path
		{
			get
			{
				return this.path;
			}
			set
			{
				base.ThrowIfReadOnly();
				if (string.IsNullOrWhiteSpace(value))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNullOrEmpty("Path");
				}
				this.path = value;
			}
		}

		internal override bool RequiresEncryption
		{
			get
			{
				return this.Authorization.RequiresEncryption;
			}
		}

		public EntityStatus Status
		{
			get
			{
				EntityStatus? internalStatus = this.InternalStatus;
				if (!internalStatus.HasValue)
				{
					return EntityStatus.Active;
				}
				return internalStatus.GetValueOrDefault();
			}
			set
			{
				this.InternalStatus = new EntityStatus?(value);
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

		public EventHubDescription(string path)
		{
			this.Path = path;
		}

		internal EventHubDescription()
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

		internal override void OverrideEntityStatus(EntityStatus status)
		{
			this.Status = status;
		}

		internal override void UpdateForVersion(ApiVersion version, EntityDescription existingDescription = null)
		{
			base.UpdateForVersion(version, existingDescription);
		}
	}
}