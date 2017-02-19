using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Common.Parallel;
using Microsoft.ServiceBus.PerformanceCounters;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Transactions;

namespace Microsoft.ServiceBus.Messaging
{
	public abstract class MessageSender : MessageClientEntity, IMessageSender
	{
		private readonly OpenOnceManager openOnceManager;

		private readonly TimeSpan operationTimeout;

		private TimeSpan batchFlushInterval;

		public virtual TimeSpan BatchFlushInterval
		{
			get
			{
				return this.batchFlushInterval;
			}
			internal set
			{
				base.ThrowIfDisposedOrImmutable();
				this.batchFlushInterval = value;
			}
		}

		protected bool BatchingEnabled
		{
			get
			{
				return this.BatchFlushInterval != TimeSpan.Zero;
			}
		}

		internal virtual MessagingEntityType? EntityType
		{
			get;
			set;
		}

		internal Microsoft.ServiceBus.Messaging.MessagingFactory MessagingFactory
		{
			get;
			private set;
		}

		internal override TimeSpan OperationTimeout
		{
			get
			{
				return this.operationTimeout;
			}
		}

		internal string PartitionId
		{
			get;
			set;
		}

		public abstract string Path
		{
			get;
		}

		internal MessageSender(Microsoft.ServiceBus.Messaging.MessagingFactory messagingFactory, Microsoft.ServiceBus.RetryPolicy retryPolicy)
		{
			this.MessagingFactory = messagingFactory;
			this.operationTimeout = messagingFactory.OperationTimeout;
			base.RetryPolicy = retryPolicy ?? messagingFactory.RetryPolicy.Clone();
			this.openOnceManager = new OpenOnceManager(this);
		}

		internal IAsyncResult BeginCancelScheduledMessage(long sequenceNumber, AsyncCallback callback, object state)
		{
			List<long> nums = new List<long>()
			{
				sequenceNumber
			};
			return this.BeginCancelScheduledMessage(null, nums, this.OperationTimeout, callback, state);
		}

		internal IAsyncResult BeginCancelScheduledMessage(TrackingContext trackingContext, IEnumerable<long> sequenceNumbers, TimeSpan timeout, AsyncCallback callback, object state)
		{
			TrackingContext instance = trackingContext;
			base.ThrowIfDisposed();
			TimeoutHelper.ThrowIfNegativeArgument(timeout);
			MessagingUtilities.CheckValidSequenceNumbers(sequenceNumbers);
			if (instance == null)
			{
				instance = TrackingContext.GetInstance(Guid.NewGuid(), this.Path);
			}
			if (!this.openOnceManager.ShouldOpen)
			{
				MessageSender.RetryCancelScheduledSenderAsyncResult retryCancelScheduledSenderAsyncResult1 = new MessageSender.RetryCancelScheduledSenderAsyncResult(this, instance, sequenceNumbers, timeout, callback, state);
				retryCancelScheduledSenderAsyncResult1.Start();
				return retryCancelScheduledSenderAsyncResult1;
			}
			return this.openOnceManager.Begin(callback, state, (AsyncCallback c, object s) => {
				MessageSender.RetryCancelScheduledSenderAsyncResult retryCancelScheduledSenderAsyncResult = new MessageSender.RetryCancelScheduledSenderAsyncResult(this, instance, sequenceNumbers, timeout, c, s);
				retryCancelScheduledSenderAsyncResult.Start();
				return retryCancelScheduledSenderAsyncResult;
			}, new Action<IAsyncResult>(MessageSender.RetryCancelScheduledSenderAsyncResult.End));
		}

		internal IAsyncResult BeginScheduleMessage(BrokeredMessage message, DateTimeOffset scheduleEnqueueTime, AsyncCallback callback, object state)
		{
			if (message == null)
			{
				throw FxTrace.Exception.ArgumentNullOrEmpty("message");
			}
			message.ScheduledEnqueueTimeUtc = scheduleEnqueueTime.UtcDateTime;
			BrokeredMessage[] brokeredMessageArray = new BrokeredMessage[] { message };
			return this.BeginScheduleMessage(null, brokeredMessageArray, this.OperationTimeout, callback, state);
		}

		internal IAsyncResult BeginScheduleMessage(TrackingContext trackingContext, IEnumerable<BrokeredMessage> messages, TimeSpan timeout, AsyncCallback callback, object state)
		{
			TrackingContext instance = trackingContext;
			base.ThrowIfDisposed();
			MessagingUtilities.CheckValidMessages(messages, true);
			TimeoutHelper.ThrowIfNegativeArgument(timeout);
			if (instance == null)
			{
				instance = TrackingContext.GetInstance(Guid.NewGuid(), this.Path);
			}
			MessagingUtilities.ValidateAndSetConsumedMessages(messages);
			if (!this.openOnceManager.ShouldOpen)
			{
				MessageSender.RetryScheduleSenderAsyncResult retryScheduleSenderAsyncResult1 = new MessageSender.RetryScheduleSenderAsyncResult(this, instance, messages, timeout, callback, state);
				retryScheduleSenderAsyncResult1.Start();
				return retryScheduleSenderAsyncResult1;
			}
			return this.openOnceManager.Begin<IEnumerable<long>>(callback, state, (AsyncCallback c, object s) => {
				MessageSender.RetryScheduleSenderAsyncResult retryScheduleSenderAsyncResult = new MessageSender.RetryScheduleSenderAsyncResult(this, instance, messages, timeout, c, s);
				retryScheduleSenderAsyncResult.Start();
				return retryScheduleSenderAsyncResult;
			}, new Func<IAsyncResult, IEnumerable<long>>(MessageSender.RetryScheduleSenderAsyncResult.ScheduleSenderEnd));
		}

