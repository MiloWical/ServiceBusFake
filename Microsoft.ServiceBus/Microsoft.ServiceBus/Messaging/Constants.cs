using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.ServiceBus.Messaging
{
	internal static class Constants
	{
		private const string VersionYear = "2011";

		private const string VersionMonth = "06";

		private const string ServiceBusService = "servicebus";

		public const string Namespace = "http://schemas.microsoft.com/netservices/2011/06/servicebus";

		public const bool DefaultEnableDeadLetteringOnMessageExpiration = false;

		public const bool DefaultEnableDeadLetteringOnFilterEvaluationExceptions = true;

		public const bool DefaultEnableBatchedOperations = true;

		public const bool DefaultIsAnonymousAccessible = false;

		public const bool DefaultIsExpress = false;

		public const bool DefaultEnablePublicationIdentification = false;

		public const string DefaultOperationTimeoutString = "0.00:01:00.00";

		public const string DefaultBatchFlushIntervalString = "0.00:00:00.20";

		public const int MinimumSizeForCompression = 200;

		public const long DefaultMessageRetentionInDays = 7L;

		public const bool DefaultEnableCheckpoint = false;

		public const string NotificationHub = "NotificationHub";

		public const string Queue = "Queue";

		public const string Topic = "Topic";

		public const string VolatileTopic = "VolatileTopic";

		public const string EventHub = "EventHub";

		public const string ConsumerGroup = "ConsumerGroup";

		public const string ConsumerGroups = "ConsumerGroups";

		public const string Subscription = "Subscription";

		public const string Subscriptions = "Subscriptions";

		public const string Partitions = "Partitions";

		public const string Publishers = "Publishers";

		public const string Rule = "Rule";

		public const string AuthClaimType = "net.windows.servicebus.action";

		public const string ManageClaim = "Manage";

		public const string SendClaim = "Send";

		public const string ListenClaim = "Listen";

		public const int SupportedClaimsCount = 3;

		public const string ClaimSeparator = ",";

		public const string PathDelimiter = "/";

		public const string SubQueuePrefix = "$";

		public const string EntityDelimiter = "|";

		public const string EmptyEntityDelimiter = "||";

		public const string ColonDelimiter = ":";

		public const string PartDelimiter = ":";

		public const string DeadLetterQueueSuffix = "DeadLetterQueue";

		public const string DeadLetterQueueName = "$DeadLetterQueue";

		public const string RuleNameHeader = "RuleName";

		public const string DeadLetterReasonHeader = "DeadLetterReason";

		public const string DeadLetterErrorDescriptionHeader = "DeadLetterErrorDescription";

		public const string TransferQueueSuffix = "Transfer";

		public const string TransferQueueName = "$Transfer";

		public const string TTLExpiredExceptionType = "TTLExpiredException";

		public const string MaxDeliveryCountExceededExceptionType = "MaxDeliveryCountExceeded";

		public const string HeaderSizeExceededExceptionType = "HeaderSizeExceeded";

		public const string IsAnonymousAccessibleHeader = "X-MS-ISANONYMOUSACCESSIBLE";

		public const string ServiceBusSupplementartyAuthorizationHeaderName = "ServiceBusSupplementaryAuthorization";

		public const string ServiceBusDlqSupplementaryAuthorizationHeaderName = "ServiceBusDlqSupplementaryAuthorization";

		public const string ServiceBusOriginalViaProperty = "ServiceBusOriginalVia";

		public const string ParentLinkIdProperty = "ParentLinkId";

		public const string DisableOperationalLogConfigName = "disableOperationalLog";

		public const string TransactionWorkUnit = "TxnWorkUnit";

		public const string WorkUnitInfo = "WorkUnitInfo";

		public const string Identifier = "Identifier";

		public const string SequenceNumber = "SequenceNumber";

		public const string ContinuationTokenHeaderName = "x-ms-continuationtoken";

		public const string ContinuationTokenQueryName = "continuationtoken";

		public const string AuthorizationHeaderName = "Microsoft.Cloud.ServiceBus.HttpAutorizationhHeaders";

		public const string SystemScope = "sys";

		public const string UserScope = "user";

		public const int DefaultCompatibilityLevel = 20;

		public const int MaximumRuleActionStatements = 32;

		public const int MaximumSqlFilterStatementLength = 1024;

		public const int MaximumSqlRuleActionStatementLength = 1024;

		public const int MaximumLambdaExpressionNodeCount = 1024;

		public const int MaximumLambdaExpressionTreeDepth = 32;

		public const int MaxMessageIdLength = 128;

		public const int MaxSessionIdLength = 128;

		public const int MaxPartitionKeyLength = 128;

		public const int MaxDestinationLength = 128;

		public const int MaxJobIdLength = 128;

		public const int QueueNameMaximumLength = 260;

		public const int TopicNameMaximumLength = 260;

		public const int EventHubNameMaximumLength = 260;

		public const int VolatileTopicNameMaximumLength = 260;

		public const int NotificationHubNameMaximumLength = 260;

		public const int ConsumerGroupNameMaximumLength = 50;

		public const int SubscriptionNameMaximumLength = 50;

		public const int PartitionNameMaximumLength = 50;

		public const int RuleNameMaximumLength = 50;

		public const string BrokerInvalidOperationPrefix = "BR0012";

		public const string InternalServiceFault = "InternalServiceFault";

		public const string ConnectionFailedFault = "ConnectionFailedFault";

		public const string EndpointNotFoundFault = "EndpointNotFoundFault";

		public const string AuthorizationFailedFault = "AuthorizationFailedFault";

		public const string NoTransportSecurityFault = "NoTransportSecurityFault";

		public const string QuotaExceededFault = "QuotaExceededFault";

		public const string PartitionNotOwnedFault = "PartitionNotOwnedException";

		public const string UndeterminableExceptionType = "UndeterminableExceptionType";

		public const string InvalidOperationFault = "InvalidOperationFault";

		public const string SessionLockLostFault = "SessionLockLostFault";

		public const string TimeoutFault = "TimeoutFault";

		public const string ArgumentFault = "ArgumentFault";

		public const string MessagingEntityDisabledFault = "MessagingEntityDisabledFault";

		public const string ServerBusyFault = "ServerBusyFault";

		public const int MaxSizeInMegabytes = 1024;

		public const int DefaultConnectionTimeoutInSeconds = 30;

		public const int MaximumUserMetadataLength = 1024;

		public const int MaxNotificationHubPathLength = 290;

		public const int DefaultMaxRetryCount = 10;

		public const string ExceptionHeaderConfigName = "ReadExceptionHeader";

		public const string ContainerConfigSectionName = "Container";

		public const string HttpErrorSubCodeFormatString = "SubCode={0}";

		public const long DefaultStartingCheckpoint = 0L;

		public const string BacklogQueueNameBaseFormatString = "{0}/x-servicebus-transfer/";

		public const string BacklogQueueNameFormatString = "{0}/x-servicebus-transfer/{1}";

		public const string BacklogQueueNameQueryString = "startswith(path, '{0}/x-servicebus-transfer/') eq true";

		public const string BacklogScheduledEnqueueTimeUtcProperty = "x-ms-scheduledenqueuetimeutc";

		public const string BacklogSessionIdProperty = "x-ms-sessionid";

		public const string BacklogTimeToLiveProperty = "x-ms-timetolive";

		public const string BacklogPathProperty = "x-ms-path";

		public const int BacklogBatchReceiveSize = 100;

		public const string BacklogPingMessageContentType = "application/vnd.ms-servicebus-ping";

		public const int BacklogQueueSize = 5120;

		public const string NullString = "(null)";

		public const string Batch = "-Batch";

		public const string Send = "Send";

		public const string TryReceive = "TryReceive";

		public const string SendBatch = "Send-Batch";

		public const string TryReceiveBatch = "TryReceive-Batch";

		public const string RenewLock = "RenewLock";

		public const string RenewLockBatch = "RenewLock-Batch";

		public const string Peek = "Peek";

		public const string PeekBatch = "Peek-Batch";

		public const string BadCommand = "BadCommand";

		public const string ScheduleMessage = "ScheduleMessage";

		public const string CancelScheduledMessage = "CancelScheduledMessage";

		public const string HttpLocationHeaderName = "Location";

		public const long DefaultMaxSizeEventHub = 10995116277760L;

		public const int DefaultPartitionCount = 16;

		public const bool DefaultHubIsDisabled = false;

		public readonly static TimeSpan DefaultOperationTimeout;

		public readonly static TimeSpan MaxOperationTimeout;

		public readonly static TimeSpan TokenRequestOperationTimeout;

		public readonly static int ServicePointMaxIdleTimeMilliSeconds;

		public readonly static TimeSpan DefaultBatchFlushInterval;

		public readonly static int DefaultBatchFlushIntervalInMilliseconds;

		public readonly static int DefaultBatchSchedulerLevel1Threshold;

		public readonly static double DefaultUsedSpaceAlertPercentage;

		public readonly static TimeSpan DefaultLockDuration;

		public readonly static TimeSpan DefaultDuplicateDetectionHistoryExpiryDuration;

		public readonly static TimeSpan DefaultAllowedTimeToLive;

		public readonly static TimeSpan MaximumAllowedTimeToLive;

		public readonly static TimeSpan PartitionedEntityMaximumAllowedTimeToLive;

		public readonly static TimeSpan PartitionedEntityDefaultAllowedTimeToLive;

		public readonly static TimeSpan MinimumAllowedTimeToLive;

		public readonly static TimeSpan MinimumLockDuration;

		public readonly static TimeSpan MaximumLockDuration;

		public readonly static TimeSpan MaximumRenewBufferDuration;

		public readonly static TimeSpan DebuggingLockDuration;

		public readonly static TimeSpan MaximumAllowedIdleTimeoutForAutoDelete;

		public readonly static int MaximumTagSize;

		public static CursorType DefaultCursorType;

		public readonly static TimeSpan MaximumDuplicateDetectionHistoryTimeWindow;

		public readonly static TimeSpan MinimumDuplicateDetectionHistoryTimeWindow;

		public readonly static TimeSpan DefaultRetryDelay;

		public readonly static int DefaultRetryLimit;

		public readonly static int DefaultSqlFlushThreshold;

		public readonly static int FlushBatchThreshold;

		public readonly static int MaximumRequestSchedulerQueueDepth;

		public readonly static int MaximumBatchSchedulerQueueDepth;

		public readonly static int MaximumEntityNameLength;

		public readonly static int MaximumMessageHeaderPropertySize;

		public readonly static string ContainerShortName;

		public readonly static int DefaultClientPumpPrefetchCount;

		public readonly static int DefaultPrefetchCount;

		public readonly static int DefaultEventHubPrefetchCount;

		public readonly static int EventHubMinimumPrefetchCount;

		public readonly static int DefaultMessageSessionPrefetchCount;

		public readonly static int DefaultMaxDeliveryCount;

		public readonly static int MinAllowedMaxDeliveryCount;

		public readonly static int MaxAllowedMaxDeliveryCount;

		public readonly static long DefaultLastPeekedSequenceNumber;

		public readonly static TimeSpan DefaultRegistrationTtl;

		public readonly static TimeSpan MaximumRegistrationTtl;

		public readonly static TimeSpan MinimumRegistrationTtl;

		public readonly static string Windows;

		public readonly static string IssuedToken;

		public readonly static string Anonymous;

		public static List<string> SupportedClaims;

		public static List<string> SupportedSubQueueNames;

		public readonly static Type ConstantsType;

		public readonly static Type MessageType;

		public readonly static Type GuidType;

		public readonly static Type ObjectType;

		public readonly static MethodInfo NewGuid;

		public readonly static TimeSpan AutoDeleteOnIdleDefaultValue;

		public readonly static TimeSpan DefaultRetryMinBackoff;

		public readonly static TimeSpan DefaultRetryMaxBackoff;

		public readonly static TimeSpan DefaultRetryDeltaBackoff;

		public readonly static TimeSpan DefaultRetryTerminationBuffer;

		public readonly static TimeSpan DefaultPrimaryFailoverInterval;

		public readonly static TimeSpan MinPrimaryFailoverInterval;

		public readonly static TimeSpan MaxPrimaryFailoverInterval;

		public readonly static TimeSpan GetRuntimeEntityDescriptionTimeout;

		public readonly static TimeSpan GetRuntimeEntityDescriptionNonTransientSleepTimeout;

		public readonly static Uri AnonymousUri;

		public readonly static UriTemplate SubscriptionUriTemplate;

		public readonly static TimeSpan ClientPumpRenewLockTimeout;

		static Constants()
		{
			Constants.DefaultOperationTimeout = TimeSpan.FromMinutes(1);
			Constants.MaxOperationTimeout = TimeSpan.FromDays(1);
			Constants.TokenRequestOperationTimeout = TimeSpan.FromMinutes(3);
			Constants.ServicePointMaxIdleTimeMilliSeconds = 50000;
			Constants.DefaultBatchFlushInterval = TimeSpan.FromMilliseconds(20);
			Constants.DefaultBatchFlushIntervalInMilliseconds = 20;
			Constants.DefaultBatchSchedulerLevel1Threshold = 10;
			Constants.DefaultUsedSpaceAlertPercentage = 70;
			Constants.DefaultLockDuration = TimeSpan.FromSeconds(60);
			Constants.DefaultDuplicateDetectionHistoryExpiryDuration = TimeSpan.FromMinutes(10);
			Constants.DefaultAllowedTimeToLive = TimeSpan.MaxValue;
			Constants.MaximumAllowedTimeToLive = TimeSpan.MaxValue;
			Constants.PartitionedEntityMaximumAllowedTimeToLive = TimeSpan.MaxValue;
			Constants.PartitionedEntityDefaultAllowedTimeToLive = Constants.PartitionedEntityMaximumAllowedTimeToLive;
			Constants.MinimumAllowedTimeToLive = TimeSpan.FromSeconds(1);
			Constants.MinimumLockDuration = TimeSpan.FromSeconds(5);
			Constants.MaximumLockDuration = TimeSpan.FromMinutes(5);
			Constants.MaximumRenewBufferDuration = TimeSpan.FromSeconds(10);
			Constants.DebuggingLockDuration = TimeSpan.FromDays(1);
			Constants.MaximumAllowedIdleTimeoutForAutoDelete = TimeSpan.MaxValue;
			Constants.MaximumTagSize = 120;
			Constants.DefaultCursorType = CursorType.Server;
			Constants.MaximumDuplicateDetectionHistoryTimeWindow = TimeSpan.FromDays(7);
			Constants.MinimumDuplicateDetectionHistoryTimeWindow = TimeSpan.FromSeconds(20);
			Constants.DefaultRetryDelay = TimeSpan.FromSeconds(10);
			Constants.DefaultRetryLimit = 3;
			Constants.DefaultSqlFlushThreshold = 4500;
			Constants.FlushBatchThreshold = 100;
			Constants.MaximumRequestSchedulerQueueDepth = 15000;
			Constants.MaximumBatchSchedulerQueueDepth = 100000;
			Constants.MaximumEntityNameLength = 400;
			Constants.MaximumMessageHeaderPropertySize = 65535;
			Constants.ContainerShortName = ".";
			Constants.DefaultClientPumpPrefetchCount = 10;
			Constants.DefaultPrefetchCount = 0;
			Constants.DefaultEventHubPrefetchCount = 10000;
			Constants.EventHubMinimumPrefetchCount = 10;
			Constants.DefaultMessageSessionPrefetchCount = 0;
			Constants.DefaultMaxDeliveryCount = 10;
			Constants.MinAllowedMaxDeliveryCount = 1;
			Constants.MaxAllowedMaxDeliveryCount = 2147483647;
			Constants.DefaultLastPeekedSequenceNumber = (long)0;
			Constants.DefaultRegistrationTtl = TimeSpan.FromDays(90);
			Constants.MaximumRegistrationTtl = TimeSpan.FromDays(90);
			Constants.MinimumRegistrationTtl = TimeSpan.FromDays(1);
			Constants.Windows = "windows";
			Constants.IssuedToken = "issuedToken";
			Constants.Anonymous = "anonymous";
			Constants.SupportedClaims = new List<string>()
			{
				"Manage",
				"Send",
				"Listen"
			};
			Constants.SupportedSubQueueNames = new List<string>()
			{
				"$DeadLetterQueue"
			};
			Constants.ConstantsType = typeof(Constants);
			Constants.MessageType = typeof(BrokeredMessage);
			Constants.GuidType = typeof(Guid);
			Constants.ObjectType = typeof(object);
			Constants.NewGuid = Constants.GuidType.GetMethod("NewGuid", BindingFlags.Static | BindingFlags.Public);
			Constants.AutoDeleteOnIdleDefaultValue = Constants.DefaultAllowedTimeToLive;
			Constants.DefaultRetryMinBackoff = TimeSpan.FromSeconds(0);
			Constants.DefaultRetryMaxBackoff = TimeSpan.FromSeconds(30);
			Constants.DefaultRetryDeltaBackoff = TimeSpan.FromSeconds(3);
			Constants.DefaultRetryTerminationBuffer = TimeSpan.FromSeconds(5);
			Constants.DefaultPrimaryFailoverInterval = TimeSpan.FromMinutes(1);
			Constants.MinPrimaryFailoverInterval = TimeSpan.Zero;
			Constants.MaxPrimaryFailoverInterval = TimeSpan.FromMinutes(60);
			Constants.GetRuntimeEntityDescriptionTimeout = TimeSpan.FromHours(1);
			Constants.GetRuntimeEntityDescriptionNonTransientSleepTimeout = TimeSpan.FromMinutes(10);
			Constants.AnonymousUri = new Uri("http://www.w3.org/2005/08/addressing/anonymous");
			Constants.SubscriptionUriTemplate = new UriTemplate("/Subscriptions/{subscriptionName}/*", true);
			Constants.ClientPumpRenewLockTimeout = TimeSpan.FromMinutes(5);
		}
	}
}