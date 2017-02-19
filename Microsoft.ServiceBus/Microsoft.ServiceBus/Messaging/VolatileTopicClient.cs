using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Common.Parallel;
using Microsoft.ServiceBus.Messaging.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus.Messaging
{
	internal abstract class VolatileTopicClient : MessageClientEntity
	{
		private readonly OpenOnceManager openOnceManager;

		public string ClientId
		{
			get;
			private set;
		}

		public Microsoft.ServiceBus.Messaging.Filter Filter
		{
			get;
			private set;
		}

		private MessageReceiver InternalReceiver
		{
			get;
			set;
		}

		private MessageSender InternalSender
		{
			get;
			set;
		}

		public Microsoft.ServiceBus.Messaging.MessagingFactory MessagingFactory
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

		public string Path
		{
			get;
			private set;
		}

		protected VolatileTopicClient(Microsoft.ServiceBus.Messaging.MessagingFactory messagingFactory, string path, string clientId, Microsoft.ServiceBus.RetryPolicy retryPolicy, Microsoft.ServiceBus.Messaging.Filter filter)
		{
			this.MessagingFactory = messagingFactory;
			base.ClientEntityManager = new MessageClientEntityManager();
			this.openOnceManager = new OpenOnceManager(this);
			this.Path = path;
			this.ClientId = (string.IsNullOrEmpty(clientId) ? Guid.NewGuid().ToString() : clientId);
			base.RetryPolicy = retryPolicy;
			this.Filter = filter;
		}

		private IAsyncResult BeginCreateReceiver(TimeSpan timeout, AsyncCallback callback, object state)
		{
			base.ThrowIfDisposed();
			if (!this.openOnceManager.ShouldOpen)
			{
				return this.OnBeginCreateReceiver(timeout, callback, state);
			}
			OpenOnceManager openOnceManager = this.openOnceManager;
			AsyncCallback asyncCallback = callback;
			object obj = state;
			VolatileTopicClient volatileTopicClient = this;
			return openOnceManager.Begin<MessageReceiver>(asyncCallback, obj, (AsyncCallback c, object s) => this.OnBeginCreateReceiver(timeout, c, s), new Func<IAsyncResult, MessageReceiver>(volatileTopicClient.OnEndCreateReceiver));
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
			VolatileTopicClient volatileTopicClient = this;
			return openOnceManager.Begin<MessageSender>(asyncCallback, obj, (AsyncCallback c, object s) => this.OnBeginCreateSender(timeout, c, s), new Func<IAsyncResult, MessageSender>(volatileTopicClient.OnEndCreateSender));
		}

		public IAsyncResult BeginReceive(TimeSpan serverWaitTime, AsyncCallback callback, object state)
		{
			this.ThrowIfReceiverNull("BeginReceive");
			return this.InternalReceiver.BeginReceive(serverWaitTime, callback, state);
		}

		public IAsyncResult BeginReceive(AsyncCallback callback, object state)
		{
			this.ThrowIfReceiverNull("BeginReceive");
			return this.InternalReceiver.BeginReceive(callback, state);
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

		public static VolatileTopicClient Create(string path)
		{
			return VolatileTopicClient.Create(path, null, null);
		}

		public static VolatileTopicClient Create(string path, string clientId)
		{
			return VolatileTopicClient.Create(path, clientId, null);
		}

		public static VolatileTopicClient Create(string path, string clientId, Microsoft.ServiceBus.Messaging.Filter filter)
		{
			return (new KeyValueConfigurationManager()).CreateMessagingFactory().CreateVolatileTopicClient(path, clientId, filter);
		}

		public static VolatileTopicClient CreateFromConnectionString(string connectionString, string path)
		{
			return VolatileTopicClient.CreateFromConnectionString(connectionString, path, null, null);
		}

		public static VolatileTopicClient CreateFromConnectionString(string connectionString, string path, string clientId)
		{
			return VolatileTopicClient.CreateFromConnectionString(connectionString, path, clientId, null);
		}

		public static VolatileTopicClient CreateFromConnectionString(string connectionString, string path, string clientId, Microsoft.ServiceBus.Messaging.Filter filter)
		{
			KeyValueConfigurationManager keyValueConfigurationManager = new KeyValueConfigurationManager(connectionString);
			return keyValueConfigurationManager.CreateMessagingFactory().CreateVolatileTopicClient(path, clientId, filter);
		}

		private MessageReceiver EndCreateReceiver(IAsyncResult result)
		{
			MessageReceiver messageReceiver;
			messageReceiver = (!OpenOnceManager.ShouldEnd<MessageReceiver>(result) ? this.OnEndCreateReceiver(result) : OpenOnceManager.End<MessageReceiver>(result));
			this.RegisterMessageClientEntity(messageReceiver);
			return messageReceiver;
		}

		private MessageSender EndCreateSender(IAsyncResult result)
		{
			MessageSender messageSender;
			messageSender = (!OpenOnceManager.ShouldEnd<MessageSender>(result) ? this.OnEndCreateSender(result) : OpenOnceManager.End<MessageSender>(result));
			this.RegisterMessageClientEntity(messageSender);
			return messageSender;
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

		private void EnsureCreateInternalReceiver()
		{
			if (this.InternalReceiver == null)
			{
				lock (base.ThisLock)
				{
					if (this.InternalReceiver == null)
					{
						this.InternalReceiver = this.EndCreateReceiver(this.BeginCreateReceiver(this.MessagingFactory.OperationTimeout, null, null));
					}
				}
			}
		}

		private void EnsureCreateInternalSender()
		{
			if (this.InternalSender == null)
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

		protected override void OnAbort()
		{
			VolatileTopicClient.CloseAsyncResult closeAsyncResult = new VolatileTopicClient.CloseAsyncResult(this, true, TimeSpan.MaxValue, null, null);
			closeAsyncResult.RunSynchronously();
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return (new VolatileTopicClient.CloseAsyncResult(this, false, timeout, callback, state)).Start();
		}

		protected abstract IAsyncResult OnBeginCreateReceiver(TimeSpan timeout, AsyncCallback callback, object state);

		protected abstract IAsyncResult OnBeginCreateSender(TimeSpan timeout, AsyncCallback callback, object state);

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult(callback, state);
		}

		protected override void OnClose(TimeSpan timeout)
		{
			VolatileTopicClient.CloseAsyncResult closeAsyncResult = new VolatileTopicClient.CloseAsyncResult(this, false, timeout, null, null);
			closeAsyncResult.RunSynchronously();
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			AsyncResult<VolatileTopicClient.CloseAsyncResult>.End(result);
		}

		protected abstract MessageReceiver OnEndCreateReceiver(IAsyncResult result);

		protected abstract MessageSender OnEndCreateSender(IAsyncResult result);

		protected override void OnEndOpen(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
		}

		protected override void OnOpen(TimeSpan timeout)
		{
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

		public Task<BrokeredMessage> ReceiveAsync()
		{
			return TaskHelpers.CreateTask<BrokeredMessage>(new Func<AsyncCallback, object, IAsyncResult>(this.BeginReceive), new Func<IAsyncResult, BrokeredMessage>(this.EndReceive));
		}

		public Task<BrokeredMessage> ReceiveAsync(TimeSpan serverWaitTime)
		{
			return TaskHelpers.CreateTask<BrokeredMessage>((AsyncCallback c, object s) => this.BeginReceive(serverWaitTime, c, s), new Func<IAsyncResult, BrokeredMessage>(this.EndReceive));
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

		public Task<IEnumerable<BrokeredMessage>> ReceiveBatchAsync(int messageCount)
		{
			return TaskHelpers.CreateTask<IEnumerable<BrokeredMessage>>((AsyncCallback c, object s) => this.BeginReceiveBatch(messageCount, c, s), new Func<IAsyncResult, IEnumerable<BrokeredMessage>>(this.EndReceiveBatch));
		}

		public Task<IEnumerable<BrokeredMessage>> ReceiveBatchAsync(int messageCount, TimeSpan serverWaitTime)
		{
			return TaskHelpers.CreateTask<IEnumerable<BrokeredMessage>>((AsyncCallback c, object s) => this.BeginReceiveBatch(messageCount, serverWaitTime, c, s), new Func<IAsyncResult, IEnumerable<BrokeredMessage>>(this.EndReceiveBatch));
		}

		private void RegisterMessageClientEntity(MessageClientEntity child)
		{
			base.ClientEntityManager.Add(child);
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

		private sealed class CloseAsyncResult : IteratorAsyncResult<VolatileTopicClient.CloseAsyncResult>
		{
			private readonly VolatileTopicClient owner;

			private readonly bool shouldAbort;

			public CloseAsyncResult(VolatileTopicClient owner, bool shouldAbort, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.owner = owner;
				this.shouldAbort = shouldAbort;
			}

			protected override IEnumerator<IteratorAsyncResult<VolatileTopicClient.CloseAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				if (!this.shouldAbort)
				{
					VolatileTopicClient.CloseAsyncResult closeAsyncResult = this;
					IteratorAsyncResult<VolatileTopicClient.CloseAsyncResult>.BeginCall beginCall = (VolatileTopicClient.CloseAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.owner.ClientEntityManager.BeginClose(t, c, s);
					yield return closeAsyncResult.CallAsync(beginCall, (VolatileTopicClient.CloseAsyncResult thisPtr, IAsyncResult r) => thisPtr.owner.ClientEntityManager.EndClose(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				}
				else
				{
					this.owner.ClientEntityManager.Abort();
				}
			}
		}
	}
}