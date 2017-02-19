using Microsoft.ServiceBus.Common.Parallel;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus.Messaging
{
	public class PartitionContext : ICheckpointer
	{
		private readonly AutoResetEvent checkpointing;

		private readonly ICheckpointManager checkpointManager;

		private long lastCheckpointedSequenceNumber;

		public string ConsumerGroupName
		{
			get;
			set;
		}

		public string EventHubPath
		{
			get;
			set;
		}

		public Microsoft.ServiceBus.Messaging.Lease Lease
		{
			get;
			set;
		}

		internal string Offset
		{
			get;
			set;
		}

		internal long SequenceNumber
		{
			get;
			set;
		}

		internal PartitionContext(ICheckpointManager checkpointManager)
		{
			this.checkpointManager = checkpointManager;
			this.checkpointing = new AutoResetEvent(true);
		}

		public Task CheckpointAsync()
		{
			return this.PerformCheckpointAsync(this.SequenceNumber, this.Offset);
		}

		public Task CheckpointAsync(EventData data)
		{
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			if (data.SequenceNumber > this.SequenceNumber)
			{
				throw new ArgumentOutOfRangeException("sequenceNumber");
			}
			return this.PerformCheckpointAsync(data.SequenceNumber, data.Offset);
		}

		private static bool InterlockedExchangeIfGreaterThanOrEqual(ref long location, long newValue)
		{
			long num;
			do
			{
				num = location;
				if (num <= newValue)
				{
					continue;
				}
				return false;
			}
			while (Interlocked.CompareExchange(ref location, newValue, num) != num);
			return true;
		}

		private Task PerformCheckpointAsync(long sequenceNumber, string offset)
		{
			Task task;
			if (PartitionContext.InterlockedExchangeIfGreaterThanOrEqual(ref this.lastCheckpointedSequenceNumber, sequenceNumber))
			{
				try
				{
					this.checkpointing.WaitOne();
					if (!PartitionContext.InterlockedExchangeIfGreaterThanOrEqual(ref this.lastCheckpointedSequenceNumber, sequenceNumber))
					{
						throw new ArgumentOutOfRangeException("offset");
					}
					else
					{
						task = this.checkpointManager.CheckpointAsync(this.Lease, offset, sequenceNumber).Then<object>(() => {
							this.Lease.SequenceNumber = sequenceNumber;
							this.Lease.Offset = offset;
							return TaskHelpers.GetCompletedTask<object>(null);
						});
					}
				}
				finally
				{
					this.checkpointing.Set();
				}
				return task;
			}
			throw new ArgumentOutOfRangeException("offset");
		}
	}
}