using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	internal sealed class CreateSenderLinkSettings : CreateLinkSettings
	{
		public SbmpMessageSender MessageSender
		{
			get;
			private set;
		}

		public CreateSenderLinkSettings(SbmpMessagingFactory messagingFactory, string entityName, MessagingEntityType? entityType, RetryPolicy retryPolicy) : this(messagingFactory, entityName, entityType, null, retryPolicy)
		{
		}

		public CreateSenderLinkSettings(SbmpMessagingFactory messagingFactory, string entityName, MessagingEntityType? entityType, string transferDestinationEntityName) : this(messagingFactory, entityName, entityType, transferDestinationEntityName, null)
		{
		}

		private CreateSenderLinkSettings(SbmpMessagingFactory messagingFactory, string entityName, MessagingEntityType? entityType, string transferDestinationEntityName, RetryPolicy retryPolicy)
		{
			SbmpMessagingFactory sbmpMessagingFactory = messagingFactory;
			string str = entityName;
			string str1 = entityName;
			Microsoft.ServiceBus.Messaging.Sbmp.LinkInfo linkInfo = new Microsoft.ServiceBus.Messaging.Sbmp.LinkInfo()
			{
				LinkId = messagingFactory.GetNextLinkId(),
				ConnectionId = messagingFactory.ConnectionId,
				LinkType = LinkType.Send,
				EntityName = entityName,
				EntityType = entityType
			};
			Microsoft.ServiceBus.Messaging.Sbmp.LinkInfo linkInfo1 = linkInfo;
			if (string.IsNullOrEmpty(transferDestinationEntityName))
			{
				string str2 = null;
			}
			else
			{
				str2 = messagingFactory.CreateUri(transferDestinationEntityName).ToString();
			}
			linkInfo1.TransferDestinationEntityAddress = str2;
			base(sbmpMessagingFactory, str, str1, linkInfo, null);
			this.MessageSender = new SbmpMessageSender(base.EntityName, base.MessagingFactory, base.MessageCreator, retryPolicy);
		}
	}
}