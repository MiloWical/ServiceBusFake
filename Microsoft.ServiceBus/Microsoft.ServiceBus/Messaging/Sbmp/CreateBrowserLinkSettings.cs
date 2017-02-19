using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	internal sealed class CreateBrowserLinkSettings : CreateLinkSettings
	{
		public SbmpMessageBrowser MessageBrowser
		{
			get;
			private set;
		}

		public CreateBrowserLinkSettings(SbmpMessagingFactory messagingFactory, string entityPath, string entityName, MessagingEntityType? entityType, Lazy<SbmpMessageCreator> controlMessageCreator, RetryPolicy retryPolicy, bool isSessionful) : base(messagingFactory, entityPath, entityName, new Microsoft.ServiceBus.Messaging.Sbmp.LinkInfo()
		{
			LinkId = messagingFactory.GetNextLinkId(),
			ConnectionId = messagingFactory.ConnectionId,
			LinkType = LinkType.Receive,
			EntityName = entityName,
			EntityType = entityType,
			IsSessionReceiver = isSessionful,
			IsBrowseMode = true
		}, controlMessageCreator)
		{
			this.MessageBrowser = new SbmpMessageBrowser(base.EntityPath, base.MessagingFactory, base.MessageCreator, base.ControlMessageCreator, retryPolicy, false);
		}
	}
}