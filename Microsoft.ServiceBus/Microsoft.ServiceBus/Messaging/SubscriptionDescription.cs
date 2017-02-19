using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Name="SubscriptionDescription", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public sealed class SubscriptionDescription : EntityDescription, IResourceDescription
	{
		public readonly static TimeSpan MessageTimeToLiveDefaultValue;

		private string topicPath;

		private string name;

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
					return SubscriptionDescription.MessageTimeToLiveDefaultValue;
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

		[DataMember(Name="DefaultRuleDescription", IsRequired=false, Order=1008, EmitDefaultValue=false)]
		internal RuleDescription DefaultRuleDescription
		{
			get;
			set;
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

		public bool EnableDeadLetteringOnFilterEvaluationExceptions
		{
			get
			{
				bool? internalEnableDeadLetteringOnFilterEvaluationExceptions = this.InternalEnableDeadLetteringOnFilterEvaluationExceptions;
				if (!internalEnableDeadLetteringOnFilterEvaluationExceptions.HasValue)
				{
					return true;
				}
				return internalEnableDeadLetteringOnFilterEvaluationExceptions.GetValueOrDefault();
			}
			set
			{
				base.ThrowIfReadOnly();
				this.InternalEnableDeadLetteringOnFilterEvaluationExceptions = new bool?(value);
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

		public string ForwardDeadLetteredMessagesTo
		{
			get
			{
				return this.InternalForwardDeadLetteredMessagesTo;
			}
			set
			{
				base.ThrowIfReadOnly();
				if (string.Equals(this.TopicPath, value, StringComparison.CurrentCultureIgnoreCase))
				{
					throw Fx.Exception.AsError(new InvalidOperationException(SRCore.AutoForwardToSelf(this.TopicPath)), null);
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
				if (string.Equals(this.TopicPath, value, StringComparison.CurrentCultureIgnoreCase))
				{
					throw Fx.Exception.AsError(new InvalidOperationException(SRCore.AutoForwardToSelf(this.TopicPath)), null);
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

		[DataMember(Name="AutoDeleteOnIdle", IsRequired=false, Order=1025, EmitDefaultValue=false)]
		internal TimeSpan? InternalAutoDeleteOnIdle
		{
			get;
			set;
		}

		[DataMember(Name="EntityAvailabilityStatus", IsRequired=false, Order=1026, EmitDefaultValue=false)]
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

		[DataMember(Name="DefaultMessageTimeToLive", IsRequired=false, Order=1004, EmitDefaultValue=false)]
		internal TimeSpan? InternalDefaultMessageTimeToLive
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

		[DataMember(Name="DeadLetteringOnFilterEvaluationExceptions", IsRequired=false, Order=1006, EmitDefaultValue=false)]
		internal bool? InternalEnableDeadLetteringOnFilterEvaluationExceptions
		{
			get;
			set;
		}

		[DataMember(Name="DeadLetteringOnMessageExpiration", IsRequired=false, Order=1005, EmitDefaultValue=false)]
		internal bool? InternalEnableDeadLetteringOnMessageExpiration
		{
			get;
			set;
		}

		[DataMember(Name="ForwardDeadLetteredMessagesTo", IsRequired=false, Order=1024, EmitDefaultValue=false)]
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

		[DataMember(Name="MessageCount", IsRequired=false, Order=1009, EmitDefaultValue=false)]
		internal long? InternalMessageCount
		{
			get;
			set;
		}

		[DataMember(Name="CountDetails", IsRequired=false, Order=1023, EmitDefaultValue=false)]
		internal Microsoft.ServiceBus.Messaging.MessageCountDetails InternalMessageCountDetails
		{
			get;
			set;
		}

		[DataMember(Name="RequiresSession", IsRequired=false, Order=1003, EmitDefaultValue=false)]
		internal bool? InternalRequiresSession
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
				return "Subscriptions";
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

		public string TopicPath
		{
			get
			{
				return this.topicPath;
			}
			set
			{
				base.ThrowIfReadOnly();
				if (string.IsNullOrWhiteSpace(value))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNullOrEmpty("TopicPath");
				}
				this.topicPath = value;
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

		static SubscriptionDescription()
		{
			SubscriptionDescription.MessageTimeToLiveDefaultValue = Constants.DefaultAllowedTimeToLive;
		}

		public SubscriptionDescription(string topicPath, string subscriptionName)
		{
			this.TopicPath = topicPath;
			this.Name = subscriptionName;
		}

		internal SubscriptionDescription()
		{
		}

		internal override bool IsValidForVersion(ApiVersion version)
		{
			if (!base.IsValidForVersion(version))
			{
				return false;
			}
			if (version < ApiVersion.Two && (this.InternalStatus.HasValue || this.InternalForwardTo != null || this.InternalCreatedAt.HasValue || this.InternalUpdatedAt.HasValue || this.InternalAccessedAt.HasValue || this.InternalUserMetadata != null || this.InternalMessageCountDetails != null))
			{
				return false;
			}
			if (version < ApiVersion.Three)
			{
				if (this.InternalAutoDeleteOnIdle.HasValue)
				{
					return false;
				}
				if (this.Status == EntityStatus.SendDisabled)
				{
					return false;
				}
				return this.Status != EntityStatus.ReceiveDisabled;
			}
			if (version < ApiVersion.Four && this.InternalAvailabilityStatus.HasValue)
			{
				return false;
			}
			if (version < ApiVersion.Eight && this.InternalForwardDeadLetteredMessagesTo != null)
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
			EntityAvailabilityStatus? internalAvailabilityStatus;
			TimeSpan? internalAutoDeleteOnIdle;
			EntityStatus? internalStatus;
			string internalForwardTo;
			DateTime? internalCreatedAt;
			DateTime? internalUpdatedAt;
			DateTime? internalAccessedAt;
			string internalUserMetadata;
			Microsoft.ServiceBus.Messaging.MessageCountDetails internalMessageCountDetails;
			SubscriptionDescription subscriptionDescription = existingDescription as SubscriptionDescription;
			base.UpdateForVersion(version, existingDescription);
			if (version < ApiVersion.Two)
			{
				if (subscriptionDescription == null)
				{
					internalStatus = null;
				}
				else
				{
					internalStatus = subscriptionDescription.InternalStatus;
				}
				this.InternalStatus = internalStatus;
				if (subscriptionDescription == null)
				{
					internalForwardTo = null;
				}
				else
				{
					internalForwardTo = subscriptionDescription.InternalForwardTo;
				}
				this.InternalForwardTo = internalForwardTo;
				if (subscriptionDescription == null)
				{
					internalCreatedAt = null;
				}
				else
				{
					internalCreatedAt = subscriptionDescription.InternalCreatedAt;
				}
				this.InternalCreatedAt = internalCreatedAt;
				if (subscriptionDescription == null)
				{
					internalUpdatedAt = null;
				}
				else
				{
					internalUpdatedAt = subscriptionDescription.InternalUpdatedAt;
				}
				this.InternalUpdatedAt = internalUpdatedAt;
				if (subscriptionDescription == null)
				{
					internalAccessedAt = null;
				}
				else
				{
					internalAccessedAt = subscriptionDescription.InternalAccessedAt;
				}
				this.InternalAccessedAt = internalAccessedAt;
				if (subscriptionDescription == null)
				{
					internalUserMetadata = null;
				}
				else
				{
					internalUserMetadata = subscriptionDescription.InternalUserMetadata;
				}
				this.InternalUserMetadata = internalUserMetadata;
				if (subscriptionDescription == null)
				{
					internalMessageCountDetails = null;
				}
				else
				{
					internalMessageCountDetails = subscriptionDescription.InternalMessageCountDetails;
				}
				this.InternalMessageCountDetails = internalMessageCountDetails;
			}
			if (version < ApiVersion.Three)
			{
				if (this.Status == EntityStatus.ReceiveDisabled || this.Status == EntityStatus.SendDisabled)
				{
					this.Status = EntityStatus.Active;
				}
				if (subscriptionDescription == null)
				{
					internalAutoDeleteOnIdle = null;
				}
				else
				{
					internalAutoDeleteOnIdle = subscriptionDescription.InternalAutoDeleteOnIdle;
				}
				this.InternalAutoDeleteOnIdle = internalAutoDeleteOnIdle;
			}
			if (version < ApiVersion.Four)
			{
				if (subscriptionDescription == null)
				{
					internalAvailabilityStatus = null;
				}
				else
				{
					internalAvailabilityStatus = subscriptionDescription.InternalAvailabilityStatus;
				}
				this.InternalAvailabilityStatus = internalAvailabilityStatus;
			}
			if (version < ApiVersion.Eight)
			{
				if (subscriptionDescription == null)
				{
					internalForwardDeadLetteredMessagesTo = null;
				}
				else
				{
					internalForwardDeadLetteredMessagesTo = subscriptionDescription.InternalForwardDeadLetteredMessagesTo;
				}
				this.InternalForwardDeadLetteredMessagesTo = internalForwardDeadLetteredMessagesTo;
			}
		}
	}
}