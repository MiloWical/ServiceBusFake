using Microsoft.ServiceBus.Messaging;
using System;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	internal sealed class CreateControlLinkSettings : CreateLinkSettings
	{
		public CreateControlLinkSettings(SbmpMessagingFactory messagingFactory, string entityPath, string entityName, MessagingEntityType entityType, string fromOffset = null) : base(messagingFactory, entityPath, entityName, new Microsoft.ServiceBus.Messaging.Sbmp.LinkInfo()
		{
			LinkId = messagingFactory.GetNextLinkId(),
			ConnectionId = messagingFactory.ConnectionId,
			LinkType = LinkType.Control,
			EntityName = entityName,
			EntityType = new MessagingEntityType?(entityType),
			FromOffset = fromOffset
		}, null)
		{
		}
	}
}