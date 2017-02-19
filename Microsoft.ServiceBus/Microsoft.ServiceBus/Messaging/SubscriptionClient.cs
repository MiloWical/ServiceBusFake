using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Common.Parallel;
using Microsoft.ServiceBus.Messaging.Configuration;
using Microsoft.ServiceBus.PerformanceCounters;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Transactions;

namespace Microsoft.ServiceBus.Messaging
{
	public abstract class SubscriptionClient : MessageClientEntity, IMessageSessionEntity, IMessageClientEntity, IMessageReceiver, IMessageBrowser
	{
		private readonly OpenOnceManager openOnceManager;

		private readonly MessageSessionPumpHost pumpHost;

		private MessageBrowser InternalBrowser
		{
			get;
			set;
		}

		internal MessageReceiver InternalReceiver
		{
			get;
			set;
		}

		private bool IsSubQueue
		{
			get;
			set;
		}

		public Microsoft.ServiceBus.Messaging.MessagingFactory MessagingFactory
		{
			get;
			private set;
		}

		public ReceiveMode Mode
		{
			get
			{
				return JustDecompileGenerated_get_Mode();
			}
			set
			{
				JustDecompileGenerated_set_Mode(value);
			}
		}

		private ReceiveMode JustDecompileGenerated_Mode_k__BackingField;

		public ReceiveMode JustDecompileGenerated_get_Mode()
		{
			return this.JustDecompileGenerated_Mode_k__BackingField;
		}

		private void JustDecompileGenerated_set_Mode(ReceiveMode value)
		{
			this.JustDecompileGenerated_Mode_k__BackingField = value;
		}

		public string Name
		{
			get;
			private set;
		}

		internal override TimeSpan OperationTimeout
		{
			get
			{
				return this.MessagingFactory.OperationTimeout;
			}
		}

		public int PrefetchCount
		{
			get
			{
				this.ThrowIfReceiverNull("PrefetchCount_Get");
				return this.InternalReceiver.PrefetchCount;
			}
			set
			{
				this.ThrowIfReceiverNull("PrefetchCount_Set");
				this.InternalReceiver.PrefetchCount = value;
			}
		}

		internal string SubscriptionPath
		{
			get;
			private set;
		}

		public string TopicPath
		{
			get;
			private set;
		}

		internal SubscriptionClient(Microsoft.ServiceBus.Messaging.MessagingFactory messagingFactory, string topicPath, string name, ReceiveMode receiveMode)
		{
			this.MessagingFactory = messagingFactory;
			this.TopicPath = topicPath;
			this.Name = name;
			this.SubscriptionPath = EntityNameHelper.FormatSubscriptionPath(this.TopicPath, this.Name);
			this.openOnceManager = new OpenOnceManager(this);
			base.ClientEntityManager = new MessageClientEntityManager();
			this.Mode = receiveMode;
			this.IsSubQueue = MessagingUtilities.IsSubQueue(name);
			this.InternalReceiver = null;
			this.InternalBrowser = null;
			base.RetryPolicy = messagingFactory.RetryPolicy.Clone();
			this.pumpHost = new MessageSessionPumpHost(base.ThisLock, this.SubscriptionPath, this);
		}

		internal SubscriptionClient(Microsoft.ServiceBus.Messaging.MessagingFactory messagingFactory, string subscriptionPath, ReceiveMode receiveMode)
		{
			this.MessagingFactory = messagingFactory;
			this.SubscriptionPath = subscriptionPath;
			this.openOnceManager = new OpenOnceManager(this);
			base.ClientEntityManager = new MessageClientEntityManager();
			this.Mode = receiveMode;
			this.IsSubQueue = MessagingUtilities.IsSubQueue(subscriptionPath);
			this.InternalReceiver = null;
			this.InternalBrowser = null;
			base.RetryPolicy = messagingFactory.RetryPolicy.Clone();
			this.pumpHost = new MessageSessionPumpHost(base.ThisLock, this.SubscriptionPath, this);
		}

		public void Abandon(Guid lockToken)
		{
			this.ThrowIfReceiverNull("Abandon");
			this.Abandon(lockToken, null);
		}

		public void Abandon(Guid lockToken, IDictionary<string, object> propertiesToModify)
		{
			this.ThrowIfReceiverNull("Abandon");
			this.InternalReceiver.Abandon(lockToken, propertiesToModify);
		}

