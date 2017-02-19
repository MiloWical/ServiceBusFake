using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal static class ClientConstants
	{
		public const string SchemeInternalAmqp1 = "amqp1";

		public const string SchemeInternalAmqp2 = "amqp2";

		public const string FilterOffsetPartName = "amqp.annotation.x-opt-offset";

		public const string FilterOffset = "amqp.annotation.x-opt-offset > ";

		public const string FilterInclusiveOffset = "amqp.annotation.x-opt-offset >= ";

		public const string FilterOffsetFormatString = "amqp.annotation.x-opt-offset > '{0}'";

		public const string FilterInclusiveOffsetFormatString = "amqp.annotation.x-opt-offset >= '{0}'";

		public const string FilterReceivedAtPartName = "amqp.annotation.x-opt-enqueuedtimeutc";

		public const string FilterReceivedAt = "amqp.annotation.x-opt-enqueuedtimeutc > ";

		public const string FilterReceivedAtFormatString = "amqp.annotation.x-opt-enqueuedtimeutc > {0}";

		public const string CbsAddress = "$cbs";

		public const string SwtTokenType = "amqp:swt";

		public const string SasTokenType = "servicebus.windows.net:sastoken";

		public const string ApplicationPropertiesSection = "application-properties";

		public const string PutTokenOperation = "operation";

		public const string PutTokenOperationValue = "put-token";

		public const string PutTokenType = "type";

		public const string PutTokenAudience = "name";

		public const string PutTokenExpiration = "expiration";

		public const string PutTokenStatusCode = "status-code";

		public const string PutTokenStatusDescription = "status-description";

		public const string ManagementAddress = "$management";

		public const string EntityNameKey = "name";

		public const string ManagementOperationKey = "operation";

		public const string ReadOperationValue = "READ";

		public const string ManagementEntityTypeKey = "type";

		public const string ManagementEventHubEntityTypeValue = "com.microsoft:eventhub";

		public const string ManagementStatusCodeKey = "status-code";

		public const string ManagementStatusDescriptionKey = "status-description";

		public const string ManagementEventHubCreatedAt = "created_at";

		public const string ManagementEventHubPartitionCount = "partition_count";

		public const string ManagementEventHubPartitionIds = "partition_ids";

		public readonly static AmqpSymbol BatchFlushIntervalName;

		public readonly static AmqpSymbol TimeoutName;

		public readonly static AmqpSymbol EntityTypeName;

		public readonly static AmqpSymbol StackTraceName;

		public readonly static AmqpSymbol TrackingIdName;

		public readonly static AmqpSymbol ClientAuthenticationRequiredName;

		public readonly static AmqpSymbol DisplayName;

		public readonly static AmqpSymbol DynamicRelay;

		public readonly static AmqpSymbol ListenerTypeName;

		public readonly static AmqpSymbol TransportSecurityRequiredName;

		public readonly static AmqpSymbol RequiresPublicRegistry;

		public readonly static AmqpSymbol ClientAgent;

		public readonly static AmqpSymbol AttachEpoch;

		public readonly static AmqpSymbol DeadLetterName;

		public readonly static AmqpSymbol TimeoutError;

		public readonly static AmqpSymbol AddressAlreadyInUseError;

		public readonly static AmqpSymbol AuthorizationFailedError;

		public readonly static AmqpSymbol MessageLockLostError;

		public readonly static AmqpSymbol SessionLockLostError;

		public readonly static AmqpSymbol StoreLockLostError;

		public readonly static AmqpSymbol SessionCannotBeLockedError;

		public readonly static AmqpSymbol NoMatchingSubscriptionError;

		public readonly static AmqpSymbol ServerBusyError;

		public readonly static AmqpSymbol ArgumentError;

		public readonly static AmqpSymbol ArgumentOutOfRangeError;

		public readonly static AmqpSymbol PartitionNotOwnedError;

		public readonly static AmqpSymbol EntityDisabledError;

		public readonly static AmqpSymbol OperationCancelledError;

		public readonly static AmqpSymbol EntityAlreadyExistsError;

		public readonly static AmqpSymbol RelayNotFoundError;

		public readonly static AmqpSymbol SessionFilterName;

		public readonly static AmqpSymbol MessageReceiptsFilterName;

		public readonly static AmqpSymbol ClientSideCursorFilterName;

		public readonly static TimeSpan ClientMinimumTokenRefreshInterval;

		public readonly static string[] CbsSupportedTokenTypes;

		public readonly static TimeSpan MaximumTokenTimeToLive;

		public readonly static TimeSpan MinimumTokenRefreshInterval;

		static ClientConstants()
		{
			ClientConstants.BatchFlushIntervalName = "com.microsoft:batch-flush-interval";
			ClientConstants.TimeoutName = "com.microsoft:timeout";
			ClientConstants.EntityTypeName = "com.microsoft:entity-type";
			ClientConstants.StackTraceName = "com.microsoft:stack-trace";
			ClientConstants.TrackingIdName = "com.microsoft:tracking-id";
			ClientConstants.ClientAuthenticationRequiredName = "com.microsoft:client-authentication-required";
			ClientConstants.DisplayName = "com.microsoft:display-name";
			ClientConstants.DynamicRelay = "com.microsoft:dynamic-relay";
			ClientConstants.ListenerTypeName = "com.microsoft:listener-type";
			ClientConstants.TransportSecurityRequiredName = "com.microsoft:transport-security-required";
			ClientConstants.RequiresPublicRegistry = "com.microsoft:requires-public-registry";
			ClientConstants.ClientAgent = "com.microsoft:client-agent";
			ClientConstants.AttachEpoch = "com.microsoft:epoch";
			ClientConstants.DeadLetterName = "com.microsoft:dead-letter";
			ClientConstants.TimeoutError = "com.microsoft:timeout";
			ClientConstants.AddressAlreadyInUseError = "com.microsoft:address-already-in-use";
			ClientConstants.AuthorizationFailedError = "com.microsoft:auth-failed";
			ClientConstants.MessageLockLostError = "com.microsoft:message-lock-lost";
			ClientConstants.SessionLockLostError = "com.microsoft:session-lock-lost";
			ClientConstants.StoreLockLostError = "com.microsoft:store-lock-lost";
			ClientConstants.SessionCannotBeLockedError = "com.microsoft:session-cannot-be-locked";
			ClientConstants.NoMatchingSubscriptionError = "com.microsoft:no-matching-subscription";
			ClientConstants.ServerBusyError = "com.microsoft:server-busy";
			ClientConstants.ArgumentError = "com.microsoft:argument-error";
			ClientConstants.ArgumentOutOfRangeError = "com.microsoft:argument-out-of-range";
			ClientConstants.PartitionNotOwnedError = "com.microsoft:partition-not-owned";
			ClientConstants.EntityDisabledError = "com.microsoft:entity-disabled";
			ClientConstants.OperationCancelledError = "com.microsoft:operation-cancelled";
			ClientConstants.EntityAlreadyExistsError = "com.microsoft:entity-already-exists";
			ClientConstants.RelayNotFoundError = "com.microsoft:relay-not-found";
			ClientConstants.SessionFilterName = "com.microsoft:session-filter";
			ClientConstants.MessageReceiptsFilterName = "com.microsoft:message-receipts-filter";
			ClientConstants.ClientSideCursorFilterName = "com.microsoft:client-side-filter";
			ClientConstants.ClientMinimumTokenRefreshInterval = TimeSpan.FromMinutes(4);
			ClientConstants.CbsSupportedTokenTypes = new string[] { "amqp:swt", "servicebus.windows.net:sastoken" };
			ClientConstants.MaximumTokenTimeToLive = TimeSpan.FromDays(60);
			ClientConstants.MinimumTokenRefreshInterval = TimeSpan.FromMinutes(5);
		}
	}
}