		public IAsyncResult BeginSend(BrokeredMessage message, AsyncCallback callback, object state)
		{
			BrokeredMessage[] brokeredMessageArray = new BrokeredMessage[] { message };
			return this.BeginSend(null, brokeredMessageArray, this.OperationTimeout, callback, state);
		}

		internal IAsyncResult BeginSend(BrokeredMessage message, TimeSpan timeout, AsyncCallback callback, object state)
		{
			BrokeredMessage[] brokeredMessageArray = new BrokeredMessage[] { message };
			return this.BeginSend(null, brokeredMessageArray, timeout, callback, state);
		}

		internal IAsyncResult BeginSend(IEnumerable<BrokeredMessage> messages, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.BeginSend(null, messages, timeout, callback, state);
		}

		internal IAsyncResult BeginSend(IEnumerable<BrokeredMessage> messages, AsyncCallback callback, object state)
		{
			return this.BeginSend(null, messages, this.OperationTimeout, callback, state);
		}

		internal IAsyncResult BeginSend(TrackingContext trackingContext, IEnumerable<BrokeredMessage> messages, TimeSpan timeout, AsyncCallback callback, object state)
		{
			TrackingContext instance = trackingContext;
			base.ThrowIfDisposed();
			MessagingUtilities.CheckValidMessages(messages, true);
			TimeoutHelper.ThrowIfNegativeArgument(timeout);
			if (instance == null)
			{
				instance = TrackingContext.GetInstance(Guid.NewGuid(), this.Path);
			}
			MessagingUtilities.ValidateAndSetConsumedMessages(messages);
			if (!this.openOnceManager.ShouldOpen)
			{
				MessageSender.RetrySenderAsyncResult retrySenderAsyncResult1 = new MessageSender.RetrySenderAsyncResult(this, instance, messages, false, timeout, callback, state);
				retrySenderAsyncResult1.Start();
				return retrySenderAsyncResult1;
			}
			return this.openOnceManager.Begin(callback, state, (AsyncCallback c, object s) => {
				MessageSender.RetrySenderAsyncResult retrySenderAsyncResult = new MessageSender.RetrySenderAsyncResult(this, instance, messages, false, timeout, c, s);
				retrySenderAsyncResult.Start();
				return retrySenderAsyncResult;
			}, new Action<IAsyncResult>(MessageSender.RetrySenderAsyncResult.End));
		}

		public IAsyncResult BeginSendBatch(IEnumerable<BrokeredMessage> messages, AsyncCallback callback, object state)
		{
			return this.BeginSend(null, messages, this.OperationTimeout, callback, state);
		}

		internal IAsyncResult BeginSendEventData(TrackingContext trackingContext, IEnumerable<EventData> eventDatas, TimeSpan timeout, AsyncCallback callback, object state)
		{
			TrackingContext instance = trackingContext;
			base.ThrowIfDisposed();
			TimeoutHelper.ThrowIfNegativeArgument(timeout);
			this.Validate(eventDatas);
			if (instance == null)
			{
				instance = TrackingContext.GetInstance(Guid.NewGuid(), this.Path);
			}
			if (!this.openOnceManager.ShouldOpen)
			{
				return (new MessageSender.RetrySenderEventDataAsyncResult(this, instance, eventDatas, timeout, callback, state)).Start();
			}
			return this.openOnceManager.Begin(callback, state, (AsyncCallback c, object s) => {
				MessageSender.RetrySenderEventDataAsyncResult retrySenderEventDataAsyncResult = new MessageSender.RetrySenderEventDataAsyncResult(this, instance, eventDatas, timeout, c, s);
				retrySenderEventDataAsyncResult.Start();
				return retrySenderEventDataAsyncResult;
			}, new Action<IAsyncResult>(MessageSender.RetrySenderEventDataAsyncResult.End));
		}

		internal void EndCancelScheduledMessage(IAsyncResult result)
		{
			if (OpenOnceManager.ShouldEnd(result))
			{
				OpenOnceManager.End(result);
				return;
			}
			MessageSender.RetryCancelScheduledSenderAsyncResult.End(result);
		}

		internal IEnumerable<long> EndScheduleMessage(IAsyncResult result)
		{
			if (OpenOnceManager.ShouldEnd<IEnumerable<long>>(result))
			{
				return OpenOnceManager.End<IEnumerable<long>>(result);
			}
			return MessageSender.RetryScheduleSenderAsyncResult.ScheduleSenderEnd(result);
		}

