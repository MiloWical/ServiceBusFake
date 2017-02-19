using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal sealed class AmqpSubscriptionClient : SubscriptionClient
	{
		private readonly AmqpMessagingFactory messagingFactory;

		private readonly FaultTolerantObject<SendingAmqpLink> controlLink;

		private readonly ActiveClientLinkManager clientControlLinkManager;

		private int controlMessageCount;

		public AmqpSubscriptionClient(AmqpMessagingFactory messagingFactory, string topicPath, string name, ReceiveMode receiveMode) : base(messagingFactory, topicPath, name, receiveMode)
		{
			this.controlLink = new FaultTolerantObject<SendingAmqpLink>(this, (SendingAmqpLink link) => link.Session.SafeClose(), new Func<TimeSpan, AsyncCallback, object, IAsyncResult>(this.BeginCreateControlLink), new Func<IAsyncResult, SendingAmqpLink>(this.EndCreateControlLink));
			this.messagingFactory = messagingFactory;
			this.clientControlLinkManager = new ActiveClientLinkManager(this.messagingFactory);
		}

		internal AmqpSubscriptionClient(AmqpMessagingFactory messagingFactory, string subscriptionPath, ReceiveMode receiveMode) : base(messagingFactory, subscriptionPath, receiveMode)
		{
			this.controlLink = new FaultTolerantObject<SendingAmqpLink>(this, (SendingAmqpLink link) => link.Session.SafeClose(), new Func<TimeSpan, AsyncCallback, object, IAsyncResult>(this.BeginCreateControlLink), new Func<IAsyncResult, SendingAmqpLink>(this.EndCreateControlLink));
			this.messagingFactory = messagingFactory;
			this.clientControlLinkManager = new ActiveClientLinkManager(this.messagingFactory);
		}

		private IAsyncResult BaseOnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return base.OnBeginClose(timeout, callback, state);
		}

		private void BaseOnClose(TimeSpan timeout)
		{
			base.OnClose(timeout);
		}

		private void BaseOnEndClose(IAsyncResult result)
		{
			base.OnEndClose(result);
		}

		private IAsyncResult BeginCreateControlLink(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.messagingFactory.BeginOpenControlEntity(base.SubscriptionPath, timeout, callback, state);
		}

		private SendingAmqpLink EndCreateControlLink(IAsyncResult result)
		{
			ActiveClientLink activeClientLink = this.messagingFactory.EndOpenEntity(result);
			this.clientControlLinkManager.SetActiveLink(activeClientLink);
			return (SendingAmqpLink)activeClientLink.Link;
		}

		private ArraySegment<byte> GetControlMessageDeliveryTag()
		{
			int num = Interlocked.Increment(ref this.controlMessageCount);
			return new ArraySegment<byte>(BitConverter.GetBytes(num));
		}

		protected override void OnAbort()
		{
			this.clientControlLinkManager.Close();
			base.OnAbort();
		}

		protected override IAsyncResult OnBeginAcceptMessageSession(string sessionId, ReceiveMode receiveMode, TimeSpan serverWaitTime, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.messagingFactory.BeginAcceptSessionInternal(base.SubscriptionPath, new MessagingEntityType?(MessagingEntityType.Subscriber), sessionId, base.RetryPolicy, receiveMode, serverWaitTime, timeout, callback, state);
		}

		protected override IAsyncResult OnBeginAddRule(RuleDescription description, TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new NotSupportedException(SRAmqp.AmqpOperationNotSupported("AddRule"));
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new AmqpSubscriptionClient.CloseAsyncResult(this, timeout, callback, state);
		}

		internal override IAsyncResult OnBeginCreateBrowser(TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new NotSupportedException();
		}

		protected override IAsyncResult OnBeginCreateReceiver(ReceiveMode receiveMode, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.OnBeginCreateReceiver(base.SubscriptionPath, base.Name, receiveMode, timeout, callback, state);
		}

		protected override IAsyncResult OnBeginCreateReceiver(string subQueuePath, string subQueueName, ReceiveMode receiveMode, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult<AmqpMessageReceiver>(new AmqpMessageReceiver(this.messagingFactory, base.SubscriptionPath, new MessagingEntityType?(MessagingEntityType.Subscriber), base.RetryPolicy, receiveMode), callback, state);
		}

		protected override IAsyncResult OnBeginGetMessageSessions(DateTime lastUpdatedTime, AsyncCallback callback, object state)
		{
			throw new NotSupportedException();
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult(callback, state);
		}

		protected override IAsyncResult OnBeginRemoveRule(string ruleName, TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new NotSupportedException(SRAmqp.AmqpOperationNotSupported("RemoveRule"));
		}

		protected override IAsyncResult OnBeginRemoveRulesByTag(string tag, TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new NotSupportedException();
		}

		protected override void OnClose(TimeSpan timeout)
		{
			AsyncResult<AmqpSubscriptionClient.CloseAsyncResult>.End(new AmqpSubscriptionClient.CloseAsyncResult(this, timeout, null, null));
		}

		protected override MessageSession OnEndAcceptMessageSession(IAsyncResult result)
		{
			return this.messagingFactory.EndAcceptSessionInternal(result);
		}

		protected override void OnEndAddRule(IAsyncResult result)
		{
			throw new NotSupportedException();
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			AsyncResult<AmqpSubscriptionClient.CloseAsyncResult>.End(result);
			this.clientControlLinkManager.Close();
		}

		internal override MessageBrowser OnEndCreateBrowser(IAsyncResult result)
		{
			throw new NotSupportedException();
		}

		protected override MessageReceiver OnEndCreateReceiver(IAsyncResult result)
		{
			return CompletedAsyncResult<AmqpMessageReceiver>.End(result);
		}

		protected override IEnumerable<MessageSession> OnEndGetMessageSessions(IAsyncResult result)
		{
			throw new NotSupportedException();
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
		}

		protected override void OnEndRemoveRule(IAsyncResult result)
		{
			throw new NotSupportedException();
		}

		protected override void OnEndRemoveRules(IAsyncResult result)
		{
			throw new NotSupportedException();
		}

		protected override void OnOpen(TimeSpan timeout)
		{
		}

		private sealed class AddRuleAsyncResult : AmqpSubscriptionClient.RuleAsyncResult
		{
			private RuleDescription description;

			public AddRuleAsyncResult(AmqpSubscriptionClient client, RuleDescription description, string operation, TimeSpan timeout, AsyncCallback callback, object state) : base(client, timeout, callback, state)
			{
				this.description = description;
				base.Start();
			}

			protected override Performative CreateCommand()
			{
				AmqpRuleDescription ruleDescription = MessageConverter.GetRuleDescription(this.description);
				AmqpAddRule amqpAddRule = new AmqpAddRule()
				{
					RuleName = this.description.Name,
					RuleDescription = ruleDescription
				};
				return amqpAddRule;
			}
		}

		private sealed class CloseAsyncResult : IteratorAsyncResult<AmqpSubscriptionClient.CloseAsyncResult>
		{
			private readonly AmqpSubscriptionClient client;

			private SendingAmqpLink amqpLink;

			public CloseAsyncResult(AmqpSubscriptionClient client, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.client = client;
				base.Start();
			}

			protected override IEnumerator<IteratorAsyncResult<AmqpSubscriptionClient.CloseAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				if (this.client.controlLink.TryGetOpenedObject(out this.amqpLink))
				{
					AmqpSubscriptionClient.CloseAsyncResult closeAsyncResult = this;
					IteratorAsyncResult<AmqpSubscriptionClient.CloseAsyncResult>.BeginCall beginCall = (AmqpSubscriptionClient.CloseAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.client.messagingFactory.BeginCloseEntity(thisPtr.amqpLink, t, c, s);
					yield return closeAsyncResult.CallAsync(beginCall, (AmqpSubscriptionClient.CloseAsyncResult thisPtr, IAsyncResult r) => thisPtr.client.messagingFactory.EndCloseEntity(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				}
				AmqpSubscriptionClient.CloseAsyncResult closeAsyncResult1 = this;
				IteratorAsyncResult<AmqpSubscriptionClient.CloseAsyncResult>.BeginCall beginCall1 = (AmqpSubscriptionClient.CloseAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.client.BaseOnBeginClose(t, c, s);
				IteratorAsyncResult<AmqpSubscriptionClient.CloseAsyncResult>.EndCall endCall = (AmqpSubscriptionClient.CloseAsyncResult thisPtr, IAsyncResult r) => thisPtr.client.BaseOnEndClose(r);
				yield return closeAsyncResult1.CallAsync(beginCall1, endCall, (AmqpSubscriptionClient.CloseAsyncResult thisPtr, TimeSpan t) => thisPtr.client.BaseOnClose(t), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
			}
		}

		private sealed class RemoveRuleAsyncResult : AmqpSubscriptionClient.RuleAsyncResult
		{
			private string ruleName;

			public RemoveRuleAsyncResult(AmqpSubscriptionClient client, string ruleName, string operation, TimeSpan timeout, AsyncCallback callback, object state) : base(client, timeout, callback, state)
			{
				this.ruleName = ruleName;
				base.Start();
			}

			protected override Performative CreateCommand()
			{
				return new AmqpDeleteRule()
				{
					RuleName = this.ruleName
				};
			}
		}

		private abstract class RuleAsyncResult : IteratorAsyncResult<AmqpSubscriptionClient.RuleAsyncResult>
		{
			private readonly AmqpSubscriptionClient client;

			private SendingAmqpLink amqpLink;

			private Outcome outcome;

			protected RuleAsyncResult(AmqpSubscriptionClient client, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.client = client;
			}

			protected abstract Performative CreateCommand();

			protected override IEnumerator<IteratorAsyncResult<AmqpSubscriptionClient.RuleAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				if (!this.client.controlLink.TryGetOpenedObject(out this.amqpLink))
				{
					AmqpSubscriptionClient.RuleAsyncResult ruleAsyncResult = this;
					IteratorAsyncResult<AmqpSubscriptionClient.RuleAsyncResult>.BeginCall beginCall = (AmqpSubscriptionClient.RuleAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.client.controlLink.BeginGetInstance(t, c, s);
					yield return ruleAsyncResult.CallAsync(beginCall, (AmqpSubscriptionClient.RuleAsyncResult thisPtr, IAsyncResult r) => thisPtr.amqpLink = thisPtr.client.controlLink.EndGetInstance(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				}
				AmqpValue amqpValue = new AmqpValue()
				{
					Value = this.CreateCommand()
				};
				AmqpMessage amqpMessage = AmqpMessage.Create(amqpValue);
				amqpMessage.Batchable = false;
				AmqpSubscriptionClient.RuleAsyncResult ruleAsyncResult1 = this;
				IteratorAsyncResult<AmqpSubscriptionClient.RuleAsyncResult>.BeginCall beginCall1 = (AmqpSubscriptionClient.RuleAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.amqpLink.BeginSendMessage(amqpMessage, thisPtr.client.GetControlMessageDeliveryTag(), new ArraySegment<byte>(), t, c, s);
				yield return ruleAsyncResult1.CallAsync(beginCall1, (AmqpSubscriptionClient.RuleAsyncResult thisPtr, IAsyncResult r) => thisPtr.outcome = thisPtr.amqpLink.EndSendMessage(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				if (this.outcome.DescriptorCode == Rejected.Code)
				{
					base.Complete(ExceptionHelper.ToMessagingContract(((Rejected)this.outcome).Error));
				}
			}
		}
	}
}