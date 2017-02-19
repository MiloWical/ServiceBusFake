using System;
using System.Runtime.Serialization;
using System.Threading;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Name="MessageCountDetails", Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	public sealed class MessageCountDetails
	{
		private long activeMessageCount;

		private long deadLetterMessageCount;

		private long scheduledMessageCount;

		private long transferMessageCount;

		private long transferDeadLetterMessageCount;

		[DataMember(Order=65537)]
		public long ActiveMessageCount
		{
			get
			{
				return this.activeMessageCount;
			}
			private set
			{
				this.activeMessageCount = value;
			}
		}

		[DataMember(Order=65538)]
		public long DeadLetterMessageCount
		{
			get
			{
				return this.deadLetterMessageCount;
			}
			private set
			{
				this.deadLetterMessageCount = value;
			}
		}

		[DataMember(Order=65539)]
		public long ScheduledMessageCount
		{
			get
			{
				return this.scheduledMessageCount;
			}
			private set
			{
				this.scheduledMessageCount = value;
			}
		}

		[DataMember(Order=65541)]
		public long TransferDeadLetterMessageCount
		{
			get
			{
				return this.transferDeadLetterMessageCount;
			}
			private set
			{
				this.transferDeadLetterMessageCount = value;
			}
		}

		[DataMember(Order=65540)]
		public long TransferMessageCount
		{
			get
			{
				return this.transferMessageCount;
			}
			private set
			{
				this.transferMessageCount = value;
			}
		}

		public MessageCountDetails() : this((long)0, (long)0, (long)0, (long)0, (long)0)
		{
		}

		public MessageCountDetails(long activeMessageCount, long deadletterMessageCount, long scheduledMessageCount, long transferMessageCount, long transferDlqMessageCount)
		{
			this.ActiveMessageCount = activeMessageCount;
			this.DeadLetterMessageCount = deadletterMessageCount;
			this.ScheduledMessageCount = scheduledMessageCount;
			this.TransferMessageCount = transferMessageCount;
			this.TransferDeadLetterMessageCount = transferDlqMessageCount;
		}

		private static void AddMessageCount(ref long value, long delta)
		{
			if (delta != (long)0)
			{
				Interlocked.Add(ref value, delta);
			}
		}

		internal void AddMessageCounts(long activeMessageDelta, long scheduledMessageDelta, long deadLetterMessageDelta = 0L, long transferMessageDelta = 0L, long transferDeadLetterMessageDelta = 0L)
		{
			MessageCountDetails.AddMessageCount(ref this.activeMessageCount, activeMessageDelta);
			MessageCountDetails.AddMessageCount(ref this.scheduledMessageCount, scheduledMessageDelta);
			MessageCountDetails.AddMessageCount(ref this.deadLetterMessageCount, deadLetterMessageDelta);
			MessageCountDetails.AddMessageCount(ref this.transferMessageCount, transferMessageDelta);
			MessageCountDetails.AddMessageCount(ref this.transferDeadLetterMessageCount, transferDeadLetterMessageDelta);
		}

		internal void InitializeMessageCounts(long initialActiveMessageCount, long initialScheduledMessageCount, long initialDeadLetterMessageCount, long initialTransferMessageCount = 0L, long initialTransferDeadLetterMessageCount = 0L)
		{
			Interlocked.Exchange(ref this.activeMessageCount, initialActiveMessageCount);
			Interlocked.Exchange(ref this.scheduledMessageCount, initialScheduledMessageCount);
			Interlocked.Exchange(ref this.deadLetterMessageCount, initialDeadLetterMessageCount);
			Interlocked.Exchange(ref this.transferMessageCount, initialTransferMessageCount);
			Interlocked.Exchange(ref this.transferDeadLetterMessageCount, initialTransferDeadLetterMessageCount);
		}
	}
}