		public void EndSend(IAsyncResult result)
		{
			if (OpenOnceManager.ShouldEnd(result))
			{
				OpenOnceManager.End(result);
				return;
			}
			MessageSender.RetrySenderAsyncResult.End(result);
		}

		public void EndSendBatch(IAsyncResult result)
		{
			if (OpenOnceManager.ShouldEnd(result))
			{
				OpenOnceManager.End(result);
				return;
			}
			MessageSender.RetrySenderAsyncResult.End(result);
		}

		internal void EndSendEventData(IAsyncResult result)
		{
			if (OpenOnceManager.ShouldEnd(result))
			{
				OpenOnceManager.End(result);
				return;
			}
			MessageSender.RetrySenderEventDataAsyncResult.End(result);
		}

		protected abstract IAsyncResult OnBeginCancelScheduledMessage(TrackingContext trackingContext, IEnumerable<long> sequenceNumbers, TimeSpan timeout, AsyncCallback callback, object state);

		protected abstract IAsyncResult OnBeginScheduleMessage(TrackingContext trackingContext, IEnumerable<BrokeredMessage> messages, TimeSpan timeout, AsyncCallback callback, object state);

		protected abstract IAsyncResult OnBeginSend(TrackingContext trackingContext, IEnumerable<BrokeredMessage> messages, bool fromSync, TimeSpan timeout, AsyncCallback callback, object state);

		protected abstract IAsyncResult OnBeginSendEventData(TrackingContext trackingContext, IEnumerable<EventData> eventDatas, TimeSpan timeout, AsyncCallback callback, object state);

		protected abstract void OnEndCancelScheduledMessage(IAsyncResult result);

		protected abstract IEnumerable<long> OnEndScheduleMessage(IAsyncResult result);

		protected abstract void OnEndSend(IAsyncResult result);

		protected abstract void OnEndSendEventData(IAsyncResult result);

		protected virtual void OnSend(TrackingContext trackingContext, IEnumerable<BrokeredMessage> messages, TimeSpan timeout)
		{
			MessageSender.RetrySenderAsyncResult retrySenderAsyncResult = new MessageSender.RetrySenderAsyncResult(this, trackingContext, messages, true, timeout, null, null);
			retrySenderAsyncResult.RunSynchronously();
		}

		public void Send(BrokeredMessage message)
		{
			if (message == null)
			{
				throw FxTrace.Exception.ArgumentNull("message");
			}
			BrokeredMessage[] brokeredMessageArray = new BrokeredMessage[] { message };
			this.Send(null, brokeredMessageArray, this.OperationTimeout);
		}

		internal void Send(BrokeredMessage message, TimeSpan timeout)
		{
			this.Send(null, new BrokeredMessage[] { message }, timeout);
		}

		internal void Send(IEnumerable<BrokeredMessage> messages)
		{
			this.Send(null, messages, this.OperationTimeout);
		}

		internal void Send(IEnumerable<BrokeredMessage> messages, TimeSpan timeout)
		{
			this.Send(null, messages, timeout);
		}

		internal void Send(TrackingContext trackingContext, IEnumerable<BrokeredMessage> messages, TimeSpan timeout)
		{
			MessagingUtilities.CheckValidMessages(messages, true);
			TimeoutHelper.ThrowIfNegativeArgument(timeout);
			base.ThrowIfDisposed();
			if (trackingContext == null)
			{
				trackingContext = TrackingContext.GetInstance(Guid.NewGuid(), this.Path);
			}
			if (this.openOnceManager.ShouldOpen)
			{
				this.openOnceManager.Open();
			}
			MessagingUtilities.ValidateAndSetConsumedMessages(messages);
			this.OnSend(trackingContext, messages, timeout);
		}

