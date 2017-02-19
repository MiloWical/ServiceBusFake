using System;

namespace Microsoft.ServiceBus.Messaging
{
	internal enum FaultInjectionTarget
	{
		ResourceCollectionManagementOperationHandler,
		ManagementOperationHandler,
		GatewayMessagingRuntimeProvider,
		GatewaySoapMessagingRuntimeProvider,
		GatewayHttpMessagingRuntimeProvider,
		GatewayHttpMessagingRuntimeProviderSendMessage,
		GatewayScaledReceiveRuntimeProvider,
		BrokerSbmpConnection,
		StorageConnection,
		Journal,
		JournalRecovery,
		JournalCommit,
		JournalWrite,
		JournalAppendToSubscription,
		JournalRead,
		JournalAck,
		JournalMoveToSubQueue,
		JournalUpdateMessageState,
		JournalGetSessionState,
		JournalSetSessionState,
		IndexCheckpoint,
		IndexRead,
		Extent,
		ExtentRead,
		ExtentWrite,
		ManagementWcfService,
		MessagingManagementEndpoint
	}
}