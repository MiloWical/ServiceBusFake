using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Transactions;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	internal sealed class SbmpSubscriptionClient : SubscriptionClient
	{
		private Lazy<SbmpMessageCreator> ControlMessageCreator
		{
			get;
			set;
		}

		public SbmpSubscriptionClient(SbmpMessagingFactory messagingFactory, string topicPath, string name, ReceiveMode receiveMode) : base(messagingFactory, topicPath, name, receiveMode)
		{
			this.ControlMessageCreator = new Lazy<SbmpMessageCreator>(new Func<SbmpMessageCreator>(this.InitializeControlLink));
		}

		private void BaseAbort()
		{
			base.OnAbort();
		}

		private IAsyncResult BaseBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return base.OnBeginClose(timeout, callback, state);
		}

		private void EndBaseClose(IAsyncResult result)
		{
			base.OnEndClose(result);
		}

		private SbmpMessageCreator InitializeControlLink()
		{
			CreateControlLinkSettings createControlLinkSetting = new CreateControlLinkSettings((SbmpMessagingFactory)base.MessagingFactory, base.SubscriptionPath, base.Name, MessagingEntityType.Subscriber, null);
			return createControlLinkSetting.MessageCreator;
		}

		protected override void OnAbort()
		{
			(new SbmpSubscriptionClient.CloseAsyncResult(this, true, this.OperationTimeout, (IAsyncResult r) => {
				try
				{
					AsyncResult<SbmpSubscriptionClient.CloseAsyncResult>.End(r);
				}
				catch (Exception exception)
				{
					if (Fx.IsFatal(exception))
					{
						throw;
					}
				}
			}, null)).Start();
		}

		protected override IAsyncResult OnBeginAcceptMessageSession(string sessionId, ReceiveMode receiveMode, TimeSpan serverWaitTime, TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult acceptMessageSessionAsyncResult;
			try
			{
				acceptMessageSessionAsyncResult = new AcceptMessageSessionAsyncResult((SbmpMessagingFactory)base.MessagingFactory, base.SubscriptionPath, sessionId, new MessagingEntityType?(MessagingEntityType.Subscriber), receiveMode, base.PrefetchCount, this.ControlMessageCreator, base.RetryPolicy, serverWaitTime, timeout, callback, state);
			}
			catch (CommunicationException communicationException1)
			{
				CommunicationException communicationException = communicationException1;
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(MessagingExceptionHelper.Unwrap(communicationException, base.IsClosedOrClosing), null);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception) && base.IsClosedOrClosing)
				{
					throw new OperationCanceledException(SRClient.EntityClosedOrAborted, exception);
				}
				throw;
			}
			return acceptMessageSessionAsyncResult;
		}

		protected override IAsyncResult OnBeginAddRule(RuleDescription description, TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult addRuleAsyncResult;
			try
			{
				addRuleAsyncResult = new SbmpSubscriptionClient.AddRuleAsyncResult(this, description, timeout, callback, state);
			}
			catch (CommunicationException communicationException1)
			{
				CommunicationException communicationException = communicationException1;
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(MessagingExceptionHelper.Unwrap(communicationException, base.IsClosedOrClosing), null);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception) && base.IsClosedOrClosing)
				{
					throw new OperationCanceledException(SRClient.EntityClosedOrAborted, exception);
				}
				throw;
			}
			return addRuleAsyncResult;
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult asyncResult;
			try
			{
				asyncResult = (new SbmpSubscriptionClient.CloseAsyncResult(this, false, timeout, callback, state)).Start();
			}
			catch (CommunicationException communicationException1)
			{
				CommunicationException communicationException = communicationException1;
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(MessagingExceptionHelper.Unwrap(communicationException, false), null);
			}
			return asyncResult;
		}

		internal override IAsyncResult OnBeginCreateBrowser(TimeSpan timeout, AsyncCallback callback, object state)
		{
			CreateBrowserLinkSettings createBrowserLinkSetting = new CreateBrowserLinkSettings((SbmpMessagingFactory)base.MessagingFactory, base.SubscriptionPath, base.Name, new MessagingEntityType?(MessagingEntityType.Subscriber), this.ControlMessageCreator, base.RetryPolicy, false);
			return new CompletedAsyncResult<SbmpMessageBrowser>(createBrowserLinkSetting.MessageBrowser, callback, state);
		}

		protected override IAsyncResult OnBeginCreateReceiver(ReceiveMode receiveMode, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.OnBeginCreateReceiver(base.SubscriptionPath, base.Name, receiveMode, timeout, callback, state);
		}

		protected override IAsyncResult OnBeginCreateReceiver(string subQueuePath, string subQueueName, ReceiveMode receiveMode, TimeSpan timeout, AsyncCallback callback, object state)
		{
			CreateReceiverLinkSettings createReceiverLinkSetting = new CreateReceiverLinkSettings((SbmpMessagingFactory)base.MessagingFactory, subQueuePath, subQueueName, new MessagingEntityType?(MessagingEntityType.Subscriber), receiveMode, this.ControlMessageCreator, base.RetryPolicy, false);
			return new CompletedAsyncResult<SbmpMessageReceiver>(createReceiverLinkSetting.MessageReceiver, callback, state);
		}

		protected override IAsyncResult OnBeginGetMessageSessions(DateTime lastUpdateTime, AsyncCallback callback, object state)
		{
			IAsyncResult getMessageSessionsAsyncResult;
			try
			{
				getMessageSessionsAsyncResult = new GetMessageSessionsAsyncResult((SbmpMessagingFactory)base.MessagingFactory, base.SubscriptionPath, lastUpdateTime, this.ControlMessageCreator.Value, base.RetryPolicy, MessagingEntityType.Subscriber, this.OperationTimeout, callback, state);
			}
			catch (CommunicationException communicationException1)
			{
				CommunicationException communicationException = communicationException1;
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(MessagingExceptionHelper.Unwrap(communicationException, base.IsClosedOrClosing), null);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception) && base.IsClosedOrClosing)
				{
					throw new OperationCanceledException(SRClient.EntityClosedOrAborted, exception);
				}
				throw;
			}
			return getMessageSessionsAsyncResult;
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult(callback, state);
		}

		protected override IAsyncResult OnBeginRemoveRule(string ruleName, TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult removeRuleAsyncResult;
			try
			{
				removeRuleAsyncResult = new SbmpSubscriptionClient.RemoveRuleAsyncResult(this, ruleName, timeout, callback, state);
			}
			catch (CommunicationException communicationException1)
			{
				CommunicationException communicationException = communicationException1;
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(MessagingExceptionHelper.Unwrap(communicationException, base.IsClosedOrClosing), null);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception) && base.IsClosedOrClosing)
				{
					throw new OperationCanceledException(SRClient.EntityClosedOrAborted, exception);
				}
				throw;
			}
			return removeRuleAsyncResult;
		}

		protected override IAsyncResult OnBeginRemoveRulesByTag(string tag, TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult removeRulesByTagAsyncResult;
			try
			{
				removeRulesByTagAsyncResult = new SbmpSubscriptionClient.RemoveRulesByTagAsyncResult(this, tag, timeout, callback, state);
			}
			catch (CommunicationException communicationException1)
			{
				CommunicationException communicationException = communicationException1;
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(MessagingExceptionHelper.Unwrap(communicationException, base.IsClosedOrClosing), null);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception) && base.IsClosedOrClosing)
				{
					throw new OperationCanceledException(SRClient.EntityClosedOrAborted, exception);
				}
				throw;
			}
			return removeRulesByTagAsyncResult;
		}

		protected override MessageSession OnEndAcceptMessageSession(IAsyncResult result)
		{
			MessageSession messageSession;
			try
			{
				messageSession = AcceptMessageSessionAsyncResult.End(result);
			}
			catch (CommunicationException communicationException1)
			{
				CommunicationException communicationException = communicationException1;
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(MessagingExceptionHelper.Unwrap(communicationException, base.IsClosedOrClosing), null);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception) && base.IsClosedOrClosing)
				{
					throw new OperationCanceledException(SRClient.EntityClosedOrAborted, exception);
				}
				throw;
			}
			return messageSession;
		}

		protected override void OnEndAddRule(IAsyncResult result)
		{
			try
			{
				AsyncResult<SbmpSubscriptionClient.AddRuleAsyncResult>.End(result);
			}
			catch (CommunicationException communicationException1)
			{
				CommunicationException communicationException = communicationException1;
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(MessagingExceptionHelper.Unwrap(communicationException, base.IsClosedOrClosing), null);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception) && base.IsClosedOrClosing)
				{
					throw new OperationCanceledException(SRClient.EntityClosedOrAborted, exception);
				}
				throw;
			}
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			try
			{
				AsyncResult<SbmpSubscriptionClient.CloseAsyncResult>.End(result);
			}
			catch (CommunicationException communicationException1)
			{
				CommunicationException communicationException = communicationException1;
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(MessagingExceptionHelper.Unwrap(communicationException, false), null);
			}
		}

		internal override MessageBrowser OnEndCreateBrowser(IAsyncResult result)
		{
			return CompletedAsyncResult<SbmpMessageBrowser>.End(result);
		}

		protected override MessageReceiver OnEndCreateReceiver(IAsyncResult result)
		{
			return CompletedAsyncResult<SbmpMessageReceiver>.End(result);
		}

		protected override IEnumerable<MessageSession> OnEndGetMessageSessions(IAsyncResult result)
		{
			IEnumerable<MessageSession> messageSessions;
			try
			{
				messageSessions = GetMessageSessionsAsyncResult.End(result);
			}
			catch (CommunicationException communicationException1)
			{
				CommunicationException communicationException = communicationException1;
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(MessagingExceptionHelper.Unwrap(communicationException, base.IsClosedOrClosing), null);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception) && base.IsClosedOrClosing)
				{
					throw new OperationCanceledException(SRClient.EntityClosedOrAborted, exception);
				}
				throw;
			}
			return messageSessions;
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
		}

		protected override void OnEndRemoveRule(IAsyncResult result)
		{
			try
			{
				AsyncResult<SbmpSubscriptionClient.RemoveRuleAsyncResult>.End(result);
			}
			catch (CommunicationException communicationException1)
			{
				CommunicationException communicationException = communicationException1;
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(MessagingExceptionHelper.Unwrap(communicationException, base.IsClosedOrClosing), null);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception) && base.IsClosedOrClosing)
				{
					throw new OperationCanceledException(SRClient.EntityClosedOrAborted, exception);
				}
				throw;
			}
		}

		protected override void OnEndRemoveRules(IAsyncResult result)
		{
			try
			{
				AsyncResult<SbmpSubscriptionClient.RemoveRulesByTagAsyncResult>.End(result);
			}
			catch (CommunicationException communicationException1)
			{
				CommunicationException communicationException = communicationException1;
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(MessagingExceptionHelper.Unwrap(communicationException, base.IsClosedOrClosing), null);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception) && base.IsClosedOrClosing)
				{
					throw new OperationCanceledException(SRClient.EntityClosedOrAborted, exception);
				}
				throw;
			}
		}

		private sealed class AddRuleAsyncResult : SbmpTransactionalAsyncResult<SbmpSubscriptionClient.AddRuleAsyncResult>
		{
			private readonly RuleDescription ruleDescription;

			private readonly SbmpSubscriptionClient subscriptionClient;

			public AddRuleAsyncResult(SbmpSubscriptionClient subscriptionClient, RuleDescription ruleDescription, TimeSpan timeout, AsyncCallback callback, object state) : base((SbmpMessagingFactory)subscriptionClient.MessagingFactory, subscriptionClient.ControlMessageCreator.Value, null, timeout, callback, state)
			{
				if (ruleDescription == null)
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("ruleDescription");
				}
				if (string.IsNullOrWhiteSpace(ruleDescription.Name))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNullOrEmpty("ruleDescription.Name");
				}
				this.ruleDescription = ruleDescription;
				this.subscriptionClient = subscriptionClient;
				base.Start();
			}

			protected override Message CreateWcfMessage()
			{
				string localIdentifier;
				Transaction transaction = base.Transaction;
				AddRuleCommand addRuleCommand = new AddRuleCommand()
				{
					RuleName = this.ruleDescription.Name,
					RuleDescription = this.ruleDescription,
					Timeout = base.RemainingTime()
				};
				AddRuleCommand addRuleCommand1 = addRuleCommand;
				if (transaction != null)
				{
					localIdentifier = transaction.TransactionInformation.LocalIdentifier;
				}
				else
				{
					localIdentifier = null;
				}
				addRuleCommand1.TransactionId = localIdentifier;
				AddRuleCommand addRuleCommand2 = addRuleCommand;
				RequestInfo requestInfo = new RequestInfo()
				{
					ServerTimeout = new TimeSpan?(addRuleCommand2.Timeout),
					TransactionId = addRuleCommand2.TransactionId
				};
				RequestInfo requestInfo1 = requestInfo;
				Message message = base.MessageCreator.CreateWcfMessage("http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpConnection/AddRule", addRuleCommand2, null, this.subscriptionClient.RetryPolicy, null, requestInfo1);
				return message;
			}

			protected override void PartitionInfoSetter(RequestInfo requestInfo)
			{
			}
		}

		private sealed class CloseAsyncResult : IteratorAsyncResult<SbmpSubscriptionClient.CloseAsyncResult>
		{
			private readonly bool aborting;

			private readonly SbmpSubscriptionClient parent;

			public CloseAsyncResult(SbmpSubscriptionClient parent, bool aborting, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.aborting = aborting;
				this.parent = parent;
			}

			protected override IEnumerator<IteratorAsyncResult<SbmpSubscriptionClient.CloseAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				if (!this.aborting)
				{
					if (this.parent.ControlMessageCreator.IsValueCreated)
					{
						SbmpSubscriptionClient.CloseAsyncResult closeAsyncResult = this;
						IteratorAsyncResult<SbmpSubscriptionClient.CloseAsyncResult>.BeginCall beginCall = (SbmpSubscriptionClient.CloseAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => (new CloseOrAbortLinkAsyncResult(thisPtr.parent.ControlMessageCreator.Value, ((SbmpMessagingFactory)thisPtr.parent.MessagingFactory).Channel, null, t, false, c, s)).Start();
						yield return closeAsyncResult.CallAsync(beginCall, (SbmpSubscriptionClient.CloseAsyncResult thisPtr, IAsyncResult r) => AsyncResult<CloseOrAbortLinkAsyncResult>.End(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
					}
					SbmpSubscriptionClient.CloseAsyncResult closeAsyncResult1 = this;
					IteratorAsyncResult<SbmpSubscriptionClient.CloseAsyncResult>.BeginCall beginCall1 = (SbmpSubscriptionClient.CloseAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.parent.BaseBeginClose(t, c, s);
					yield return closeAsyncResult1.CallAsync(beginCall1, (SbmpSubscriptionClient.CloseAsyncResult thisPtr, IAsyncResult r) => thisPtr.parent.EndBaseClose(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				}
				else
				{
					if (this.parent.ControlMessageCreator.IsValueCreated)
					{
						CloseOrAbortLinkAsyncResult closeOrAbortLinkAsyncResult = new CloseOrAbortLinkAsyncResult(this.parent.ControlMessageCreator.Value, ((SbmpMessagingFactory)this.parent.MessagingFactory).Channel, null, base.RemainingTime(), true, null, null);
						closeOrAbortLinkAsyncResult.Schedule();
					}
					this.parent.BaseAbort();
				}
			}
		}

		private sealed class RemoveRuleAsyncResult : SbmpTransactionalAsyncResult<SbmpSubscriptionClient.RemoveRuleAsyncResult>
		{
			private readonly string ruleName;

			private readonly SbmpSubscriptionClient subscriptionClient;

			public RemoveRuleAsyncResult(SbmpSubscriptionClient subscriptionClient, string ruleName, TimeSpan timeout, AsyncCallback callback, object state) : base((SbmpMessagingFactory)subscriptionClient.MessagingFactory, subscriptionClient.ControlMessageCreator.Value, null, timeout, callback, state)
			{
				if (ruleName == null)
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("ruleName");
				}
				this.subscriptionClient = subscriptionClient;
				this.ruleName = ruleName;
				base.Start();
			}

			protected override Message CreateWcfMessage()
			{
				string localIdentifier;
				Transaction transaction = base.Transaction;
				DeleteRuleCommand deleteRuleCommand = new DeleteRuleCommand()
				{
					RuleName = this.ruleName,
					Timeout = base.RemainingTime()
				};
				DeleteRuleCommand deleteRuleCommand1 = deleteRuleCommand;
				if (transaction != null)
				{
					localIdentifier = transaction.TransactionInformation.LocalIdentifier;
				}
				else
				{
					localIdentifier = null;
				}
				deleteRuleCommand1.TransactionId = localIdentifier;
				DeleteRuleCommand deleteRuleCommand2 = deleteRuleCommand;
				RequestInfo requestInfo = new RequestInfo()
				{
					ServerTimeout = new TimeSpan?(deleteRuleCommand2.Timeout),
					TransactionId = deleteRuleCommand2.TransactionId
				};
				RequestInfo requestInfo1 = requestInfo;
				Message message = base.MessageCreator.CreateWcfMessage("http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpConnection/DeleteRule", deleteRuleCommand2, null, this.subscriptionClient.RetryPolicy, null, requestInfo1);
				return message;
			}

			protected override void PartitionInfoSetter(RequestInfo requestInfo)
			{
			}
		}

		private sealed class RemoveRulesByTagAsyncResult : SbmpTransactionalAsyncResult<SbmpSubscriptionClient.RemoveRulesByTagAsyncResult>
		{
			private readonly string tag;

			private readonly SbmpSubscriptionClient subscriptionClient;

			public RemoveRulesByTagAsyncResult(SbmpSubscriptionClient subscriptionClient, string tag, TimeSpan timeout, AsyncCallback callback, object state) : base((SbmpMessagingFactory)subscriptionClient.MessagingFactory, subscriptionClient.ControlMessageCreator.Value, null, timeout, callback, state)
			{
				if (string.IsNullOrEmpty(tag))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("tag");
				}
				this.subscriptionClient = subscriptionClient;
				this.tag = tag;
				base.Start();
			}

			protected override Message CreateWcfMessage()
			{
				string localIdentifier;
				Transaction transaction = base.Transaction;
				DeleteRulesByTagCommand deleteRulesByTagCommand = new DeleteRulesByTagCommand()
				{
					Tag = this.tag,
					Timeout = base.RemainingTime()
				};
				DeleteRulesByTagCommand deleteRulesByTagCommand1 = deleteRulesByTagCommand;
				if (transaction != null)
				{
					localIdentifier = transaction.TransactionInformation.LocalIdentifier;
				}
				else
				{
					localIdentifier = null;
				}
				deleteRulesByTagCommand1.TransactionId = localIdentifier;
				DeleteRulesByTagCommand deleteRulesByTagCommand2 = deleteRulesByTagCommand;
				RequestInfo requestInfo = new RequestInfo()
				{
					ServerTimeout = new TimeSpan?(deleteRulesByTagCommand2.Timeout),
					TransactionId = deleteRulesByTagCommand2.TransactionId
				};
				RequestInfo requestInfo1 = requestInfo;
				Message message = base.MessageCreator.CreateWcfMessage("http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpConnection/DeleteRulesByTag", deleteRulesByTagCommand2, null, this.subscriptionClient.RetryPolicy, null, requestInfo1);
				return message;
			}

			protected override void PartitionInfoSetter(RequestInfo requestInfo)
			{
			}
		}
	}
}