		public Task SendAsync(BrokeredMessage message)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginSend(message, c, s), new Action<IAsyncResult>(this.EndSend));
		}

		public void SendBatch(IEnumerable<BrokeredMessage> messages)
		{
			this.Send(null, messages, this.OperationTimeout);
		}

		public Task SendBatchAsync(IEnumerable<BrokeredMessage> messages)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginSendBatch(messages, c, s), new Action<IAsyncResult>(this.EndSendBatch));
		}

		private void Validate(IEnumerable<EventData> events)
		{
			if (events == null)
			{
				throw FxTrace.Exception.AsError(new ArgumentException(SRClient.EventDataListIsNullOrEmpty), null);
			}
			bool flag = false;
			foreach (EventData @event in events)
			{
				flag = true;
				if (this.PartitionId == null || @event.PartitionKey == null)
				{
					continue;
				}
				throw FxTrace.Exception.AsError(new ArgumentException(SRClient.PartitionInvalidPartitionKey(@event.PartitionKey, this.PartitionId)), null);
			}
			if (!flag)
			{
				throw FxTrace.Exception.AsError(new ArgumentException(SRClient.EventDataListIsNullOrEmpty), null);
			}
		}

		private sealed class RetryCancelScheduledSenderAsyncResult : RetryAsyncResult<MessageSender.RetryCancelScheduledSenderAsyncResult>
		{
			private readonly MessageSender sender;

			private readonly TrackingContext trackingContext;

			private readonly IEnumerable<long> sequenceNumbers;

			private readonly List<long> sequenceNumbersBuffer;

			private readonly int messageCount;

			public RetryCancelScheduledSenderAsyncResult(MessageSender sender, TrackingContext trackingContext, IEnumerable<long> sequenceNumbers, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				if (trackingContext == null)
				{
					throw Fx.Exception.ArgumentNull("trackingContext");
				}
				if (sender == null)
				{
					throw Fx.Exception.ArgumentNull("sender");
				}
				this.sender = sender;
				this.trackingContext = trackingContext;
				this.sequenceNumbers = sequenceNumbers;
				this.sequenceNumbersBuffer = new List<long>();
				this.messageCount = this.sequenceNumbers.Count<long>();
				if (!(sender.RetryPolicy is NoRetry))
				{
					try
					{
						foreach (long sequenceNumber in this.sequenceNumbers)
						{
							this.sequenceNumbersBuffer.Add(sequenceNumber);
						}
					}
					catch (Exception exception1)
					{
						Exception exception = exception1;
						MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRetryPolicyStreamNotClonable(this.trackingContext.Activity, this.trackingContext.TrackingId, this.sender.RetryPolicy.GetType().Name, "CancelScheduledMessage", exception.GetType().FullName, exception.Message));
						this.sequenceNumbersBuffer.Clear();
					}
				}
			}

			public static new void End(IAsyncResult r)
			{
				AsyncResult<MessageSender.RetryCancelScheduledSenderAsyncResult>.End(r);
			}

			protected override IEnumerator<IteratorAsyncResult<MessageSender.RetryCancelScheduledSenderAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				TimeSpan timeSpan;
				bool flag;
				int num1 = 0;
				Stopwatch stopwatch = Stopwatch.StartNew();
				try
				{
					timeSpan = (this.sender.RetryPolicy.IsServerBusy ? Microsoft.ServiceBus.RetryPolicy.ServerBusyBaseSleepTime : TimeSpan.Zero);
					TimeSpan timeSpan1 = timeSpan;
					if (!this.sender.RetryPolicy.IsServerBusy || !(Microsoft.ServiceBus.RetryPolicy.ServerBusyBaseSleepTime >= base.OriginalTimeout))
					{
						while (true)
						{
							bool flag1 = false;
							if (timeSpan1 != TimeSpan.Zero)
							{
								yield return base.CallAsyncSleep(timeSpan1);
							}
							List<long> list = null;
							if (num1 != 0)
							{
								List<long> nums = this.sequenceNumbersBuffer;
								list = (
									from num in nums
									select num).ToList<long>();
							}
							try
							{
								MessageSender.RetryCancelScheduledSenderAsyncResult retryCancelScheduledSenderAsyncResult = this;
								Transaction ambientTransaction = base.AmbientTransaction;
								IteratorAsyncResult<MessageSender.RetryCancelScheduledSenderAsyncResult>.BeginCall beginCall = (MessageSender.RetryCancelScheduledSenderAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => {
									MessageSender messageSender = thisPtr.sender;
									TrackingContext trackingContext = thisPtr.trackingContext;
									object obj = list;
									if (obj == null)
									{
										obj = thisPtr.sequenceNumbers;
									}
									return messageSender.OnBeginCancelScheduledMessage(trackingContext, (IEnumerable<long>)obj, t, c, s);
								};
								yield return retryCancelScheduledSenderAsyncResult.CallTransactionalAsync(ambientTransaction, beginCall, (MessageSender.RetryCancelScheduledSenderAsyncResult thisPtr, IAsyncResult r) => thisPtr.sender.OnEndCancelScheduledMessage(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
							}
							finally
							{
								if (list != null)
								{
									list.Clear();
								}
							}
							if (base.LastAsyncStepException == null)
							{
								MessagingPerformanceCounters.IncrementCancelScheduledMessageSuccessPerSec(this.sender.MessagingFactory.Address, this.messageCount);
								this.sender.RetryPolicy.ResetServerBusy();
							}
							else
							{
								MessagingPerformanceCounters.IncrementCancelScheduledMessageFailurePerSec(this.sender.MessagingFactory.Address, this.messageCount);
								MessagingPerformanceCounters.IncrementExceptionPerSec(this.sender.MessagingFactory.Address, 1, base.LastAsyncStepException);
								if (this.sequenceNumbersBuffer.Count > 0)
								{
									flag = (base.TransactionExists ? false : this.sender.RetryPolicy.ShouldRetry(base.RemainingTime(), num1, base.LastAsyncStepException, out timeSpan1));
									flag1 = flag;
								}
								if (flag1)
								{
									MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRetryPolicyIteration(this.trackingContext.Activity, this.trackingContext.TrackingId, this.sender.RetryPolicy.GetType().Name, "CancelScheduledMessage", num1, timeSpan1.ToString(), this.LastAsyncStepException.GetType().FullName, this.LastAsyncStepException.Message));
									num1++;
								}
							}
							if (!flag1)
							{
								break;
							}
						}
					}
					else
					{
						string str = this.sender.RetryPolicy.ServerBusyExceptionMessage;
						yield return base.CallAsyncSleep(base.RemainingTime());
						base.Complete(new ServerBusyException(str, this.trackingContext));
						goto Label0;
					}
				}
				finally
				{
					stopwatch.Stop();
					MessagingPerformanceCounters.IncrementCancelScheduledMessageLatency(this.sender.MessagingFactory.Address, stopwatch.ElapsedTicks);
				}
				this.sequenceNumbersBuffer.Clear();
				base.Complete(base.LastAsyncStepException);
			Label0:
				yield break;
			}
		}

		private sealed class RetryScheduleSenderAsyncResult : RetryAsyncResult<MessageSender.RetryScheduleSenderAsyncResult>
		{
			private readonly MessageSender sender;

			private readonly TrackingContext trackingContext;

			private readonly IEnumerable<BrokeredMessage> messages;

			private readonly List<BrokeredMessage> messagesBuffer;

			public IEnumerable<long> SequenceNumbers
			{
				get;
				private set;
			}

			public RetryScheduleSenderAsyncResult(MessageSender sender, TrackingContext trackingContext, IEnumerable<BrokeredMessage> messages, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				if (trackingContext == null)
				{
					throw Fx.Exception.ArgumentNull("trackingContext");
				}
				if (sender == null)
				{
					throw Fx.Exception.ArgumentNull("sender");
				}
				this.sender = sender;
				this.trackingContext = trackingContext;
				this.messages = messages;
				this.messagesBuffer = new List<BrokeredMessage>();
				BrokeredMessage[] array = this.messages as BrokeredMessage[] ?? this.messages.ToArray<BrokeredMessage>();
				if (!(sender.RetryPolicy is NoRetry))
				{
					try
					{
						BrokeredMessage[] brokeredMessageArray = array;
						for (int i = 0; i < (int)brokeredMessageArray.Length; i++)
						{
							BrokeredMessage brokeredMessage = brokeredMessageArray[i];
							this.messagesBuffer.Add(brokeredMessage.Clone());
						}
					}
					catch (Exception exception1)
					{
						Exception exception = exception1;
						MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRetryPolicyStreamNotClonable(this.trackingContext.Activity, this.trackingContext.TrackingId, this.sender.RetryPolicy.GetType().Name, "ScheduleSend", exception.GetType().FullName, exception.Message));
						foreach (BrokeredMessage brokeredMessage1 in this.messagesBuffer)
						{
							brokeredMessage1.Dispose();
						}
						this.messagesBuffer.Clear();
					}
				}
			}

			protected override IEnumerator<IteratorAsyncResult<MessageSender.RetryScheduleSenderAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				TimeSpan timeSpan;
				bool flag;
				int num = 0;
				Stopwatch stopwatch = Stopwatch.StartNew();
				try
				{
					timeSpan = (this.sender.RetryPolicy.IsServerBusy ? Microsoft.ServiceBus.RetryPolicy.ServerBusyBaseSleepTime : TimeSpan.Zero);
					TimeSpan timeSpan1 = timeSpan;
					if (!this.sender.RetryPolicy.IsServerBusy || !(Microsoft.ServiceBus.RetryPolicy.ServerBusyBaseSleepTime >= base.OriginalTimeout))
					{
						while (true)
						{
							bool flag1 = false;
							if (timeSpan1 != TimeSpan.Zero)
							{
								yield return base.CallAsyncSleep(timeSpan1);
							}
							List<BrokeredMessage> list = null;
							if (num != 0)
							{
								List<BrokeredMessage> brokeredMessages = this.messagesBuffer;
								list = (
									from brokeredMessage in brokeredMessages
									select brokeredMessage.Clone()).ToList<BrokeredMessage>();
							}
							try
							{
								MessageSender.RetryScheduleSenderAsyncResult retryScheduleSenderAsyncResult = this;
								Transaction ambientTransaction = base.AmbientTransaction;
								IteratorAsyncResult<MessageSender.RetryScheduleSenderAsyncResult>.BeginCall beginCall = (MessageSender.RetryScheduleSenderAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => {
									MessageSender messageSender = thisPtr.sender;
									TrackingContext trackingContext = thisPtr.trackingContext;
									object obj = list;
									if (obj == null)
									{
										obj = thisPtr.messages;
									}
									return messageSender.OnBeginScheduleMessage(trackingContext, (IEnumerable<BrokeredMessage>)obj, t, c, s);
								};
								yield return retryScheduleSenderAsyncResult.CallTransactionalAsync(ambientTransaction, beginCall, (MessageSender.RetryScheduleSenderAsyncResult thisPtr, IAsyncResult r) => thisPtr.SequenceNumbers = thisPtr.sender.OnEndScheduleMessage(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
							}
							finally
							{
								if (list != null)
								{
									list.ForEach((BrokeredMessage brokeredMessage) => brokeredMessage.Dispose());
									list.Clear();
								}
							}
							if (base.LastAsyncStepException == null)
							{
								this.sender.RetryPolicy.ResetServerBusy();
							}
							else
							{
								MessagingPerformanceCounters.IncrementExceptionPerSec(this.sender.MessagingFactory.Address, 1, base.LastAsyncStepException);
								if (this.messagesBuffer.Count > 0)
								{
									flag = (base.TransactionExists ? false : this.sender.RetryPolicy.ShouldRetry(base.RemainingTime(), num, base.LastAsyncStepException, out timeSpan1));
									flag1 = flag;
								}
								if (flag1)
								{
									MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRetryPolicyIteration(this.trackingContext.Activity, this.trackingContext.TrackingId, this.sender.RetryPolicy.GetType().Name, "ScheduleMessage", num, timeSpan1.ToString(), this.LastAsyncStepException.GetType().FullName, this.LastAsyncStepException.Message));
									num++;
								}
							}
							if (!flag1)
							{
								break;
							}
						}
					}
					else
					{
						string str = this.sender.RetryPolicy.ServerBusyExceptionMessage;
						yield return base.CallAsyncSleep(base.RemainingTime());
						base.Complete(new ServerBusyException(str, this.trackingContext));
						goto Label0;
					}
				}
				finally
				{
					stopwatch.Stop();
				}
				List<BrokeredMessage> brokeredMessages1 = this.messagesBuffer;
				brokeredMessages1.ForEach((BrokeredMessage brokeredMessage) => brokeredMessage.Dispose());
				this.messagesBuffer.Clear();
				base.Complete(base.LastAsyncStepException);
			Label0:
				yield break;
			}

			public static IEnumerable<long> ScheduleSenderEnd(IAsyncResult r)
			{
				return AsyncResult<MessageSender.RetryScheduleSenderAsyncResult>.End(r).SequenceNumbers;
			}
		}

		private sealed class RetrySenderAsyncResult : RetryAsyncResult<MessageSender.RetrySenderAsyncResult>
		{
			private readonly MessageSender sender;

			private readonly TrackingContext trackingContext;

			private readonly IEnumerable<BrokeredMessage> messages;

			private readonly List<BrokeredMessage> messagesBuffer;

			private readonly bool fromSync;

			private readonly int messageCount;

			public RetrySenderAsyncResult(MessageSender sender, TrackingContext trackingContext, IEnumerable<BrokeredMessage> messages, bool fromSync, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				if (trackingContext == null)
				{
					throw Fx.Exception.ArgumentNull("trackingContext");
				}
				if (sender == null)
				{
					throw Fx.Exception.ArgumentNull("sender");
				}
				this.fromSync = fromSync;
				this.sender = sender;
				this.trackingContext = trackingContext;
				this.messages = messages;
				this.messagesBuffer = new List<BrokeredMessage>();
				BrokeredMessage[] array = this.messages as BrokeredMessage[] ?? this.messages.ToArray<BrokeredMessage>();
				this.messageCount = array.Count<BrokeredMessage>();
				if (!(sender.RetryPolicy is NoRetry))
				{
					try
					{
						BrokeredMessage[] brokeredMessageArray = array;
						for (int i = 0; i < (int)brokeredMessageArray.Length; i++)
						{
							BrokeredMessage brokeredMessage = brokeredMessageArray[i];
							this.messagesBuffer.Add(brokeredMessage.Clone());
						}
					}
					catch (Exception exception1)
					{
						Exception exception = exception1;
						MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRetryPolicyStreamNotClonable(this.trackingContext.Activity, this.trackingContext.TrackingId, this.sender.RetryPolicy.GetType().Name, "Send", exception.GetType().FullName, exception.Message));
						foreach (BrokeredMessage brokeredMessage1 in this.messagesBuffer)
						{
							brokeredMessage1.Dispose();
						}
						this.messagesBuffer.Clear();
					}
				}
			}

			public static new void End(IAsyncResult r)
			{
				AsyncResult<MessageSender.RetrySenderAsyncResult>.End(r);
			}

			protected override IEnumerator<IteratorAsyncResult<MessageSender.RetrySenderAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				TimeSpan timeSpan;
				bool flag;
				int num = 0;
				Stopwatch stopwatch = Stopwatch.StartNew();
				try
				{
					timeSpan = (this.sender.RetryPolicy.IsServerBusy ? Microsoft.ServiceBus.RetryPolicy.ServerBusyBaseSleepTime : TimeSpan.Zero);
					TimeSpan timeSpan1 = timeSpan;
					if (!this.sender.RetryPolicy.IsServerBusy || !(Microsoft.ServiceBus.RetryPolicy.ServerBusyBaseSleepTime >= base.OriginalTimeout))
					{
						while (true)
						{
							bool flag1 = false;
							if (timeSpan1 != TimeSpan.Zero)
							{
								yield return base.CallAsyncSleep(timeSpan1);
							}
							List<BrokeredMessage> list = null;
							if (num != 0)
							{
								List<BrokeredMessage> brokeredMessages = this.messagesBuffer;
								list = (
									from brokeredMessage in brokeredMessages
									select brokeredMessage.Clone()).ToList<BrokeredMessage>();
							}
							try
							{
								MessageSender.RetrySenderAsyncResult retrySenderAsyncResult = this;
								Transaction ambientTransaction = base.AmbientTransaction;
								IteratorAsyncResult<MessageSender.RetrySenderAsyncResult>.BeginCall beginCall = (MessageSender.RetrySenderAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => {
									MessageSender messageSender = thisPtr.sender;
									TrackingContext trackingContext = thisPtr.trackingContext;
									object obj = list;
									if (obj == null)
									{
										obj = thisPtr.messages;
									}
									return messageSender.OnBeginSend(trackingContext, (IEnumerable<BrokeredMessage>)obj, thisPtr.fromSync, t, c, s);
								};
								yield return retrySenderAsyncResult.CallTransactionalAsync(ambientTransaction, beginCall, (MessageSender.RetrySenderAsyncResult thisPtr, IAsyncResult r) => thisPtr.sender.OnEndSend(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
							}
							finally
							{
								if (list != null)
								{
									list.ForEach((BrokeredMessage brokeredMessage) => brokeredMessage.Dispose());
									list.Clear();
								}
							}
							if (base.LastAsyncStepException == null)
							{
								MessagingPerformanceCounters.IncrementSendMessageSuccessPerSec(this.sender.MessagingFactory.Address, this.messageCount);
								this.sender.RetryPolicy.ResetServerBusy();
							}
							else
							{
								MessagingPerformanceCounters.IncrementSendMessageFailurePerSec(this.sender.MessagingFactory.Address, this.messageCount);
								MessagingPerformanceCounters.IncrementExceptionPerSec(this.sender.MessagingFactory.Address, 1, base.LastAsyncStepException);
								if (this.messagesBuffer.Count > 0)
								{
									flag = (base.TransactionExists ? false : this.sender.RetryPolicy.ShouldRetry(base.RemainingTime(), num, base.LastAsyncStepException, out timeSpan1));
									flag1 = flag;
								}
								if (flag1)
								{
									MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRetryPolicyIteration(this.trackingContext.Activity, this.trackingContext.TrackingId, this.sender.RetryPolicy.GetType().Name, "Send", num, timeSpan1.ToString(), this.LastAsyncStepException.GetType().FullName, this.LastAsyncStepException.Message));
									num++;
								}
							}
							if (!flag1)
							{
								break;
							}
						}
					}
					else
					{
						string str = this.sender.RetryPolicy.ServerBusyExceptionMessage;
						yield return base.CallAsyncSleep(base.RemainingTime());
						base.Complete(new ServerBusyException(str, this.trackingContext));
						goto Label0;
					}
				}
				finally
				{
					stopwatch.Stop();
					MessagingPerformanceCounters.IncrementSendMessageLatency(this.sender.MessagingFactory.Address, stopwatch.ElapsedTicks);
				}
				List<BrokeredMessage> brokeredMessages1 = this.messagesBuffer;
				brokeredMessages1.ForEach((BrokeredMessage brokeredMessage) => brokeredMessage.Dispose());
				this.messagesBuffer.Clear();
				base.Complete(base.LastAsyncStepException);
			Label0:
				yield break;
			}
		}

		private sealed class RetrySenderEventDataAsyncResult : RetryAsyncResult<MessageSender.RetrySenderEventDataAsyncResult>
		{
			private readonly MessageSender sender;

			private readonly TrackingContext trackingContext;

			private readonly IEnumerable<EventData> messages;

			private readonly List<EventData> eventBuffer;

			private readonly int messageCount;

			public RetrySenderEventDataAsyncResult(MessageSender sender, TrackingContext trackingContext, IEnumerable<EventData> messages, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				if (trackingContext == null)
				{
					throw Fx.Exception.ArgumentNull("trackingContext");
				}
				if (sender == null)
				{
					throw Fx.Exception.ArgumentNull("sender");
				}
				this.sender = sender;
				this.trackingContext = trackingContext;
				this.messages = messages;
				this.eventBuffer = new List<EventData>();
				EventData[] array = this.messages as EventData[] ?? this.messages.ToArray<EventData>();
				this.messageCount = array.Count<EventData>();
				if (((IEnumerable<EventData>)array).Any<EventData>((EventData e) => {
					if (!e.BodyStream.CanSeek)
					{
						return false;
					}
					return e.BodyStream.Position != (long)0;
				}))
				{
					throw Fx.Exception.AsError(new InvalidOperationException(SRClient.MessageBodyConsumed), this.trackingContext.Activity);
				}
				if (!(sender.RetryPolicy is NoRetry))
				{
					try
					{
						EventData[] eventDataArray = array;
						for (int i = 0; i < (int)eventDataArray.Length; i++)
						{
							EventData eventDatum = eventDataArray[i];
							this.eventBuffer.Add(eventDatum.Clone());
						}
					}
					catch (Exception exception1)
					{
						Exception exception = exception1;
						MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRetryPolicyStreamNotClonable(this.trackingContext.Activity, this.trackingContext.TrackingId, this.sender.RetryPolicy.GetType().Name, "Send", exception.GetType().FullName, exception.Message));
						foreach (EventData eventDatum1 in this.eventBuffer)
						{
							eventDatum1.Dispose();
						}
						this.eventBuffer.Clear();
					}
				}
			}

			public static new void End(IAsyncResult r)
			{
				AsyncResult<MessageSender.RetrySenderEventDataAsyncResult>.End(r);
			}

			protected override IEnumerator<IteratorAsyncResult<MessageSender.RetrySenderEventDataAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				TimeSpan timeSpan;
				bool flag;
				int num = 0;
				Stopwatch stopwatch = Stopwatch.StartNew();
				try
				{
					timeSpan = (this.sender.RetryPolicy.IsServerBusy ? Microsoft.ServiceBus.RetryPolicy.ServerBusyBaseSleepTime : TimeSpan.Zero);
					TimeSpan timeSpan1 = timeSpan;
					if (!this.sender.RetryPolicy.IsServerBusy || !(Microsoft.ServiceBus.RetryPolicy.ServerBusyBaseSleepTime >= base.OriginalTimeout))
					{
						while (true)
						{
							bool flag1 = false;
							if (timeSpan1 != TimeSpan.Zero)
							{
								yield return base.CallAsyncSleep(timeSpan1);
							}
							List<EventData> list = null;
							if (num != 0)
							{
								List<EventData> eventDatas = this.eventBuffer;
								list = (
									from eventData in eventDatas
									select eventData.Clone()).ToList<EventData>();
							}
							try
							{
								MessageSender.RetrySenderEventDataAsyncResult retrySenderEventDataAsyncResult = this;
								Transaction ambientTransaction = base.AmbientTransaction;
								IteratorAsyncResult<MessageSender.RetrySenderEventDataAsyncResult>.BeginCall beginCall = (MessageSender.RetrySenderEventDataAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => {
									MessageSender messageSender = thisPtr.sender;
									TrackingContext trackingContext = thisPtr.trackingContext;
									object obj = list;
									if (obj == null)
									{
										obj = thisPtr.messages;
									}
									return messageSender.OnBeginSendEventData(trackingContext, (IEnumerable<EventData>)obj, t, c, s);
								};
								yield return retrySenderEventDataAsyncResult.CallTransactionalAsync(ambientTransaction, beginCall, (MessageSender.RetrySenderEventDataAsyncResult thisPtr, IAsyncResult r) => thisPtr.sender.OnEndSendEventData(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
							}
							finally
							{
								if (list != null)
								{
									list.ForEach((EventData eventData) => eventData.Dispose());
									list.Clear();
								}
							}
							if (base.LastAsyncStepException == null)
							{
								MessagingPerformanceCounters.IncrementSendMessageSuccessPerSec(this.sender.MessagingFactory.Address, this.messageCount);
								this.sender.RetryPolicy.ResetServerBusy();
							}
							else
							{
								MessagingPerformanceCounters.IncrementSendMessageFailurePerSec(this.sender.MessagingFactory.Address, this.messageCount);
								MessagingPerformanceCounters.IncrementExceptionPerSec(this.sender.MessagingFactory.Address, 1, base.LastAsyncStepException);
								if (this.eventBuffer.Count > 0)
								{
									flag = (base.TransactionExists ? false : this.sender.RetryPolicy.ShouldRetry(base.RemainingTime(), num, base.LastAsyncStepException, out timeSpan1));
									flag1 = flag;
								}
								if (flag1)
								{
									MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRetryPolicyIteration(this.trackingContext.Activity, this.trackingContext.TrackingId, this.sender.RetryPolicy.GetType().Name, "Send", num, timeSpan1.ToString(), this.LastAsyncStepException.GetType().FullName, this.LastAsyncStepException.Message));
									num++;
								}
							}
							if (!flag1)
							{
								break;
							}
						}
					}
					else
					{
						string str = this.sender.RetryPolicy.ServerBusyExceptionMessage;
						yield return base.CallAsyncSleep(base.RemainingTime());
						base.Complete(new ServerBusyException(str, this.trackingContext));
						goto Label0;
					}
				}
				finally
				{
					stopwatch.Stop();
					MessagingPerformanceCounters.IncrementSendMessageLatency(this.sender.MessagingFactory.Address, stopwatch.ElapsedTicks);
				}
				List<EventData> eventDatas1 = this.eventBuffer;
				eventDatas1.ForEach((EventData eventData) => eventData.Dispose());
				this.eventBuffer.Clear();
				base.Complete(base.LastAsyncStepException);
			Label0:
				yield break;
			}
		}
	}
}