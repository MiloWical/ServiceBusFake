using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	internal sealed class SbmpTopicClient : TopicClient
	{
		public SbmpTopicClient(SbmpMessagingFactory messagingFactory, string path) : base(messagingFactory, path)
		{
		}

		protected override IAsyncResult OnBeginCreateSender(TimeSpan timeout, AsyncCallback callback, object state)
		{
			CreateSenderLinkSettings createSenderLinkSetting = new CreateSenderLinkSettings((SbmpMessagingFactory)base.MessagingFactory, base.Path, new MessagingEntityType?(MessagingEntityType.Topic), base.RetryPolicy);
			return new CompletedAsyncResult<SbmpMessageSender>(createSenderLinkSetting.MessageSender, callback, state);
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult(callback, state);
		}

		protected override MessageSender OnEndCreateSender(IAsyncResult result)
		{
			return CompletedAsyncResult<SbmpMessageSender>.End(result);
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
		}

		protected override void OnOpen(TimeSpan timeout)
		{
		}
	}
}