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
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus.Messaging
{
	public abstract class QueueClient : MessageClientEntity, IMessageSessionEntity, IMessageClientEntity, IMessageSender, IMessageReceiver, IMessageBrowser
	{
		private readonly OpenOnceManager openOnceManager;

		private readonly MessageSessionPumpHost pumpHost;

		internal MessageBrowser InternalBrowser
		{
			get;
			set;
		}

		internal MessageReceiver InternalReceiver
		{
			get;
			set;
		}

		internal MessageSender InternalSender
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

		internal override TimeSpan OperationTimeout
		{
			get
			{
				return this.MessagingFactory.OperationTimeout;
			}
		}

		public string Path
		{
			get;
			private set;
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

		internal QueueClient(Microsoft.ServiceBus.Messaging.MessagingFactory messagingFactory, string path, ReceiveMode receiveMode)
		{
			base.ClientEntityManager = new MessageClientEntityManager();
			this.MessagingFactory = messagingFactory;
			this.Path = path;
			this.openOnceManager = new OpenOnceManager(this);
			this.Mode = receiveMode;
			this.IsSubQueue = MessagingUtilities.IsSubQueue(path);
			this.InternalReceiver = null;
			this.InternalSender = null;
			this.InternalBrowser = null;
			base.RetryPolicy = messagingFactory.RetryPolicy.Clone();
			this.pumpHost = new MessageSessionPumpHost(base.ThisLock, this.Path, this);
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

		internal IAsyncResult BeginCancelScheduledMessage(long sequenceNumber, AsyncCallback callback, object state)
		{
			this.ThrowIfSenderNull("BeginCancelScheduledMessage");
			return this.InternalSender.BeginCancelScheduledMessage(sequenceNumber, callback, state);
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
			QueueClient queueClient = this;
			return openOnceManager.Begin<MessageBrowser>(asyncCallback, obj, (AsyncCallback c, object s) => this.OnBeginCreateBrowser(timeout, c, s), new Func<IAsyncResult, MessageBrowser>(queueClient.OnEndCreateBrowser));
		}

		private IAsyncResult BeginCreateReceiver(string subQueuePath, ReceiveMode receiveMode, TimeSpan timeout, AsyncCallback callback, object state)
		{
			base.ThrowIfDisposed();
			if (!this.openOnceManager.ShouldOpen)
			{
				return this.OnBeginCreateReceiver(subQueuePath, receiveMode, timeout, callback, state);
			}
			OpenOnceManager openOnceManager = this.openOnceManager;
			AsyncCallback asyncCallback = callback;
			object obj = state;
			QueueClient queueClient = this;
			return openOnceManager.Begin<MessageReceiver>(asyncCallback, obj, (AsyncCallback c, object s) => this.OnBeginCreateReceiver(subQueuePath, receiveMode, timeout, c, s), new Func<IAsyncResult, MessageReceiver>(queueClient.OnEndCreateReceiver));
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
			QueueClient queueClient = this;
			return openOnceManager.Begin<MessageReceiver>(asyncCallback, obj, (AsyncCallback c, object s) => this.OnBeginCreateReceiver(receiveMode, timeout, c, s), new Func<IAsyncResult, MessageReceiver>(queueClient.OnEndCreateReceiver));
		}

		private IAsyncResult BeginCreateSender(TimeSpan timeout, AsyncCallback callback, object state)
		{
			base.ThrowIfDisposed();
			if (!this.openOnceManager.ShouldOpen)
			{
				return this.OnBeginCreateSender(timeout, callback, state);
			}
			OpenOnceManager openOnceManager = this.openOnceManager;
			AsyncCallback asyncCallback = callback;
			object obj = state;
			QueueClient queueClient = this;
			return openOnceManager.Begin<MessageSender>(asyncCallback, obj, (AsyncCallback c, object s) => this.OnBeginCreateSender(timeout, c, s), new Func<IAsyncResult, MessageSender>(queueClient.OnEndCreateSender));
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
				QueueClient.RetryAcceptMessageSessionAsyncResult retryAcceptMessageSessionAsyncResult1 = new QueueClient.RetryAcceptMessageSessionAsyncResult(this, null, sessionId, this.Mode, serverWaitTime, timeout, callback, state);
				retryAcceptMessageSessionAsyncResult1.Start();
				return retryAcceptMessageSessionAsyncResult1;
			}
			return this.openOnceManager.Begin<MessageSession>(callback, state, (AsyncCallback c, object s) => {
				QueueClient.RetryAcceptMessageSessionAsyncResult retryAcceptMessageSessionAsyncResult = new QueueClient.RetryAcceptMessageSessionAsyncResult(this, null, sessionId, this.Mode, serverWaitTime, timeout, c, s);
				retryAcceptMessageSessionAsyncResult.Start();
				return retryAcceptMessageSessionAsyncResult;
			}, new Func<IAsyncResult, MessageSession>(QueueClient.RetryAcceptMessageSessionAsyncResult.End));
		}

		private IAsyncResult BeginInternalGetMessageSessions(DateTime lastUpdatedTime, AsyncCallback callback, object state)
		{
			base.ThrowIfDisposed();
			if (!this.openOnceManager.ShouldOpen)
			{
				QueueClient.RetryGetMessageSessionsAsyncResult retryGetMessageSessionsAsyncResult1 = new QueueClient.RetryGetMessageSessionsAsyncResult(this, null, lastUpdatedTime, callback, state);
				retryGetMessageSessionsAsyncResult1.Start();
				return retryGetMessageSessionsAsyncResult1;
			}
			return this.openOnceManager.Begin<IEnumerable<MessageSession>>(callback, state, (AsyncCallback c, object s) => {
				QueueClient.RetryGetMessageSessionsAsyncResult retryGetMessageSessionsAsyncResult = new QueueClient.RetryGetMessageSessionsAsyncResult(this, null, lastUpdatedTime, c, s);
				retryGetMessageSessionsAsyncResult.Start();
				return retryGetMessageSessionsAsyncResult;
			}, new Func<IAsyncResult, IEnumerable<MessageSession>>(QueueClient.RetryGetMessageSessionsAsyncResult.End));
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

		internal IAsyncResult BeginScheduleMessage(BrokeredMessage message, DateTimeOffset scheduleEnqueueTime, AsyncCallback callback, object state)
		{
			this.ThrowIfSenderNull("BeginScheduleMessage");
			return this.InternalSender.BeginScheduleMessage(message, scheduleEnqueueTime, callback, state);
		}

		public IAsyncResult BeginSend(BrokeredMessage message, AsyncCallback callback, object state)
		{
			this.ThrowIfSenderNull("BeginSend");
			return this.InternalSender.BeginSend(message, callback, state);
		}

		public IAsyncResult BeginSendBatch(IEnumerable<BrokeredMessage> messages, AsyncCallback callback, object state)
		{
			this.ThrowIfSenderNull("BeginSendBatch");
			return this.InternalSender.BeginSendBatch(messages, callback, state);
		}

		internal Task CancelScheduledMessageAsync(long sequenceNumber)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginCancelScheduledMessage(sequenceNumber, c, s), new Action<IAsyncResult>(this.EndCancelScheduledMessage));
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

		public static QueueClient Create(string path)
		{
			return (new KeyValueConfigurationManager()).CreateMessagingFactory().CreateQueueClient(path);
		}

		public static QueueClient Create(string path, ReceiveMode mode)
		{
			return (new KeyValueConfigurationManager()).CreateMessagingFactory().CreateQueueClient(path, mode);
		}

		public static QueueClient CreateFromConnectionString(string connectionString, string path)
		{
			return (new KeyValueConfigurationManager(connectionString)).CreateMessagingFactory().CreateQueueClient(path);
		}

		public static QueueClient CreateFromConnectionString(string connectionString, string path, ReceiveMode mode)
		{
			return (new KeyValueConfigurationManager(connectionString)).CreateMessagingFactory().CreateQueueClient(path, mode);
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
			messageSession = (!OpenOnceManager.ShouldEnd<MessageSession>(result) ? QueueClient.RetryAcceptMessageSessionAsyncResult.End(result) : OpenOnceManager.End<MessageSession>(result));
			messageSession.ShouldLinkRetryPolicy = true;
			this.RegisterMessageClientEntity(messageSession);
			return messageSession;
		}

		internal void EndCancelScheduledMessage(IAsyncResult result)
		{
			this.ThrowIfSenderNull("EndCancelScheduledMessage");
			this.InternalSender.EndCancelScheduledMessage(result);
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

		private MessageSender EndCreateSender(IAsyncResult result)
		{
			MessageSender messageSender;
			messageSender = (!OpenOnceManager.ShouldEnd<MessageSender>(result) ? this.OnEndCreateSender(result) : OpenOnceManager.End<MessageSender>(result));
			messageSender.ShouldLinkRetryPolicy = true;
			this.RegisterMessageClientEntity(messageSender);
			IPairedNamespaceFactory pairedNamespaceFactory = this.MessagingFactory.PairedNamespaceFactory;
			if (pairedNamespaceFactory != null)
			{
				messageSender = pairedNamespaceFactory.CreateMessageSender(messageSender);
				this.RegisterMessageClientEntity(messageSender);
			}
			return messageSender;
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
			messageSessions = (!OpenOnceManager.ShouldEnd<IEnumerable<MessageSession>>(result) ? QueueClient.RetryGetMessageSessionsAsyncResult.End(result) : OpenOnceManager.End<IEnumerable<MessageSession>>(result));
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

		internal long EndScheduleMessage(IAsyncResult result)
		{
			this.ThrowIfSenderNull("EndScheduleMessage");
			return this.InternalSender.EndScheduleMessage(result).First<long>();
		}

		public void EndSend(IAsyncResult result)
		{
			this.ThrowIfSenderNull("EndSend");
			this.InternalSender.EndSend(result);
		}

		public void EndSendBatch(IAsyncResult result)
		{
			this.ThrowIfSenderNull("EndSendBatch");
			this.InternalSender.EndSendBatch(result);
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
							this.InternalReceiver = this.EndCreateReceiver(this.BeginCreateReceiver(this.Path, this.Mode, this.MessagingFactory.OperationTimeout, null, null));
						}
						else
						{
							this.InternalReceiver = this.EndCreateReceiver(this.BeginCreateReceiver(this.Mode, this.MessagingFactory.OperationTimeout, null, null));
						}
					}
				}
			}
		}

		private void EnsureCreateInternalSender()
		{
			if (!this.IsSubQueue && this.InternalSender == null)
			{
				lock (base.ThisLock)
				{
					if (this.InternalSender == null)
					{
						this.InternalSender = this.EndCreateSender(this.BeginCreateSender(this.MessagingFactory.OperationTimeout, null, null));
					}
				}
			}
		}

		public static string FormatDeadLetterPath(string queuePath)
		{
			return EntityNameHelper.FormatSubQueuePath(queuePath, "$DeadLetterQueue");
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
			QueueClient.CloseOrAbortAsyncResult closeOrAbortAsyncResult = new QueueClient.CloseOrAbortAsyncResult(this, true, TimeSpan.MaxValue, null, null);
			closeOrAbortAsyncResult.RunSynchronously();
		}

		protected abstract IAsyncResult OnBeginAcceptMessageSession(string sessionId, ReceiveMode receiveMode, TimeSpan serverWaitTime, TimeSpan timeout, AsyncCallback callback, object state);

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return (new QueueClient.CloseOrAbortAsyncResult(this, false, timeout, callback, state)).Start();
		}

		internal abstract IAsyncResult OnBeginCreateBrowser(TimeSpan timeout, AsyncCallback callback, object state);

		protected abstract IAsyncResult OnBeginCreateReceiver(ReceiveMode receiveMode, TimeSpan timeout, AsyncCallback callback, object state);

		protected abstract IAsyncResult OnBeginCreateReceiver(string subQueueName, ReceiveMode receiveMode, TimeSpan timeout, AsyncCallback callback, object state);

		protected abstract IAsyncResult OnBeginCreateSender(TimeSpan timeout, AsyncCallback callback, object state);

		protected abstract IAsyncResult OnBeginGetMessageSessions(DateTime lastUpdatedTime, AsyncCallback callback, object state);

		protected override void OnClose(TimeSpan timeout)
		{
			QueueClient.CloseOrAbortAsyncResult closeOrAbortAsyncResult = new QueueClient.CloseOrAbortAsyncResult(this, false, timeout, null, null);
			closeOrAbortAsyncResult.RunSynchronously();
		}

		protected abstract MessageSession OnEndAcceptMessageSession(IAsyncResult result);

		protected override void OnEndClose(IAsyncResult result)
		{
			AsyncResult<QueueClient.CloseOrAbortAsyncResult>.End(result);
		}

		internal abstract MessageBrowser OnEndCreateBrowser(IAsyncResult result);

		protected abstract MessageReceiver OnEndCreateReceiver(IAsyncResult result);

		protected abstract MessageSender OnEndCreateSender(IAsyncResult result);

		protected abstract IEnumerable<MessageSession> OnEndGetMessageSessions(IAsyncResult result);

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
			OnMessageOptions onMessageOption = new OnMessageOptions()
			{
				ReceiveTimeOut = this.OperationTimeout
			};
			this.OnMessageAsync(callback, onMessageOption);
		}

		public void OnMessageAsync(Func<BrokeredMessage, Task> callback, OnMessageOptions onMessageOptions)
		{
			this.EnsureCreateInternalReceiver();
			onMessageOptions.MessageClientEntity = this;
			onMessageOptions.ReceiveTimeOut = this.OperationTimeout;
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

		internal Task<long> ScheduleMessageAsync(BrokeredMessage message, DateTimeOffset scheduleEnqueueTimeUtc)
		{
			return TaskHelpers.CreateTask<long>((AsyncCallback c, object s) => this.BeginScheduleMessage(message, scheduleEnqueueTimeUtc, c, s), new Func<IAsyncResult, long>(this.EndScheduleMessage));
		}

		public void Send(BrokeredMessage message)
		{
			this.ThrowIfSenderNull("Send");
			this.InternalSender.Send(message);
		}

		public Task SendAsync(BrokeredMessage message)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginSend(message, c, s), new Action<IAsyncResult>(this.EndSend));
		}

		public void SendBatch(IEnumerable<BrokeredMessage> messages)
		{
			this.ThrowIfSenderNull("SendBatch");
			this.InternalSender.Send(messages);
		}

		public Task SendBatchAsync(IEnumerable<BrokeredMessage> messages)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginSendBatch(messages, c, s), new Action<IAsyncResult>(this.EndSendBatch));
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

		private void ThrowIfSenderNull(string operationName)
		{
			this.EnsureCreateInternalSender();
			if (this.InternalSender == null)
			{
				throw FxTrace.Exception.AsError(new InvalidOperationException(SRCore.UnsupportedOperation(operationName)), null);
			}
		}

		private sealed class CloseOrAbortAsyncResult : IteratorAsyncResult<QueueClient.CloseOrAbortAsyncResult>
		{
			private readonly QueueClient owner;

			private readonly bool shouldAbort;

			public CloseOrAbortAsyncResult(QueueClient owner, bool shouldAbort, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.owner = owner;
				this.shouldAbort = shouldAbort;
			}

			protected override IEnumerator<IteratorAsyncResult<QueueClient.CloseOrAbortAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				if (!this.shouldAbort)
				{
					QueueClient.CloseOrAbortAsyncResult closeOrAbortAsyncResult = this;
					IteratorAsyncResult<QueueClient.CloseOrAbortAsyncResult>.BeginCall beginCall = (QueueClient.CloseOrAbortAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.owner.ClientEntityManager.BeginClose(t, c, s);
					yield return closeOrAbortAsyncResult.CallAsync(beginCall, (QueueClient.CloseOrAbortAsyncResult thisPtr, IAsyncResult r) => thisPtr.owner.ClientEntityManager.EndClose(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				}
				else
				{
					this.owner.ClientEntityManager.Abort();
				}
				if (this.owner.pumpHost != null)
				{
					if (!this.shouldAbort)
					{
						QueueClient.CloseOrAbortAsyncResult closeOrAbortAsyncResult1 = this;
						IteratorAsyncResult<QueueClient.CloseOrAbortAsyncResult>.BeginCall beginCall1 = (QueueClient.CloseOrAbortAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.owner.pumpHost.BeginClose(t, c, s);
						IteratorAsyncResult<QueueClient.CloseOrAbortAsyncResult>.EndCall endCall = (QueueClient.CloseOrAbortAsyncResult thisPtr, IAsyncResult r) => thisPtr.owner.pumpHost.EndClose(r);
						yield return closeOrAbortAsyncResult1.CallAsync(beginCall1, endCall, (QueueClient.CloseOrAbortAsyncResult thisPtr, TimeSpan t) => thisPtr.owner.pumpHost.Close(t), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
					}
					else
					{
						this.owner.pumpHost.Abort();
					}
				}
			}
		}

		private sealed class RetryAcceptMessageSessionAsyncResult : RetryAsyncResult<QueueClient.RetryAcceptMessageSessionAsyncResult>
		{
			private readonly QueueClient client;

			private readonly TrackingContext trackingContext;

			private readonly string sessionId;

			private readonly ReceiveMode receiveMode;

			private readonly TimeSpan serverWaitTime;

			private readonly TimeSpan operationTimeout;

			private MessageSession Session
			{
				get;
				set;
			}

			public RetryAcceptMessageSessionAsyncResult(QueueClient client, TrackingContext trackingContext, string sessionId, ReceiveMode receiveMode, TimeSpan serverWaitTime, TimeSpan timeout, AsyncCallback callback, object state) : base(serverWaitTime, callback, state)
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
				this.trackingContext = trackingContext ?? TrackingContext.GetInstance(Guid.NewGuid(), this.client.Path);
				MessagingPerformanceCounters.IncrementPendingAcceptMessageSessionCount(this.client.MessagingFactory.Address, 1);
			}

			public static new MessageSession End(IAsyncResult r)
			{
				MessageSession session;
				QueueClient.RetryAcceptMessageSessionAsyncResult retryAcceptMessageSessionAsyncResult = r as QueueClient.RetryAcceptMessageSessionAsyncResult;
				try
				{
					retryAcceptMessageSessionAsyncResult = AsyncResult<QueueClient.RetryAcceptMessageSessionAsyncResult>.End(r);
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

			protected override IEnumerator<IteratorAsyncResult<QueueClient.RetryAcceptMessageSessionAsyncResult>.AsyncStep> GetAsyncSteps()
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
						QueueClient.RetryAcceptMessageSessionAsyncResult retryAcceptMessageSessionAsyncResult = this;
						IteratorAsyncResult<QueueClient.RetryAcceptMessageSessionAsyncResult>.BeginCall beginCall = (QueueClient.RetryAcceptMessageSessionAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.client.OnBeginAcceptMessageSession(thisPtr.sessionId, thisPtr.receiveMode, thisPtr.serverWaitTime, thisPtr.operationTimeout, c, s);
						yield return retryAcceptMessageSessionAsyncResult.CallAsync(beginCall, (QueueClient.RetryAcceptMessageSessionAsyncResult thisPtr, IAsyncResult r) => thisPtr.Session = thisPtr.client.OnEndAcceptMessageSession(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
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

		private sealed class RetryGetMessageSessionsAsyncResult : RetryAsyncResult<QueueClient.RetryGetMessageSessionsAsyncResult>
		{
			private readonly QueueClient client;

			private readonly TrackingContext trackingContext;

			private readonly DateTime lastUpdatedTime;

			public IEnumerable<MessageSession> Sessions
			{
				get;
				private set;
			}

			public RetryGetMessageSessionsAsyncResult(QueueClient client, TrackingContext trackingContext, DateTime lastUpdatedTime, AsyncCallback callback, object state) : base(client.OperationTimeout, callback, state)
			{
				if (client == null)
				{
					throw Fx.Exception.ArgumentNull("client");
				}
				this.client = client;
				this.lastUpdatedTime = lastUpdatedTime;
				this.trackingContext = trackingContext ?? TrackingContext.GetInstance(Guid.NewGuid(), this.client.Path);
				MessagingPerformanceCounters.IncrementPendingAcceptMessageSessionCount(this.client.MessagingFactory.Address, 1);
			}

			public static new IEnumerable<MessageSession> End(IAsyncResult r)
			{
				IEnumerable<MessageSession> sessions;
				QueueClient.RetryGetMessageSessionsAsyncResult retryGetMessageSessionsAsyncResult = r as QueueClient.RetryGetMessageSessionsAsyncResult;
				try
				{
					retryGetMessageSessionsAsyncResult = AsyncResult<QueueClient.RetryGetMessageSessionsAsyncResult>.End(r);
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

			protected override IEnumerator<IteratorAsyncResult<QueueClient.RetryGetMessageSessionsAsyncResult>.AsyncStep> GetAsyncSteps()
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
						QueueClient.RetryGetMessageSessionsAsyncResult retryGetMessageSessionsAsyncResult = this;
						IteratorAsyncResult<QueueClient.RetryGetMessageSessionsAsyncResult>.BeginCall beginCall = (QueueClient.RetryGetMessageSessionsAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.client.OnBeginGetMessageSessions(thisPtr.lastUpdatedTime, c, s);
						yield return retryGetMessageSessionsAsyncResult.CallAsync(beginCall, (QueueClient.RetryGetMessageSessionsAsyncResult thisPtr, IAsyncResult r) => thisPtr.Sessions = thisPtr.client.OnEndGetMessageSessions(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
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
	}
}