		public Task AbandonAsync(Guid lockToken)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginAbandon(lockToken, c, s), new Action<IAsyncResult>(this.EndAbandon));
		}

		public Task AbandonAsync(Guid lockToken, IDictionary<string, object> propertiesToModify)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginAbandon(lockToken, propertiesToModify, c, s), new Action<IAsyncResult>(this.EndAbandon));
		}

		public MessageSession AcceptMessageSession()
		{
			return this.EndAcceptMessageSession(this.BeginAcceptMessageSession(this.MessagingFactory.OperationTimeout, null, null));
		}

		public MessageSession AcceptMessageSession(string sessionId)
		{
			return this.EndAcceptMessageSession(this.BeginAcceptMessageSession(sessionId, this.MessagingFactory.OperationTimeout, null, null));
		}

		public MessageSession AcceptMessageSession(TimeSpan serverWaitTime)
		{
			return this.EndAcceptMessageSession(this.BeginAcceptMessageSession(serverWaitTime, null, null));
		}

		public MessageSession AcceptMessageSession(string sessionId, TimeSpan serverWaitTime)
		{
			return this.EndAcceptMessageSession(this.BeginAcceptMessageSession(sessionId, serverWaitTime, null, null));
		}

		public Task<MessageSession> AcceptMessageSessionAsync()
		{
			return TaskHelpers.CreateTask<MessageSession>(new Func<AsyncCallback, object, IAsyncResult>(this.BeginAcceptMessageSession), new Func<IAsyncResult, MessageSession>(this.EndAcceptMessageSession));
		}

		public Task<MessageSession> AcceptMessageSessionAsync(string sessionId)
		{
			return TaskHelpers.CreateTask<MessageSession>((AsyncCallback c, object s) => this.BeginAcceptMessageSession(sessionId, c, s), new Func<IAsyncResult, MessageSession>(this.EndAcceptMessageSession));
		}

		public Task<MessageSession> AcceptMessageSessionAsync(TimeSpan serverWaitTime)
		{
			return TaskHelpers.CreateTask<MessageSession>((AsyncCallback c, object s) => this.BeginAcceptMessageSession(serverWaitTime, c, s), new Func<IAsyncResult, MessageSession>(this.EndAcceptMessageSession));
		}

		public Task<MessageSession> AcceptMessageSessionAsync(string sessionId, TimeSpan serverWaitTime)
		{
			return TaskHelpers.CreateTask<MessageSession>((AsyncCallback c, object s) => this.BeginAcceptMessageSession(sessionId, serverWaitTime, c, s), new Func<IAsyncResult, MessageSession>(this.EndAcceptMessageSession));
		}

		public void AddRule(RuleDescription description)
		{
			this.EndAddRule(this.BeginAddRule(description, this.MessagingFactory.OperationTimeout, null, null));
		}

		public void AddRule(string ruleName, Filter filter)
		{
			RuleDescription ruleDescription = new RuleDescription()
			{
				Name = ruleName,
				Filter = filter,
				Action = EmptyRuleAction.Default
			};
			this.EndAddRule(this.BeginAddRule(ruleDescription, this.MessagingFactory.OperationTimeout, null, null));
		}

		public Task AddRuleAsync(string ruleName, Filter filter)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginAddRule(ruleName, filter, c, s), new Action<IAsyncResult>(this.EndAddRule));
		}

		public Task AddRuleAsync(RuleDescription description)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginAddRule(description, c, s), new Action<IAsyncResult>(this.EndAddRule));
		}

		public IAsyncResult BeginAbandon(Guid lockToken, AsyncCallback callback, object state)
		{
			this.ThrowIfReceiverNull("BeginAbandon");
			return this.BeginAbandon(lockToken, null, callback, state);
		}

		public IAsyncResult BeginAbandon(Guid lockToken, IDictionary<string, object> propertiesToModify, AsyncCallback callback, object state)
		{
			this.ThrowIfReceiverNull("BeginAbandon");
			return this.InternalReceiver.BeginAbandon(lockToken, propertiesToModify, callback, state);
		}

		public IAsyncResult BeginAcceptMessageSession(AsyncCallback callback, object state)
		{
			return this.BeginAcceptMessageSession(this.MessagingFactory.OperationTimeout, callback, state);
		}

		public IAsyncResult BeginAcceptMessageSession(string sessionId, AsyncCallback callback, object state)
		{
			return this.BeginAcceptMessageSession(sessionId, this.MessagingFactory.OperationTimeout, callback, state);
		}

		public IAsyncResult BeginAcceptMessageSession(TimeSpan serverWaitTime, AsyncCallback callback, object state)
		{
			return this.BeginInternalAcceptMessageSession(null, serverWaitTime, (serverWaitTime > this.OperationTimeout ? serverWaitTime : this.OperationTimeout), callback, state);
		}

		public IAsyncResult BeginAcceptMessageSession(string sessionId, TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (sessionId == null)
			{
				throw Fx.Exception.ArgumentNull("sessionId");
			}
			return this.BeginInternalAcceptMessageSession(sessionId, timeout, timeout, callback, state);
		}

		public IAsyncResult BeginAddRule(string ruleName, Filter filter, AsyncCallback callback, object state)
		{
			RuleDescription ruleDescription = new RuleDescription()
			{
				Name = ruleName,
				Filter = filter,
				Action = EmptyRuleAction.Default
			};
			return this.BeginAddRule(ruleDescription, this.MessagingFactory.OperationTimeout, callback, state);
		}

		public IAsyncResult BeginAddRule(RuleDescription description, AsyncCallback callback, object state)
		{
			return this.BeginAddRule(description, this.MessagingFactory.OperationTimeout, callback, state);
		}

		internal IAsyncResult BeginAddRule(RuleDescription description, TimeSpan timeout, AsyncCallback callback, object state)
		{
			base.ThrowIfDisposed();
			if (description == null)
			{
				throw FxTrace.Exception.ArgumentNull("description");
			}
			Microsoft.ServiceBus.Messaging.MessagingFactory.CheckValidEntityName(description.Name, 50, false, "description.Name");
			if (!this.openOnceManager.ShouldOpen)
			{
				return this.OnBeginAddRule(description, timeout, callback, state);
			}
			OpenOnceManager openOnceManager = this.openOnceManager;
			AsyncCallback asyncCallback = callback;
			object obj = state;
			SubscriptionClient subscriptionClient = this;
			return openOnceManager.Begin(asyncCallback, obj, (AsyncCallback c, object s) => this.OnBeginAddRule(description, timeout, c, s), new Action<IAsyncResult>(subscriptionClient.OnEndAddRule));
		}

		public IAsyncResult BeginComplete(Guid lockToken, AsyncCallback callback, object state)
		{
			this.ThrowIfReceiverNull("BeginComplete");
			return this.InternalReceiver.BeginComplete(lockToken, callback, state);
		}

		public IAsyncResult BeginCompleteBatch(IEnumerable<Guid> lockTokens, AsyncCallback callback, object state)
		{
			this.ThrowIfReceiverNull("BeginCompleteBatch");
			return this.InternalReceiver.BeginCompleteBatch(lockTokens, callback, state);
		}

		private IAsyncResult BeginCreateBrowser(TimeSpan timeout, AsyncCallback callback, object state)
		{
			base.ThrowIfDisposed();
			if (!this.openOnceManager.ShouldOpen)
			{
				return this.OnBeginCreateBrowser(timeout, callback, state);
			}
			OpenOnceManager openOnceManager = this.openOnceManager;
			AsyncCallback asyncCallback = callback;
			object obj = state;
			SubscriptionClient subscriptionClient = this;
			return openOnceManager.Begin<MessageBrowser>(asyncCallback, obj, (AsyncCallback c, object s) => this.OnBeginCreateBrowser(timeout, c, s), new Func<IAsyncResult, MessageBrowser>(subscriptionClient.OnEndCreateBrowser));
		}

		private IAsyncResult BeginCreateReceiver(string subQueuePath, ReceiveMode receiveMode, TimeSpan timeout, AsyncCallback callback, object state)
		{
			base.ThrowIfDisposed();
			string str = EntityNameHelper.FormatSubQueueEntityName(this.Name);
			if (!this.openOnceManager.ShouldOpen)
			{
				return this.OnBeginCreateReceiver(subQueuePath, str, receiveMode, timeout, callback, state);
			}
			OpenOnceManager openOnceManager = this.openOnceManager;
			AsyncCallback asyncCallback = callback;
			object obj = state;
			SubscriptionClient subscriptionClient = this;
			return openOnceManager.Begin<MessageReceiver>(asyncCallback, obj, (AsyncCallback c, object s) => this.OnBeginCreateReceiver(subQueuePath, str, receiveMode, timeout, c, s), new Func<IAsyncResult, MessageReceiver>(subscriptionClient.OnEndCreateReceiver));
		}

		private IAsyncResult BeginCreateReceiver(ReceiveMode receiveMode, TimeSpan timeout, AsyncCallback callback, object state)
		{
			base.ThrowIfDisposed();
			if (!this.openOnceManager.ShouldOpen)
			{
				return this.OnBeginCreateReceiver(receiveMode, timeout, callback, state);
			}
			OpenOnceManager openOnceManager = this.openOnceManager;
			AsyncCallback asyncCallback = callback;
			object obj = state;
			SubscriptionClient subscriptionClient = this;
			return openOnceManager.Begin<MessageReceiver>(asyncCallback, obj, (AsyncCallback c, object s) => this.OnBeginCreateReceiver(receiveMode, timeout, c, s), new Func<IAsyncResult, MessageReceiver>(subscriptionClient.OnEndCreateReceiver));
		}

		public IAsyncResult BeginDeadLetter(Guid lockToken, AsyncCallback callback, object state)
		{
			this.ThrowIfReceiverNull("BeginDeadLetter");
			return this.BeginDeadLetter(lockToken, null, callback, state);
		}

		public IAsyncResult BeginDeadLetter(Guid lockToken, IDictionary<string, object> propertiesToModify, AsyncCallback callback, object state)
		{
			this.ThrowIfReceiverNull("BeginDeadLetter");
			return this.InternalReceiver.BeginDeadLetter(lockToken, propertiesToModify, callback, state);
		}

		public IAsyncResult BeginDeadLetter(Guid lockToken, string deadLetterReason, string deadLetterErrorDescription, AsyncCallback callback, object state)
		{
			this.ThrowIfReceiverNull("BeginDeadLetter");
			return this.InternalReceiver.BeginDeadLetter(lockToken, deadLetterReason, deadLetterErrorDescription, callback, state);
		}

		public IAsyncResult BeginDefer(Guid lockToken, AsyncCallback callback, object state)
		{
			this.ThrowIfReceiverNull("BeginDefer");
			return this.BeginDefer(lockToken, null, callback, state);
		}

		public IAsyncResult BeginDefer(Guid lockToken, IDictionary<string, object> propertiesToModify, AsyncCallback callback, object state)
		{
			this.ThrowIfReceiverNull("BeginDefer");
			return this.InternalReceiver.BeginDefer(lockToken, propertiesToModify, callback, state);
		}

		public IAsyncResult BeginGetMessageSessions(AsyncCallback callback, object state)
		{
			return this.BeginInternalGetMessageSessions(DateTime.MaxValue, callback, state);
		}

		public IAsyncResult BeginGetMessageSessions(DateTime lastUpdatedTime, AsyncCallback callback, object state)
		{
			if (lastUpdatedTime == DateTime.MaxValue)
			{
				throw Fx.Exception.AsError(new ArgumentOutOfRangeException("lastUpdatedTime"), null);
			}
			return this.BeginInternalGetMessageSessions(lastUpdatedTime, callback, state);
		}

		private IAsyncResult BeginInternalAcceptMessageSession(string sessionId, TimeSpan serverWaitTime, TimeSpan timeout, AsyncCallback callback, object state)
		{
			base.ThrowIfDisposed();
			TimeoutHelper.ThrowIfNonPositiveArgument(serverWaitTime, "serverWaitTime");
			if (!this.openOnceManager.ShouldOpen)
			{
				SubscriptionClient.RetryAcceptMessageSessionAsyncResult retryAcceptMessageSessionAsyncResult1 = new SubscriptionClient.RetryAcceptMessageSessionAsyncResult(this, null, sessionId, this.Mode, serverWaitTime, timeout, callback, state);
				retryAcceptMessageSessionAsyncResult1.Start();
				return retryAcceptMessageSessionAsyncResult1;
			}
			return this.openOnceManager.Begin<MessageSession>(callback, state, (AsyncCallback c, object s) => {
				SubscriptionClient.RetryAcceptMessageSessionAsyncResult retryAcceptMessageSessionAsyncResult = new SubscriptionClient.RetryAcceptMessageSessionAsyncResult(this, null, sessionId, this.Mode, serverWaitTime, timeout, c, s);
				retryAcceptMessageSessionAsyncResult.Start();
				return retryAcceptMessageSessionAsyncResult;
			}, new Func<IAsyncResult, MessageSession>(SubscriptionClient.RetryAcceptMessageSessionAsyncResult.End));
		}

		private IAsyncResult BeginInternalGetMessageSessions(DateTime lastUpdatedTime, AsyncCallback callback, object state)
		{
			base.ThrowIfDisposed();
			if (!this.openOnceManager.ShouldOpen)
			{
				SubscriptionClient.RetryGetMessageSessionsAsyncResult retryGetMessageSessionsAsyncResult1 = new SubscriptionClient.RetryGetMessageSessionsAsyncResult(this, null, lastUpdatedTime, callback, state);
				retryGetMessageSessionsAsyncResult1.Start();
				return retryGetMessageSessionsAsyncResult1;
			}
			return this.openOnceManager.Begin<IEnumerable<MessageSession>>(callback, state, (AsyncCallback c, object s) => {
				SubscriptionClient.RetryGetMessageSessionsAsyncResult retryGetMessageSessionsAsyncResult = new SubscriptionClient.RetryGetMessageSessionsAsyncResult(this, null, lastUpdatedTime, c, s);
				retryGetMessageSessionsAsyncResult.Start();
				return retryGetMessageSessionsAsyncResult;
			}, new Func<IAsyncResult, IEnumerable<MessageSession>>(SubscriptionClient.RetryGetMessageSessionsAsyncResult.End));
		}

		public IAsyncResult BeginPeek(AsyncCallback callback, object state)
		{
			this.ThrowIfBrowserNull("BeginPeek");
			return this.InternalBrowser.BeginPeek(callback, state);
		}

		public IAsyncResult BeginPeek(long fromSequenceNumber, AsyncCallback callback, object state)
		{
			this.ThrowIfBrowserNull("BeginPeek");
			return this.InternalBrowser.BeginPeek(fromSequenceNumber, callback, state);
		}

		public IAsyncResult BeginPeekBatch(int messageCount, AsyncCallback callback, object state)
		{
			this.ThrowIfBrowserNull("BeginPeekBatch");
			return this.InternalBrowser.BeginPeekBatch(messageCount, callback, state);
		}

		public IAsyncResult BeginPeekBatch(long fromSequenceNumber, int messageCount, AsyncCallback callback, object state)
		{
			this.ThrowIfBrowserNull("BeginPeekBatch");
			return this.InternalBrowser.BeginPeekBatch(fromSequenceNumber, messageCount, callback, state);
		}

		public IAsyncResult BeginReceive(AsyncCallback callback, object state)
		{
			this.ThrowIfReceiverNull("BeginReceive");
			return this.InternalReceiver.BeginReceive(callback, state);
		}

		public IAsyncResult BeginReceive(TimeSpan serverWaitTime, AsyncCallback callback, object state)
		{
			this.ThrowIfReceiverNull("BeginReceive");
			return this.InternalReceiver.BeginReceive(serverWaitTime, callback, state);
		}

		public IAsyncResult BeginReceive(long sequenceNumber, AsyncCallback callback, object state)
		{
			this.ThrowIfReceiverNull("BeginReceive");
			return this.InternalReceiver.BeginReceive(sequenceNumber, callback, state);
		}

		public IAsyncResult BeginReceiveBatch(int messageCount, AsyncCallback callback, object state)
		{
			this.ThrowIfReceiverNull("BeginReceiveBatch");
			return this.InternalReceiver.BeginReceiveBatch(messageCount, callback, state);
		}

		public IAsyncResult BeginReceiveBatch(int messageCount, TimeSpan serverWaitTime, AsyncCallback callback, object state)
		{
			this.ThrowIfReceiverNull("BeginReceiveBatch");
			return this.InternalReceiver.BeginReceiveBatch(messageCount, serverWaitTime, callback, state);
		}

		public IAsyncResult BeginReceiveBatch(IEnumerable<long> sequenceNumbers, AsyncCallback callback, object state)
		{
			this.ThrowIfReceiverNull("BeginReceiveBatch");
			return this.InternalReceiver.BeginReceiveBatch(sequenceNumbers, callback, state);
		}

		public IAsyncResult BeginRemoveRule(string ruleName, AsyncCallback callback, object state)
		{
			return this.BeginRemoveRule(ruleName, this.MessagingFactory.OperationTimeout, callback, state);
		}

		internal IAsyncResult BeginRemoveRule(string ruleName, TimeSpan timeout, AsyncCallback callback, object state)
		{
			base.ThrowIfDisposed();
			if (string.IsNullOrWhiteSpace(ruleName))
			{
				throw FxTrace.Exception.ArgumentNullOrEmpty("ruleName");
			}
			if (!this.openOnceManager.ShouldOpen)
			{
				SubscriptionClient.RetryRemoveRuleAsyncResult retryRemoveRuleAsyncResult1 = new SubscriptionClient.RetryRemoveRuleAsyncResult(this, null, ruleName, null, timeout, callback, state);
				retryRemoveRuleAsyncResult1.Start();
				return retryRemoveRuleAsyncResult1;
			}
			return this.openOnceManager.Begin(callback, state, (AsyncCallback c, object s) => {
				SubscriptionClient.RetryRemoveRuleAsyncResult retryRemoveRuleAsyncResult = new SubscriptionClient.RetryRemoveRuleAsyncResult(this, null, ruleName, null, timeout, c, s);
				retryRemoveRuleAsyncResult.Start();
				return retryRemoveRuleAsyncResult;
			}, new Action<IAsyncResult>(SubscriptionClient.RetryRemoveRuleAsyncResult.End));
		}

		internal IAsyncResult BeginRemoveRulesByTag(string tag, AsyncCallback callback, object state)
		{
			return this.BeginRemoveRulesByTag(tag, this.MessagingFactory.OperationTimeout, null, null);
		}

		internal IAsyncResult BeginRemoveRulesByTag(string tag, TimeSpan timeout, AsyncCallback callback, object state)
		{
			base.ThrowIfDisposed();
			if (!this.openOnceManager.ShouldOpen)
			{
				SubscriptionClient.RetryRemoveRuleAsyncResult retryRemoveRuleAsyncResult1 = new SubscriptionClient.RetryRemoveRuleAsyncResult(this, null, null, tag, timeout, callback, state);
				retryRemoveRuleAsyncResult1.Start();
				return retryRemoveRuleAsyncResult1;
			}
			return this.openOnceManager.Begin(callback, state, (AsyncCallback c, object s) => {
				SubscriptionClient.RetryRemoveRuleAsyncResult retryRemoveRuleAsyncResult = new SubscriptionClient.RetryRemoveRuleAsyncResult(this, null, null, tag, timeout, c, s);
				retryRemoveRuleAsyncResult.Start();
				return retryRemoveRuleAsyncResult;
			}, new Action<IAsyncResult>(SubscriptionClient.RetryRemoveRuleAsyncResult.End));
		}

		public void Complete(Guid lockToken)
		{
			this.ThrowIfReceiverNull("Complete");
			this.InternalReceiver.Complete(lockToken);
		}

		public Task CompleteAsync(Guid lockToken)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginComplete(lockToken, c, s), new Action<IAsyncResult>(this.EndComplete));
		}

		public void CompleteBatch(IEnumerable<Guid> lockTokens)
		{
			this.ThrowIfReceiverNull("CompleteBatch");
			this.InternalReceiver.CompleteBatch(lockTokens);
		}

		public Task CompleteBatchAsync(IEnumerable<Guid> lockTokens)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginCompleteBatch(lockTokens, c, s), new Action<IAsyncResult>(this.EndCompleteBatch));
		}

		public static SubscriptionClient Create(string topicPath, string name)
		{
			return (new KeyValueConfigurationManager()).CreateMessagingFactory().CreateSubscriptionClient(topicPath, name);
		}

		public static SubscriptionClient Create(string topicPath, string name, ReceiveMode mode)
		{
			return (new KeyValueConfigurationManager()).CreateMessagingFactory().CreateSubscriptionClient(topicPath, name, mode);
		}

		public static SubscriptionClient CreateFromConnectionString(string connectionString, string topicPath, string name)
		{
			return (new KeyValueConfigurationManager(connectionString)).CreateMessagingFactory().CreateSubscriptionClient(topicPath, name);
		}

		public static SubscriptionClient CreateFromConnectionString(string connectionString, string topicPath, string name, ReceiveMode mode)
		{
			KeyValueConfigurationManager keyValueConfigurationManager = new KeyValueConfigurationManager(connectionString);
			return keyValueConfigurationManager.CreateMessagingFactory().CreateSubscriptionClient(topicPath, name, mode);
		}

		public void DeadLetter(Guid lockToken)
		{
			this.ThrowIfReceiverNull("DeadLetter");
			this.DeadLetter(lockToken, null);
		}

		public void DeadLetter(Guid lockToken, IDictionary<string, object> propertiesToModify)
		{
			this.ThrowIfReceiverNull("DeadLetter");
			this.InternalReceiver.DeadLetter(lockToken, propertiesToModify);
		}

		public void DeadLetter(Guid lockToken, string deadLetterReason, string deadLetterErrorDescription)
		{
			this.ThrowIfReceiverNull("DeadLetter");
			this.InternalReceiver.DeadLetter(lockToken, deadLetterReason, deadLetterErrorDescription);
		}

		public Task DeadLetterAsync(Guid lockToken)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginDeadLetter(lockToken, c, s), new Action<IAsyncResult>(this.EndDeadLetter));
		}

		public Task DeadLetterAsync(Guid lockToken, IDictionary<string, object> propertiesToModify)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginDeadLetter(lockToken, propertiesToModify, c, s), new Action<IAsyncResult>(this.EndDeadLetter));
		}

		public Task DeadLetterAsync(Guid lockToken, string deadLetterReason, string deadLetterErrorDescription)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginDeadLetter(lockToken, deadLetterReason, deadLetterErrorDescription, c, s), new Action<IAsyncResult>(this.EndDeadLetter));
		}

		public void Defer(Guid lockToken)
		{
			this.ThrowIfReceiverNull("Defer");
			this.Defer(lockToken, null);
		}

		public void Defer(Guid lockToken, IDictionary<string, object> propertiesToModify)
		{
			this.ThrowIfReceiverNull("Defer");
			this.InternalReceiver.Defer(lockToken, propertiesToModify);
		}

		public Task DeferAsync(Guid lockToken)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginDefer(lockToken, c, s), new Action<IAsyncResult>(this.EndDefer));
		}

		public Task DeferAsync(Guid lockToken, IDictionary<string, object> propertiesToModify)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginDefer(lockToken, propertiesToModify, c, s), new Action<IAsyncResult>(this.EndDefer));
		}

		public void EndAbandon(IAsyncResult result)
		{
			this.ThrowIfReceiverNull("EndAbandon");
			this.InternalReceiver.EndAbandon(result);
		}

		public MessageSession EndAcceptMessageSession(IAsyncResult result)
		{
			MessageSession messageSession;
			messageSession = (!OpenOnceManager.ShouldEnd<MessageSession>(result) ? SubscriptionClient.RetryAcceptMessageSessionAsyncResult.End(result) : OpenOnceManager.End<MessageSession>(result));
			messageSession.ShouldLinkRetryPolicy = true;
			this.RegisterMessageClientEntity(messageSession);
			return messageSession;
		}

		public void EndAddRule(IAsyncResult result)
		{
			if (OpenOnceManager.ShouldEnd(result))
			{
				OpenOnceManager.End(result);
				return;
			}
			this.OnEndAddRule(result);
		}

		public void EndComplete(IAsyncResult result)
		{
			this.ThrowIfReceiverNull("EndComplete");
			this.InternalReceiver.EndComplete(result);
		}

		public void EndCompleteBatch(IAsyncResult result)
		{
			this.ThrowIfReceiverNull("EndCompleteBatch");
			this.InternalReceiver.EndCompleteBatch(result);
		}

		private MessageBrowser EndCreateBrowser(IAsyncResult result)
		{
			MessageBrowser messageBrowser;
			messageBrowser = (!OpenOnceManager.ShouldEnd<MessageBrowser>(result) ? this.OnEndCreateBrowser(result) : OpenOnceManager.End<MessageBrowser>(result));
			messageBrowser.ShouldLinkRetryPolicy = true;
			this.RegisterMessageClientEntity(messageBrowser);
			return messageBrowser;
		}

		private MessageReceiver EndCreateReceiver(IAsyncResult result)
		{
			MessageReceiver messageReceiver;
			messageReceiver = (!OpenOnceManager.ShouldEnd<MessageReceiver>(result) ? this.OnEndCreateReceiver(result) : OpenOnceManager.End<MessageReceiver>(result));
			messageReceiver.ShouldLinkRetryPolicy = true;
			this.RegisterMessageClientEntity(messageReceiver);
			return messageReceiver;
		}

		public void EndDeadLetter(IAsyncResult result)
		{
			this.ThrowIfReceiverNull("EndDeadLetter");
			this.InternalReceiver.EndDeadLetter(result);
		}

		public void EndDefer(IAsyncResult result)
		{
			this.ThrowIfReceiverNull("EndDefer");
			this.InternalReceiver.EndDefer(result);
		}

		public IEnumerable<MessageSession> EndGetMessageSessions(IAsyncResult result)
		{
			IEnumerable<MessageSession> messageSessions;
			messageSessions = (!OpenOnceManager.ShouldEnd<IEnumerable<MessageSession>>(result) ? SubscriptionClient.RetryGetMessageSessionsAsyncResult.End(result) : OpenOnceManager.End<IEnumerable<MessageSession>>(result));
			foreach (MessageSession messageSession in messageSessions)
			{
				messageSession.ShouldLinkRetryPolicy = true;
				this.RegisterMessageClientEntity(messageSession);
			}
			return messageSessions;
		}

		public BrokeredMessage EndPeek(IAsyncResult result)
		{
			this.ThrowIfBrowserNull("EndPeek");
			return this.InternalBrowser.EndPeek(result);
		}

		public IEnumerable<BrokeredMessage> EndPeekBatch(IAsyncResult result)
		{
			this.ThrowIfBrowserNull("EndPeekBatch");
			return this.InternalBrowser.EndPeekBatch(result);
		}

		public BrokeredMessage EndReceive(IAsyncResult result)
		{
			this.ThrowIfReceiverNull("EndReceive");
			return this.InternalReceiver.EndReceive(result);
		}

		public IEnumerable<BrokeredMessage> EndReceiveBatch(IAsyncResult result)
		{
			this.ThrowIfReceiverNull("EndReceiveBatch");
			return this.InternalReceiver.EndReceiveBatch(result);
		}

		public void EndRemoveRule(IAsyncResult result)
		{
			if (OpenOnceManager.ShouldEnd(result))
			{
				OpenOnceManager.End(result);
				return;
			}
			SubscriptionClient.RetryRemoveRuleAsyncResult.End(result);
		}

		internal void EndRemoveRulesByTag(IAsyncResult result)
		{
			if (OpenOnceManager.ShouldEnd(result))
			{
				OpenOnceManager.End(result);
				return;
			}
			SubscriptionClient.RetryRemoveRuleAsyncResult.End(result);
		}

		private void EnsureCreateInternalBrowser()
		{
			if (this.InternalBrowser == null)
			{
				lock (base.ThisLock)
				{
					if (this.InternalBrowser == null)
					{
						this.InternalBrowser = this.EndCreateBrowser(this.BeginCreateBrowser(this.MessagingFactory.OperationTimeout, null, null));
					}
				}
			}
		}

		private void EnsureCreateInternalReceiver()
		{
			if (this.InternalReceiver == null)
			{
				lock (base.ThisLock)
				{
					if (this.InternalReceiver == null)
					{
						if (this.IsSubQueue)
						{
							this.InternalReceiver = this.EndCreateReceiver(this.BeginCreateReceiver(this.SubscriptionPath, this.Mode, this.MessagingFactory.OperationTimeout, null, null));
						}
						else
						{
							this.InternalReceiver = this.EndCreateReceiver(this.BeginCreateReceiver(this.Mode, this.MessagingFactory.OperationTimeout, null, null));
						}
					}
				}
			}
		}

		public static string FormatDeadLetterPath(string topicPath, string subscriptionName)
		{
			return EntityNameHelper.FormatSubQueuePath(SubscriptionClient.FormatSubscriptionPath(topicPath, subscriptionName), "$DeadLetterQueue");
		}

		public static string FormatSubscriptionPath(string topicPath, string subscriptionName)
		{
			return EntityNameHelper.FormatSubscriptionPath(topicPath, subscriptionName);
		}

		public IEnumerable<MessageSession> GetMessageSessions()
		{
			return this.EndGetMessageSessions(this.BeginGetMessageSessions(null, null));
		}

		public IEnumerable<MessageSession> GetMessageSessions(DateTime lastUpdatedTime)
		{
			return this.EndGetMessageSessions(this.BeginGetMessageSessions(lastUpdatedTime, null, null));
		}

		public Task<IEnumerable<MessageSession>> GetMessageSessionsAsync()
		{
			return TaskHelpers.CreateTask<IEnumerable<MessageSession>>(new Func<AsyncCallback, object, IAsyncResult>(this.BeginGetMessageSessions), new Func<IAsyncResult, IEnumerable<MessageSession>>(this.EndGetMessageSessions));
		}

		public Task<IEnumerable<MessageSession>> GetMessageSessionsAsync(DateTime lastUpdatedTime)
		{
			return TaskHelpers.CreateTask<IEnumerable<MessageSession>>((AsyncCallback c, object s) => this.BeginGetMessageSessions(lastUpdatedTime, c, s), new Func<IAsyncResult, IEnumerable<MessageSession>>(this.EndGetMessageSessions));
		}

		protected override void OnAbort()
		{
			SubscriptionClient.CloseOrAbortAsyncResult closeOrAbortAsyncResult = new SubscriptionClient.CloseOrAbortAsyncResult(this, true, TimeSpan.MaxValue, null, null);
			closeOrAbortAsyncResult.RunSynchronously();
		}

		protected abstract IAsyncResult OnBeginAcceptMessageSession(string sessionId, ReceiveMode receiveMode, TimeSpan serverWaitTime, TimeSpan timeout, AsyncCallback callback, object state);

		protected abstract IAsyncResult OnBeginAddRule(RuleDescription description, TimeSpan timeout, AsyncCallback callback, object state);

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return (new SubscriptionClient.CloseOrAbortAsyncResult(this, false, timeout, callback, state)).Start();
		}

		internal abstract IAsyncResult OnBeginCreateBrowser(TimeSpan timeout, AsyncCallback callback, object state);

		protected abstract IAsyncResult OnBeginCreateReceiver(ReceiveMode receiveMode, TimeSpan timeout, AsyncCallback callback, object state);

		protected abstract IAsyncResult OnBeginCreateReceiver(string subQueuePath, string subQueueName, ReceiveMode receiveMode, TimeSpan timeout, AsyncCallback callback, object state);

		protected abstract IAsyncResult OnBeginGetMessageSessions(DateTime lastUpdatedTime, AsyncCallback callback, object state);

		protected abstract IAsyncResult OnBeginRemoveRule(string ruleName, TimeSpan timeout, AsyncCallback callback, object state);

		protected abstract IAsyncResult OnBeginRemoveRulesByTag(string tag, TimeSpan timeout, AsyncCallback callback, object state);

		protected override void OnClose(TimeSpan timeout)
		{
			SubscriptionClient.CloseOrAbortAsyncResult closeOrAbortAsyncResult = new SubscriptionClient.CloseOrAbortAsyncResult(this, false, timeout, null, null);
			closeOrAbortAsyncResult.RunSynchronously();
		}

		protected abstract MessageSession OnEndAcceptMessageSession(IAsyncResult result);

		protected abstract void OnEndAddRule(IAsyncResult result);

		protected override void OnEndClose(IAsyncResult result)
		{
			AsyncResult<SubscriptionClient.CloseOrAbortAsyncResult>.End(result);
		}

		internal abstract MessageBrowser OnEndCreateBrowser(IAsyncResult result);

		protected abstract MessageReceiver OnEndCreateReceiver(IAsyncResult result);

		protected abstract IEnumerable<MessageSession> OnEndGetMessageSessions(IAsyncResult result);

		protected abstract void OnEndRemoveRule(IAsyncResult result);

		protected abstract void OnEndRemoveRules(IAsyncResult result);

		public void OnMessage(Action<BrokeredMessage> callback)
		{
			this.EnsureCreateInternalReceiver();
			OnMessageOptions onMessageOption = new OnMessageOptions()
			{
				ReceiveTimeOut = this.OperationTimeout
			};
			this.OnMessage(callback, onMessageOption);
		}

		public void OnMessage(Action<BrokeredMessage> callback, OnMessageOptions onMessageOptions)
		{
			this.EnsureCreateInternalReceiver();
			onMessageOptions.MessageClientEntity = this;
			onMessageOptions.ReceiveTimeOut = this.OperationTimeout;
			this.InternalReceiver.OnMessage(callback, onMessageOptions);
		}

		public void OnMessageAsync(Func<BrokeredMessage, Task> callback)
		{
			this.EnsureCreateInternalReceiver();
			MessageReceiver internalReceiver = this.InternalReceiver;
			OnMessageOptions onMessageOption = new OnMessageOptions()
			{
				ReceiveTimeOut = this.OperationTimeout
			};
			internalReceiver.OnMessageAsync(callback, onMessageOption);
		}

		public void OnMessageAsync(Func<BrokeredMessage, Task> callback, OnMessageOptions onMessageOptions)
		{
			this.EnsureCreateInternalReceiver();
			onMessageOptions.MessageClientEntity = this;
			this.InternalReceiver.OnMessageAsync(callback, onMessageOptions);
		}

		public BrokeredMessage Peek()
		{
			this.ThrowIfBrowserNull("Peek");
			return this.InternalBrowser.Peek();
		}

		public BrokeredMessage Peek(long fromSequenceNumber)
		{
			this.ThrowIfBrowserNull("Peek");
			return this.InternalBrowser.Peek(fromSequenceNumber);
		}

		public Task<BrokeredMessage> PeekAsync()
		{
			return TaskHelpers.CreateTask<BrokeredMessage>(new Func<AsyncCallback, object, IAsyncResult>(this.BeginPeek), new Func<IAsyncResult, BrokeredMessage>(this.EndPeek));
		}

		public Task<BrokeredMessage> PeekAsync(long fromSequenceNumber)
		{
			return TaskHelpers.CreateTask<BrokeredMessage>((AsyncCallback c, object s) => this.BeginPeek(fromSequenceNumber, c, s), new Func<IAsyncResult, BrokeredMessage>(this.EndPeek));
		}

		public IEnumerable<BrokeredMessage> PeekBatch(int messageCount)
		{
			this.ThrowIfBrowserNull("PeekBatch");
			return this.InternalBrowser.PeekBatch(messageCount);
		}

		public IEnumerable<BrokeredMessage> PeekBatch(long fromSequenceNumber, int messageCount)
		{
			this.ThrowIfBrowserNull("PeekBatch");
			return this.InternalBrowser.PeekBatch(fromSequenceNumber, messageCount);
		}

		public Task<IEnumerable<BrokeredMessage>> PeekBatchAsync(int messageCount)
		{
			return TaskHelpers.CreateTask<IEnumerable<BrokeredMessage>>((AsyncCallback c, object s) => this.BeginPeekBatch(messageCount, c, s), new Func<IAsyncResult, IEnumerable<BrokeredMessage>>(this.EndPeekBatch));
		}

		public Task<IEnumerable<BrokeredMessage>> PeekBatchAsync(long fromSequenceNumber, int messageCount)
		{
			return TaskHelpers.CreateTask<IEnumerable<BrokeredMessage>>((AsyncCallback c, object s) => this.BeginPeekBatch(fromSequenceNumber, messageCount, c, s), new Func<IAsyncResult, IEnumerable<BrokeredMessage>>(this.EndPeekBatch));
		}

		public BrokeredMessage Receive()
		{
			this.ThrowIfReceiverNull("Receive");
			return this.InternalReceiver.Receive();
		}

		public BrokeredMessage Receive(TimeSpan serverWaitTime)
		{
			this.ThrowIfReceiverNull("Receive");
			return this.InternalReceiver.Receive(serverWaitTime);
		}

		public BrokeredMessage Receive(long sequenceNumber)
		{
			this.ThrowIfReceiverNull("Receive");
			return this.InternalReceiver.Receive(sequenceNumber);
		}

		public Task<BrokeredMessage> ReceiveAsync()
		{
			return TaskHelpers.CreateTask<BrokeredMessage>(new Func<AsyncCallback, object, IAsyncResult>(this.BeginReceive), new Func<IAsyncResult, BrokeredMessage>(this.EndReceive));
		}

		public Task<BrokeredMessage> ReceiveAsync(TimeSpan serverWaitTime)
		{
			return TaskHelpers.CreateTask<BrokeredMessage>((AsyncCallback c, object s) => this.BeginReceive(serverWaitTime, c, s), new Func<IAsyncResult, BrokeredMessage>(this.EndReceive));
		}

		public Task<BrokeredMessage> ReceiveAsync(long sequenceNumber)
		{
			return TaskHelpers.CreateTask<BrokeredMessage>((AsyncCallback c, object s) => this.BeginReceive(sequenceNumber, c, s), new Func<IAsyncResult, BrokeredMessage>(this.EndReceive));
		}

		public IEnumerable<BrokeredMessage> ReceiveBatch(int messageCount)
		{
			return this.ReceiveBatch(messageCount, this.OperationTimeout);
		}

		public IEnumerable<BrokeredMessage> ReceiveBatch(int messageCount, TimeSpan serverWaitTime)
		{
			this.ThrowIfReceiverNull("ReceiveBatch");
			return this.InternalReceiver.ReceiveBatch(messageCount, serverWaitTime);
		}

		public IEnumerable<BrokeredMessage> ReceiveBatch(IEnumerable<long> sequenceNumbers)
		{
			this.ThrowIfReceiverNull("ReceiveBatch");
			return this.InternalReceiver.ReceiveBatch(sequenceNumbers);
		}

		public Task<IEnumerable<BrokeredMessage>> ReceiveBatchAsync(int messageCount)
		{
			return TaskHelpers.CreateTask<IEnumerable<BrokeredMessage>>((AsyncCallback c, object s) => this.BeginReceiveBatch(messageCount, c, s), new Func<IAsyncResult, IEnumerable<BrokeredMessage>>(this.EndReceiveBatch));
		}

		public Task<IEnumerable<BrokeredMessage>> ReceiveBatchAsync(int messageCount, TimeSpan serverWaitTime)
		{
			return TaskHelpers.CreateTask<IEnumerable<BrokeredMessage>>((AsyncCallback c, object s) => this.BeginReceiveBatch(messageCount, serverWaitTime, c, s), new Func<IAsyncResult, IEnumerable<BrokeredMessage>>(this.EndReceiveBatch));
		}

		public Task<IEnumerable<BrokeredMessage>> ReceiveBatchAsync(IEnumerable<long> sequenceNumbers)
		{
			return TaskHelpers.CreateTask<IEnumerable<BrokeredMessage>>((AsyncCallback c, object s) => this.BeginReceiveBatch(sequenceNumbers, c, s), new Func<IAsyncResult, IEnumerable<BrokeredMessage>>(this.EndReceiveBatch));
		}

		internal void RegisterMessageClientEntity(MessageClientEntity child)
		{
			base.ClientEntityManager.Add(child);
		}

		public void RegisterSessionHandler(Type handlerType)
		{
			base.ThrowIfDisposed();
			this.pumpHost.RegisterSessionHandler(handlerType);
		}

		public void RegisterSessionHandler(Type handlerType, SessionHandlerOptions options)
		{
			base.ThrowIfDisposed();
			this.pumpHost.RegisterSessionHandler(handlerType, options);
		}

		public Task RegisterSessionHandlerAsync(Type handlerType)
		{
			base.ThrowIfDisposed();
			return this.pumpHost.RegisterSessionHandlerAsync(handlerType);
		}

		public Task RegisterSessionHandlerAsync(Type handlerType, SessionHandlerOptions options)
		{
			base.ThrowIfDisposed();
			return this.pumpHost.RegisterSessionHandlerAsync(handlerType, options);
		}

		public void RegisterSessionHandlerFactory(IMessageSessionHandlerFactory factory, SessionHandlerOptions options)
		{
			base.ThrowIfDisposed();
			this.pumpHost.RegisterSessionHandlerFactory(factory, options);
		}

		public void RegisterSessionHandlerFactory(IMessageSessionAsyncHandlerFactory factory, SessionHandlerOptions options)
		{
			base.ThrowIfDisposed();
			this.pumpHost.RegisterSessionHandlerFactory(factory, options);
		}

		public Task RegisterSessionHandlerFactoryAsync(IMessageSessionHandlerFactory factory, SessionHandlerOptions options)
		{
			base.ThrowIfDisposed();
			return this.pumpHost.RegisterSessionHandlerFactoryAsync(factory, options);
		}

		public Task RegisterSessionHandlerFactoryAsync(IMessageSessionAsyncHandlerFactory factory, SessionHandlerOptions options)
		{
			base.ThrowIfDisposed();
			return this.pumpHost.RegisterSessionHandlerFactoryAsync(factory, options);
		}

		public void RemoveRule(string ruleName)
		{
			this.EndRemoveRule(this.BeginRemoveRule(ruleName, this.MessagingFactory.OperationTimeout, null, null));
		}

		public Task RemoveRuleAsync(string ruleName)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginRemoveRule(ruleName, c, s), new Action<IAsyncResult>(this.EndRemoveRule));
		}

		internal void RemoveRulesByTag(string tag)
		{
			this.EndRemoveRulesByTag(this.BeginRemoveRulesByTag(tag, this.MessagingFactory.OperationTimeout, null, null));
		}

		private void ThrowIfBrowserNull(string operationName)
		{
			this.EnsureCreateInternalBrowser();
			if (this.InternalBrowser == null)
			{
				throw FxTrace.Exception.AsError(new InvalidOperationException(SRCore.UnsupportedOperation(operationName)), null);
			}
		}

		private void ThrowIfReceiverNull(string operationName)
		{
			this.EnsureCreateInternalReceiver();
			if (this.InternalReceiver == null)
			{
				throw FxTrace.Exception.AsError(new InvalidOperationException(SRCore.UnsupportedOperation(operationName)), null);
			}
		}

		private sealed class CloseOrAbortAsyncResult : IteratorAsyncResult<SubscriptionClient.CloseOrAbortAsyncResult>
		{
			private readonly SubscriptionClient owner;

			private readonly bool shouldAbort;

			public CloseOrAbortAsyncResult(SubscriptionClient owner, bool shouldAbort, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.owner = owner;
				this.shouldAbort = shouldAbort;
			}

			protected override IEnumerator<IteratorAsyncResult<SubscriptionClient.CloseOrAbortAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				if (!this.shouldAbort)
				{
					SubscriptionClient.CloseOrAbortAsyncResult closeOrAbortAsyncResult = this;
					IteratorAsyncResult<SubscriptionClient.CloseOrAbortAsyncResult>.BeginCall beginCall = (SubscriptionClient.CloseOrAbortAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.owner.ClientEntityManager.BeginClose(t, c, s);
					yield return closeOrAbortAsyncResult.CallAsync(beginCall, (SubscriptionClient.CloseOrAbortAsyncResult thisPtr, IAsyncResult r) => thisPtr.owner.ClientEntityManager.EndClose(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				}
				else
				{
					this.owner.ClientEntityManager.Abort();
				}
				if (this.owner.pumpHost != null)
				{
					if (!this.shouldAbort)
					{
						SubscriptionClient.CloseOrAbortAsyncResult closeOrAbortAsyncResult1 = this;
						IteratorAsyncResult<SubscriptionClient.CloseOrAbortAsyncResult>.BeginCall beginCall1 = (SubscriptionClient.CloseOrAbortAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.owner.pumpHost.BeginClose(t, c, s);
						IteratorAsyncResult<SubscriptionClient.CloseOrAbortAsyncResult>.EndCall endCall = (SubscriptionClient.CloseOrAbortAsyncResult thisPtr, IAsyncResult r) => thisPtr.owner.pumpHost.EndClose(r);
						yield return closeOrAbortAsyncResult1.CallAsync(beginCall1, endCall, (SubscriptionClient.CloseOrAbortAsyncResult thisPtr, TimeSpan t) => thisPtr.owner.pumpHost.Close(t), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
					}
					else
					{
						this.owner.pumpHost.Abort();
					}
				}
			}
		}

		private sealed class RetryAcceptMessageSessionAsyncResult : RetryAsyncResult<SubscriptionClient.RetryAcceptMessageSessionAsyncResult>
		{
			private readonly SubscriptionClient client;

			private readonly TrackingContext trackingContext;

			private readonly string sessionId;

			private readonly ReceiveMode receiveMode;

			private readonly TimeSpan serverWaitTime;

			private readonly TimeSpan operationTimeout;

			public MessageSession Session
			{
				get;
				private set;
			}

			public RetryAcceptMessageSessionAsyncResult(SubscriptionClient client, TrackingContext trackingContext, string sessionId, ReceiveMode receiveMode, TimeSpan serverWaitTime, TimeSpan timeout, AsyncCallback callback, object state) : base(serverWaitTime, callback, state)
			{
				if (client == null)
				{
					throw Fx.Exception.ArgumentNull("client");
				}
				this.client = client;
				this.sessionId = sessionId;
				this.receiveMode = receiveMode;
				this.serverWaitTime = serverWaitTime;
				this.operationTimeout = timeout;
				this.trackingContext = trackingContext ?? TrackingContext.GetInstance(Guid.NewGuid(), this.client.Name);
				MessagingPerformanceCounters.IncrementPendingAcceptMessageSessionCount(this.client.MessagingFactory.Address, 1);
			}

			public static new MessageSession End(IAsyncResult r)
			{
				MessageSession session;
				SubscriptionClient.RetryAcceptMessageSessionAsyncResult retryAcceptMessageSessionAsyncResult = r as SubscriptionClient.RetryAcceptMessageSessionAsyncResult;
				try
				{
					retryAcceptMessageSessionAsyncResult = AsyncResult<SubscriptionClient.RetryAcceptMessageSessionAsyncResult>.End(r);
					session = retryAcceptMessageSessionAsyncResult.Session;
				}
				finally
				{
					if (retryAcceptMessageSessionAsyncResult != null && retryAcceptMessageSessionAsyncResult.client != null)
					{
						MessagingPerformanceCounters.DecrementPendingAcceptMessageSessionCount(retryAcceptMessageSessionAsyncResult.client.MessagingFactory.Address, 1);
					}
				}
				return session;
			}

			protected override IEnumerator<IteratorAsyncResult<SubscriptionClient.RetryAcceptMessageSessionAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				TimeSpan timeSpan;
				bool flag;
				bool flag1;
				int num = 0;
				timeSpan = (this.client.RetryPolicy.IsServerBusy ? Microsoft.ServiceBus.RetryPolicy.ServerBusyBaseSleepTime : TimeSpan.Zero);
				TimeSpan timeSpan1 = timeSpan;
				if (!this.client.RetryPolicy.IsServerBusy || !(Microsoft.ServiceBus.RetryPolicy.ServerBusyBaseSleepTime >= base.OriginalTimeout))
				{
					do
					{
						flag1 = false;
						if (timeSpan1 != TimeSpan.Zero)
						{
							yield return base.CallAsyncSleep(timeSpan1);
						}
						SubscriptionClient.RetryAcceptMessageSessionAsyncResult retryAcceptMessageSessionAsyncResult = this;
						IteratorAsyncResult<SubscriptionClient.RetryAcceptMessageSessionAsyncResult>.BeginCall beginCall = (SubscriptionClient.RetryAcceptMessageSessionAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.client.OnBeginAcceptMessageSession(thisPtr.sessionId, thisPtr.receiveMode, thisPtr.serverWaitTime, thisPtr.operationTimeout, c, s);
						yield return retryAcceptMessageSessionAsyncResult.CallAsync(beginCall, (SubscriptionClient.RetryAcceptMessageSessionAsyncResult thisPtr, IAsyncResult r) => thisPtr.Session = thisPtr.client.OnEndAcceptMessageSession(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
						if (base.LastAsyncStepException == null)
						{
							this.client.RetryPolicy.ResetServerBusy();
						}
						else
						{
							MessagingPerformanceCounters.IncrementExceptionPerSec(this.client.MessagingFactory.Address, 1, base.LastAsyncStepException);
							flag = (base.TransactionExists ? false : this.client.RetryPolicy.ShouldRetry(base.RemainingTime(), num, base.LastAsyncStepException, out timeSpan1));
							flag1 = flag;
							if (!flag1)
							{
								continue;
							}
							MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRetryPolicyIteration(this.trackingContext.Activity, this.trackingContext.TrackingId, this.client.RetryPolicy.GetType().Name, "AcceptMessageSession", num, timeSpan1.ToString(), this.LastAsyncStepException.GetType().FullName, this.LastAsyncStepException.Message));
							num++;
						}
					}
					while (flag1);
					base.Complete(base.LastAsyncStepException);
				}
				else
				{
					string str = this.client.RetryPolicy.ServerBusyExceptionMessage;
					yield return base.CallAsyncSleep(base.RemainingTime());
					base.Complete(new ServerBusyException(str, this.trackingContext));
				}
			}
		}

		private sealed class RetryGetMessageSessionsAsyncResult : RetryAsyncResult<SubscriptionClient.RetryGetMessageSessionsAsyncResult>
		{
			private readonly SubscriptionClient client;

			private readonly TrackingContext trackingContext;

			private readonly DateTime lastUpdatedTime;

			public IEnumerable<MessageSession> Sessions
			{
				get;
				private set;
			}

			public RetryGetMessageSessionsAsyncResult(SubscriptionClient client, TrackingContext trackingContext, DateTime lastUpdatedTime, AsyncCallback callback, object state) : base(client.OperationTimeout, callback, state)
			{
				if (client == null)
				{
					throw Fx.Exception.ArgumentNull("client");
				}
				this.client = client;
				this.lastUpdatedTime = lastUpdatedTime;
				this.trackingContext = trackingContext ?? TrackingContext.GetInstance(Guid.NewGuid(), this.client.Name);
				MessagingPerformanceCounters.IncrementPendingAcceptMessageSessionCount(this.client.MessagingFactory.Address, 1);
			}

			public static new IEnumerable<MessageSession> End(IAsyncResult r)
			{
				IEnumerable<MessageSession> sessions;
				SubscriptionClient.RetryGetMessageSessionsAsyncResult retryGetMessageSessionsAsyncResult = r as SubscriptionClient.RetryGetMessageSessionsAsyncResult;
				try
				{
					retryGetMessageSessionsAsyncResult = AsyncResult<SubscriptionClient.RetryGetMessageSessionsAsyncResult>.End(r);
					sessions = retryGetMessageSessionsAsyncResult.Sessions;
				}
				finally
				{
					if (retryGetMessageSessionsAsyncResult != null && retryGetMessageSessionsAsyncResult.client != null)
					{
						MessagingPerformanceCounters.DecrementPendingAcceptMessageSessionCount(retryGetMessageSessionsAsyncResult.client.MessagingFactory.Address, 1);
					}
				}
				return sessions;
			}

			protected override IEnumerator<IteratorAsyncResult<SubscriptionClient.RetryGetMessageSessionsAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				TimeSpan timeSpan;
				bool flag;
				bool flag1;
				int num = 0;
				timeSpan = (this.client.RetryPolicy.IsServerBusy ? Microsoft.ServiceBus.RetryPolicy.ServerBusyBaseSleepTime : TimeSpan.Zero);
				TimeSpan timeSpan1 = timeSpan;
				if (!this.client.RetryPolicy.IsServerBusy || !(Microsoft.ServiceBus.RetryPolicy.ServerBusyBaseSleepTime >= base.OriginalTimeout))
				{
					do
					{
						flag1 = false;
						if (timeSpan1 != TimeSpan.Zero)
						{
							yield return base.CallAsyncSleep(timeSpan1);
						}
						SubscriptionClient.RetryGetMessageSessionsAsyncResult retryGetMessageSessionsAsyncResult = this;
						IteratorAsyncResult<SubscriptionClient.RetryGetMessageSessionsAsyncResult>.BeginCall beginCall = (SubscriptionClient.RetryGetMessageSessionsAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.client.OnBeginGetMessageSessions(thisPtr.lastUpdatedTime, c, s);
						yield return retryGetMessageSessionsAsyncResult.CallAsync(beginCall, (SubscriptionClient.RetryGetMessageSessionsAsyncResult thisPtr, IAsyncResult r) => thisPtr.Sessions = thisPtr.client.OnEndGetMessageSessions(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
						if (base.LastAsyncStepException == null)
						{
							this.client.RetryPolicy.ResetServerBusy();
						}
						else
						{
							MessagingPerformanceCounters.IncrementExceptionPerSec(this.client.MessagingFactory.Address, 1, base.LastAsyncStepException);
							flag = (base.TransactionExists ? false : this.client.RetryPolicy.ShouldRetry(base.RemainingTime(), num, base.LastAsyncStepException, out timeSpan1));
							flag1 = flag;
							if (!flag1)
							{
								continue;
							}
							MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRetryPolicyIteration(this.trackingContext.Activity, this.trackingContext.TrackingId, this.client.RetryPolicy.GetType().Name, "GetMessageSessions", num, timeSpan1.ToString(), this.LastAsyncStepException.GetType().FullName, this.LastAsyncStepException.Message));
							num++;
						}
					}
					while (flag1);
					base.Complete(base.LastAsyncStepException);
				}
				else
				{
					string str = this.client.RetryPolicy.ServerBusyExceptionMessage;
					yield return base.CallAsyncSleep(base.RemainingTime());
					base.Complete(new ServerBusyException(str, this.trackingContext));
				}
			}
		}

		private sealed class RetryRemoveRuleAsyncResult : RetryAsyncResult<SubscriptionClient.RetryRemoveRuleAsyncResult>
		{
			private readonly SubscriptionClient client;

			private readonly TrackingContext trackingContext;

			private readonly string ruleName;

			private readonly string tag;

			public RetryRemoveRuleAsyncResult(SubscriptionClient client, TrackingContext trackingContext, string ruleName, string tag, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				if (client == null)
				{
					throw Fx.Exception.ArgumentNull("client");
				}
				this.client = client;
				this.ruleName = ruleName;
				this.tag = tag;
				this.trackingContext = trackingContext ?? TrackingContext.GetInstance(Guid.NewGuid(), this.client.Name);
			}

			public static new void End(IAsyncResult r)
			{
				AsyncResult<SubscriptionClient.RetryRemoveRuleAsyncResult>.End(r);
			}

			protected override IEnumerator<IteratorAsyncResult<SubscriptionClient.RetryRemoveRuleAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				TimeSpan timeSpan;
				bool flag;
				bool flag1;
				int num = 0;
				timeSpan = (this.client.RetryPolicy.IsServerBusy ? Microsoft.ServiceBus.RetryPolicy.ServerBusyBaseSleepTime : TimeSpan.Zero);
				TimeSpan timeSpan1 = timeSpan;
				if (!this.client.RetryPolicy.IsServerBusy || !(Microsoft.ServiceBus.RetryPolicy.ServerBusyBaseSleepTime >= base.OriginalTimeout))
				{
					do
					{
						flag1 = false;
						if (timeSpan1 != TimeSpan.Zero)
						{
							yield return base.CallAsyncSleep(timeSpan1);
						}
						if (string.IsNullOrWhiteSpace(this.ruleName))
						{
							SubscriptionClient.RetryRemoveRuleAsyncResult retryRemoveRuleAsyncResult = this;
							Transaction ambientTransaction = base.AmbientTransaction;
							IteratorAsyncResult<SubscriptionClient.RetryRemoveRuleAsyncResult>.BeginCall beginCall = (SubscriptionClient.RetryRemoveRuleAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.client.OnBeginRemoveRulesByTag(thisPtr.tag, t, c, s);
							yield return retryRemoveRuleAsyncResult.CallTransactionalAsync(ambientTransaction, beginCall, (SubscriptionClient.RetryRemoveRuleAsyncResult thisPtr, IAsyncResult r) => thisPtr.client.OnEndRemoveRules(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
						}
						else
						{
							SubscriptionClient.RetryRemoveRuleAsyncResult retryRemoveRuleAsyncResult1 = this;
							Transaction transaction = base.AmbientTransaction;
							IteratorAsyncResult<SubscriptionClient.RetryRemoveRuleAsyncResult>.BeginCall beginCall1 = (SubscriptionClient.RetryRemoveRuleAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.client.OnBeginRemoveRule(thisPtr.ruleName, t, c, s);
							yield return retryRemoveRuleAsyncResult1.CallTransactionalAsync(transaction, beginCall1, (SubscriptionClient.RetryRemoveRuleAsyncResult thisPtr, IAsyncResult r) => thisPtr.client.OnEndRemoveRule(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
						}
						if (base.LastAsyncStepException is MessagingEntityNotFoundException)
						{
							base.LastAsyncStepException = null;
						}
						if (base.LastAsyncStepException == null)
						{
							this.client.RetryPolicy.ResetServerBusy();
						}
						else
						{
							MessagingPerformanceCounters.IncrementExceptionPerSec(this.client.MessagingFactory.Address, 1, base.LastAsyncStepException);
							flag = (base.TransactionExists ? false : this.client.RetryPolicy.ShouldRetry(base.RemainingTime(), num, base.LastAsyncStepException, out timeSpan1));
							flag1 = flag;
							if (!flag1)
							{
								continue;
							}
							MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRetryPolicyIteration(this.trackingContext.Activity, this.trackingContext.TrackingId, this.client.RetryPolicy.GetType().Name, (string.IsNullOrWhiteSpace(this.ruleName) ? "RemoveRulesByTag" : "RemoveRule"), num, timeSpan1.ToString(), this.LastAsyncStepException.GetType().FullName, this.LastAsyncStepException.Message));
							num++;
						}
					}
					while (flag1);
					base.Complete(base.LastAsyncStepException);
				}
				else
				{
					string str = this.client.RetryPolicy.ServerBusyExceptionMessage;
					yield return base.CallAsyncSleep(base.RemainingTime());
					base.Complete(new ServerBusyException(str, this.trackingContext));
				}
			}
		}
	}
}