using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Common.Parallel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus.Messaging
{
	public sealed class EventHubReceiver : ClientEntity
	{
		internal bool EnableCheckpoint
		{
			get
			{
				this.EnsureCreateInternalReceiver();
				return this.InternalReceiver.Mode == ReceiveMode.PeekLock;
			}
		}

		public long? Epoch
		{
			get;
			private set;
		}

		public string EventHubPath
		{
			get;
			private set;
		}

		internal MessageReceiver InternalReceiver
		{
			get;
			set;
		}

		internal Microsoft.ServiceBus.Messaging.MessagingFactory MessagingFactory
		{
			get;
			private set;
		}

		public string Name
		{
			get;
			private set;
		}

		public bool OffsetInclusive
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

		public string PartitionId
		{
			get;
			private set;
		}

		public int PrefetchCount
		{
			get
			{
				this.EnsureCreateInternalReceiver();
				return this.InternalReceiver.PrefetchCount;
			}
			set
			{
				EventHubClient.ValidateEventHubPrefetchCount(value);
				this.EnsureCreateInternalReceiver();
				this.InternalReceiver.PrefetchCount = value;
			}
		}

		public DateTime? StartingDateTimeUtc
		{
			get;
			private set;
		}

		public string StartingOffset
		{
			get;
			private set;
		}

		internal EventHubReceiver(Microsoft.ServiceBus.Messaging.MessagingFactory factory, string path, string consumerName, string startingOffset, string partitionId, int prefetchCount, long? epoch, bool offsetInclusive) : this(factory, path, consumerName, partitionId, epoch, offsetInclusive)
		{
			this.StartingOffset = startingOffset;
			this.PrefetchCount = prefetchCount;
		}

		internal EventHubReceiver(Microsoft.ServiceBus.Messaging.MessagingFactory factory, string path, string consumerName, DateTime? startingDateTimeUtc, string partitionId, int prefetchCount, long? epoch) : this(factory, path, consumerName, partitionId, epoch, false)
		{
			this.StartingDateTimeUtc = startingDateTimeUtc;
			this.PrefetchCount = prefetchCount;
		}

		private EventHubReceiver(Microsoft.ServiceBus.Messaging.MessagingFactory factory, string path, string consumerName, string partitionId, long? epoch, bool offsetInclusive)
		{
			if (factory == null)
			{
				throw Fx.Exception.ArgumentNull("factory");
			}
			if (partitionId == null)
			{
				throw Fx.Exception.ArgumentNull("partitionId");
			}
			if (string.IsNullOrWhiteSpace(path))
			{
				throw Fx.Exception.ArgumentNullOrWhiteSpace("path");
			}
			this.Name = consumerName;
			if (string.IsNullOrWhiteSpace(consumerName))
			{
				this.Name = "$Default";
			}
			this.EventHubPath = path;
			this.MessagingFactory = factory;
			this.PartitionId = partitionId;
			base.RetryPolicy = factory.RetryPolicy.Clone();
			this.Epoch = epoch;
			this.OffsetInclusive = offsetInclusive;
		}

		internal void Checkpoint(EventData data)
		{
			this.ThrowIfReceiverNull("Receive");
			if (data == null)
			{
				throw Fx.Exception.ArgumentNull("data");
			}
			if (this.InternalReceiver.Mode == ReceiveMode.ReceiveAndDelete)
			{
				throw FxTrace.Exception.AsError(new InvalidOperationException(SRClient.CannotCheckpointWithCurrentConsumerGroup), null);
			}
			this.InternalReceiver.EndCheckpoint(this.InternalReceiver.BeginCheckpoint(null, data.DeliveryTag, null, null));
		}

		internal Task CheckpointAsync(EventData data)
		{
			this.ThrowIfReceiverNull("Receive");
			if (data == null)
			{
				throw Fx.Exception.ArgumentNull("data");
			}
			if (this.InternalReceiver.Mode == ReceiveMode.ReceiveAndDelete)
			{
				throw FxTrace.Exception.AsError(new InvalidOperationException(SRClient.CannotCheckpointWithCurrentConsumerGroup), null);
			}
			return TaskHelpers.CreateTask((AsyncCallback callback, object o) => this.InternalReceiver.BeginCheckpoint(null, data.DeliveryTag, callback, o), (IAsyncResult result) => this.InternalReceiver.EndCheckpoint(result));
		}

		private void EnsureCreateInternalReceiver()
		{
			MessageReceiver result;
			base.ThrowIfDisposed();
			if (this.InternalReceiver == null)
			{
				lock (base.ThisLock)
				{
					if (this.InternalReceiver == null)
					{
						try
						{
							ReceiveMode receiveMode = ReceiveMode.ReceiveAndDelete;
							if (string.IsNullOrWhiteSpace(this.StartingOffset) && !this.StartingDateTimeUtc.HasValue)
							{
								receiveMode = ReceiveMode.PeekLock;
							}
							if (this.StartingDateTimeUtc.HasValue)
							{
								Microsoft.ServiceBus.Messaging.MessagingFactory messagingFactory = this.MessagingFactory;
								string eventHubPath = this.EventHubPath;
								string name = this.Name;
								string partitionId = this.PartitionId;
								DateTime? startingDateTimeUtc = this.StartingDateTimeUtc;
								result = messagingFactory.CreateReceiverAsync(eventHubPath, name, receiveMode, partitionId, startingDateTimeUtc.Value, this.Epoch).Result;
							}
							else
							{
								result = this.MessagingFactory.CreateReceiverAsync(this.EventHubPath, this.Name, receiveMode, this.PartitionId, EventHubReceiver.GetValueOrDefaultOffset(this.StartingOffset), this.OffsetInclusive, this.Epoch).Result;
							}
							this.InternalReceiver = result;
							this.InternalReceiver.PrefetchCount = this.PrefetchCount;
							this.InternalReceiver.RetryPolicy = base.RetryPolicy;
							this.InternalReceiver.EntityType = new MessagingEntityType?(MessagingEntityType.ConsumerGroup);
						}
						catch (AggregateException aggregateException)
						{
							throw aggregateException.Flatten().InnerException;
						}
					}
				}
			}
		}

		private static string GetValueOrDefaultOffset(string startingOffset)
		{
			if (string.IsNullOrWhiteSpace(startingOffset))
			{
				return "-1";
			}
			return startingOffset;
		}

		protected override void OnAbort()
		{
			if (this.InternalReceiver != null)
			{
				this.InternalReceiver.Abort();
			}
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (this.InternalReceiver == null)
			{
				return new CompletedAsyncResult(callback, state);
			}
			return this.InternalReceiver.BeginClose(timeout, callback, state);
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			this.ThrowIfReceiverNull("BeginOpen");
			return this.InternalReceiver.BeginOpen(timeout, callback, state);
		}

		protected override void OnClose(TimeSpan timeout)
		{
			if (this.InternalReceiver != null)
			{
				this.InternalReceiver.Close(timeout);
			}
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			if (this.InternalReceiver == null)
			{
				CompletedAsyncResult.End(result);
				return;
			}
			this.InternalReceiver.EndClose(result);
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			this.InternalReceiver.EndOpen(result);
		}

		protected override void OnOpen(TimeSpan timeout)
		{
			this.ThrowIfReceiverNull("Open");
			this.InternalReceiver.Open(timeout);
		}

		public EventData Receive()
		{
			return this.Receive(this.OperationTimeout);
		}

		public EventData Receive(TimeSpan waitTime)
		{
			IEnumerable<EventData> eventDatas;
			this.ThrowIfReceiverNull("Receive");
			if (!this.InternalReceiver.EndTryReceiveEventData(this.InternalReceiver.BeginTryReceiveEventData(null, 1, waitTime, null, null), out eventDatas))
			{
				return null;
			}
			return eventDatas.FirstOrDefault<EventData>();
		}

		public IEnumerable<EventData> Receive(int maxCount)
		{
			return this.Receive(maxCount, this.OperationTimeout);
		}

		public IEnumerable<EventData> Receive(int maxCount, TimeSpan waitTime)
		{
			IEnumerable<EventData> eventDatas;
			this.ThrowIfReceiverNull("Receive");
			if (!this.InternalReceiver.EndTryReceiveEventData(this.InternalReceiver.BeginTryReceiveEventData(null, maxCount, waitTime, null, null), out eventDatas))
			{
				return Enumerable.Empty<EventData>();
			}
			return eventDatas;
		}

		public Task<EventData> ReceiveAsync()
		{
			return this.ReceiveAsync(this.OperationTimeout);
		}

		public Task<EventData> ReceiveAsync(TimeSpan waitTime)
		{
			this.ThrowIfReceiverNull("Receive");
			return TaskHelpers.CreateTask<EventData>((AsyncCallback callback, object o) => this.InternalReceiver.BeginTryReceiveEventData(null, 1, waitTime, callback, o), (IAsyncResult result) => {
				IEnumerable<EventData> eventDatas;
				if (!this.InternalReceiver.EndTryReceiveEventData(result, out eventDatas))
				{
					return null;
				}
				return eventDatas.FirstOrDefault<EventData>();
			});
		}

		public Task<IEnumerable<EventData>> ReceiveAsync(int maxCount)
		{
			return this.ReceiveAsync(maxCount, this.OperationTimeout);
		}

		public Task<IEnumerable<EventData>> ReceiveAsync(int maxCount, TimeSpan waitTime)
		{
			this.ThrowIfReceiverNull("Receive");
			return TaskHelpers.CreateTask<IEnumerable<EventData>>((AsyncCallback callback, object o) => this.InternalReceiver.BeginTryReceiveEventData(null, maxCount, waitTime, callback, o), (IAsyncResult result) => {
				IEnumerable<EventData> eventDatas;
				if (!this.InternalReceiver.EndTryReceiveEventData(result, out eventDatas))
				{
					return Enumerable.Empty<EventData>();
				}
				return eventDatas;
			});
		}

		private void ThrowIfReceiverNull(string operationName)
		{
			this.EnsureCreateInternalReceiver();
			if (this.InternalReceiver == null)
			{
				throw FxTrace.Exception.AsError(new InvalidOperationException(SRCore.UnsupportedOperation(operationName)), null);
			}
		}
	}
}