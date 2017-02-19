using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Common.Parallel;
using Microsoft.ServiceBus.Properties;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus.Messaging
{
	public sealed class EventHubConsumerGroup : ClientEntity
	{
		public const string DefaultGroupName = "$Default";

		public const string StartOfStream = "-1";

		private readonly ConcurrentDictionary<Lease, EventProcessorLifecycleManager> handlers = new ConcurrentDictionary<Lease, EventProcessorLifecycleManager>();

		private int prefetchCount;

		public string EventHubPath
		{
			get;
			private set;
		}

		public string GroupName
		{
			get;
			private set;
		}

		internal MessageSender InternalSender
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
				return this.MessagingFactory.OperationTimeout;
			}
		}

		public int PrefetchCount
		{
			get
			{
				return this.prefetchCount;
			}
			set
			{
				EventHubClient.ValidateEventHubPrefetchCount(value);
				this.prefetchCount = value;
			}
		}

		internal EventHubConsumerGroup(Microsoft.ServiceBus.Messaging.MessagingFactory messagingFactory, string eventHubPath, string groupName, int prefetchCount)
		{
			this.MessagingFactory = messagingFactory;
			base.ClientEntityManager = new MessageClientEntityManager();
			this.handlers = new ConcurrentDictionary<Lease, EventProcessorLifecycleManager>();
			this.EventHubPath = eventHubPath;
			this.GroupName = groupName;
			base.RetryPolicy = messagingFactory.RetryPolicy.Clone();
			this.PrefetchCount = prefetchCount;
		}

		public EventHubReceiver CreateReceiver(string partitionId, long epoch)
		{
			base.ThrowIfDisposed();
			DateTime? nullable = null;
			return (new EventHubConsumerGroup.CreateReceiverAsyncResult(this, partitionId, null, nullable, new long?(epoch), this.OperationTimeout, null, null)).RunSynchronously().Receiver;
		}

		public EventHubReceiver CreateReceiver(string partitionId)
		{
			base.ThrowIfDisposed();
			DateTime? nullable = null;
			long? nullable1 = null;
			return (new EventHubConsumerGroup.CreateReceiverAsyncResult(this, partitionId, "-1", nullable, nullable1, this.OperationTimeout, null, null)).RunSynchronously().Receiver;
		}

		public EventHubReceiver CreateReceiver(string partitionId, string startingOffset)
		{
			return this.CreateReceiver(partitionId, startingOffset, false);
		}

		public EventHubReceiver CreateReceiver(string partitionId, string startingOffset, bool offsetInclusive)
		{
			base.ThrowIfDisposed();
			DateTime? nullable = null;
			long? nullable1 = null;
			return (new EventHubConsumerGroup.CreateReceiverAsyncResult(this, partitionId, startingOffset, nullable, nullable1, offsetInclusive, this.OperationTimeout, null, null)).RunSynchronously().Receiver;
		}

		public EventHubReceiver CreateReceiver(string partitionId, string startingOffset, long epoch)
		{
			return this.CreateReceiver(partitionId, startingOffset, false, epoch);
		}

		public EventHubReceiver CreateReceiver(string partitionId, string startingOffset, bool offsetInclusive, long epoch)
		{
			base.ThrowIfDisposed();
			DateTime? nullable = null;
			return (new EventHubConsumerGroup.CreateReceiverAsyncResult(this, partitionId, startingOffset, nullable, new long?(epoch), offsetInclusive, this.OperationTimeout, null, null)).RunSynchronously().Receiver;
		}

		public EventHubReceiver CreateReceiver(string partitionId, DateTime startingDateTimeUtc)
		{
			base.ThrowIfDisposed();
			long? nullable = null;
			return (new EventHubConsumerGroup.CreateReceiverAsyncResult(this, partitionId, null, new DateTime?(startingDateTimeUtc), nullable, this.OperationTimeout, null, null)).RunSynchronously().Receiver;
		}

		public EventHubReceiver CreateReceiver(string partitionId, DateTime startingDateTimeUtc, long epoch)
		{
			base.ThrowIfDisposed();
			return (new EventHubConsumerGroup.CreateReceiverAsyncResult(this, partitionId, null, new DateTime?(startingDateTimeUtc), new long?(epoch), this.OperationTimeout, null, null)).RunSynchronously().Receiver;
		}

		public Task<EventHubReceiver> CreateReceiverAsync(string partitionId, long epoch)
		{
			base.ThrowIfDisposed();
			return TaskHelpers.CreateTask<EventHubConsumerGroup, EventHubReceiver>((EventHubConsumerGroup thisPtr, AsyncCallback c, object s) => (new EventHubConsumerGroup.CreateReceiverAsyncResult(thisPtr, partitionId, null, null, new long?(epoch), thisPtr.OperationTimeout, c, s)).Start(), (EventHubConsumerGroup thisPtr, IAsyncResult r) => AsyncResult<EventHubConsumerGroup.CreateReceiverAsyncResult>.End(r).Receiver, this);
		}

		public Task<EventHubReceiver> CreateReceiverAsync(string partitionId)
		{
			base.ThrowIfDisposed();
			return TaskHelpers.CreateTask<EventHubConsumerGroup, EventHubReceiver>((EventHubConsumerGroup thisPtr, AsyncCallback c, object s) => (new EventHubConsumerGroup.CreateReceiverAsyncResult(thisPtr, partitionId, "-1", null, null, thisPtr.OperationTimeout, c, s)).Start(), (EventHubConsumerGroup thisPtr, IAsyncResult r) => AsyncResult<EventHubConsumerGroup.CreateReceiverAsyncResult>.End(r).Receiver, this);
		}

		public Task<EventHubReceiver> CreateReceiverAsync(string partitionId, string startingOffset)
		{
			return this.CreateReceiverAsync(partitionId, startingOffset, false);
		}

		public Task<EventHubReceiver> CreateReceiverAsync(string partitionId, string startingOffset, bool offsetInclusive)
		{
			base.ThrowIfDisposed();
			return TaskHelpers.CreateTask<EventHubConsumerGroup, EventHubReceiver>((EventHubConsumerGroup thisPtr, AsyncCallback c, object s) => (new EventHubConsumerGroup.CreateReceiverAsyncResult(thisPtr, partitionId, startingOffset, null, null, offsetInclusive, thisPtr.OperationTimeout, c, s)).Start(), (EventHubConsumerGroup thisPtr, IAsyncResult r) => AsyncResult<EventHubConsumerGroup.CreateReceiverAsyncResult>.End(r).Receiver, this);
		}

		public Task<EventHubReceiver> CreateReceiverAsync(string partitionId, DateTime startingDateTimeUtc)
		{
			base.ThrowIfDisposed();
			return TaskHelpers.CreateTask<EventHubConsumerGroup, EventHubReceiver>((EventHubConsumerGroup thisPtr, AsyncCallback c, object s) => (new EventHubConsumerGroup.CreateReceiverAsyncResult(thisPtr, partitionId, null, new DateTime?(startingDateTimeUtc), null, thisPtr.OperationTimeout, c, s)).Start(), (EventHubConsumerGroup thisPtr, IAsyncResult r) => AsyncResult<EventHubConsumerGroup.CreateReceiverAsyncResult>.End(r).Receiver, this);
		}

		public Task<EventHubReceiver> CreateReceiverAsync(string partitionId, string startingOffset, long epoch)
		{
			return this.CreateReceiverAsync(partitionId, startingOffset, false, epoch);
		}

		public Task<EventHubReceiver> CreateReceiverAsync(string partitionId, string startingOffset, bool offsetInclusive, long epoch)
		{
			base.ThrowIfDisposed();
			return TaskHelpers.CreateTask<EventHubConsumerGroup, EventHubReceiver>((EventHubConsumerGroup thisPtr, AsyncCallback c, object s) => (new EventHubConsumerGroup.CreateReceiverAsyncResult(thisPtr, partitionId, startingOffset, null, new long?(epoch), offsetInclusive, thisPtr.OperationTimeout, c, s)).Start(), (EventHubConsumerGroup thisPtr, IAsyncResult r) => AsyncResult<EventHubConsumerGroup.CreateReceiverAsyncResult>.End(r).Receiver, this);
		}

		public Task<EventHubReceiver> CreateReceiverAsync(string partitionId, DateTime startingDateTimeUtc, long epoch)
		{
			base.ThrowIfDisposed();
			return TaskHelpers.CreateTask<EventHubConsumerGroup, EventHubReceiver>((EventHubConsumerGroup thisPtr, AsyncCallback c, object s) => (new EventHubConsumerGroup.CreateReceiverAsyncResult(thisPtr, partitionId, null, new DateTime?(startingDateTimeUtc), new long?(epoch), thisPtr.OperationTimeout, c, s)).Start(), (EventHubConsumerGroup thisPtr, IAsyncResult r) => AsyncResult<EventHubConsumerGroup.CreateReceiverAsyncResult>.End(r).Receiver, this);
		}

		protected override void OnAbort()
		{
			EventHubConsumerGroup.CloseOrAbortAsyncResult closeOrAbortAsyncResult = new EventHubConsumerGroup.CloseOrAbortAsyncResult(this, true, TimeSpan.MaxValue, null, null);
			closeOrAbortAsyncResult.RunSynchronously();
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return (new EventHubConsumerGroup.CloseOrAbortAsyncResult(this, false, timeout, callback, state)).Start();
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult(callback, state);
		}

		protected override void OnClose(TimeSpan timeout)
		{
			EventHubConsumerGroup.CloseOrAbortAsyncResult closeOrAbortAsyncResult = new EventHubConsumerGroup.CloseOrAbortAsyncResult(this, false, timeout, null, null);
			closeOrAbortAsyncResult.RunSynchronously();
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			AsyncResult<EventHubConsumerGroup.CloseOrAbortAsyncResult>.End(result);
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
		}

		private void RegisterMessageClientEntity(ClientEntity child)
		{
			base.ClientEntityManager.Add(child);
		}

		public void RegisterProcessor<T>(Lease lease, ICheckpointManager checkpointManager)
		where T : IEventProcessor
		{
			this.RegisterProcessor<T>(lease, checkpointManager, EventProcessorOptions.DefaultOptions);
		}

		public void RegisterProcessor<T>(Lease lease, ICheckpointManager checkpointManager, EventProcessorOptions processorOptions)
		where T : IEventProcessor
		{
			this.RegisterProcessorAsync<T>(lease, checkpointManager, processorOptions).Wait();
		}

		public Task RegisterProcessorAsync<T>(Lease lease, ICheckpointManager checkpointManager)
		where T : IEventProcessor
		{
			return this.RegisterProcessorAsync<T>(lease, checkpointManager, EventProcessorOptions.DefaultOptions);
		}

		public Task RegisterProcessorAsync<T>(Lease lease, ICheckpointManager checkpointManager, EventProcessorOptions processorOptions)
		where T : IEventProcessor
		{
			return this.RegisterProcessorFactoryAsync(lease, checkpointManager, new DefaultEventProcessorFactory<T>(), processorOptions);
		}

		public void RegisterProcessorFactory(Lease lease, ICheckpointManager checkpointManager, IEventProcessorFactory eventProcessorFactory)
		{
			this.RegisterProcessorFactory(lease, checkpointManager, eventProcessorFactory, EventProcessorOptions.DefaultOptions);
		}

		public void RegisterProcessorFactory(Lease lease, ICheckpointManager checkpointManager, IEventProcessorFactory eventProcessorFactory, EventProcessorOptions processorOptions)
		{
			this.RegisterProcessorFactoryAsync(lease, checkpointManager, eventProcessorFactory, processorOptions).Wait();
		}

		public Task RegisterProcessorFactoryAsync(Lease lease, ICheckpointManager checkpointManager, IEventProcessorFactory eventProcessorFactory)
		{
			return this.RegisterProcessorFactoryAsync(lease, checkpointManager, eventProcessorFactory, EventProcessorOptions.DefaultOptions);
		}

		public Task RegisterProcessorFactoryAsync(Lease lease, ICheckpointManager checkpointManager, IEventProcessorFactory eventProcessorFactory, EventProcessorOptions processorOptions)
		{
			base.ThrowIfDisposed();
			if (lease == null)
			{
				throw Fx.Exception.ArgumentNull("lease");
			}
			if (checkpointManager == null)
			{
				throw Fx.Exception.ArgumentNull("checkpointManager");
			}
			if (eventProcessorFactory == null)
			{
				throw Fx.Exception.ArgumentNull("eventProcessorFactory");
			}
			if (processorOptions == null)
			{
				throw Fx.Exception.ArgumentNull("processorOptions");
			}
			EventProcessorLifecycleManager eventProcessorLifecycleManager = null;
			if (this.handlers.TryGetValue(lease, out eventProcessorLifecycleManager))
			{
				ExceptionTrace exception = Fx.Exception;
				string eventProcessorAlreadyRegistered = Resources.EventProcessorAlreadyRegistered;
				object[] partitionId = new object[] { lease.PartitionId };
				throw exception.AsError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(eventProcessorAlreadyRegistered, partitionId)), null);
			}
			eventProcessorLifecycleManager = new EventProcessorLifecycleManager(this, lease, checkpointManager, processorOptions);
			if (!this.handlers.TryAdd(lease, eventProcessorLifecycleManager))
			{
				return TaskHelpers.GetCompletedTask<object>(null);
			}
			return eventProcessorLifecycleManager.RegisterProcessorFactoryAsync(eventProcessorFactory);
		}

		public void UnregisterProcessor(Lease lease, CloseReason reason)
		{
			EventProcessorLifecycleManager eventProcessorLifecycleManager = null;
			if (this.handlers.TryRemove(lease, out eventProcessorLifecycleManager))
			{
				eventProcessorLifecycleManager.UnregisterProcessorAsync(reason).Wait();
			}
		}

		public Task UnregisterProcessorAsync(Lease lease, CloseReason reason)
		{
			EventProcessorLifecycleManager eventProcessorLifecycleManager = null;
			if (!this.handlers.TryRemove(lease, out eventProcessorLifecycleManager))
			{
				return TaskHelpers.GetCompletedTask<object>(null);
			}
			return eventProcessorLifecycleManager.UnregisterProcessorAsync(reason);
		}

		private sealed class CloseOrAbortAsyncResult : IteratorAsyncResult<EventHubConsumerGroup.CloseOrAbortAsyncResult>
		{
			private readonly EventHubConsumerGroup owner;

			private readonly bool shouldAbort;

			public CloseOrAbortAsyncResult(EventHubConsumerGroup owner, bool shouldAbort, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.owner = owner;
				this.shouldAbort = shouldAbort;
			}

			protected override IEnumerator<IteratorAsyncResult<EventHubConsumerGroup.CloseOrAbortAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				foreach (EventProcessorLifecycleManager value in this.owner.handlers.Values)
				{
					EventHubConsumerGroup.CloseOrAbortAsyncResult closeOrAbortAsyncResult = this;
					yield return closeOrAbortAsyncResult.CallTask((EventHubConsumerGroup.CloseOrAbortAsyncResult AsyncResult, TimeSpan timespan) => value.UnregisterProcessorAsync(CloseReason.Shutdown), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				}
				this.owner.handlers.Clear();
				if (!this.shouldAbort)
				{
					EventHubConsumerGroup.CloseOrAbortAsyncResult closeOrAbortAsyncResult1 = this;
					IteratorAsyncResult<EventHubConsumerGroup.CloseOrAbortAsyncResult>.BeginCall beginCall = (EventHubConsumerGroup.CloseOrAbortAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.owner.ClientEntityManager.BeginClose(t, c, s);
					yield return closeOrAbortAsyncResult1.CallAsync(beginCall, (EventHubConsumerGroup.CloseOrAbortAsyncResult thisPtr, IAsyncResult r) => thisPtr.owner.ClientEntityManager.EndClose(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				}
				else
				{
					this.owner.ClientEntityManager.Abort();
				}
			}
		}

		private sealed class CreateReceiverAsyncResult : IteratorAsyncResult<EventHubConsumerGroup.CreateReceiverAsyncResult>
		{
			private readonly EventHubConsumerGroup owner;

			private readonly string partitionId;

			private readonly string startingOffset;

			private readonly DateTime? startingDateTime;

			private readonly long? epoch;

			private readonly bool offsetInclusive;

			public EventHubReceiver Receiver
			{
				get;
				private set;
			}

			public CreateReceiverAsyncResult(EventHubConsumerGroup owner, string partitionId, string startingOffset, DateTime? startingDateTime, long? epoch, TimeSpan timeout, AsyncCallback callback, object state) : this(owner, partitionId, startingOffset, startingDateTime, epoch, false, timeout, callback, state)
			{
			}

			public CreateReceiverAsyncResult(EventHubConsumerGroup owner, string partitionId, string startingOffset, DateTime? startingDateTime, long? epoch, bool offsetInclusive, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.owner = owner;
				this.partitionId = partitionId;
				this.startingOffset = startingOffset;
				this.startingDateTime = startingDateTime;
				this.epoch = epoch;
				this.offsetInclusive = offsetInclusive;
			}

			protected override IEnumerator<IteratorAsyncResult<EventHubConsumerGroup.CreateReceiverAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				if (!this.startingDateTime.HasValue)
				{
					this.Receiver = new EventHubReceiver(this.owner.MessagingFactory, this.owner.EventHubPath, this.owner.GroupName, this.startingOffset, this.partitionId, this.owner.PrefetchCount, this.epoch, this.offsetInclusive);
				}
				else
				{
					this.Receiver = new EventHubReceiver(this.owner.MessagingFactory, this.owner.EventHubPath, this.owner.GroupName, this.startingDateTime, this.partitionId, this.owner.PrefetchCount, this.epoch);
				}
				if (this.Receiver != null)
				{
					EventHubConsumerGroup.CreateReceiverAsyncResult createReceiverAsyncResult = this;
					IteratorAsyncResult<EventHubConsumerGroup.CreateReceiverAsyncResult>.BeginCall beginCall = (EventHubConsumerGroup.CreateReceiverAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.Receiver.BeginOpen(t, c, s);
					yield return createReceiverAsyncResult.CallAsync(beginCall, (EventHubConsumerGroup.CreateReceiverAsyncResult thisPtr, IAsyncResult r) => thisPtr.Receiver.EndOpen(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
					this.owner.RegisterMessageClientEntity(this.Receiver);
				}
			}
		}
	}
}