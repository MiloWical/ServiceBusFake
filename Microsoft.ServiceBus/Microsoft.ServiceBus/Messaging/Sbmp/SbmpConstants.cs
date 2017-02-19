using System;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	internal static class SbmpConstants
	{
		public const string DefaultNamespaceUri = "http://schemas.microsoft.com/servicebus/2010/08/protocol/";

		public const string AppMessageIds = "appMessageIds";

		public const string Commit = "commit";

		public const string CompleteTransaction = "CompleteTransaction";

		public const string CompleteTransactionResponse = "CompleteTransactionResponse";

		public const string CreateTransaction = "CreateTransaction";

		public const string CreateNew = "createNew";

		public const string Details = "details";

		public const string SessionId = "sessionId";

		public const string Sessions = "sessions";

		public const string SessionState = "sessionState";

		public const string EntityName = "entityName";

		public const string InstanceHandle = "instanceHandle";

		public const string EntityPath = "entityPath";

		public const string IsSessionReceiver = "isSessionReceiver";

		public const string IsSessionBrowser = "isSessionBrowser";

		public const string ConnectionNeutral = "connectionNeutral";

		public const string IsBrowseMode = "isBrowseMode";

		public const string LinkId = "LinkId";

		public const string ConnectionId = "ConnectionId";

		public const string LinkInfo = "LinkInfo";

		public const string LinkType = "LinkType";

		public const string LockTokens = "lockTokens";

		public const string PrefetchCount = "prefetchCount";

		public const string MessageCount = "messageCount";

		public const string FromSequenceNumber = "fromSequenceNumber";

		public const string MessageVersion = "messageVersion";

		public const string MessageReceipts = "messageReceipts";

		public const string Messages = "messages";

		public const string MessageDisposition = "messageDisposition";

		public const string NamespaceName = "namespaceName";

		public const string PartitionKey = "partitionKey";

		public const string ReceiveMode = "receiveMode";

		public const string PartitionId = "partitionId";

		public const string EntityType = "entityType";

		public const string Result = "result";

		public const string RuleDescription = "ruleDescription";

		public const string RuleName = "ruleName";

		public const string SubscriptionName = "subscriptionName";

		public const string Tag = "tag";

		public const string Timeout = "timeout";

		public const string OperationTimeout = "operationTimeout";

		public const string LastUpdatedTime = "lastUpdatedTime";

		public const string Skip = "skip";

		public const string Top = "top";

		public const string TransactionId = "transactionId";

		public const string TransferDestinationEntityName = "transferDestinationEntityName";

		public const string TransferDestinationResourceId = "transferDestinationResourceId";

		public const string TransferDestinationInstanceHandle = "transferDestinationInstanceHandle";

		public const string TransferDestinationAuthorizationHeader = "transferDestinationAuthorizationHeader";

		public const string DeadLetterInfo = "deadLetterInfo";

		public const string DeadLetterReason = "deadLetterReason";

		public const string DeadLetterErrorDescription = "deadLetterErrorDescription";

		public const string PropertiesToModify = "propertiesToModify";

		public const string RedirectTo = "redirectTo";

		public const string ContainerNameResolutionMode = "containerNameResolutionMode";

		public const string EntityDelimiter = "|";

		public const string SessionLockDuration = "sessionLockDuration";

		public const string LockedUntilUtc = "lockedUntilUtc";

		public const string LockedUntilUtcs = "lockedUntilUtcs";

		public const string IsHttp = "isHttp";

		public const string FromOffset = "fromOffset";

		public const string FromTimestamp = "fromTimestamp";

		public const string SbmpConnectionName = "SbmpConnection";

		public const string SbmpConnectionFullName = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpConnection";

		public const string RuleSuffix = "Rule";

		public const string RulesByTagSuffix = "RulesByTag";

		public const string AddRule = "AddRule";

		public const string DeleteRule = "DeleteRule";

		public const string DeleteRulesByTag = "DeleteRulesByTag";

		public const string AddRuleAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpConnection/AddRule";

		public const string AddRuleResponseAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpConnection/AddRuleResponse";

		public const string CompleteTransactionAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpConnection/CompleteTransaction";

		public const string CompleteTransactionResponseAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpConnectionResponse";

		public const string CreateTransactionAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpConnection/CreateTransaction";

		public const string CreateTransactionResponseAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpConnectionResponse";

		public const string DeleteRuleAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpConnection/DeleteRule";

		public const string DeleteRuleResponseAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpConnection/DeleteRuleResponse";

		public const string DeleteRulesByTagAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpConnection/DeleteRulesByTag";

		public const string DeleteRulesByTagResponseAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpConnection/DeleteRulesByTagResponse";

		public const string SbmpRedirectName = "SbmpRedirect";

		public const string SbmpRedirectFullName = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpRedirect";

		public const string SbmpRedirect = "Redirect";

		public const string SbmpRedirectResponse = "RedirectResponse";

		public const string SbmpRedirectAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpRedirect/Redirect";

		public const string SbmpRedirectResponseAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpRedirect/RedirectResponse";

		public const string SbmpMessageReceiverName = "SbmpMessageReceiver";

		public const string SbmpMessageReceiverFullName = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageReceiver";

		public const string Abandon = "Abandon";

		public const string AbandonResponse = "AbandonResponse";

		public const string AbandonAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageReceiver/Abandon";

		public const string AbandonResponseAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageReceiver/AbandonResponse";

		public const string Initialize = "Initialize";

		public const string SbmpMessageBrowserName = "SbmpMessageBrowser";

		public const string SbmpMessageBrowserFullName = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageBrowser";

		public const string Peek = "Peek";

		public const string PeekResponse = "PeekResponse";

		public const string PeekAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageBrowser/Peek";

		public const string PeekResponseAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageBrowser/PeekResponse";

		public const string AcceptMessageSession = "AcceptMessageSession";

		public const string AcceptMessageSessionResponse = "AcceptMessageSessionResponse";

		public const string AcceptMessageSessionAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageReceiver/AcceptMessageSession";

		public const string AcceptMessageSessionResponseAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageReceiver/AcceptMessageSessionResponse";

		public const string GetMessageSessions = "GetMessageSessions";

		public const string GetMessageSessionsResponse = "GetMessageSessionsResponse";

		public const string GetMessageSessionsAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageReceiver/GetMessageSessions";

		public const string GetMessageSessionsResponseAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageReceiver/GetMessageSessionsResponse";

		public const string AcceptMessageSessionForNamespace = "AcceptMessageSessionForNamespace";

		public const string AcceptMessageSessionForNamespaceResponse = "AcceptMessageSessionForNamespaceResponse";

		public const string AcceptMessageSessionForNamespaceAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpConnection/AcceptMessageSessionForNamespace";

		public const string AcceptMessageSessionForNamespaceResponseAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpConnection/AcceptMessageSessionForNamespaceResponse";

		public const string CloseClient = "CloseClient";

		public const string CloseClientResponse = "CloseLinkResponse";

		public const string CloseClientAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpConnection/CloseClient";

		public const string CloseClientResponseAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpConnection/CloseLinkResponse";

		public const string AutoRenewLockSessionForNamespace = "AutoRenewLockSessionForNamespace";

		public const string AutoRenewLockSessionForNamespaceResponse = "AutoRenewLockSessionForNamespaceResponse";

		public const string CloseLink = "CloseLink";

		public const string CloseLinkResponse = "CloseLinkResponse";

		public const string CloseLinkAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpConnection/CloseLink";

		public const string CloseLinkResponseAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpConnection/CloseLinkResponse";

		public const string TryReceive = "TryReceive";

		public const string TryReceiveResponse = "TryReceiveResponse";

		public const string TryReceiveResult = "TryReceiveResult";

		public const string TryReceiveAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageReceiver/TryReceive";

		public const string TryReceiveResponseAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageReceiver/TryReceiveResponse";

		public const string UpdateMessageState = "UpdateMessageState";

		public const string UpdateMessageStateResponse = "UpdateMessageStateResponse";

		public const string UpdateMessageStateAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageReceiver/UpdateMessageState";

		public const string UpdateMessageStateResponseAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageReceiver/UpdateMessageStateResponse";

		public const string SessionRenewLock = "SessionRenewLock";

		public const string SessionRenewLockResponse = "SessionRenewLockResponse";

		public const string SessionRenewLockResult = "SessionRenewLockResult";

		public const string SessionRenewLockAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageReceiver/SessionRenewLock";

		public const string SessionRenewLockResponseAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageReceiver/SessionRenewLockResponse";

		public const string MessageRenewLock = "MessageRenewLock";

		public const string MessageRenewLockResponse = "MessageRenewLockResponse";

		public const string MessageRenewLockResult = "MessageRenewLockResult";

		public const string MessageRenewLockAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageReceiver/MessageRenewLock";

		public const string MessageRenewLockResponseAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageReceiver/MessageRenewLockResponse";

		public const string GetSessionState = "GetSessionState";

		public const string GetSessionStateResponse = "GetSessionStateResponse";

		public const string GetSessionStateResult = "GetSessionStateResult";

		public const string GetSessionStateAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageReceiver/GetSessionState";

		public const string GetSessionStateResponseAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageReceiver/GetSessionStateResponse";

		public const string SetSessionState = "SetSessionState";

		public const string SetSessionStateResponse = "SetSessionStateResponse";

		public const string SetSessionStateResult = "SetSessionStateResult";

		public const string SetSessionStateAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageReceiver/SetSessionState";

		public const string SetSessionStateResponseAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageReceiver/SetSessionStateResponse";

		public const string PingOperation = "Ping";

		public const string PingAction = "http://schemas.microsoft.com/servicebus/2010/08/protocol/Ping";

		public const string AuthorizationHeaderName = "Authorization";

		public const string SbmpMessageSenderName = "SbmpMessageSender";

		public const string SbmpMessageSenderFullName = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageSender";

		public const string Send = "Send";

		public const string SendResponse = "SendResponse";

		public const string SendAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageSender/Send";

		public const string SendResponseAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageSender/SendResponse";

		public const string SbmpMessageRecycleName = "SbmpChannelRecycle";

		public const string SbmpMessageRecycleFullName = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpChannelRecycle";

		public const string Recycle = "Recycle";

		public const string RecycleResponse = "RecycleResponse";

		public const string RecycleAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpChannelRecycle/Recycle";

		public const string RecycleResponseAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpChannelRecycle/RecycleResponse";

		public const string ChannelMapping = "ChannelMapping";

		public const string GetRuntimeEntityDescriptionName = "SbmpGetRuntimeEntityDescription";

		public const string GetRuntimeEntityDescriptionFullName = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpGetRuntimeEntityDescription";

		public const string GetRuntimeEntityDescription = "GetRuntimeEntityDescription";

		public const string GetRuntimeEntityDescriptionResponseCommand = "GetRuntimeEntityDescriptionResponse";

		public const string GetRuntimeEntityDescriptionAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpGetRuntimeEntityDescription/GetRuntimeEntityDescription";

		public const string GetRuntimeEntityDescriptionResponseAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpGetRuntimeEntityDescription/GetRuntimeEntityDescriptionResponse";

		public const string ScheduleMessage = "ScheduleMessage";

		public const string ScheduleMessageResponse = "ScheduleMessageResponse";

		public const string ScheduleMessageAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageSender/ScheduleMessage";

		public const string ScheduleMessageResponseAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageSender/ScheduleMessageResponse";

		public const string CancelScheduledMessage = "CancelScheduledMessage";

		public const string CancelScheduledMessageResponse = "CancelScheduledMessageResponse";

		public const string CancelScheduledMessageAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageSender/CancelScheduledMessage";

		public const string CancelScheduledMessageResponseAction = "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageSender/CancelScheduledMessageResponse";

		public const string FaultAction = "http://schemas.microsoft.com/net/2005/12/windowscommunicationfoundation/dispatcher/fault";

		public const string TraceScaleReceivesInfo = "TraceScaleReceivesInfo";

		public const string ScaleReceivesInfo = "ScaleReceivesInfo";

		public const string HeaderNamespace = "http://schemas.microsoft.com/netservices/2011/06/servicebus";

		public const string BrokeredMessageSessionIdHeaderName = "BrokeredMessageSessionId";

		public const string DistinctBrokeredMessageSessionIdsHeaderName = "DistinctBrokeredMessageSessionIds";

		public const string LockTokensHeaderName = "LockTokens";

		public const string LockedUntilUtcHeaderName = "LockedUntilUtc";

		public const string SequenceNumbersHeaderName = "SequenceNumbers";

		public const string FromSequenceNumberHeaderName = "FromSequenceNumber";

		public const string EndOfEntityHeaderName = "EndOfEntity";

		public const string BrokeredMessageIdsHeaderName = "BrokeredMessageIds";

		public const string MessagesCountHeaderName = "MessagesCount";

		public const string MessagesSizeHeaderName = "MessagesSize";

		public const string SessionStateSizeHeaderName = "SessionStateSize";

		public const string NextPartitionHeaderName = "NextPartition";

		public const string MaxMessagesCountHeaderName = "MaxMessagesCount";

		public const string RequestInfoHeaderName = "RequestInfo";

		public const string PartitionedEntitySessionInfo = "PartitionedEntitySessionInfo";

		public const string PartitionedEntitySessions = "PartitionedEntitySessions";

		public const string ServerTimeoutHeaderName = "ServerTimeout";

		public const string Version = "Version";

		public const string FaultInjectionInfoHeaderName = "FaultInjectionInfo";

		private const string ResponseSuffix = "Response";

		private const string ResultSuffix = "Result";

		public readonly static TimeSpan ConnectionPingOperationTimeout;

		public readonly static TimeSpan ConnectionPingTimeout;

		static SbmpConstants()
		{
			SbmpConstants.ConnectionPingOperationTimeout = TimeSpan.FromSeconds(10);
			SbmpConstants.ConnectionPingTimeout = TimeSpan.FromSeconds(50);
		}
	}
}