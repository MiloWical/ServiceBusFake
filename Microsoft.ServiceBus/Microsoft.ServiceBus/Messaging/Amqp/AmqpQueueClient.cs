using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal sealed class AmqpQueueClient : QueueClient
	{
		private AmqpMessagingFactory messagingFactory;

		public AmqpQueueClient(AmqpMessagingFactory messagingFactory, string name, ReceiveMode receiveMode) : base(messagingFactory, name, receiveMode)
		{
			this.messagingFactory = messagingFactory;
		}

		protected override IAsyncResult OnBeginAcceptMessageSession(string sessionId, ReceiveMode receiveMode, TimeSpan serverWaitTime, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.messagingFactory.BeginAcceptSessionInternal(base.Path, new MessagingEntityType?(MessagingEntityType.Queue), sessionId, base.RetryPolicy, receiveMode, serverWaitTime, timeout, callback, state);
		}

		internal override IAsyncResult OnBeginCreateBrowser(TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new NotImplementedException();
		}

		protected override IAsyncResult OnBeginCreateReceiver(ReceiveMode receiveMode, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult<AmqpMessageReceiver>(new AmqpMessageReceiver(this.messagingFactory, base.Path, new MessagingEntityType?(MessagingEntityType.Queue), base.RetryPolicy, receiveMode), callback, state);
		}

		protected override IAsyncResult OnBeginCreateReceiver(string subQueueName, ReceiveMode receiveMode, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult<AmqpMessageReceiver>(new AmqpMessageReceiver(this.messagingFactory, base.Path, new MessagingEntityType?(MessagingEntityType.Queue), base.RetryPolicy, receiveMode), callback, state);
		}

		protected override IAsyncResult OnBeginCreateSender(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult<AmqpMessageSender>(new AmqpMessageSender(this.messagingFactory, base.Path, new MessagingEntityType?(MessagingEntityType.Queue), base.RetryPolicy), callback, state);
		}

		protected override IAsyncResult OnBeginGetMessageSessions(DateTime lastUpdatedTime, AsyncCallback callback, object state)
		{
			throw new NotSupportedException();
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult(callback, state);
		}

		protected override MessageSession OnEndAcceptMessageSession(IAsyncResult result)
		{
			return this.messagingFactory.EndAcceptSessionInternal(result);
		}

		internal override MessageBrowser OnEndCreateBrowser(IAsyncResult result)
		{
			throw new NotImplementedException();
		}

		protected override MessageReceiver OnEndCreateReceiver(IAsyncResult result)
		{
			return CompletedAsyncResult<AmqpMessageReceiver>.End(result);
		}

		protected override MessageSender OnEndCreateSender(IAsyncResult result)
		{
			return CompletedAsyncResult<AmqpMessageSender>.End(result);
		}

		protected override IEnumerable<MessageSession> OnEndGetMessageSessions(IAsyncResult result)
		{
			throw new NotSupportedException();
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