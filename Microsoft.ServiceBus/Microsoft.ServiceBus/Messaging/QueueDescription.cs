using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Name="QueueDescription", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public sealed class QueueDescription : EntityDescription, IResourceDescription
	{
		public readonly static TimeSpan MessageTimeToLiveDefaultValue;

		private string path;

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
					return QueueDescription.MessageTimeToLiveDefaultValue;
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

		public bool EnableDeadLetteringOnMessageExpiration
		{
			get
			{
				bool? internalEnableDeadLetteringOnMessageExpiration = this.InternalEnableDeadLetteringOnMessageExpiration;
				if (!internalEnableDeadLetteringOnMessageExpiration.HasValue)
				{
					return false;
				}
				return internalEnableDeadLetteringOnMessageExpiration.GetValueOrDefault();
			}
			set
			{
				base.ThrowIfReadOnly();
				this.InternalEnableDeadLetteringOnMessageExpiration = new bool?(value);
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

		public string ForwardDeadLetteredMessagesTo
		{
			get
			{
				return this.InternalForwardDeadLetteredMessagesTo;
			}
			set
			{
				base.ThrowIfReadOnly();
				if (string.Equals(this.Path, value, StringComparison.CurrentCultureIgnoreCase))
				{
					throw Fx.Exception.AsError(new InvalidOperationException(SRCore.AutoForwardToSelf(this.path)), null);
				}
				this.InternalForwardDeadLetteredMessagesTo = value;
			}
		}

		public string ForwardTo
		{
			get
			{
				return this.InternalForwardTo;
			}
			set
			{
				base.ThrowIfReadOnly();
				if (string.Equals(this.Path, value, StringComparison.CurrentCultureIgnoreCase))
				{
					throw Fx.Exception.AsError(new InvalidOperationException(SRCore.AutoForwardToSelf(this.path)), null);
				}
				this.InternalForwardTo = value;
			}
		}

		[DataMember(Name="AccessedAt", IsRequired=false, Order=1021, EmitDefaultValue=false)]
		internal DateTime? InternalAccessedAt
		{
			get;
			set;
		}

		[DataMember(Name="AuthorizationRules", IsRequired=false, Order=1016, EmitDefaultValue=false)]
		internal AuthorizationRules InternalAuthorization
		{
			get;
			set;
		}

		[DataMember(Name="AutoDeleteOnIdle", IsRequired=false, Order=1025, EmitDefaultValue=false)]
		internal TimeSpan? InternalAutoDeleteOnIdle
		{
			get;
			set;
		}

		[DataMember(Name="EntityAvailabilityStatus", IsRequired=false, Order=1027, EmitDefaultValue=false)]
		internal EntityAvailabilityStatus? InternalAvailabilityStatus
		{
			get;
			set;
		}

		[DataMember(Name="CreatedAt", IsRequired=false, Order=1019, EmitDefaultValue=false)]
		internal DateTime? InternalCreatedAt
		{
			get;
			set;
		}

		[DataMember(Name="DefaultMessageTimeToLive", IsRequired=false, Order=1007, EmitDefaultValue=false)]
		internal TimeSpan? InternalDefaultMessageTimeToLive
		{
			get;
			set;
		}

		[DataMember(Name="DuplicateDetectionHistoryTimeWindow", IsRequired=false, Order=1009, EmitDefaultValue=false)]
		internal TimeSpan? InternalDuplicateDetectionHistoryTimeWindow
		{
			get;
			set;
		}

		[DataMember(Name="EnableBatchedOperations", IsRequired=false, Order=1011, EmitDefaultValue=false)]
		internal bool? InternalEnableBatchedOperations
		{
			get;
			set;
		}

		[DataMember(Name="DeadLetteringOnMessageExpiration", IsRequired=false, Order=1008, EmitDefaultValue=false)]
		internal bool? InternalEnableDeadLetteringOnMessageExpiration
		{
			get;
			set;
		}

		[DataMember(Name="EnableExpress", IsRequired=false, Order=1029, EmitDefaultValue=false)]
		internal bool? InternalEnableExpress
		{
			get;
			set;
		}

		[DataMember(Name="EnablePartitioning", IsRequired=false, Order=1026, EmitDefaultValue=false)]
		internal bool? InternalEnablePartitioning
		{
			get;
			set;
		}

		[DataMember(Name="ForwardDeadLetteredMessagesTo", IsRequired=false, Order=1028, EmitDefaultValue=false)]
		internal string InternalForwardDeadLetteredMessagesTo
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

		internal bool? InternalIsExpress
		{
			get;
			set;
		}

		[DataMember(Name="LockDuration", IsRequired=false, Order=1002, EmitDefaultValue=false)]
		internal TimeSpan? InternalLockDuration
		{
			get;
			set;
		}

		[DataMember(Name="MaxDeliveryCount", IsRequired=false, Order=1010, EmitDefaultValue=false)]
		internal int? InternalMaxDeliveryCount
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

		[DataMember(Name="MessageCount", IsRequired=false, Order=1013, EmitDefaultValue=false)]
		internal long? InternalMessageCount
		{
			get;
			set;
		}

		[DataMember(Name="CountDetails", IsRequired=false, Order=1024, EmitDefaultValue=false)]
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

		[DataMember(Name="RequiresSession", IsRequired=false, Order=1006, EmitDefaultValue=false)]
		internal bool? InternalRequiresSession
		{
			get;
			set;
		}

		[DataMember(Name="SizeInBytes", IsRequired=false, Order=1012, EmitDefaultValue=false)]
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

		[DataMember(Name="SupportOrdering", IsRequired=false, Order=1023, EmitDefaultValue=false)]
		internal bool? InternalSupportOrdering
		{
			get;
			set;
		}

		[DataMember(Name="UpdatedAt", IsRequired=false, Order=1020, EmitDefaultValue=false)]
		internal DateTime? InternalUpdatedAt
		{
			get;
			set;
		}

		[DataMember(Name="UserMetadata", IsRequired=false, Order=1022, EmitDefaultValue=false)]
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

		public TimeSpan LockDuration
		{
			get
			{
				TimeSpan? internalLockDuration = this.InternalLockDuration;
				if (!internalLockDuration.HasValue)
				{
					return Constants.DefaultLockDuration;
				}
				return internalLockDuration.GetValueOrDefault();
			}
			set
			{
				base.ThrowIfReadOnly();
				TimeoutHelper.ThrowIfNonPositiveArgument(value, "LockDuration");
				this.InternalLockDuration = new TimeSpan?(value);
			}
		}

		public int MaxDeliveryCount
		{
			get
			{
				int? internalMaxDeliveryCount = this.InternalMaxDeliveryCount;
				if (!internalMaxDeliveryCount.HasValue)
				{
					return Constants.DefaultMaxDeliveryCount;
				}
				return internalMaxDeliveryCount.GetValueOrDefault();
			}
			set
			{
				base.ThrowIfReadOnly();
				if (value < Constants.MinAllowedMaxDeliveryCount || value > Constants.MaxAllowedMaxDeliveryCount)
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentOutOfRange("MaxDeliveryCount", value, SRClient.ArgumentOutOfRange(Constants.MinAllowedMaxDeliveryCount, Constants.MaxAllowedMaxDeliveryCount));
				}
				this.InternalMaxDeliveryCount = new int?(value);
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

		public long MessageCount
		{
			get
			{
				long? internalMessageCount = this.InternalMessageCount;
				if (!internalMessageCount.HasValue)
				{
					return (long)0;
				}
				return internalMessageCount.GetValueOrDefault();
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
				return "Queues";
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

		public bool RequiresSession
		{
			get
			{
				bool? internalRequiresSession = this.InternalRequiresSession;
				if (!internalRequiresSession.HasValue)
				{
					return false;
				}
				return internalRequiresSession.GetValueOrDefault();
			}
			set
			{
				base.ThrowIfReadOnly();
				this.InternalRequiresSession = new bool?(value);
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

		public bool SupportOrdering
		{
			get
			{
				bool? internalSupportOrdering = this.InternalSupportOrdering;
				if (internalSupportOrdering.HasValue)
				{
					return internalSupportOrdering.GetValueOrDefault();
				}
				if (!this.EnablePartitioning)
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

		static QueueDescription()
		{
			QueueDescription.MessageTimeToLiveDefaultValue = Constants.DefaultAllowedTimeToLive;
		}

		internal QueueDescription()
		{
		}

		public QueueDescription(string path)
		{
			this.Path = path;
		}

		internal override bool IsValidForVersion(ApiVersion version)
		{
			if (!base.IsValidForVersion(version))
			{
				return false;
			}
			if (version < ApiVersion.Two && (this.InternalAuthorization != null || this.InternalIsAnonymousAccessible.HasValue || this.InternalStatus.HasValue || this.InternalForwardTo != null || this.InternalCreatedAt.HasValue || this.InternalUpdatedAt.HasValue || this.InternalAccessedAt.HasValue || this.InternalUserMetadata != null || this.InternalSupportOrdering.HasValue || this.InternalMessageCountDetails != null))
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
			if (version < ApiVersion.Six && (this.InternalEnablePartitioning.HasValue || this.InternalIsExpress.HasValue))
			{
				return false;
			}
			if (version < ApiVersion.Eight && this.InternalForwardDeadLetteredMessagesTo != null)
			{
				return false;
			}
			if (version < ApiVersion.Eight && this.InternalEnableExpress.HasValue)
			{
				return false;
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
			string internalForwardDeadLetteredMessagesTo;
			bool? internalEnableExpress;
			bool? internalEnablePartitioning;
			bool? internalIsExpress;
			EntityAvailabilityStatus? internalAvailabilityStatus;
			TimeSpan? internalAutoDeleteOnIdle;
			AuthorizationRules authorization;
			AuthorizationRules internalAuthorization;
			bool? internalIsAnonymousAccessible;
			EntityStatus? internalStatus;
			string internalForwardTo;
			DateTime? internalCreatedAt;
			DateTime? internalUpdatedAt;
			DateTime? internalAccessedAt;
			string internalUserMetadata;
			bool? internalSupportOrdering;
			Microsoft.ServiceBus.Messaging.MessageCountDetails internalMessageCountDetails;
			QueueDescription queueDescription = existingDescription as QueueDescription;
			base.UpdateForVersion(version, existingDescription);
			bool flag = false;
			if (version < ApiVersion.Two)
			{
				if (queueDescription == null)
				{
					internalAuthorization = null;
				}
				else
				{
					internalAuthorization = queueDescription.InternalAuthorization;
				}
				this.InternalAuthorization = internalAuthorization;
				flag = true;
				if (queueDescription == null)
				{
					internalIsAnonymousAccessible = null;
				}
				else
				{
					internalIsAnonymousAccessible = queueDescription.InternalIsAnonymousAccessible;
				}
				this.InternalIsAnonymousAccessible = internalIsAnonymousAccessible;
				if (queueDescription == null)
				{
					internalStatus = null;
				}
				else
				{
					internalStatus = queueDescription.InternalStatus;
				}
				this.InternalStatus = internalStatus;
				if (queueDescription == null)
				{
					internalForwardTo = null;
				}
				else
				{
					internalForwardTo = queueDescription.InternalForwardTo;
				}
				this.InternalForwardTo = internalForwardTo;
				if (queueDescription == null)
				{
					internalCreatedAt = null;
				}
				else
				{
					internalCreatedAt = queueDescription.InternalCreatedAt;
				}
				this.InternalCreatedAt = internalCreatedAt;
				if (queueDescription == null)
				{
					internalUpdatedAt = null;
				}
				else
				{
					internalUpdatedAt = queueDescription.InternalUpdatedAt;
				}
				this.InternalUpdatedAt = internalUpdatedAt;
				if (queueDescription == null)
				{
					internalAccessedAt = null;
				}
				else
				{
					internalAccessedAt = queueDescription.InternalAccessedAt;
				}
				this.InternalAccessedAt = internalAccessedAt;
				if (queueDescription == null)
				{
					internalUserMetadata = null;
				}
				else
				{
					internalUserMetadata = queueDescription.InternalUserMetadata;
				}
				this.InternalUserMetadata = internalUserMetadata;
				if (queueDescription == null)
				{
					internalSupportOrdering = null;
				}
				else
				{
					internalSupportOrdering = queueDescription.InternalSupportOrdering;
				}
				this.InternalSupportOrdering = internalSupportOrdering;
				if (queueDescription == null)
				{
					internalMessageCountDetails = null;
				}
				else
				{
					internalMessageCountDetails = queueDescription.InternalMessageCountDetails;
				}
				this.InternalMessageCountDetails = internalMessageCountDetails;
			}
			if (version < ApiVersion.Three)
			{
				if (this.InternalAuthorization != null && !flag)
				{
					AuthorizationRules authorizationRules = this.InternalAuthorization;
					ApiVersion apiVersion = version;
					if (queueDescription == null)
					{
						authorization = null;
					}
					else
					{
						authorization = queueDescription.Authorization;
					}
					authorizationRules.UpdateForVersion(apiVersion, authorization);
				}
				if (this.Status == EntityStatus.ReceiveDisabled || this.Status == EntityStatus.SendDisabled)
				{
					this.Status = EntityStatus.Active;
				}
				if (queueDescription == null)
				{
					internalAutoDeleteOnIdle = null;
				}
				else
				{
					internalAutoDeleteOnIdle = queueDescription.InternalAutoDeleteOnIdle;
				}
				this.InternalAutoDeleteOnIdle = internalAutoDeleteOnIdle;
			}
			if (version < ApiVersion.Four)
			{
				if (queueDescription == null)
				{
					internalAvailabilityStatus = null;
				}
				else
				{
					internalAvailabilityStatus = queueDescription.InternalAvailabilityStatus;
				}
				this.InternalAvailabilityStatus = internalAvailabilityStatus;
			}
			if (version < ApiVersion.Six)
			{
				if (queueDescription == null)
				{
					internalEnablePartitioning = null;
				}
				else
				{
					internalEnablePartitioning = queueDescription.InternalEnablePartitioning;
				}
				this.InternalEnablePartitioning = internalEnablePartitioning;
				if (queueDescription == null)
				{
					internalIsExpress = null;
				}
				else
				{
					internalIsExpress = queueDescription.InternalIsExpress;
				}
				this.InternalIsExpress = internalIsExpress;
			}
			if (version < ApiVersion.Eight)
			{
				if (queueDescription == null)
				{
					internalForwardDeadLetteredMessagesTo = null;
				}
				else
				{
					internalForwardDeadLetteredMessagesTo = queueDescription.InternalForwardDeadLetteredMessagesTo;
				}
				this.InternalForwardDeadLetteredMessagesTo = internalForwardDeadLetteredMessagesTo;
				if (queueDescription == null)
				{
					internalEnableExpress = null;
				}
				else
				{
					internalEnableExpress = queueDescription.InternalEnableExpress;
				}
				this.InternalEnableExpress = internalEnableExpress;
			}
		}
	}
}