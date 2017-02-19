using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Common.Parallel;
using Microsoft.ServiceBus.Messaging.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus.Messaging
{
	public abstract class TopicClient : MessageClientEntity, IMessageSender
	{
		private readonly OpenOnceManager openOnceManager;

		internal MessageSender InternalSender
		{
			get;
			set;
		}

		protected bool IsSubQueue
		{
			get;
			private set;
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

		internal TopicClient(Microsoft.ServiceBus.Messaging.MessagingFactory messagingFactory, string path)
		{
			this.MessagingFactory = messagingFactory;
			this.Path = path;
			this.openOnceManager = new OpenOnceManager(this);
			base.ClientEntityManager = new MessageClientEntityManager();
			this.IsSubQueue = MessagingUtilities.IsSubQueue(path);
			base.RetryPolicy = messagingFactory.RetryPolicy.Clone();
		}

		internal IAsyncResult BeginCancelScheduledMessage(long sequenceNumber, AsyncCallback callback, object state)
		{
			this.ThrowIfSenderNull("BeginCancelScheduledMessage");
			return this.InternalSender.BeginCancelScheduledMessage(sequenceNumber, callback, state);
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
			TopicClient topicClient = this;
			return openOnceManager.Begin<MessageSender>(asyncCallback, obj, (AsyncCallback c, object s) => this.OnBeginCreateSender(timeout, c, s), new Func<IAsyncResult, MessageSender>(topicClient.OnEndCreateSender));
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

		public static TopicClient Create(string path)
		{
			return (new KeyValueConfigurationManager()).CreateMessagingFactory().CreateTopicClient(path);
		}

		public static TopicClient CreateFromConnectionString(string connectionString, string path)
		{
			return (new KeyValueConfigurationManager(connectionString)).CreateMessagingFactory().CreateTopicClient(path);
		}

		internal void EndCancelScheduledMessage(IAsyncResult result)
		{
			this.ThrowIfSenderNull("EndCancelScheduledMessage");
			this.InternalSender.EndCancelScheduledMessage(result);
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

		protected override void OnAbort()
		{
			base.ClientEntityManager.Abort();
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return base.ClientEntityManager.BeginClose(timeout, callback, state);
		}

		protected abstract IAsyncResult OnBeginCreateSender(TimeSpan timeout, AsyncCallback callback, object state);

		protected override void OnClose(TimeSpan timeout)
		{
			this.OnEndClose(this.OnBeginClose(timeout, null, null));
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			base.ClientEntityManager.EndClose(result);
		}

		protected abstract MessageSender OnEndCreateSender(IAsyncResult result);

		internal void RegisterMessageClientEntity(MessageClientEntity child)
		{
			base.ClientEntityManager.Add(child);
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
			this.InternalSender.SendBatch(messages);
		}

		public Task SendBatchAsync(IEnumerable<BrokeredMessage> messages)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginSendBatch(messages, c, s), new Action<IAsyncResult>(this.EndSendBatch));
		}

		private void ThrowIfSenderNull(string operationName)
		{
			this.EnsureCreateInternalSender();
			if (this.InternalSender == null)
			{
				throw FxTrace.Exception.AsError(new NotSupportedException(SRCore.UnsupportedOperation(operationName)), null);
			}
		}
	}
}