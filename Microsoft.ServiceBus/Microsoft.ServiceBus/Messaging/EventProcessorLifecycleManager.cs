using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Common.Parallel;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus.Messaging
{
	internal class EventProcessorLifecycleManager
	{
		private readonly static TimeSpan ServerBusyExceptionBackoffAmount;

		private readonly static TimeSpan OtherExceptionBackoffAmount;

		private EventHubConsumerGroup eventHubConsumer;

		private EventHubReceiver eventHubReceiver;

		private Lease lease;

		private ICheckpointManager checkpointManager;

		private EventProcessorOptions processorOptions;

		private IEventProcessor processor;

		private IEventProcessorFactory processorFactory;

		private int isStarted;

		private PartitionContext context;

		private Task dispatchTask;

		static EventProcessorLifecycleManager()
		{
			EventProcessorLifecycleManager.ServerBusyExceptionBackoffAmount = TimeSpan.FromSeconds(10);
			EventProcessorLifecycleManager.OtherExceptionBackoffAmount = TimeSpan.FromSeconds(1);
		}

		public EventProcessorLifecycleManager(EventHubConsumerGroup eventHubConsumer, Lease lease, ICheckpointManager checkpointManager, EventProcessorOptions processorOptions)
		{
			this.eventHubConsumer = eventHubConsumer;
			this.lease = lease;
			this.checkpointManager = checkpointManager;
			this.processorOptions = processorOptions;
		}

		private PartitionContext CreateContext()
		{
			PartitionContext partitionContext = new PartitionContext(this.checkpointManager)
			{
				EventHubPath = this.eventHubConsumer.EventHubPath,
				ConsumerGroupName = this.eventHubConsumer.GroupName,
				Offset = this.lease.Offset,
				Lease = this.lease
			};
			return partitionContext;
		}

		private PartitionContext InitializeManager(EventHubReceiver receiver)
		{
			this.eventHubReceiver = receiver;
			this.context = this.CreateContext();
			this.processor = this.processorFactory.CreateEventProcessor(this.context);
			return this.context;
		}

		private void RaiseExceptionReceivedEvent(Exception exception, string action)
		{
			try
			{
				this.processorOptions.RaiseExceptionReceived(this.processor, new ExceptionReceivedEventArgs(exception, action));
			}
			catch (Exception exception1)
			{
				Environment.FailFast(exception1.ToString());
			}
		}

		public Task RegisterProcessorAsync<T>()
		where T : IEventProcessor
		{
			return this.RegisterProcessorFactoryAsync(new DefaultEventProcessorFactory<T>());
		}

		public Task RegisterProcessorFactoryAsync(IEventProcessorFactory factory)
		{
			this.processorFactory = factory;
			return this.StartAsync();
		}

		private Task StartAsync()
		{
			if (Interlocked.CompareExchange(ref this.isStarted, 1, 0) != 0)
			{
				throw new InvalidOperationException("EventHubDispatcher has already started");
			}
			return TaskHelpers.CreateTask<EventProcessorLifecycleManager.EventProcessorInitializeAsyncResult>((AsyncCallback c, object s) => EventProcessorLifecycleManager.EventProcessorInitializeAsyncResult.Begin(this, this.lease, c, s), (IAsyncResult r) => AsyncResult<EventProcessorLifecycleManager.EventProcessorInitializeAsyncResult>.End(r));
		}

		public Task UnregisterProcessorAsync(CloseReason reason)
		{
			if (Interlocked.CompareExchange(ref this.isStarted, 0, 1) != 1)
			{
				return TaskHelpers.GetCompletedTask<object>(null);
			}
			return TaskHelpers.CreateTask<EventProcessorLifecycleManager.EventProcessorShutdownAsyncResult>((AsyncCallback c, object s) => EventProcessorLifecycleManager.EventProcessorShutdownAsyncResult.Begin(this, this.eventHubReceiver, this.processor, this.context, reason, this.dispatchTask, c, s), (IAsyncResult r) => AsyncResult<EventProcessorLifecycleManager.EventProcessorShutdownAsyncResult>.End(r));
		}

		private sealed class EventDataPumpAsyncResult : IteratorAsyncResult<EventProcessorLifecycleManager.EventDataPumpAsyncResult>
		{
			private readonly TrackingContext trackingContext;

			private EventProcessorLifecycleManager lifeCycleManager;

			private EventHubReceiver receiver;

			private PartitionContext context;

			private IEnumerable<EventData> messages;

			public EventDataPumpAsyncResult(EventProcessorLifecycleManager lifeCycleManager, EventHubReceiver receiver, PartitionContext context, AsyncCallback callback, object state) : base(TimeSpan.MaxValue, callback, state)
			{
				this.context = context;
				this.lifeCycleManager = lifeCycleManager;
				this.receiver = receiver;
				this.trackingContext = TrackingContext.GetInstance(Guid.NewGuid(), this.receiver.Name);
				base.Start();
			}

			public static IAsyncResult Begin(EventProcessorLifecycleManager lifeCycleManager, EventHubReceiver receiver, PartitionContext context, AsyncCallback callback, object state)
			{
				return new EventProcessorLifecycleManager.EventDataPumpAsyncResult(lifeCycleManager, receiver, context, callback, state);
			}

			protected override IEnumerator<IteratorAsyncResult<EventProcessorLifecycleManager.EventDataPumpAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				TimeSpan timeSpan;
				while (this.lifeCycleManager.isStarted == 1 && this.receiver.IsOpened)
				{
					this.messages = null;
					EventProcessorLifecycleManager.EventDataPumpAsyncResult eventDataPumpAsyncResult = this;
					yield return eventDataPumpAsyncResult.CallTask((EventProcessorLifecycleManager.EventDataPumpAsyncResult thisPtr, TimeSpan t) => this.receiver.ReceiveAsync(this.lifeCycleManager.processorOptions.MaxBatchSize, this.lifeCycleManager.processorOptions.ReceiveTimeOut).Then<IEnumerable<EventData>, object>((IEnumerable<EventData> m) => {
						if (m != null && m.Any<EventData>())
						{
							this.messages = m;
						}
						return TaskHelpers.GetCompletedTask<object>(null);
					}), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
					if (base.LastAsyncStepException != null)
					{
						MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageReceivePumpReceiveException(this.trackingContext.Activity, this.trackingContext.TrackingId, this.trackingContext.SystemTracker, base.LastAsyncStepException.ToString()));
						this.lifeCycleManager.RaiseExceptionReceivedEvent(base.LastAsyncStepException, "Receive");
						if (base.LastAsyncStepException is ReceiverDisconnectedException)
						{
							if (Interlocked.CompareExchange(ref this.lifeCycleManager.isStarted, 0, 1) != 1)
							{
								continue;
							}
							EventProcessorLifecycleManager.EventDataPumpAsyncResult eventDataPumpAsyncResult1 = this;
							IteratorAsyncResult<EventProcessorLifecycleManager.EventDataPumpAsyncResult>.BeginCall beginCall = (EventProcessorLifecycleManager.EventDataPumpAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => EventProcessorLifecycleManager.EventProcessorShutdownAsyncResult.Begin(thisPtr.lifeCycleManager, thisPtr.receiver, thisPtr.lifeCycleManager.processor, thisPtr.context, CloseReason.LeaseLost, null, c, s);
							yield return eventDataPumpAsyncResult1.CallAsync(beginCall, (EventProcessorLifecycleManager.EventDataPumpAsyncResult thisPtr, IAsyncResult r) => AsyncResult<EventProcessorLifecycleManager.EventProcessorShutdownAsyncResult>.End(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
							continue;
						}
						else if (EventProcessorLifecycleManager.EventDataPumpAsyncResult.ShouldBackoff(base.LastAsyncStepException, out timeSpan))
						{
							MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageReceivePumpBackoff(this.trackingContext.Activity, this.trackingContext.TrackingId, this.trackingContext.SystemTracker, (int)EventProcessorLifecycleManager.ServerBusyExceptionBackoffAmount.TotalMilliseconds, base.LastAsyncStepException.ToString()));
							yield return base.CallAsyncSleep(timeSpan);
						}
					}
					if (this.messages == null || !this.messages.Any<EventData>())
					{
						continue;
					}
					EventProcessorLifecycleManager.EventDataPumpAsyncResult eventDataPumpAsyncResult2 = this;
					yield return eventDataPumpAsyncResult2.CallTask((EventProcessorLifecycleManager.EventDataPumpAsyncResult thisPtr, TimeSpan t) => {
						if (this.messages == null || !this.messages.Any<EventData>())
						{
							return TaskHelpers.GetCompletedTask<object>(null);
						}
						EventData eventDatum = this.messages.Last<EventData>();
						if (eventDatum != null)
						{
							this.context.Offset = eventDatum.Offset;
							this.context.SequenceNumber = eventDatum.SequenceNumber;
						}
						return this.lifeCycleManager.processor.ProcessEventsAsync(this.context, this.messages);
					}, IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
					if (base.LastAsyncStepException == null)
					{
						continue;
					}
					MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageReceivePumpUserCallbackException(this.trackingContext.Activity, this.trackingContext.TrackingId, this.trackingContext.SystemTracker, base.LastAsyncStepException.ToString()));
					this.lifeCycleManager.RaiseExceptionReceivedEvent(base.LastAsyncStepException, "ProcessMessages");
					if (!(base.LastAsyncStepException is LeaseLostException))
					{
						continue;
					}
					bool flag = true;
					try
					{
						AggregateException lastAsyncStepException = base.LastAsyncStepException as AggregateException;
						if (lastAsyncStepException != null)
						{
							AggregateException aggregateException = lastAsyncStepException;
							aggregateException.Handle((Exception e) => e is LeaseLostException);
						}
					}
					catch
					{
						flag = false;
					}
					if (!flag || Interlocked.CompareExchange(ref this.lifeCycleManager.isStarted, 0, 1) != 1)
					{
						continue;
					}
					EventProcessorLifecycleManager.EventDataPumpAsyncResult eventDataPumpAsyncResult3 = this;
					IteratorAsyncResult<EventProcessorLifecycleManager.EventDataPumpAsyncResult>.BeginCall beginCall1 = (EventProcessorLifecycleManager.EventDataPumpAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => EventProcessorLifecycleManager.EventProcessorShutdownAsyncResult.Begin(thisPtr.lifeCycleManager, thisPtr.receiver, thisPtr.lifeCycleManager.processor, thisPtr.context, CloseReason.LeaseLost, null, c, s);
					yield return eventDataPumpAsyncResult3.CallAsync(beginCall1, (EventProcessorLifecycleManager.EventDataPumpAsyncResult thisPtr, IAsyncResult r) => AsyncResult<EventProcessorLifecycleManager.EventProcessorShutdownAsyncResult>.End(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
				}
			}

			private static bool ShouldBackoff(Exception exception, out TimeSpan amount)
			{
				if (exception is ServerBusyException || exception is MessagingEntityNotFoundException)
				{
					amount = EventProcessorLifecycleManager.ServerBusyExceptionBackoffAmount;
					return true;
				}
				MessagingException messagingException = exception as MessagingException;
				if (messagingException != null && messagingException.IsTransient)
				{
					amount = TimeSpan.Zero;
					return false;
				}
				amount = EventProcessorLifecycleManager.OtherExceptionBackoffAmount;
				return true;
			}
		}

		private sealed class EventProcessorInitializeAsyncResult : IteratorAsyncResult<EventProcessorLifecycleManager.EventProcessorInitializeAsyncResult>
		{
			private readonly static Action<AsyncResult, Exception> OnFinallyDelegate;

			private readonly TrackingContext trackingContext;

			private readonly EventProcessorLifecycleManager lifeCycleManager;

			private readonly Lease lease;

			private readonly string partitionId;

			private readonly string startingOffset;

			private readonly long epoch;

			private EventHubReceiver receiver;

			private PartitionContext partitionContext;

			static EventProcessorInitializeAsyncResult()
			{
				EventProcessorLifecycleManager.EventProcessorInitializeAsyncResult.OnFinallyDelegate = new Action<AsyncResult, Exception>(EventProcessorLifecycleManager.EventProcessorInitializeAsyncResult.OnFinally);
			}

			public EventProcessorInitializeAsyncResult(EventProcessorLifecycleManager lifeCycleManager, Lease lease, AsyncCallback callback, object state) : base(TimeSpan.MaxValue, callback, state)
			{
				this.lifeCycleManager = lifeCycleManager;
				this.lease = lease;
				this.trackingContext = TrackingContext.GetInstance(Guid.NewGuid(), lease.PartitionId);
				this.partitionId = lease.PartitionId;
				this.startingOffset = lease.Offset;
				this.epoch = lease.Epoch;
				EventProcessorLifecycleManager.EventProcessorInitializeAsyncResult eventProcessorInitializeAsyncResult = this;
				eventProcessorInitializeAsyncResult.OnCompleting = (Action<AsyncResult, Exception>)Delegate.Combine(eventProcessorInitializeAsyncResult.OnCompleting, EventProcessorLifecycleManager.EventProcessorInitializeAsyncResult.OnFinallyDelegate);
				base.Start();
			}

			public static IAsyncResult Begin(EventProcessorLifecycleManager lifeCycleManager, Lease lease, AsyncCallback callback, object state)
			{
				return new EventProcessorLifecycleManager.EventProcessorInitializeAsyncResult(lifeCycleManager, lease, callback, state);
			}

			protected override IEnumerator<IteratorAsyncResult<EventProcessorLifecycleManager.EventProcessorInitializeAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				object obj;
				Func<Task<EventHubReceiver>> func = null;
				if (!string.IsNullOrWhiteSpace(this.startingOffset))
				{
					func = () => this.lifeCycleManager.eventHubConsumer.CreateReceiverAsync(this.partitionId, this.startingOffset, this.epoch);
				}
				else
				{
					Func<string, object> initialOffsetProvider = this.lifeCycleManager.processorOptions.InitialOffsetProvider;
					obj = (initialOffsetProvider != null ? initialOffsetProvider(this.lease.PartitionId) : null);
					object obj1 = obj;
					if (obj1 != null)
					{
						string str = obj1 as string;
						if (str == null)
						{
							DateTime? nullable = (DateTime?)(obj1 as DateTime?);
							if (!nullable.HasValue)
							{
								throw Fx.Exception.AsError(new InvalidOperationException(SRClient.InitialOffsetProviderReturnTypeNotSupported(obj1.GetType().ToString())), null);
							}
							func = () => this.lifeCycleManager.eventHubConsumer.CreateReceiverAsync(this.partitionId, nullable.Value, this.epoch);
						}
						else
						{
							func = () => this.lifeCycleManager.eventHubConsumer.CreateReceiverAsync(this.partitionId, str, this.epoch);
						}
					}
					else
					{
						func = () => this.lifeCycleManager.eventHubConsumer.CreateReceiverAsync(this.partitionId, "-1", this.epoch);
					}
				}
				yield return base.CallTask((EventProcessorLifecycleManager.EventProcessorInitializeAsyncResult thisPtr, TimeSpan t) => func().Then<EventHubReceiver>((EventHubReceiver receiver) => {
					thisPtr.partitionContext = thisPtr.lifeCycleManager.InitializeManager(receiver);
					thisPtr.receiver = receiver;
					return thisPtr.lifeCycleManager.processor.OpenAsync(thisPtr.partitionContext);
				}), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				TaskCompletionSource<object> taskCompletionSource = new TaskCompletionSource<object>();
				try
				{
					EventProcessorLifecycleManager eventProcessorLifecycleManager = this.lifeCycleManager;
					EventHubReceiver eventHubReceiver = this.receiver;
					PartitionContext partitionContext = this.partitionContext;
					EventProcessorLifecycleManager.EventDataPumpAsyncResult.Begin(eventProcessorLifecycleManager, eventHubReceiver, partitionContext, (IAsyncResult ar) => taskCompletionSource.TrySetResult(null), null);
				}
				catch (Exception exception)
				{
					Environment.FailFast(exception.ToString());
				}
				this.lifeCycleManager.dispatchTask = taskCompletionSource.Task;
			}

			private static void OnFinally(AsyncResult asyncResult, Exception exception)
			{
				EventProcessorLifecycleManager.EventProcessorInitializeAsyncResult eventProcessorInitializeAsyncResult = (EventProcessorLifecycleManager.EventProcessorInitializeAsyncResult)asyncResult;
				if (exception != null)
				{
					eventProcessorInitializeAsyncResult.lifeCycleManager.RaiseExceptionReceivedEvent(exception, "Open");
				}
			}
		}

		private sealed class EventProcessorShutdownAsyncResult : IteratorAsyncResult<EventProcessorLifecycleManager.EventProcessorShutdownAsyncResult>
		{
			private readonly static Action<AsyncResult, Exception> OnFinallyDelegate;

			private readonly EventProcessorLifecycleManager lifeCycleManager;

			private readonly EventHubReceiver receiver;

			private readonly IEventProcessor processor;

			private readonly PartitionContext context;

			private readonly CloseReason reason;

			private readonly Task dispatchTask;

			static EventProcessorShutdownAsyncResult()
			{
				EventProcessorLifecycleManager.EventProcessorShutdownAsyncResult.OnFinallyDelegate = new Action<AsyncResult, Exception>(EventProcessorLifecycleManager.EventProcessorShutdownAsyncResult.OnFinally);
			}

			public EventProcessorShutdownAsyncResult(EventProcessorLifecycleManager lifeCycleManager, EventHubReceiver receiver, IEventProcessor processor, PartitionContext context, CloseReason reason, Task dispatchTask, AsyncCallback callback, object state) : base(TimeSpan.MaxValue, callback, state)
			{
				this.lifeCycleManager = lifeCycleManager;
				this.receiver = receiver;
				this.processor = processor;
				this.context = context;
				this.reason = reason;
				this.dispatchTask = dispatchTask;
				base.Start();
			}

			public static IAsyncResult Begin(EventProcessorLifecycleManager lifeCycleManager, EventHubReceiver receiver, IEventProcessor processor, PartitionContext context, CloseReason reason, Task dispatchTask, AsyncCallback callback, object state)
			{
				return new EventProcessorLifecycleManager.EventProcessorShutdownAsyncResult(lifeCycleManager, receiver, processor, context, reason, dispatchTask, callback, state);
			}

			protected override IEnumerator<IteratorAsyncResult<EventProcessorLifecycleManager.EventProcessorShutdownAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				if (this.receiver != null)
				{
					EventProcessorLifecycleManager.EventProcessorShutdownAsyncResult eventProcessorShutdownAsyncResult = this;
					yield return eventProcessorShutdownAsyncResult.CallTask((EventProcessorLifecycleManager.EventProcessorShutdownAsyncResult thisPtr, TimeSpan t) => thisPtr.receiver.CloseAsync().Then(() => {
						if (this.dispatchTask != null)
						{
							return this.dispatchTask;
						}
						return TaskHelpers.GetCompletedTask<object>(null);
					}), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
					if (this.processor != null)
					{
						EventProcessorLifecycleManager.EventProcessorShutdownAsyncResult eventProcessorShutdownAsyncResult1 = this;
						yield return eventProcessorShutdownAsyncResult1.CallTask((EventProcessorLifecycleManager.EventProcessorShutdownAsyncResult thisPtr, TimeSpan t) => thisPtr.processor.CloseAsync(thisPtr.context, thisPtr.reason), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
					}
				}
			}

			private static void OnFinally(AsyncResult asyncResult, Exception exception)
			{
				EventProcessorLifecycleManager.EventProcessorShutdownAsyncResult eventProcessorShutdownAsyncResult = (EventProcessorLifecycleManager.EventProcessorShutdownAsyncResult)asyncResult;
				if (exception != null)
				{
					eventProcessorShutdownAsyncResult.lifeCycleManager.RaiseExceptionReceivedEvent(exception, "Close");
				}
			}
		}
	}
}