using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	internal sealed class CreateReceiverLinkSettings : CreateLinkSettings
	{
		public SbmpMessageReceiver MessageReceiver
		{
			get;
			private set;
		}

		public CreateReceiverLinkSettings(SbmpMessagingFactory messagingFactory, string entityPath, string entityName, MessagingEntityType? entityType, ReceiveMode receiveMode, Lazy<SbmpMessageCreator> controlMessageCreator, bool isSessionful) : this(messagingFactory, entityPath, entityName, entityType, receiveMode, controlMessageCreator, null, isSessionful, null)
		{
		}

		public CreateReceiverLinkSettings(SbmpMessagingFactory messagingFactory, string entityPath, string entityName, MessagingEntityType? entityType, ReceiveMode receiveMode, Lazy<SbmpMessageCreator> controlMessageCreator, RetryPolicy retryPolicy, bool isSessionful) : this(messagingFactory, entityPath, entityName, entityType, receiveMode, controlMessageCreator, retryPolicy, isSessionful, null)
		{
		}

		public CreateReceiverLinkSettings(SbmpMessagingFactory messagingFactory, string entityPath, string entityName, MessagingEntityType? entityType, ReceiveMode receiveMode, Lazy<SbmpMessageCreator> controlMessageCreator, RetryPolicy retryPolicy, bool isSessionful, string fromOffset) : base(messagingFactory, entityPath, entityName, new Microsoft.ServiceBus.Messaging.Sbmp.LinkInfo()
		{
			LinkId = messagingFactory.GetNextLinkId(),
			ConnectionId = messagingFactory.ConnectionId,
			LinkType = LinkType.Receive,
			EntityName = entityName,
			EntityType = entityType,
			ReceiveMode = receiveMode,
			IsSessionReceiver = isSessionful,
			FromOffset = fromOffset
		}, controlMessageCreator)
		{
			this.MessageReceiver = new SbmpMessageReceiver(base.EntityPath, false, base.MessagingFactory, base.MessageCreator, base.ControlMessageCreator, base.LinkInfo, retryPolicy);
		}
	}
}