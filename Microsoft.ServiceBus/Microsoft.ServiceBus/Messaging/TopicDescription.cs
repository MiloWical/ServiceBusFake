using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Name="TopicDescription", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public sealed class TopicDescription : EntityDescription, IResourceDescription
	{
		public readonly static TimeSpan MessageTimeToLiveDefaultValue;

		private string path;

		[DataMember(Name="AuthorizationRules", IsRequired=false, Order=1016, EmitDefaultValue=false)]
		internal AuthorizationRules InternalAuthorization;

		public DateTime AccessedAt
		{
			get
			{
				DateTime? internalAccessedAt = this.InternalAccessedAt;
				if (!internalAccessedAt.HasValue)
				{
					return DateTime.MinValue;
				}
				return internalAccessedAt.GetValueOrDefault();
			}
		}

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

		public TimeSpan AutoDeleteOnIdle
		{
			get
			{
				TimeSpan? internalAutoDeleteOnIdle = this.InternalAutoDeleteOnIdle;
				if (!internalAutoDeleteOnIdle.HasValue)
				{
					return Constants.AutoDeleteOnIdleDefaultValue;
				}
				return internalAutoDeleteOnIdle.GetValueOrDefault();
			}
			set
			{
				base.ThrowIfReadOnly();
				this.InternalAutoDeleteOnIdle = new TimeSpan?(value);
			}
		}

		public EntityAvailabilityStatus AvailabilityStatus
		{
			get
			{
				EntityAvailabilityStatus? internalAvailabilityStatus = this.InternalAvailabilityStatus;
				if (!internalAvailabilityStatus.HasValue)
				{
					return EntityAvailabilityStatus.Unknown;
				}
				return internalAvailabilityStatus.GetValueOrDefault();
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

		public TimeSpan DefaultMessageTimeToLive
		{
			get
			{
				TimeSpan? internalDefaultMessageTimeToLive = this.InternalDefaultMessageTimeToLive;
				if (!internalDefaultMessageTimeToLive.HasValue)
				{
					return TopicDescription.MessageTimeToLiveDefaultValue;
				}
				return internalDefaultMessageTimeToLive.GetValueOrDefault();
			}
			set
			{
				base.ThrowIfReadOnly();
				if (value < Constants.MinimumAllowedTimeToLive || value > Constants.MaximumAllowedTimeToLive)
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentOutOfRange("DefaultMessageTimeToLive", value, SRClient.ArgumentOutOfRange(Constants.MinimumAllowedTimeToLive, Constants.MaximumAllowedTimeToLive));
				}
				this.InternalDefaultMessageTimeToLive = new TimeSpan?(value);
			}
		}

		public TimeSpan DuplicateDetectionHistoryTimeWindow
		{
			get
			{
				TimeSpan? internalDuplicateDetectionHistoryTimeWindow = this.InternalDuplicateDetectionHistoryTimeWindow;
				if (!internalDuplicateDetectionHistoryTimeWindow.HasValue)
				{
					return Constants.DefaultDuplicateDetectionHistoryExpiryDuration;
				}
				return internalDuplicateDetectionHistoryTimeWindow.GetValueOrDefault();
			}
			set
			{
				base.ThrowIfReadOnly();
				TimeoutHelper.ThrowIfNonPositiveArgument(value, "DuplicateDetectionHistoryTimeWindow");
				if (value > Constants.MaximumDuplicateDetectionHistoryTimeWindow)
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentOutOfRange("DuplicateDetectionHistoryTimeWindow", value, SRClient.ArgumentOutOfRange(Constants.MinimumDuplicateDetectionHistoryTimeWindow, Constants.MaximumDuplicateDetectionHistoryTimeWindow));
				}
				if (value < Constants.MinimumDuplicateDetectionHistoryTimeWindow)
				{
					this.InternalDuplicateDetectionHistoryTimeWindow = new TimeSpan?(Constants.MinimumDuplicateDetectionHistoryTimeWindow);
					return;
				}
				this.InternalDuplicateDetectionHistoryTimeWindow = new TimeSpan?(value);
			}
		}

		public bool EnableBatchedOperations
		{
			get
			{
				bool? internalEnableBatchedOperations = this.InternalEnableBatchedOperations;
				if (!internalEnableBatchedOperations.HasValue)
				{
					return true;
				}
				return internalEnableBatchedOperations.GetValueOrDefault();
			}
			set
			{
				base.ThrowIfReadOnly();
				this.InternalEnableBatchedOperations = new bool?(value);
			}
		}

		public bool EnableExpress
		{
			get
			{
				bool? internalEnableExpress = this.InternalEnableExpress;
				if (!internalEnableExpress.HasValue)
				{
					return false;
				}
				return internalEnableExpress.GetValueOrDefault();
			}
			set
			{
				base.ThrowIfReadOnly();
				this.InternalEnableExpress = new bool?(value);
			}
		}

		public bool EnableFilteringMessagesBeforePublishing
		{
			get
			{
				bool? internalEnableFilteringMessagesBeforePublishing = this.InternalEnableFilteringMessagesBeforePublishing;
				if (!internalEnableFilteringMessagesBeforePublishing.HasValue)
				{
					return false;
				}
				return internalEnableFilteringMessagesBeforePublishing.GetValueOrDefault();
			}
			set
			{
				base.ThrowIfReadOnly();
				this.InternalEnableFilteringMessagesBeforePublishing = new bool?(value);
			}
		}

		public bool EnablePartitioning
		{
			get
			{
				bool? internalEnablePartitioning = this.InternalEnablePartitioning;
				if (!internalEnablePartitioning.HasValue)
				{
					return false;
				}
				return internalEnablePartitioning.GetValueOrDefault();
			}
			set
			{
				base.ThrowIfReadOnly();
				this.InternalEnablePartitioning = new bool?(value);
			}
		}

		internal bool EnableSubscriptionPartitioning
		{
			get
			{
				bool? internalEnableSubscriptionPartitioning = this.InternalEnableSubscriptionPartitioning;
				if (!internalEnableSubscriptionPartitioning.HasValue)
				{
					return false;
				}
				return internalEnableSubscriptionPartitioning.GetValueOrDefault();
			}
			set
			{
				base.ThrowIfReadOnly();
				this.InternalEnableSubscriptionPartitioning = new bool?(value);
			}
		}

		[DataMember(Name="AccessedAt", IsRequired=false, Order=1022, EmitDefaultValue=false)]
		internal DateTime? InternalAccessedAt
		{
			get;
			set;
		}

		[DataMember(Name="AutoDeleteOnIdle", IsRequired=false, Order=1027, EmitDefaultValue=false)]
		internal TimeSpan? InternalAutoDeleteOnIdle
		{
			get;
			set;
		}

		[DataMember(Name="EntityAvailabilityStatus", IsRequired=false, Order=1030, EmitDefaultValue=false)]
		internal EntityAvailabilityStatus? InternalAvailabilityStatus
		{
			get;
			set;
		}

		[DataMember(Name="CreatedAt", IsRequired=false, Order=1020, EmitDefaultValue=false)]
		internal DateTime? InternalCreatedAt
		{
			get;
			set;
		}

		[DataMember(Name="DefaultMessageTimeToLive", IsRequired=false, Order=1002, EmitDefaultValue=false)]
		internal TimeSpan? InternalDefaultMessageTimeToLive
		{
			get;
			set;
		}

		[DataMember(Name="DuplicateDetectionHistoryTimeWindow", IsRequired=false, Order=1006, EmitDefaultValue=false)]
		internal TimeSpan? InternalDuplicateDetectionHistoryTimeWindow
		{
			get;
			set;
		}

		[DataMember(Name="EnableBatchedOperations", IsRequired=false, Order=1007, EmitDefaultValue=false)]
		internal bool? InternalEnableBatchedOperations
		{
			get;
			set;
		}

		[DataMember(Name="EnableExpress", IsRequired=false, Order=1032, EmitDefaultValue=false)]
		internal bool? InternalEnableExpress
		{
			get;
			set;
		}

		[DataMember(Name="FilteringMessagesBeforePublishing", IsRequired=false, Order=1014, EmitDefaultValue=false)]
		internal bool? InternalEnableFilteringMessagesBeforePublishing
		{
			get;
			set;
		}

		[DataMember(Name="EnablePartitioning", IsRequired=false, Order=1028, EmitDefaultValue=false)]
		internal bool? InternalEnablePartitioning
		{
			get;
			set;
		}

		[DataMember(Name="EnableSubscriptionPartitioning", IsRequired=false, Order=1031, EmitDefaultValue=false)]
		internal bool? InternalEnableSubscriptionPartitioning
		{
			get;
			set;
		}

		[DataMember(Name="ForwardTo", IsRequired=false, Order=1018, EmitDefaultValue=false)]
		internal string InternalForwardTo
		{
			get;
			set;
		}

		[DataMember(Name="IsAnonymousAccessible", IsRequired=false, Order=1015, EmitDefaultValue=false)]
		internal bool? InternalIsAnonymousAccessible
		{
			get;
			set;
		}

		[DataMember(Name="IsExpress", IsRequired=false, Order=1029, EmitDefaultValue=false)]
		internal bool? InternalIsExpress
		{
			get;
			set;
		}

		[DataMember(Name="MaxSizeInMegabytes", IsRequired=false, Order=1004, EmitDefaultValue=false)]
		internal long? InternalMaxSizeInMegabytes
		{
			get;
			set;
		}

		[DataMember(Name="CountDetails", IsRequired=false, Order=1025, EmitDefaultValue=false)]
		internal Microsoft.ServiceBus.Messaging.MessageCountDetails InternalMessageCountDetails
		{
			get;
			set;
		}

		[DataMember(Name="RequiresDuplicateDetection", IsRequired=false, Order=1005, EmitDefaultValue=false)]
		internal bool? InternalRequiresDuplicateDetection
		{
			get;
			set;
		}

		[DataMember(Name="SizeInBytes", IsRequired=false, Order=1008, EmitDefaultValue=false)]
		internal long? InternalSizeInBytes
		{
			get;
			set;
		}

		[DataMember(Name="Status", IsRequired=false, Order=1017, EmitDefaultValue=false)]
		internal EntityStatus? InternalStatus
		{
			get;
			set;
		}

		[DataMember(Name="SubscriptionCount", IsRequired=false, Order=1026, EmitDefaultValue=false)]
		internal int? InternalSubscriptionCount
		{
			get;
			set;
		}

		[DataMember(Name="SupportOrdering", IsRequired=false, Order=1024, EmitDefaultValue=false)]
		internal bool? InternalSupportOrdering
		{
			get;
			set;
		}

		[DataMember(Name="UpdatedAt", IsRequired=false, Order=1021, EmitDefaultValue=false)]
		internal DateTime? InternalUpdatedAt
		{
			get;
			set;
		}

		[DataMember(Name="UserMetadata", IsRequired=false, Order=1023, EmitDefaultValue=false)]
		internal string InternalUserMetadata
		{
			get;
			set;
		}

		public bool IsAnonymousAccessible
		{
			get
			{
				bool? internalIsAnonymousAccessible = this.InternalIsAnonymousAccessible;
				if (!internalIsAnonymousAccessible.HasValue)
				{
					return false;
				}
				return internalIsAnonymousAccessible.GetValueOrDefault();
			}
			set
			{
				base.ThrowIfReadOnly();
				this.InternalIsAnonymousAccessible = new bool?(value);
			}
		}

		internal bool IsExpress
		{
			get
			{
				bool? internalIsExpress = this.InternalIsExpress;
				if (!internalIsExpress.HasValue)
				{
					return false;
				}
				return internalIsExpress.GetValueOrDefault();
			}
			set
			{
				base.ThrowIfReadOnly();
				this.InternalIsExpress = new bool?(value);
			}
		}

		public long MaxSizeInMegabytes
		{
			get
			{
				if (!this.InternalMaxSizeInMegabytes.HasValue)
				{
					return (long)1024;
				}
				return this.InternalMaxSizeInMegabytes.Value;
			}
			set
			{
				base.ThrowIfReadOnly();
				if (value < (long)0 || value > 8796093022207L)
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentOutOfRange("MaxSizeInMegabytes", value, SRClient.ArgumentOutOfRange(0, 8796093022207L));
				}
				this.InternalMaxSizeInMegabytes = new long?(value);
			}
		}

		public Microsoft.ServiceBus.Messaging.MessageCountDetails MessageCountDetails
		{
			get
			{
				if (this.InternalMessageCountDetails == null)
				{
					this.InternalMessageCountDetails = new Microsoft.ServiceBus.Messaging.MessageCountDetails();
				}
				return this.InternalMessageCountDetails;
			}
		}

		string Microsoft.ServiceBus.Messaging.IResourceDescription.CollectionName
		{
			get
			{
				return "Topics";
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

		public bool RequiresDuplicateDetection
		{
			get
			{
				bool? internalRequiresDuplicateDetection = this.InternalRequiresDuplicateDetection;
				if (!internalRequiresDuplicateDetection.HasValue)
				{
					return false;
				}
				return internalRequiresDuplicateDetection.GetValueOrDefault();
			}
			set
			{
				base.ThrowIfReadOnly();
				this.InternalRequiresDuplicateDetection = new bool?(value);
			}
		}

		internal override bool RequiresEncryption
		{
			get
			{
				return this.Authorization.RequiresEncryption;
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

		public int SubscriptionCount
		{
			get
			{
				int? internalSubscriptionCount = this.InternalSubscriptionCount;
				if (!internalSubscriptionCount.HasValue)
				{
					return 0;
				}
				return internalSubscriptionCount.GetValueOrDefault();
			}
		}

		public bool SupportOrdering
		{
			get
			{
				bool? internalSupportOrdering = this.InternalSupportOrdering;
				if (internalSupportOrdering.HasValue)
				{
					return internalSupportOrdering.GetValueOrDefault();
				}
				if (!this.EnablePartitioning && !this.EnableSubscriptionPartitioning)
				{
					return true;
				}
				return false;
			}
			set
			{
				base.ThrowIfReadOnly();
				this.InternalSupportOrdering = new bool?(value);
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

		static TopicDescription()
		{
			TopicDescription.MessageTimeToLiveDefaultValue = Constants.DefaultAllowedTimeToLive;
		}

		internal TopicDescription()
		{
		}

		public TopicDescription(string path)
		{
			this.Path = path;
		}

		internal override bool IsValidForVersion(ApiVersion version)
		{
			if (!base.IsValidForVersion(version))
			{
				return false;
			}
			if (version < ApiVersion.Two && (this.InternalAuthorization != null || this.InternalEnableFilteringMessagesBeforePublishing.HasValue || this.InternalIsAnonymousAccessible.HasValue || this.InternalStatus.HasValue || this.InternalForwardTo != null || this.InternalCreatedAt.HasValue || this.InternalUpdatedAt.HasValue || this.InternalAccessedAt.HasValue || this.InternalUserMetadata != null || this.InternalSupportOrdering.HasValue || this.InternalMessageCountDetails != null || this.InternalSubscriptionCount.HasValue))
			{
				return false;
			}
			if (version < ApiVersion.Three)
			{
				if (this.InternalAuthorization != null)
				{
					this.InternalAuthorization.IsValidForVersion(version);
				}
				if (this.InternalAutoDeleteOnIdle.HasValue)
				{
					return false;
				}
				if (this.Status == EntityStatus.SendDisabled || this.Status == EntityStatus.ReceiveDisabled)
				{
					return false;
				}
			}
			if (version < ApiVersion.Four && this.InternalAvailabilityStatus.HasValue)
			{
				return false;
			}
			if (version < ApiVersion.Six && this.InternalEnablePartitioning.HasValue)
			{
				return false;
			}
			if (version < ApiVersion.Eight)
			{
				if (this.InternalIsExpress.HasValue)
				{
					return false;
				}
				if (this.InternalEnableSubscriptionPartitioning.HasValue)
				{
					return false;
				}
				if (this.InternalEnableExpress.HasValue)
				{
					return false;
				}
			}
			return true;
		}

		internal override void OverrideEntityAvailabilityStatus(EntityAvailabilityStatus availabilityStatus)
		{
			this.InternalAvailabilityStatus = new EntityAvailabilityStatus?(availabilityStatus);
		}

		internal override void OverrideEntityStatus(EntityStatus status)
		{
			this.Status = status;
		}

		internal override void UpdateForVersion(ApiVersion version, EntityDescription existingDescription = null)
		{
			bool? internalIsExpress;
			bool? internalEnableSubscriptionPartitioning;
			bool? internalEnableExpress;
			bool? internalEnablePartitioning;
			EntityAvailabilityStatus? internalAvailabilityStatus;
			TimeSpan? internalAutoDeleteOnIdle;
			AuthorizationRules internalAuthorization;
			AuthorizationRules authorizationRules;
			bool? internalEnableFilteringMessagesBeforePublishing;
			bool? internalIsAnonymousAccessible;
			EntityStatus? internalStatus;
			string internalForwardTo;
			DateTime? internalCreatedAt;
			DateTime? internalUpdatedAt;
			DateTime? internalAccessedAt;
			string internalUserMetadata;
			bool? internalSupportOrdering;
			Microsoft.ServiceBus.Messaging.MessageCountDetails internalMessageCountDetails;
			int? internalSubscriptionCount;
			TopicDescription topicDescription = existingDescription as TopicDescription;
			base.UpdateForVersion(version, existingDescription);
			bool flag = false;
			if (version < ApiVersion.Two)
			{
				if (topicDescription == null)
				{
					authorizationRules = null;
				}
				else
				{
					authorizationRules = topicDescription.InternalAuthorization;
				}
				this.InternalAuthorization = authorizationRules;
				flag = true;
				if (topicDescription == null)
				{
					internalEnableFilteringMessagesBeforePublishing = null;
				}
				else
				{
					internalEnableFilteringMessagesBeforePublishing = topicDescription.InternalEnableFilteringMessagesBeforePublishing;
				}
				this.InternalEnableFilteringMessagesBeforePublishing = internalEnableFilteringMessagesBeforePublishing;
				if (topicDescription == null)
				{
					internalIsAnonymousAccessible = null;
				}
				else
				{
					internalIsAnonymousAccessible = topicDescription.InternalIsAnonymousAccessible;
				}
				this.InternalIsAnonymousAccessible = internalIsAnonymousAccessible;
				if (topicDescription == null)
				{
					internalStatus = null;
				}
				else
				{
					internalStatus = topicDescription.InternalStatus;
				}
				this.InternalStatus = internalStatus;
				if (topicDescription == null)
				{
					internalForwardTo = null;
				}
				else
				{
					internalForwardTo = topicDescription.InternalForwardTo;
				}
				this.InternalForwardTo = internalForwardTo;
				if (topicDescription == null)
				{
					internalCreatedAt = null;
				}
				else
				{
					internalCreatedAt = topicDescription.InternalCreatedAt;
				}
				this.InternalCreatedAt = internalCreatedAt;
				if (topicDescription == null)
				{
					internalUpdatedAt = null;
				}
				else
				{
					internalUpdatedAt = topicDescription.InternalUpdatedAt;
				}
				this.InternalUpdatedAt = internalUpdatedAt;
				if (topicDescription == null)
				{
					internalAccessedAt = null;
				}
				else
				{
					internalAccessedAt = topicDescription.InternalAccessedAt;
				}
				this.InternalAccessedAt = internalAccessedAt;
				if (topicDescription == null)
				{
					internalUserMetadata = null;
				}
				else
				{
					internalUserMetadata = topicDescription.InternalUserMetadata;
				}
				this.InternalUserMetadata = internalUserMetadata;
				if (topicDescription == null)
				{
					internalSupportOrdering = null;
				}
				else
				{
					internalSupportOrdering = topicDescription.InternalSupportOrdering;
				}
				this.InternalSupportOrdering = internalSupportOrdering;
				if (topicDescription == null)
				{
					internalMessageCountDetails = null;
				}
				else
				{
					internalMessageCountDetails = topicDescription.InternalMessageCountDetails;
				}
				this.InternalMessageCountDetails = internalMessageCountDetails;
				if (topicDescription == null)
				{
					internalSubscriptionCount = null;
				}
				else
				{
					internalSubscriptionCount = topicDescription.InternalSubscriptionCount;
				}
				this.InternalSubscriptionCount = internalSubscriptionCount;
			}
			if (version < ApiVersion.Three)
			{
				if (this.InternalAuthorization != null && !flag)
				{
					AuthorizationRules internalAuthorization1 = this.InternalAuthorization;
					ApiVersion apiVersion = version;
					if (topicDescription == null)
					{
						internalAuthorization = null;
					}
					else
					{
						internalAuthorization = topicDescription.InternalAuthorization;
					}
					internalAuthorization1.UpdateForVersion(apiVersion, internalAuthorization);
				}
				if (this.Status == EntityStatus.ReceiveDisabled || this.Status == EntityStatus.SendDisabled)
				{
					this.Status = EntityStatus.Active;
				}
				if (topicDescription == null)
				{
					internalAutoDeleteOnIdle = null;
				}
				else
				{
					internalAutoDeleteOnIdle = topicDescription.InternalAutoDeleteOnIdle;
				}
				this.InternalAutoDeleteOnIdle = internalAutoDeleteOnIdle;
			}
			if (version < ApiVersion.Four)
			{
				if (topicDescription == null)
				{
					internalAvailabilityStatus = null;
				}
				else
				{
					internalAvailabilityStatus = topicDescription.InternalAvailabilityStatus;
				}
				this.InternalAvailabilityStatus = internalAvailabilityStatus;
			}
			if (version < ApiVersion.Six)
			{
				if (topicDescription == null)
				{
					internalEnablePartitioning = null;
				}
				else
				{
					internalEnablePartitioning = topicDescription.InternalEnablePartitioning;
				}
				this.InternalEnablePartitioning = internalEnablePartitioning;
			}
			if (version < ApiVersion.Eight)
			{
				if (topicDescription == null)
				{
					internalIsExpress = null;
				}
				else
				{
					internalIsExpress = topicDescription.InternalIsExpress;
				}
				this.InternalIsExpress = internalIsExpress;
				if (topicDescription == null)
				{
					internalEnableSubscriptionPartitioning = null;
				}
				else
				{
					internalEnableSubscriptionPartitioning = topicDescription.InternalEnableSubscriptionPartitioning;
				}
				this.InternalEnableSubscriptionPartitioning = internalEnableSubscriptionPartitioning;
				if (topicDescription == null)
				{
					internalEnableExpress = null;
				}
				else
				{
					internalEnableExpress = topicDescription.InternalEnableExpress;
				}
				this.InternalEnableExpress = internalEnableExpress;
			}
		}
	}
}