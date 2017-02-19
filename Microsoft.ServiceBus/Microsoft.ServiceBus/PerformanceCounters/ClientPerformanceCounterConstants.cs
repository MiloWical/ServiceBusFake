using System;

namespace Microsoft.ServiceBus.PerformanceCounters
{
	internal static class ClientPerformanceCounterConstants
	{
		public const string CounterCategoryName = "Service Bus Messaging Client";

		public const string SendMessageSuccessCount = "SendMessage Success Count";

		public const string SendMessageSuccessPerSec = "SendMessage Success/sec";

		public const string SendMessageFailureCount = "SendMessage Failure Count";

		public const string SendMessageFailurePerSec = "SendMessage Failures/sec";

		public const string SendMessageLatencyBase = "SendMessage Latency Base";

		public const string SendMessageLatency = "SendMessage Latency";

		public const string ReceiveMessageSuccessCount = "ReceiveMessage Success Count";

		public const string ReceiveMessageSuccessPerSec = "ReceiveMessage Success/sec";

		public const string ReceiveMessageFailureCount = "ReceiveMessage Failure Count";

		public const string ReceiveMessageFailurePerSec = "ReceiveMessage Failures/sec";

		public const string ReceiveMessageLatencyBase = "ReceiveMessage Latency Base";

		public const string ReceiveMessageLatency = "ReceiveMessage Latency";

		public const string CompleteMessageSuccessCount = "CompleteMessage Success Count";

		public const string CompleteMessageSuccessPerSec = "CompleteMessage Success/sec";

		public const string CompleteMessageFailureCount = "CompleteMessage Failures Count";

		public const string CompleteMessageFailurePerSec = "CompleteMessage Failures/sec";

		public const string CompleteMessageLatencyBase = "CompleteMessage Latency Base";

		public const string CompleteMessageLatency = "CompleteMessage Latency";

		public const string AcceptMessageSessionByNamespaceSuccessCount = "AcceptMessageSessionByNamespace Completed Count";

		public const string AcceptMessageSessionByNamespaceSuccessPerSec = "AcceptMessageSessionByNamespace Completed/sec";

		public const string AcceptMessageSessionByNamespaceFailureCount = "AcceptMessageSessionByNamespace Failure Count";

		public const string AcceptMessageSessionByNamespaceFailurePerSec = "AcceptMessageSessionByNamespace Failures/sec";

		public const string AcceptMessageSessionByNamespaceLatencyBase = "AcceptMessageSessionByNamespace Latency Base";

		public const string AcceptMessageSessionByNamespaceLatency = "AcceptMessageSessionByNamespace Latency";

		public const string ExceptionCount = "Exceptions Count";

		public const string ExceptionPerSec = "Exceptions/sec";

		public const string MessagingExceptionPerSec = "MessagingExceptions/sec";

		public const string MessagingCommunicationExceptionPerSec = "MessagingCommunicationExceptions/sec";

		public const string ServerBusyExceptionPerSec = "ServerBusyException/sec";

		public const string MessagingFactoryCount = "MessagingFactory count";

		public const string TokensAcquiredPerSec = "Tokens Acquired/sec";

		public const string TokenAcquisitionFailuresPerSec = "Token Acquisition Failures/sec";

		public const string TokenAcquisitionLatencyBase = "Token Acquisition Latency Base";

		public const string TokenAcquisitionLatency = "Token Acquisition Latency";

		public const string PendingReceiveMessageCount = "Pending ReceiveMessage Count";

		public const string PendingAcceptMessageSessionCount = "Pending AcceptMessageSession Count";

		public const string PendingAcceptMessageSessionByNamespaceCount = "Pending AcceptMessageSessionByNamespace Count";

		public const string CancelScheduledMessageSuccessCount = "Cancel Scheduled Message Success Count";

		public const string CancelScheduledMessageSuccessPerSec = "Cancel Scheduled Message Success/sec";

		public const string CancelScheduledMessageFailureCount = "Cancel Scheduled Message Failure Count";

		public const string CancelScheduledMessageFailurePerSec = "Cancel Scheduled Message Failures/sec";

		public const string CancelScheduledMessageLatencyBase = "Cancel Scheduled Message Latency Base";

		public const string CancelScheduledMessageLatency = "Cancel Scheduled Message Latency";

		public readonly static Guid MessagingProviderId;

		public readonly static Guid ClientProviderId;

		static ClientPerformanceCounterConstants()
		{
			ClientPerformanceCounterConstants.MessagingProviderId = new Guid("{F3B69C52-E054-40DB-B0A9-AE5D4A5A1D7B}");
			ClientPerformanceCounterConstants.ClientProviderId = new Guid("{0BA5DC8D-1637-4906-BDEA-458155C39CC6}");
		}
	}
}