using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal sealed class AmqpTopicClient : TopicClient
	{
		private AmqpMessagingFactory amqpMessagingFactory;

		public AmqpTopicClient(AmqpMessagingFactory messagingFactory, string name) : base(messagingFactory, name)
		{
			this.amqpMessagingFactory = messagingFactory;
		}

		protected override IAsyncResult OnBeginCreateSender(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult<AmqpMessageSender>(new AmqpMessageSender(this.amqpMessagingFactory, base.Path, new MessagingEntityType?(MessagingEntityType.Topic), base.RetryPolicy), callback, state);
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult(callback, state);
		}

		protected override MessageSender OnEndCreateSender(IAsyncResult result)
		{
			return CompletedAsyncResult<AmqpMessageSender>.End(result);
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