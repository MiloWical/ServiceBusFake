using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal sealed class AmqpVolatileTopicClient : VolatileTopicClient
	{
		private readonly AmqpMessagingFactory messagingFactory;

		public AmqpVolatileTopicClient(AmqpMessagingFactory factory, string path, string clientId, Microsoft.ServiceBus.RetryPolicy retryPolicy, Microsoft.ServiceBus.Messaging.Filter filter) : base(factory, path, clientId, retryPolicy, filter)
		{
			this.messagingFactory = factory;
		}

		protected override IAsyncResult OnBeginCreateReceiver(TimeSpan timeout, AsyncCallback callback, object state)
		{
			string str = EntityNameHelper.FormatSubscriptionPath(base.Path, base.ClientId);
			AmqpMessageReceiver amqpMessageReceiver = new AmqpMessageReceiver(this.messagingFactory, str, new MessagingEntityType?(MessagingEntityType.VolatileTopicSubscription), base.RetryPolicy, ReceiveMode.ReceiveAndDelete, base.Filter);
			return new CompletedAsyncResult<AmqpMessageReceiver>(amqpMessageReceiver, callback, state);
		}

		protected override IAsyncResult OnBeginCreateSender(TimeSpan timeout, AsyncCallback callback, object state)
		{
			AmqpMessageSender amqpMessageSender = new AmqpMessageSender(this.messagingFactory, base.Path, new MessagingEntityType?(MessagingEntityType.VolatileTopic), base.RetryPolicy);
			return new CompletedAsyncResult<AmqpMessageSender>(amqpMessageSender, callback, state);
		}

		protected override MessageReceiver OnEndCreateReceiver(IAsyncResult result)
		{
			return CompletedAsyncResult<AmqpMessageReceiver>.End(result);
		}

		protected override MessageSender OnEndCreateSender(IAsyncResult result)
		{
			return CompletedAsyncResult<AmqpMessageSender>.End(result);
		}
	}
}