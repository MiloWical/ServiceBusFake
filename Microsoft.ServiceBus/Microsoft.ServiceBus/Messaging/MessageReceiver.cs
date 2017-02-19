using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Common.Parallel;
using Microsoft.ServiceBus.Messaging.ScaledEntity;
using Microsoft.ServiceBus.PerformanceCounters;
using Microsoft.ServiceBus.Properties;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Transactions;

namespace Microsoft.ServiceBus.Messaging
{
	public abstract class MessageReceiver : MessageClientEntity, IMessageReceiver, IMessageBrowser
	{
		private readonly OpenOnceManager openOnceManager;

		private readonly TimeSpan operationTimeout;

		private readonly object receivePumpSyncRoot;

		private long lastPeekedSequenceNumber;

		private TimeSpan batchFlushInterval;

		private int prefetchCount;

		private MessageReceivePump receivePump;

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

		internal long? Epoch
		{
			get;
			set;
		}

		internal Microsoft.ServiceBus.Messaging.Filter Filter
		{
			get;
			set;
		}

		internal TrackingContext InstanceTrackingContext
		{
			get;
			set;
		}

		public virtual long LastPeekedSequenceNumber
		{
			get
			{
				return this.lastPeekedSequenceNumber;
			}
			internal set
			{
				if (value < (long)0)
				{
					throw FxTrace.Exception.ArgumentOutOfRange("LastPeekedSequenceNumber", value, SRClient.ArgumentOutOfRange(0, 9223372036854775807L));
				}
				this.lastPeekedSequenceNumber = value;
			}
		}

		internal Microsoft.ServiceBus.Messaging.MessagingFactory MessagingFactory
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

		protected void JustDecompileGenerated_set_Mode(ReceiveMode value)
		{
			this.JustDecompileGenerated_Mode_k__BackingField = value;
		}

		protected internal virtual bool OffsetInclusive
		{
			get;
			set;
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

		public virtual int PrefetchCount
		{
			get
			{
				return this.prefetchCount;
			}
			set
			{
				if (value < 0)
				{
					throw FxTrace.Exception.ArgumentOutOfRange("PrefetchCount", value, SRClient.ArgumentOutOfRange(0, 2147483647));
				}
				this.prefetchCount = value;
			}
		}

		protected internal virtual DateTime? ReceiverStartTime
		{
			get;
			set;
		}

		protected internal virtual string StartOffset
		{
			get;
			set;
		}

		protected internal abstract bool SupportsGetRuntimeEntityDescription
		{
			get;
		}

		internal MessageReceiver(Microsoft.ServiceBus.Messaging.MessagingFactory messagingFactory, Microsoft.ServiceBus.RetryPolicy retryPolicy, ReceiveMode receiveMode, Microsoft.ServiceBus.Messaging.Filter filter)
		{
			this.MessagingFactory = messagingFactory;
			this.operationTimeout = messagingFactory.OperationTimeout;
			this.Mode = receiveMode;
			this.lastPeekedSequenceNumber = Constants.DefaultLastPeekedSequenceNumber;
			base.RetryPolicy = retryPolicy ?? messagingFactory.RetryPolicy.Clone();
			this.openOnceManager = new OpenOnceManager(this);
			this.receivePumpSyncRoot = new object();
			this.Filter = filter;
		}

		public void Abandon(Guid lockToken)
		{
			Guid[] guidArray = new Guid[] { lockToken };
			this.Abandon(null, guidArray, null, this.OperationTimeout);
		}

		public void Abandon(Guid lockToken, IDictionary<string, object> propertiesToModify)
		{
			Guid[] guidArray = new Guid[] { lockToken };
			this.Abandon(null, guidArray, propertiesToModify, this.OperationTimeout);
		}

		internal void Abandon(IEnumerable<Guid> lockTokens)
		{
			this.Abandon(null, lockTokens, null, this.OperationTimeout);
		}

		internal void Abandon(IEnumerable<Guid> lockTokens, TimeSpan timeout)
		{
			this.Abandon(null, lockTokens, null, timeout);
		}

		internal void Abandon(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, IDictionary<string, object> propertiesToModify, TimeSpan timeout)
		{
			base.ThrowIfDisposed();
			TimeoutHelper.ThrowIfNegativeArgument(timeout);
			if (trackingContext == null)
			{
				trackingContext = TrackingContext.GetInstance(Guid.NewGuid(), this.Path);
			}
			if (this.openOnceManager.ShouldOpen)
			{
				this.openOnceManager.Open();
			}
			lockTokens = new List<Guid>(lockTokens);
			this.OnAbandon(trackingContext, lockTokens, propertiesToModify, timeout);
		}

		public Task AbandonAsync(Guid lockToken)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginAbandon(lockToken, c, s), new Action<IAsyncResult>(this.EndAbandon));
		}

		public Task AbandonAsync(Guid lockToken, IDictionary<string, object> propertiesToModify)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginAbandon(lockToken, propertiesToModify, c, s), new Action<IAsyncResult>(this.EndAbandon));
		}

		internal Task AbandonBatchAsync(IEnumerable<Guid> lockTokens)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginAbandon(lockTokens, c, s), new Action<IAsyncResult>(this.EndAbandon));
		}

		public IAsyncResult BeginAbandon(Guid lockToken, AsyncCallback callback, object state)
		{
			Guid[] guidArray = new Guid[] { lockToken };
			return this.BeginAbandon(null, guidArray, null, this.OperationTimeout, callback, state);
		}

		public IAsyncResult BeginAbandon(Guid lockToken, IDictionary<string, object> propertiesToModify, AsyncCallback callback, object state)
		{
			Guid[] guidArray = new Guid[] { lockToken };
			return this.BeginAbandon(null, guidArray, propertiesToModify, this.OperationTimeout, callback, state);
		}

		internal IAsyncResult BeginAbandon(IEnumerable<Guid> lockTokens, AsyncCallback callback, object state)
		{
			return this.BeginAbandon(null, lockTokens, null, this.OperationTimeout, callback, state);
		}

		internal IAsyncResult BeginAbandon(IEnumerable<Guid> lockTokens, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.BeginAbandon(null, lockTokens, null, timeout, callback, state);
		}

		internal IAsyncResult BeginAbandon(IEnumerable<Guid> lockTokens, IDictionary<string, object> propertiesToModify, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.BeginAbandon(null, lockTokens, propertiesToModify, timeout, callback, state);
		}

		internal IAsyncResult BeginAbandon(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, IDictionary<string, object> propertiesToModify, TimeSpan timeout, AsyncCallback callback, object state)
		{
			TrackingContext instance = trackingContext;
			IEnumerable<Guid> guids = lockTokens;
			base.ThrowIfDisposed();
			TimeoutHelper.ThrowIfNegativeArgument(timeout);
			if (instance == null)
			{
				instance = TrackingContext.GetInstance(Guid.NewGuid(), this.Path);
			}
			guids = new List<Guid>(guids);
			if (this.openOnceManager.ShouldOpen)
			{
				return this.openOnceManager.Begin(callback, state, (AsyncCallback c, object s) => {
					MessageReceiver.RetryMessagesOperationAsyncResult retryMessagesOperationAsyncResult = new MessageReceiver.RetryMessagesOperationAsyncResult(this, instance, DispositionStatus.Abandoned, guids, propertiesToModify, null, null, false, timeout, c, s);
					retryMessagesOperationAsyncResult.Start();
					return retryMessagesOperationAsyncResult;
				}, new Action<IAsyncResult>(MessageReceiver.RetryMessagesOperationAsyncResult.End));
			}
			MessageReceiver.RetryMessagesOperationAsyncResult retryMessagesOperationAsyncResult1 = new MessageReceiver.RetryMessagesOperationAsyncResult(this, instance, DispositionStatus.Abandoned, guids, propertiesToModify, null, null, false, timeout, callback, state);
			retryMessagesOperationAsyncResult1.Start();
			return retryMessagesOperationAsyncResult1;
		}

		internal IAsyncResult BeginCheckpoint(TrackingContext trackingContext, ArraySegment<byte> deliveryTag, AsyncCallback callback, object state)
		{
			TrackingContext instance = trackingContext;
			base.ThrowIfDisposed();
			if (instance == null)
			{
				instance = TrackingContext.GetInstance(Guid.NewGuid(), this.Path);
			}
			if (this.openOnceManager.ShouldOpen)
			{
				return this.openOnceManager.Begin(callback, state, (AsyncCallback c, object s) => (new MessageReceiver.RetryMessagesOperationAsyncResult(this, instance, DispositionStatus.Completed, new ArraySegment<byte>[] { deliveryTag }, null, null, null, false, this.OperationTimeout, c, s)).Start(), new Action<IAsyncResult>(MessageReceiver.RetryMessagesOperationAsyncResult.End));
			}
			TrackingContext trackingContext1 = instance;
			ArraySegment<byte>[] arraySegmentArrays = new ArraySegment<byte>[] { deliveryTag };
			return (new MessageReceiver.RetryMessagesOperationAsyncResult(this, trackingContext1, DispositionStatus.Completed, arraySegmentArrays, null, null, null, false, this.OperationTimeout, callback, state)).Start();
		}

		public IAsyncResult BeginComplete(Guid lockToken, AsyncCallback callback, object state)
		{
			Guid[] guidArray = new Guid[] { lockToken };
			return this.BeginComplete(null, guidArray, this.OperationTimeout, callback, state);
		}

		internal IAsyncResult BeginComplete(IEnumerable<Guid> lockTokens, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.BeginComplete(null, lockTokens, timeout, callback, state);
		}

		internal IAsyncResult BeginComplete(IEnumerable<Guid> lockTokens, AsyncCallback callback, object state)
		{
			return this.BeginComplete(null, lockTokens, this.OperationTimeout, callback, state);
		}

		internal IAsyncResult BeginComplete(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, TimeSpan timeout, AsyncCallback callback, object state)
		{
			TrackingContext instance = trackingContext;
			base.ThrowIfDisposed();
			TimeoutHelper.ThrowIfNegativeArgument(timeout);
			if (lockTokens == null)
			{
				throw FxTrace.Exception.ArgumentNullOrEmpty("lockTokens");
			}
			List<Guid> guids = new List<Guid>(lockTokens);
			if (guids.Count == 0)
			{
				throw FxTrace.Exception.ArgumentNullOrEmpty("lockTokens");
			}
			if (instance == null)
			{
				instance = TrackingContext.GetInstance(Guid.NewGuid(), this.Path);
			}
			if (!this.openOnceManager.ShouldOpen)
			{
				MessageReceiver.RetryMessagesOperationAsyncResult retryMessagesOperationAsyncResult1 = new MessageReceiver.RetryMessagesOperationAsyncResult(this, instance, DispositionStatus.Completed, guids, null, null, null, false, timeout, callback, state);
				retryMessagesOperationAsyncResult1.Start();
				return retryMessagesOperationAsyncResult1;
			}
			return this.openOnceManager.Begin(callback, state, (AsyncCallback c, object s) => {
				MessageReceiver.RetryMessagesOperationAsyncResult retryMessagesOperationAsyncResult = new MessageReceiver.RetryMessagesOperationAsyncResult(this, instance, DispositionStatus.Completed, guids, null, null, null, false, timeout, c, s);
				retryMessagesOperationAsyncResult.Start();
				return retryMessagesOperationAsyncResult;
			}, new Action<IAsyncResult>(MessageReceiver.RetryMessagesOperationAsyncResult.End));
		}

		public IAsyncResult BeginCompleteBatch(IEnumerable<Guid> lockTokens, AsyncCallback callback, object state)
		{
			return this.BeginComplete(null, lockTokens, this.OperationTimeout, callback, state);
		}

		public IAsyncResult BeginDeadLetter(Guid lockToken, AsyncCallback callback, object state)
		{
			Guid[] guidArray = new Guid[] { lockToken };
			return this.BeginDeadLetter(guidArray, null, null, null, this.OperationTimeout, callback, state);
		}

		public IAsyncResult BeginDeadLetter(Guid lockToken, IDictionary<string, object> propertiesToModify, AsyncCallback callback, object state)
		{
			Guid[] guidArray = new Guid[] { lockToken };
			return this.BeginDeadLetter(guidArray, propertiesToModify, null, null, this.OperationTimeout, callback, state);
		}

		public IAsyncResult BeginDeadLetter(Guid lockToken, string deadLetterReason, string deadLetterErrorDescription, AsyncCallback callback, object state)
		{
			Guid[] guidArray = new Guid[] { lockToken };
			return this.BeginDeadLetter(null, guidArray, null, deadLetterReason, deadLetterErrorDescription, this.OperationTimeout, callback, state);
		}

		internal IAsyncResult BeginDeadLetter(IEnumerable<Guid> lockTokens, IDictionary<string, object> propertiesToModify, string deadLetterReason, string deadLetterErrorDescription, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.BeginDeadLetter(null, lockTokens, propertiesToModify, deadLetterReason, deadLetterErrorDescription, timeout, callback, state);
		}

		internal IAsyncResult BeginDeadLetter(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, IDictionary<string, object> propertiesToModify, string deadLetterReason, string deadLetterErrorDescription, TimeSpan timeout, AsyncCallback callback, object state)
		{
			TrackingContext instance = trackingContext;
			base.ThrowIfDisposed();
			TimeoutHelper.ThrowIfNegativeArgument(timeout);
			if (instance == null)
			{
				instance = TrackingContext.GetInstance(Guid.NewGuid(), this.Path);
			}
			if (this.openOnceManager.ShouldOpen)
			{
				return this.openOnceManager.Begin(callback, state, (AsyncCallback c, object s) => {
					MessageReceiver.RetryMessagesOperationAsyncResult retryMessagesOperationAsyncResult = new MessageReceiver.RetryMessagesOperationAsyncResult(this, instance, DispositionStatus.Suspended, lockTokens, propertiesToModify, deadLetterReason, deadLetterErrorDescription, false, timeout, c, s);
					retryMessagesOperationAsyncResult.Start();
					return retryMessagesOperationAsyncResult;
				}, new Action<IAsyncResult>(MessageReceiver.RetryMessagesOperationAsyncResult.End));
			}
			MessageReceiver.RetryMessagesOperationAsyncResult retryMessagesOperationAsyncResult1 = new MessageReceiver.RetryMessagesOperationAsyncResult(this, instance, DispositionStatus.Suspended, lockTokens, propertiesToModify, deadLetterReason, deadLetterErrorDescription, false, timeout, callback, state);
			retryMessagesOperationAsyncResult1.Start();
			return retryMessagesOperationAsyncResult1;
		}

		public IAsyncResult BeginDefer(Guid lockToken, AsyncCallback callback, object state)
		{
			Guid[] guidArray = new Guid[] { lockToken };
			return this.BeginDefer(null, guidArray, null, this.OperationTimeout, callback, state);
		}

		public IAsyncResult BeginDefer(Guid lockToken, IDictionary<string, object> propertiesToModify, AsyncCallback callback, object state)
		{
			Guid[] guidArray = new Guid[] { lockToken };
			return this.BeginDefer(null, guidArray, propertiesToModify, this.OperationTimeout, callback, state);
		}

		internal IAsyncResult BeginDefer(IEnumerable<Guid> lockTokens, IDictionary<string, object> propertiesToModify, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.BeginDefer(null, lockTokens, propertiesToModify, timeout, callback, state);
		}

		internal IAsyncResult BeginDefer(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, IDictionary<string, object> propertiesToModify, TimeSpan timeout, AsyncCallback callback, object state)
		{
			TrackingContext instance = trackingContext;
			base.ThrowIfDisposed();
			TimeoutHelper.ThrowIfNegativeArgument(timeout);
			if (instance == null)
			{
				instance = TrackingContext.GetInstance(Guid.NewGuid(), this.Path);
			}
			if (this.openOnceManager.ShouldOpen)
			{
				return this.openOnceManager.Begin(callback, state, (AsyncCallback c, object s) => {
					MessageReceiver.RetryMessagesOperationAsyncResult retryMessagesOperationAsyncResult = new MessageReceiver.RetryMessagesOperationAsyncResult(this, instance, DispositionStatus.Defered, lockTokens, propertiesToModify, null, null, false, timeout, c, s);
					retryMessagesOperationAsyncResult.Start();
					return retryMessagesOperationAsyncResult;
				}, new Action<IAsyncResult>(MessageReceiver.RetryMessagesOperationAsyncResult.End));
			}
			MessageReceiver.RetryMessagesOperationAsyncResult retryMessagesOperationAsyncResult1 = new MessageReceiver.RetryMessagesOperationAsyncResult(this, instance, DispositionStatus.Defered, lockTokens, propertiesToModify, null, null, false, timeout, callback, state);
			retryMessagesOperationAsyncResult1.Start();
			return retryMessagesOperationAsyncResult1;
		}

		internal IAsyncResult BeginGetRuntimeEntityDescriptionAsyncResult(TrackingContext trackingContext, TimeSpan timeout, AsyncCallback callback, object state)
		{
			TrackingContext instance = trackingContext;
			base.ThrowIfDisposed();
			if (instance == null)
			{
				instance = TrackingContext.GetInstance(Guid.NewGuid(), this.Path);
			}
			if (!this.openOnceManager.ShouldOpen)
			{
				return this.OnBeginGetRuntimeEntityDescriptionAsyncResult(instance, timeout, callback, state);
			}
			OpenOnceManager openOnceManager = this.openOnceManager;
			AsyncCallback asyncCallback = callback;
			object obj = state;
			MessageReceiver messageReceiver = this;
			return openOnceManager.Begin<Microsoft.ServiceBus.Messaging.RuntimeEntityDescription>(asyncCallback, obj, (AsyncCallback c, object s) => this.OnBeginGetRuntimeEntityDescriptionAsyncResult(instance, timeout, c, s), new Func<IAsyncResult, Microsoft.ServiceBus.Messaging.RuntimeEntityDescription>(messageReceiver.OnEndGetRuntimeEntityDescriptionAsyncResult));
		}

		public IAsyncResult BeginPeek(AsyncCallback callback, object state)
		{
			return this.BeginPeek(this.LastPeekedSequenceNumber + (long)1, callback, state);
		}

		public IAsyncResult BeginPeek(long fromSequenceNumber, AsyncCallback callback, object state)
		{
			return this.BeginPeek(fromSequenceNumber, 1, this.OperationTimeout, callback, state);
		}

		internal IAsyncResult BeginPeek(long fromSequenceNumber, int messageCount, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.BeginPeekBatch(null, fromSequenceNumber, messageCount, timeout, callback, state);
		}

		public IAsyncResult BeginPeekBatch(int messageCount, AsyncCallback callback, object state)
		{
			return this.BeginPeekBatch(this.LastPeekedSequenceNumber + (long)1, messageCount, callback, state);
		}

		public IAsyncResult BeginPeekBatch(long fromSequenceNumber, int messageCount, AsyncCallback callback, object state)
		{
			return this.BeginPeekBatch(null, fromSequenceNumber, messageCount, this.OperationTimeout, callback, state);
		}

		internal IAsyncResult BeginPeekBatch(TrackingContext trackingContext, long fromSequenceNumber, int messageCount, TimeSpan timeout, AsyncCallback callback, object state)
		{
			TrackingContext instance = trackingContext;
			base.ThrowIfDisposed();
			TimeoutHelper.ThrowIfNegativeArgument(timeout);
			if (fromSequenceNumber < (long)0)
			{
				throw FxTrace.Exception.ArgumentOutOfRange("fromSequenceNumber", fromSequenceNumber, Resources.ValueMustBeNonNegative);
			}
			if (instance == null)
			{
				instance = TrackingContext.GetInstance(Guid.NewGuid(), this.Path);
			}
			MessageReceiver.TracePeek(EventTraceActivity.CreateFromThread(), instance);
			if (!this.openOnceManager.ShouldOpen)
			{
				MessageReceiver.RetryReceiveAsyncResult retryReceiveAsyncResult1 = new MessageReceiver.RetryReceiveAsyncResult(this, instance, messageCount, fromSequenceNumber, null, timeout, callback, state);
				retryReceiveAsyncResult1.Start();
				return retryReceiveAsyncResult1;
			}
			return this.openOnceManager.Begin<IEnumerable<BrokeredMessage>>(callback, state, (AsyncCallback c, object s) => {
				MessageReceiver.RetryReceiveAsyncResult retryReceiveAsyncResult = new MessageReceiver.RetryReceiveAsyncResult(this, instance, messageCount, fromSequenceNumber, null, timeout, c, s);
				retryReceiveAsyncResult.Start();
				return retryReceiveAsyncResult;
			}, new Func<IAsyncResult, IEnumerable<BrokeredMessage>>(MessageReceiver.RetryReceiveAsyncResult.End));
		}

		public IAsyncResult BeginReceive(AsyncCallback callback, object state)
		{
			return this.BeginTryReceive(this.OperationTimeout, callback, state);
		}

		public IAsyncResult BeginReceive(TimeSpan serverWaitTime, AsyncCallback callback, object state)
		{
			return this.BeginTryReceive(null, 1, serverWaitTime, callback, state);
		}

		internal IAsyncResult BeginReceive(TrackingContext trackingContext, TimeSpan serverWaitTime, AsyncCallback callback, object state)
		{
			return this.BeginTryReceive(trackingContext, 1, serverWaitTime, callback, state);
		}

		public IAsyncResult BeginReceive(long sequenceNumber, AsyncCallback callback, object state)
		{
			long[] numArray = new long[] { sequenceNumber };
			return this.BeginTryReceive(numArray, this.OperationTimeout, callback, state);
		}

		internal IAsyncResult BeginReceive(TrackingContext trackingContext, IEnumerable<long> sequenceNumbers, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.BeginTryReceive(trackingContext, sequenceNumbers, timeout, callback, state);
		}

		public IAsyncResult BeginReceiveBatch(int messageCount, AsyncCallback callback, object state)
		{
			return this.BeginTryReceive(messageCount, this.OperationTimeout, callback, state);
		}

		public IAsyncResult BeginReceiveBatch(int messageCount, TimeSpan serverWaitTime, AsyncCallback callback, object state)
		{
			return this.BeginTryReceive(null, messageCount, serverWaitTime, callback, state);
		}

		public IAsyncResult BeginReceiveBatch(IEnumerable<long> sequenceNumbers, AsyncCallback callback, object state)
		{
			return this.BeginTryReceive(sequenceNumbers, this.OperationTimeout, callback, state);
		}

		internal IAsyncResult BeginRenewMessageLocks(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, TimeSpan timeout, AsyncCallback callback, object state)
		{
			TrackingContext instance = trackingContext;
			base.ThrowIfDisposed();
			TimeoutHelper.ThrowIfNegativeArgument(timeout);
			if (instance == null)
			{
				instance = TrackingContext.GetInstance(Guid.NewGuid(), this.Path);
			}
			if (!this.openOnceManager.ShouldOpen)
			{
				MessageReceiver.RetryMessagesOperationAsyncResult retryMessagesOperationAsyncResult1 = new MessageReceiver.RetryMessagesOperationAsyncResult(this, instance, DispositionStatus.Renewed, lockTokens, null, null, null, false, timeout, callback, state);
				retryMessagesOperationAsyncResult1.Start();
				return retryMessagesOperationAsyncResult1;
			}
			return this.openOnceManager.Begin<IEnumerable<DateTime>>(callback, state, (AsyncCallback c, object s) => {
				MessageReceiver.RetryMessagesOperationAsyncResult retryMessagesOperationAsyncResult = new MessageReceiver.RetryMessagesOperationAsyncResult(this, instance, DispositionStatus.Renewed, lockTokens, null, null, null, false, timeout, c, s);
				retryMessagesOperationAsyncResult.Start();
				return retryMessagesOperationAsyncResult;
			}, new Func<IAsyncResult, IEnumerable<DateTime>>(MessageReceiver.RetryMessagesOperationAsyncResult.RenewLockEnd));
		}

		internal IAsyncResult BeginTryReceive(TimeSpan serverWaitTime, AsyncCallback callback, object state)
		{
			return this.BeginTryReceive(1, serverWaitTime, callback, state);
		}

		internal IAsyncResult BeginTryReceive(int messageCount, AsyncCallback callback, object state)
		{
			return this.BeginTryReceive(null, messageCount, this.OperationTimeout, callback, state);
		}

		internal IAsyncResult BeginTryReceive(int messageCount, TimeSpan serverWaitTime, AsyncCallback callback, object state)
		{
			return this.BeginTryReceive(null, messageCount, serverWaitTime, callback, state);
		}

		internal IAsyncResult BeginTryReceive(TrackingContext trackingContext, int messageCount, TimeSpan serverWaitTime, AsyncCallback callback, object state)
		{
			TrackingContext instance = trackingContext;
			base.ThrowIfDisposed();
			TimeoutHelper.ThrowIfNegativeArgument(serverWaitTime);
			if (messageCount < 1)
			{
				throw FxTrace.Exception.AsError(new ArgumentOutOfRangeException("messageCount"), null);
			}
			if (instance == null)
			{
				instance = TrackingContext.GetInstance(Guid.NewGuid(), this.Path);
			}
			MessageReceiver.TraceReceive(EventTraceActivity.CreateFromThread(), instance);
			if (!this.openOnceManager.ShouldOpen)
			{
				MessageReceiver.RetryReceiveAsyncResult retryReceiveAsyncResult1 = new MessageReceiver.RetryReceiveAsyncResult(this, instance, messageCount, (long)-1, null, serverWaitTime, callback, state);
				retryReceiveAsyncResult1.Start();
				return retryReceiveAsyncResult1;
			}
			return this.openOnceManager.Begin<IEnumerable<BrokeredMessage>>(callback, state, (AsyncCallback c, object s) => {
				MessageReceiver.RetryReceiveAsyncResult retryReceiveAsyncResult = new MessageReceiver.RetryReceiveAsyncResult(this, instance, messageCount, (long)-1, null, serverWaitTime, c, s);
				retryReceiveAsyncResult.Start();
				return retryReceiveAsyncResult;
			}, new OpenOnceManager.EndOperation<IEnumerable<BrokeredMessage>>(MessageReceiver.RetryReceiveAsyncResult.TryReceiveEnd));
		}

		internal IAsyncResult BeginTryReceive(IEnumerable<long> sequenceNumbers, AsyncCallback callback, object state)
		{
			return this.BeginTryReceive(null, sequenceNumbers, this.OperationTimeout, callback, state);
		}

		internal IAsyncResult BeginTryReceive(IEnumerable<long> sequenceNumbers, TimeSpan serverWaitTime, AsyncCallback callback, object state)
		{
			return this.BeginTryReceive(null, sequenceNumbers, serverWaitTime, callback, state);
		}

		internal IAsyncResult BeginTryReceive(TrackingContext trackingContext, IEnumerable<long> sequenceNumbers, TimeSpan serverWaitTime, AsyncCallback callback, object state)
		{
			TrackingContext instance = trackingContext;
			base.ThrowIfDisposed();
			List<long> nums = sequenceNumbers as List<long> ?? sequenceNumbers.ToList<long>();
			MessagingUtilities.CheckValidSequenceNumbers(nums);
			TimeoutHelper.ThrowIfNegativeArgument(serverWaitTime);
			if (instance == null)
			{
				instance = TrackingContext.GetInstance(Guid.NewGuid(), this.Path);
			}
			MessageReceiver.TraceReceive(EventTraceActivity.CreateFromThread(), instance);
			if (!this.openOnceManager.ShouldOpen)
			{
				MessageReceiver.RetryReceiveAsyncResult retryReceiveAsyncResult1 = new MessageReceiver.RetryReceiveAsyncResult(this, instance, 0, (long)-1, nums, serverWaitTime, callback, state);
				retryReceiveAsyncResult1.Start();
				return retryReceiveAsyncResult1;
			}
			return this.openOnceManager.Begin<IEnumerable<BrokeredMessage>>(callback, state, (AsyncCallback c, object s) => {
				MessageReceiver.RetryReceiveAsyncResult retryReceiveAsyncResult = new MessageReceiver.RetryReceiveAsyncResult(this, instance, 0, (long)-1, nums, serverWaitTime, c, s);
				retryReceiveAsyncResult.Start();
				return retryReceiveAsyncResult;
			}, new OpenOnceManager.EndOperation<IEnumerable<BrokeredMessage>>(MessageReceiver.RetryReceiveAsyncResult.TryReceiveEnd));
		}

		internal IAsyncResult BeginTryReceive2(TrackingContext trackingContext, int messageCount, TimeSpan serverWaitTime, AsyncCallback callback, object state)
		{
			TrackingContext instance = trackingContext;
			base.ThrowIfDisposed();
			TimeoutHelper.ThrowIfNegativeArgument(serverWaitTime);
			if (messageCount < 1)
			{
				throw FxTrace.Exception.AsError(new ArgumentOutOfRangeException("messageCount"), null);
			}
			if (instance == null)
			{
				instance = TrackingContext.GetInstance(Guid.NewGuid(), this.Path);
			}
			MessageReceiver.TraceReceive(EventTraceActivity.CreateFromThread(), instance);
			if (!this.openOnceManager.ShouldOpen)
			{
				return this.OnBeginTryReceive2(instance, messageCount, serverWaitTime, callback, state);
			}
			OpenOnceManager openOnceManager = this.openOnceManager;
			AsyncCallback asyncCallback = callback;
			object obj = state;
			MessageReceiver messageReceiver = this;
			return openOnceManager.Begin<IEnumerable<BrokeredMessage>>(asyncCallback, obj, (AsyncCallback c, object s) => this.OnBeginTryReceive2(instance, messageCount, serverWaitTime, c, s), new OpenOnceManager.EndOperation<IEnumerable<BrokeredMessage>>(messageReceiver.OnEndTryReceive2));
		}

		internal IAsyncResult BeginTryReceiveEventData(TrackingContext trackingContext, int messageCount, TimeSpan serverWaitTime, AsyncCallback callback, object state)
		{
			TrackingContext instance = trackingContext;
			base.ThrowIfDisposed();
			TimeoutHelper.ThrowIfNegativeArgument(serverWaitTime);
			if (messageCount < 1)
			{
				throw FxTrace.Exception.AsError(new ArgumentOutOfRangeException("messageCount"), null);
			}
			if (instance == null)
			{
				instance = TrackingContext.GetInstance(Guid.NewGuid(), this.Path);
			}
			MessageReceiver.TraceReceive(EventTraceActivity.CreateFromThread(), instance);
			if (!this.openOnceManager.ShouldOpen)
			{
				return (new MessageReceiver.RetryReceiveEventDataAsyncResult(this, instance, messageCount, serverWaitTime, callback, state)).Start();
			}
			return this.openOnceManager.Begin<IEnumerable<EventData>>(callback, state, (AsyncCallback c, object s) => (new MessageReceiver.RetryReceiveEventDataAsyncResult(this, instance, messageCount, serverWaitTime, c, s)).Start(), new OpenOnceManager.EndOperation<IEnumerable<EventData>>(MessageReceiver.RetryReceiveEventDataAsyncResult.TryReceiveEnd));
		}

		public void Complete(Guid lockToken)
		{
			Guid[] guidArray = new Guid[] { lockToken };
			this.Complete(null, guidArray, this.OperationTimeout);
		}

		internal void Complete(IEnumerable<Guid> lockTokens, TimeSpan timeout)
		{
			this.Complete(null, lockTokens, timeout);
		}

		internal void Complete(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, TimeSpan timeout)
		{
			base.ThrowIfDisposed();
			TimeoutHelper.ThrowIfNegativeArgument(timeout);
			if (lockTokens == null)
			{
				throw FxTrace.Exception.ArgumentNullOrEmpty("lockTokens");
			}
			List<Guid> guids = new List<Guid>(lockTokens);
			if (guids.Count == 0)
			{
				throw FxTrace.Exception.ArgumentNullOrEmpty("lockTokens");
			}
			if (trackingContext == null)
			{
				trackingContext = TrackingContext.GetInstance(Guid.NewGuid(), this.Path);
			}
			if (this.openOnceManager.ShouldOpen)
			{
				this.openOnceManager.Open();
			}
			this.OnComplete(trackingContext, guids, timeout);
		}

		public Task CompleteAsync(Guid lockToken)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginComplete(lockToken, c, s), new Action<IAsyncResult>(this.EndComplete));
		}

		public void CompleteBatch(IEnumerable<Guid> lockTokens)
		{
			this.Complete(null, lockTokens, this.OperationTimeout);
		}

		public Task CompleteBatchAsync(IEnumerable<Guid> lockTokens)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginCompleteBatch(lockTokens, c, s), new Action<IAsyncResult>(this.EndCompleteBatch));
		}

		public void DeadLetter(Guid lockToken, IDictionary<string, object> propertiesToModify)
		{
			Guid[] guidArray = new Guid[] { lockToken };
			this.DeadLetter(null, guidArray, propertiesToModify, null, null, this.OperationTimeout);
		}

		public void DeadLetter(Guid lockToken, string deadLetterReason, string deadLetterErrorDescription)
		{
			Guid[] guidArray = new Guid[] { lockToken };
			this.DeadLetter(guidArray, deadLetterReason, deadLetterErrorDescription, this.OperationTimeout);
		}

		public void DeadLetter(Guid lockToken)
		{
			Guid[] guidArray = new Guid[] { lockToken };
			this.DeadLetter(guidArray, null, null, this.OperationTimeout);
		}

		internal void DeadLetter(IEnumerable<Guid> lockTokens, string deadLetterReason, string deadLetterErrorDescription, TimeSpan timeout)
		{
			this.DeadLetter(null, lockTokens, null, deadLetterReason, deadLetterErrorDescription, timeout);
		}

		internal void DeadLetter(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, IDictionary<string, object> propertiesToModify, string deadLetterReason, string deadLetterErrorDescription, TimeSpan timeout)
		{
			base.ThrowIfDisposed();
			TimeoutHelper.ThrowIfNegativeArgument(timeout);
			if (trackingContext == null)
			{
				trackingContext = TrackingContext.GetInstance(Guid.NewGuid(), this.Path);
			}
			if (this.openOnceManager.ShouldOpen)
			{
				this.openOnceManager.Open();
			}
			this.OnDeadLetter(trackingContext, lockTokens, propertiesToModify, deadLetterReason, deadLetterErrorDescription, timeout);
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
			Guid[] guidArray = new Guid[] { lockToken };
			this.Defer(null, guidArray, null, this.OperationTimeout);
		}

		public void Defer(Guid lockToken, IDictionary<string, object> propertiesToModify)
		{
			Guid[] guidArray = new Guid[] { lockToken };
			this.Defer(null, guidArray, propertiesToModify, this.OperationTimeout);
		}

		internal void Defer(IEnumerable<Guid> lockTokens, TimeSpan timeout)
		{
			this.Defer(null, lockTokens, null, timeout);
		}

		internal void Defer(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, IDictionary<string, object> propertiesToModify, TimeSpan timeout)
		{
			base.ThrowIfDisposed();
			TimeoutHelper.ThrowIfNegativeArgument(timeout);
			if (trackingContext == null)
			{
				trackingContext = TrackingContext.GetInstance(Guid.NewGuid(), this.Path);
			}
			if (this.openOnceManager.ShouldOpen)
			{
				this.openOnceManager.Open();
			}
			this.OnDefer(trackingContext, lockTokens, propertiesToModify, timeout);
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
			if (OpenOnceManager.ShouldEnd(result))
			{
				OpenOnceManager.End(result);
				return;
			}
			MessageReceiver.RetryMessagesOperationAsyncResult.End(result);
		}

		internal void EndCheckpoint(IAsyncResult result)
		{
			if (OpenOnceManager.ShouldEnd(result))
			{
				OpenOnceManager.End(result);
				return;
			}
			MessageReceiver.RetryMessagesOperationAsyncResult.End(result);
		}

		public void EndComplete(IAsyncResult result)
		{
			if (OpenOnceManager.ShouldEnd(result))
			{
				OpenOnceManager.End(result);
				return;
			}
			MessageReceiver.RetryMessagesOperationAsyncResult.End(result);
		}

		public void EndCompleteBatch(IAsyncResult result)
		{
			if (OpenOnceManager.ShouldEnd(result))
			{
				OpenOnceManager.End(result);
				return;
			}
			MessageReceiver.RetryMessagesOperationAsyncResult.End(result);
		}

		public void EndDeadLetter(IAsyncResult result)
		{
			if (OpenOnceManager.ShouldEnd(result))
			{
				OpenOnceManager.End(result);
				return;
			}
			MessageReceiver.RetryMessagesOperationAsyncResult.End(result);
		}

		public void EndDefer(IAsyncResult result)
		{
			if (OpenOnceManager.ShouldEnd(result))
			{
				OpenOnceManager.End(result);
				return;
			}
			MessageReceiver.RetryMessagesOperationAsyncResult.End(result);
		}

		internal Microsoft.ServiceBus.Messaging.RuntimeEntityDescription EndGetRuntimeEntityDescriptionAsyncResult(IAsyncResult result)
		{
			Microsoft.ServiceBus.Messaging.RuntimeEntityDescription runtimeEntityDescription;
			runtimeEntityDescription = (!OpenOnceManager.ShouldEnd<Microsoft.ServiceBus.Messaging.RuntimeEntityDescription>(result) ? this.OnEndGetRuntimeEntityDescriptionAsyncResult(result) : OpenOnceManager.End<Microsoft.ServiceBus.Messaging.RuntimeEntityDescription>(result));
			return runtimeEntityDescription;
		}

		public BrokeredMessage EndPeek(IAsyncResult result)
		{
			return MessageReceiver.GetTopMessage(this.EndPeekBatch(result));
		}

		public IEnumerable<BrokeredMessage> EndPeekBatch(IAsyncResult result)
		{
			IEnumerable<BrokeredMessage> brokeredMessages = null;
			brokeredMessages = (!OpenOnceManager.ShouldEnd<IEnumerable<BrokeredMessage>>(result) ? MessageReceiver.RetryReceiveAsyncResult.End(result) : OpenOnceManager.End<IEnumerable<BrokeredMessage>>(result));
			return brokeredMessages;
		}

		public BrokeredMessage EndReceive(IAsyncResult result)
		{
			IEnumerable<BrokeredMessage> brokeredMessages;
			if (!this.EndTryReceive(result, out brokeredMessages))
			{
				return null;
			}
			return MessageReceiver.GetTopMessage(brokeredMessages);
		}

		public IEnumerable<BrokeredMessage> EndReceiveBatch(IAsyncResult result)
		{
			IEnumerable<BrokeredMessage> brokeredMessages;
			this.EndTryReceive(result, out brokeredMessages);
			return brokeredMessages;
		}

		internal IEnumerable<DateTime> EndRenewMessageLocks(IAsyncResult result)
		{
			return (OpenOnceManager.ShouldEnd<IEnumerable<DateTime>>(result) ? OpenOnceManager.End<IEnumerable<DateTime>>(result) : MessageReceiver.RetryMessagesOperationAsyncResult.RenewLockEnd(result));
		}

		internal bool EndTryReceive(IAsyncResult result, out BrokeredMessage message)
		{
			IEnumerable<BrokeredMessage> brokeredMessages;
			message = null;
			bool flag = this.EndTryReceive(result, out brokeredMessages);
			if (flag)
			{
				message = MessageReceiver.GetTopMessage(brokeredMessages);
			}
			return flag;
		}

		internal bool EndTryReceive(IAsyncResult result, out IEnumerable<BrokeredMessage> messages)
		{
			bool flag;
			flag = (!OpenOnceManager.ShouldEnd<IEnumerable<BrokeredMessage>>(result) ? MessageReceiver.RetryReceiveAsyncResult.TryReceiveEnd(result, out messages) : OpenOnceManager.End<IEnumerable<BrokeredMessage>>(result, out messages));
			this.SetReceiveContext(messages);
			return flag;
		}

		internal bool EndTryReceive2(IAsyncResult result, out IEnumerable<BrokeredMessage> messages)
		{
			bool flag;
			flag = (!OpenOnceManager.ShouldEnd<IEnumerable<BrokeredMessage>>(result) ? this.OnEndTryReceive2(result, out messages) : OpenOnceManager.End<IEnumerable<BrokeredMessage>>(result, out messages));
			this.SetReceiveContext(messages);
			return flag;
		}

		internal bool EndTryReceiveEventData(IAsyncResult result, out IEnumerable<EventData> messages)
		{
			bool flag;
			flag = (!OpenOnceManager.ShouldEnd<IEnumerable<EventData>>(result) ? MessageReceiver.RetryReceiveEventDataAsyncResult.TryReceiveEnd(result, out messages) : OpenOnceManager.End<IEnumerable<EventData>>(result, out messages));
			EventData eventDatum = messages.LastOrDefault<EventData>();
			if (flag && eventDatum != null)
			{
				this.StartOffset = eventDatum.Offset;
				this.ReceiverStartTime = new DateTime?(eventDatum.EnqueuedTimeUtc);
			}
			return flag;
		}

		protected static Guid GetLockToken(BrokeredMessage message)
		{
			return message.LockToken;
		}

		protected static IEnumerable<Guid> GetLockTokens(IEnumerable<BrokeredMessage> messages)
		{
			List<Guid> guids = new List<Guid>();
			foreach (BrokeredMessage message in messages)
			{
				guids.Add(message.LockToken);
			}
			return guids;
		}

		private static BrokeredMessage GetTopMessage(IEnumerable<BrokeredMessage> messages)
		{
			BrokeredMessage current;
			using (IEnumerator<BrokeredMessage> enumerator = messages.GetEnumerator())
			{
				if (!enumerator.MoveNext())
				{
					return null;
				}
				else
				{
					current = enumerator.Current;
				}
			}
			return current;
		}

		protected virtual void OnAbandon(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, IDictionary<string, object> propertiesToModify, TimeSpan timeout)
		{
			MessageReceiver.RetryMessagesOperationAsyncResult retryMessagesOperationAsyncResult = new MessageReceiver.RetryMessagesOperationAsyncResult(this, trackingContext, DispositionStatus.Abandoned, lockTokens, propertiesToModify, null, null, true, timeout, null, null);
			retryMessagesOperationAsyncResult.RunSynchronously();
		}

		protected abstract IAsyncResult OnBeginAbandon(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, IDictionary<string, object> propertiesToModify, bool fromSync, TimeSpan timeout, AsyncCallback callback, object state);

		protected abstract IAsyncResult OnBeginComplete(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, bool fromSync, TimeSpan timeout, AsyncCallback callback, object state);

		protected abstract IAsyncResult OnBeginComplete(TrackingContext trackingContext, IEnumerable<ArraySegment<byte>> deliveryTags, bool fromSync, TimeSpan timeout, AsyncCallback callback, object state);

		protected abstract IAsyncResult OnBeginDeadLetter(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, IDictionary<string, object> propertiesToModify, string deadLetterReason, string deadLetterErrorDescription, bool fromSync, TimeSpan timeout, AsyncCallback callback, object state);

		protected abstract IAsyncResult OnBeginDefer(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, IDictionary<string, object> propertiesToModify, bool fromSync, TimeSpan timeout, AsyncCallback callback, object state);

		internal abstract IAsyncResult OnBeginGetRuntimeEntityDescriptionAsyncResult(TrackingContext trackingContext, TimeSpan timeout, AsyncCallback callback, object state);

		protected abstract IAsyncResult OnBeginPeek(TrackingContext trackingContext, long fromSequenceNumber, int messageCount, TimeSpan timeout, AsyncCallback callback, object state);

		protected abstract IAsyncResult OnBeginRenewMessageLocks(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, bool fromSync, TimeSpan timeout, AsyncCallback callback, object state);

		protected abstract IAsyncResult OnBeginTryReceive(TrackingContext trackingContext, int messageCount, TimeSpan serverWaitTime, AsyncCallback callback, object state);

		protected abstract IAsyncResult OnBeginTryReceive(TrackingContext trackingContext, IEnumerable<long> sequenceNumbers, TimeSpan timeout, AsyncCallback callback, object state);

		protected abstract IAsyncResult OnBeginTryReceive2(TrackingContext trackingContext, int messageCount, TimeSpan serverWaitTime, AsyncCallback callback, object state);

		protected virtual IAsyncResult OnBeginTryReceiveEventData(TrackingContext trackingContext, int messageCount, TimeSpan serverWaitTime, AsyncCallback callback, object state)
		{
			throw FxTrace.Exception.AsError(new NotSupportedException(SRCore.UnsupportedTransport("Receive", TransportType.NetMessaging.ToString())), null);
		}

		protected virtual void OnComplete(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, TimeSpan timeout)
		{
			MessageReceiver.RetryMessagesOperationAsyncResult retryMessagesOperationAsyncResult = new MessageReceiver.RetryMessagesOperationAsyncResult(this, trackingContext, DispositionStatus.Completed, lockTokens, null, null, null, true, timeout, null, null);
			retryMessagesOperationAsyncResult.RunSynchronously();
		}

		internal virtual ReceiveContext OnCreateReceiveContext(BrokeredMessage message)
		{
			if (message.ReceiveContext != null)
			{
				return new ReceiveContext(message.ReceiveContext);
			}
			return new ReceiveContext(this, (this.Mode == ReceiveMode.PeekLock ? message.LockToken : Guid.Empty));
		}

		protected virtual void OnDeadLetter(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, IDictionary<string, object> propertiesToModify, string deadLetterReason, string deadLetterErrorDescription, TimeSpan timeout)
		{
			MessageReceiver.RetryMessagesOperationAsyncResult retryMessagesOperationAsyncResult = new MessageReceiver.RetryMessagesOperationAsyncResult(this, trackingContext, DispositionStatus.Suspended, lockTokens, propertiesToModify, deadLetterReason, deadLetterErrorDescription, true, timeout, null, null);
			retryMessagesOperationAsyncResult.RunSynchronously();
		}

		protected virtual void OnDefer(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, IDictionary<string, object> propertiesToModify, TimeSpan timeout)
		{
			MessageReceiver.RetryMessagesOperationAsyncResult retryMessagesOperationAsyncResult = new MessageReceiver.RetryMessagesOperationAsyncResult(this, trackingContext, DispositionStatus.Defered, lockTokens, propertiesToModify, null, null, true, timeout, null, null);
			retryMessagesOperationAsyncResult.RunSynchronously();
		}

		protected abstract void OnEndAbandon(IAsyncResult result);

		protected abstract void OnEndComplete(IAsyncResult result);

		protected abstract void OnEndDeadLetter(IAsyncResult result);

		protected abstract void OnEndDefer(IAsyncResult result);

		internal abstract Microsoft.ServiceBus.Messaging.RuntimeEntityDescription OnEndGetRuntimeEntityDescriptionAsyncResult(IAsyncResult result);

		protected abstract IEnumerable<BrokeredMessage> OnEndPeek(IAsyncResult result);

		protected abstract IEnumerable<DateTime> OnEndRenewMessageLocks(IAsyncResult result);

		protected abstract bool OnEndTryReceive(IAsyncResult result, out IEnumerable<BrokeredMessage> messages);

		protected abstract bool OnEndTryReceive2(IAsyncResult result, out IEnumerable<BrokeredMessage> messages);

		protected virtual bool OnEndTryReceiveEventData(IAsyncResult result, out IEnumerable<EventData> messages)
		{
			messages = null;
			throw FxTrace.Exception.AsError(new NotSupportedException(SRCore.UnsupportedTransport("Receive", TransportType.NetMessaging.ToString())), null);
		}

		public void OnMessage(Action<BrokeredMessage> callback, OnMessageOptions options)
		{
			this.OnMessage(new MessageReceivePump(this, options, callback));
		}

		private void OnMessage(MessageReceivePump pump)
		{
			lock (this.receivePumpSyncRoot)
			{
				if (this.receivePump != null)
				{
					throw new InvalidOperationException(SRClient.OnMessageAlreadyCalled);
				}
				try
				{
					this.receivePump = pump;
					this.receivePump.Start();
				}
				catch (Exception exception)
				{
					this.receivePump = null;
					throw;
				}
			}
		}

		public void OnMessageAsync(Func<BrokeredMessage, Task> callback, OnMessageOptions options)
		{
			this.OnMessage(new MessageReceivePump(this, options, callback));
		}

		protected virtual IEnumerable<BrokeredMessage> OnPeek(TrackingContext trackingContext, long fromSequenceNumber, int messageCount, TimeSpan timeout)
		{
			MessageReceiver.RetryReceiveAsyncResult retryReceiveAsyncResult = new MessageReceiver.RetryReceiveAsyncResult(this, trackingContext, messageCount, fromSequenceNumber, null, timeout, null, null);
			try
			{
				retryReceiveAsyncResult = retryReceiveAsyncResult.RunSynchronously();
			}
			finally
			{
				MessagingPerformanceCounters.DecrementPendingReceiveMessageCount(this.MessagingFactory.Address, 1);
			}
			return retryReceiveAsyncResult.Messages;
		}

		protected virtual IEnumerable<DateTime> OnRenewMessageLocks(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, TimeSpan timeout)
		{
			MessageReceiver.RetryMessagesOperationAsyncResult retryMessagesOperationAsyncResult = new MessageReceiver.RetryMessagesOperationAsyncResult(this, trackingContext, DispositionStatus.Renewed, lockTokens, null, null, null, true, timeout, null, null);
			return retryMessagesOperationAsyncResult.RunSynchronously().LockedUntilUtcTimes;
		}

		protected virtual bool OnTryReceive(TrackingContext trackingContext, int messageCount, TimeSpan serverWaitTime, out IEnumerable<BrokeredMessage> messages)
		{
			MessageReceiver.RetryReceiveAsyncResult retryReceiveAsyncResult = new MessageReceiver.RetryReceiveAsyncResult(this, trackingContext, messageCount, (long)-1, null, serverWaitTime, null, null);
			try
			{
				retryReceiveAsyncResult = retryReceiveAsyncResult.RunSynchronously();
			}
			finally
			{
				MessagingPerformanceCounters.DecrementPendingReceiveMessageCount(this.MessagingFactory.Address, 1);
			}
			messages = retryReceiveAsyncResult.Messages;
			return retryReceiveAsyncResult.TryReceive;
		}

		protected virtual bool OnTryReceive(TrackingContext trackingContext, IEnumerable<long> sequenceNumbers, TimeSpan timeout, out IEnumerable<BrokeredMessage> messages)
		{
			MessageReceiver.RetryReceiveAsyncResult retryReceiveAsyncResult = new MessageReceiver.RetryReceiveAsyncResult(this, trackingContext, 0, (long)-1, sequenceNumbers, timeout, null, null);
			try
			{
				retryReceiveAsyncResult = retryReceiveAsyncResult.RunSynchronously();
			}
			finally
			{
				MessagingPerformanceCounters.DecrementPendingReceiveMessageCount(this.MessagingFactory.Address, 1);
			}
			messages = retryReceiveAsyncResult.Messages;
			return retryReceiveAsyncResult.TryReceive;
		}

		public BrokeredMessage Peek()
		{
			return this.Peek(this.LastPeekedSequenceNumber + (long)1);
		}

		public BrokeredMessage Peek(long fromSequenceNumber)
		{
			return this.Peek(null, fromSequenceNumber, this.OperationTimeout);
		}

		internal BrokeredMessage Peek(TrackingContext trackingContext, long fromSequenceNumber, TimeSpan timeout)
		{
			IEnumerable<BrokeredMessage> brokeredMessages = this.PeekBatch(trackingContext, fromSequenceNumber, 1, timeout);
			return MessageReceiver.GetTopMessage(brokeredMessages);
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
			return this.PeekBatch(this.LastPeekedSequenceNumber + (long)1, messageCount);
		}

		public IEnumerable<BrokeredMessage> PeekBatch(long fromSequenceNumber, int messageCount)
		{
			return this.PeekBatch(null, fromSequenceNumber, messageCount, this.OperationTimeout);
		}

		internal IEnumerable<BrokeredMessage> PeekBatch(TrackingContext trackingContext, long fromSequenceNumber, int messageCount, TimeSpan timeout)
		{
			base.ThrowIfDisposed();
			TimeoutHelper.ThrowIfNegativeArgument(timeout);
			if (trackingContext == null)
			{
				trackingContext = TrackingContext.GetInstance(Guid.NewGuid(), this.Path);
			}
			MessageReceiver.TracePeek(EventTraceActivity.CreateFromThread(), trackingContext);
			if (this.openOnceManager.ShouldOpen)
			{
				this.openOnceManager.Open();
			}
			return this.OnPeek(trackingContext, fromSequenceNumber, messageCount, timeout);
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
			return this.Receive(this.OperationTimeout);
		}

		public BrokeredMessage Receive(TimeSpan serverWaitTime)
		{
			BrokeredMessage brokeredMessage;
			this.TryReceive(serverWaitTime, out brokeredMessage);
			return brokeredMessage;
		}

		public BrokeredMessage Receive(long sequenceNumber)
		{
			IEnumerable<BrokeredMessage> brokeredMessages;
			long[] numArray = new long[] { sequenceNumber };
			if (!this.TryReceive(numArray, this.OperationTimeout, out brokeredMessages))
			{
				return null;
			}
			return MessageReceiver.GetTopMessage(brokeredMessages);
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
			IEnumerable<BrokeredMessage> brokeredMessages;
			this.TryReceive(messageCount, serverWaitTime, out brokeredMessages);
			return brokeredMessages;
		}

		public IEnumerable<BrokeredMessage> ReceiveBatch(IEnumerable<long> sequenceNumbers)
		{
			IEnumerable<BrokeredMessage> brokeredMessages;
			this.TryReceive(sequenceNumbers, this.OperationTimeout, out brokeredMessages);
			return brokeredMessages;
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

		private void SetReceiveContext(IEnumerable<BrokeredMessage> messages)
		{
			if (messages != null)
			{
				foreach (BrokeredMessage message in messages)
				{
					message.ReceiveContext = this.OnCreateReceiveContext(message);
				}
			}
		}

		private static void TracePeek(EventTraceActivity activity, TrackingContext trackingContext)
		{
			if (activity != null && activity != EventTraceActivity.Empty && trackingContext != null)
			{
				MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessagePeekTransfer(activity, trackingContext.Activity));
			}
		}

		private static void TraceReceive(EventTraceActivity activity, TrackingContext trackingContext)
		{
			if (activity != null && activity != EventTraceActivity.Empty && trackingContext != null)
			{
				MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageReceiveTransfer(activity, trackingContext.Activity));
			}
		}

		internal bool TryReceive(TimeSpan serverWaitTime, out BrokeredMessage message)
		{
			IEnumerable<BrokeredMessage> brokeredMessages;
			message = null;
			bool flag = this.TryReceive(1, serverWaitTime, out brokeredMessages);
			if (brokeredMessages != null)
			{
				message = MessageReceiver.GetTopMessage(brokeredMessages);
			}
			return flag;
		}

		internal bool TryReceive(int messageCount, out IEnumerable<BrokeredMessage> messages)
		{
			return this.TryReceive(messageCount, this.OperationTimeout, out messages);
		}

		internal bool TryReceive(IEnumerable<long> sequenceNumbers, out IEnumerable<BrokeredMessage> messages)
		{
			return this.TryReceive(sequenceNumbers, this.OperationTimeout, out messages);
		}

		internal bool TryReceive(int messageCount, TimeSpan serverWaitTime, out IEnumerable<BrokeredMessage> messages)
		{
			return this.TryReceive(null, messageCount, serverWaitTime, out messages);
		}

		internal bool TryReceive(TrackingContext trackingContext, int messageCount, TimeSpan serverWaitTime, out IEnumerable<BrokeredMessage> messages)
		{
			base.ThrowIfDisposed();
			TimeoutHelper.ThrowIfNegativeArgument(serverWaitTime);
			if (messageCount < 1)
			{
				throw FxTrace.Exception.AsError(new ArgumentOutOfRangeException("messageCount"), null);
			}
			if (trackingContext == null)
			{
				trackingContext = TrackingContext.GetInstance(Guid.NewGuid(), this.Path);
			}
			if (this.openOnceManager.ShouldOpen)
			{
				this.openOnceManager.Open();
			}
			bool flag = this.OnTryReceive(trackingContext, messageCount, serverWaitTime, out messages);
			this.SetReceiveContext(messages);
			return flag;
		}

		internal bool TryReceive(IEnumerable<long> sequenceNumbers, TimeSpan timeout, out IEnumerable<BrokeredMessage> messages)
		{
			return this.TryReceive(null, sequenceNumbers, timeout, out messages);
		}

		internal bool TryReceive(TrackingContext trackingContext, IEnumerable<long> sequenceNumbers, TimeSpan timeout, out IEnumerable<BrokeredMessage> messages)
		{
			base.ThrowIfDisposed();
			MessagingUtilities.CheckValidSequenceNumbers(sequenceNumbers);
			TimeoutHelper.ThrowIfNegativeArgument(timeout);
			if (trackingContext == null)
			{
				trackingContext = TrackingContext.GetInstance(Guid.NewGuid(), this.Path);
			}
			MessageReceiver.TraceReceive(EventTraceActivity.CreateFromThread(), trackingContext);
			if (this.openOnceManager.ShouldOpen)
			{
				this.openOnceManager.Open();
			}
			if (!(sequenceNumbers is List<long>))
			{
				sequenceNumbers = new List<long>(sequenceNumbers);
			}
			bool flag = this.OnTryReceive(trackingContext, sequenceNumbers, timeout, out messages);
			this.SetReceiveContext(messages);
			return flag;
		}

		private sealed class RetryMessagesOperationAsyncResult : RetryAsyncResult<MessageReceiver.RetryMessagesOperationAsyncResult>
		{
			private readonly MessageReceiver receiver;

			private readonly TrackingContext trackingContext;

			private readonly IEnumerable<Guid> lockTokens;

			private readonly IEnumerable<ArraySegment<byte>> deliveryTags;

			private readonly IDictionary<string, object> propertiesToModify;

			private readonly string deadLetterReason;

			private readonly string deadLetterErrorDescription;

			private readonly DispositionStatus operation;

			private readonly bool fromSync;

			public IEnumerable<DateTime> LockedUntilUtcTimes
			{
				get;
				private set;
			}

			public RetryMessagesOperationAsyncResult(MessageReceiver receiver, TrackingContext trackingContext, DispositionStatus operation, IEnumerable<Guid> lockTokens, IDictionary<string, object> propertiesToModify, string deadLetterReason, string deadLetterErrorDescription, bool fromSync, TimeSpan timeout, AsyncCallback callback, object state) : this(receiver, trackingContext, operation, propertiesToModify, deadLetterReason, deadLetterErrorDescription, fromSync, timeout, callback, state)
			{
				this.lockTokens = lockTokens;
				if (this.lockTokens == null || !this.lockTokens.Any<Guid>())
				{
					throw Fx.Exception.ArgumentNullOrEmpty("lockTokens");
				}
			}

			public RetryMessagesOperationAsyncResult(MessageReceiver receiver, TrackingContext trackingContext, DispositionStatus operation, IEnumerable<ArraySegment<byte>> deliveryTags, IDictionary<string, object> propertiesToModify, string deadLetterReason, string deadLetterErrorDescription, bool fromSync, TimeSpan timeout, AsyncCallback callback, object state) : this(receiver, trackingContext, operation, propertiesToModify, deadLetterReason, deadLetterErrorDescription, fromSync, timeout, callback, state)
			{
				this.deliveryTags = deliveryTags;
				if (this.deliveryTags == null || !this.deliveryTags.Any<ArraySegment<byte>>())
				{
					throw Fx.Exception.ArgumentNullOrEmpty("deliveryTags");
				}
			}

			private RetryMessagesOperationAsyncResult(MessageReceiver receiver, TrackingContext trackingContext, DispositionStatus operation, IDictionary<string, object> propertiesToModify, string deadLetterReason, string deadLetterErrorDescription, bool fromSync, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				if (trackingContext == null)
				{
					throw Fx.Exception.ArgumentNull("trackingContext");
				}
				if (receiver == null)
				{
					throw Fx.Exception.ArgumentNull("receiver");
				}
				this.receiver = receiver;
				this.trackingContext = trackingContext;
				this.propertiesToModify = propertiesToModify;
				this.deadLetterErrorDescription = deadLetterErrorDescription;
				this.deadLetterReason = deadLetterReason;
				this.operation = operation;
				this.fromSync = fromSync;
			}

			public static new void End(IAsyncResult r)
			{
				AsyncResult<MessageReceiver.RetryMessagesOperationAsyncResult>.End(r);
			}

			protected override IEnumerator<IteratorAsyncResult<MessageReceiver.RetryMessagesOperationAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				// 
				// Current member / type: System.Collections.Generic.IEnumerator`1<Microsoft.ServiceBus.Messaging.IteratorAsyncResult`1/AsyncStep<Microsoft.ServiceBus.Messaging.MessageReceiver/RetryMessagesOperationAsyncResult>> Microsoft.ServiceBus.Messaging.MessageReceiver/RetryMessagesOperationAsyncResult::GetAsyncSteps()
				// File path: C:\Users\Milo.Wical\Desktop\Microsoft.ServiceBus.dll
				// 
				// Product version: 2016.3.1003.0
				// Exception in: System.Collections.Generic.IEnumerator<Microsoft.ServiceBus.Messaging.IteratorAsyncResult<TIteratorAsyncResult>/AsyncStep<Microsoft.ServiceBus.Messaging.MessageReceiver/RetryMessagesOperationAsyncResult>> GetAsyncSteps()
				// 
				// Orphaned condition not properly marked as goto!
				//    at ..( , Boolean ,  , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 358
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 294
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 701
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 263
				//    at ..( , Boolean , List`1 ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 562
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 282
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 701
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 263
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 647
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 274
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 701
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 263
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 601
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 278
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 701
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 263
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 603
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 278
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 701
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 263
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 486
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 286
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 701
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 263
				//    at ..( , Boolean , List`1 ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 562
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 282
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 701
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 263
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 601
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 278
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 701
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 263
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 647
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 274
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 701
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 263
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 647
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 274
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 701
				//    at ..( , Boolean ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 263
				//    at ..( ,  ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\StatementDecompilerStep.cs:line 69
				//    at ..(MethodBody ,  , ILanguage ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\DecompilationPipeline.cs:line 88
				//    at ..(MethodBody , ILanguage ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\DecompilationPipeline.cs:line 70
				//    at ..(MethodBody ,  ,  , Func`2 , & ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\Extensions.cs:line 104
				//    at ..(MethodBody ,  , & ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\Extensions.cs:line 139
				//    at ..() in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Steps\RebuildYieldStatementsStep.cs:line 134
				//    at ..Match( ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Steps\RebuildYieldStatementsStep.cs:line 49
				//    at ..( ,  ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Steps\RebuildYieldStatementsStep.cs:line 20
				//    at ..(MethodBody ,  , ILanguage ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\DecompilationPipeline.cs:line 88
				//    at ..(MethodBody , ILanguage ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\DecompilationPipeline.cs:line 70
				//    at ..( , ILanguage , MethodBody , & ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\Extensions.cs:line 95
				//    at ..(MethodBody , ILanguage , & ,  ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\Extensions.cs:line 58
				//    at ..(ILanguage , MethodDefinition ,  ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\WriterContextServices\BaseWriterContextService.cs:line 117
				// 
				// mailto: JustDecompilePublicFeedback@telerik.com

			}

			public static IEnumerable<DateTime> RenewLockEnd(IAsyncResult r)
			{
				return AsyncResult<MessageReceiver.RetryMessagesOperationAsyncResult>.End(r).LockedUntilUtcTimes;
			}
		}

		private sealed class RetryReceiveAsyncResult : RetryAsyncResult<MessageReceiver.RetryReceiveAsyncResult>
		{
			private readonly MessageReceiver receiver;

			private readonly TrackingContext trackingContext;

			private readonly int messageCount;

			private readonly long fromSequenceNumber;

			private readonly IEnumerable<long> sequenceNumbers;

			private IEnumerable<BrokeredMessage> messages;

			public IEnumerable<BrokeredMessage> Messages
			{
				get
				{
					return this.messages;
				}
			}

			public bool TryReceive
			{
				get;
				private set;
			}

			public RetryReceiveAsyncResult(MessageReceiver receiver, TrackingContext trackingContext, int messageCount, long fromSequenceNumber, IEnumerable<long> sequenceNumbers, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				if (trackingContext == null)
				{
					throw Fx.Exception.ArgumentNull("trackingContext");
				}
				if (receiver == null)
				{
					throw Fx.Exception.ArgumentNull("receiver");
				}
				this.receiver = receiver;
				this.trackingContext = trackingContext;
				this.messageCount = messageCount;
				this.fromSequenceNumber = fromSequenceNumber;
				this.sequenceNumbers = sequenceNumbers;
				MessagingPerformanceCounters.IncrementPendingReceiveMessageCount(this.receiver.MessagingFactory.Address, 1);
			}

			public static new IEnumerable<BrokeredMessage> End(IAsyncResult r)
			{
				IEnumerable<BrokeredMessage> messages;
				MessageReceiver.RetryReceiveAsyncResult retryReceiveAsyncResult = null;
				try
				{
					retryReceiveAsyncResult = AsyncResult<MessageReceiver.RetryReceiveAsyncResult>.End(r);
					messages = retryReceiveAsyncResult.Messages;
				}
				finally
				{
					if (retryReceiveAsyncResult != null && retryReceiveAsyncResult.receiver != null)
					{
						MessagingPerformanceCounters.DecrementPendingReceiveMessageCount(retryReceiveAsyncResult.receiver.MessagingFactory.Address, 1);
					}
				}
				return messages;
			}

			protected override IEnumerator<IteratorAsyncResult<MessageReceiver.RetryReceiveAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				TimeSpan timeSpan;
				TimeSpan timeSpan1;
				int num;
				bool flag;
				int num1 = 0;
				Stopwatch stopwatch = Stopwatch.StartNew();
				try
				{
					timeSpan = (this.receiver.RetryPolicy.IsServerBusy ? Microsoft.ServiceBus.RetryPolicy.ServerBusyBaseSleepTime : TimeSpan.Zero);
					TimeSpan timeSpan2 = timeSpan;
					timeSpan1 = (base.OriginalTimeout > this.receiver.OperationTimeout ? base.OriginalTimeout : this.receiver.OperationTimeout);
					TimeSpan timeSpan3 = timeSpan1;
					if (!this.receiver.RetryPolicy.IsServerBusy || !(Microsoft.ServiceBus.RetryPolicy.ServerBusyBaseSleepTime >= timeSpan3))
					{
						while (true)
						{
							bool flag1 = false;
							if (timeSpan2 != TimeSpan.Zero)
							{
								yield return base.CallAsyncSleep(timeSpan2);
							}
							string str = "Unknown";
							if (this.fromSequenceNumber >= (long)0)
							{
								str = "Peek";
								MessageReceiver.RetryReceiveAsyncResult retryReceiveAsyncResult = this;
								IteratorAsyncResult<MessageReceiver.RetryReceiveAsyncResult>.BeginCall beginCall = (MessageReceiver.RetryReceiveAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.receiver.OnBeginPeek(thisPtr.trackingContext, thisPtr.fromSequenceNumber, thisPtr.messageCount, t, c, s);
								yield return retryReceiveAsyncResult.CallAsync(beginCall, (MessageReceiver.RetryReceiveAsyncResult thisPtr, IAsyncResult r) => thisPtr.messages = thisPtr.receiver.OnEndPeek(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
							}
							else if (this.sequenceNumbers == null || !this.sequenceNumbers.Any<long>())
							{
								str = "Receive";
								MessageReceiver.RetryReceiveAsyncResult retryReceiveAsyncResult1 = this;
								Transaction ambientTransaction = base.AmbientTransaction;
								IteratorAsyncResult<MessageReceiver.RetryReceiveAsyncResult>.BeginCall beginCall1 = (MessageReceiver.RetryReceiveAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.receiver.OnBeginTryReceive(thisPtr.trackingContext, thisPtr.messageCount, t, c, s);
								yield return retryReceiveAsyncResult1.CallTransactionalAsync(ambientTransaction, beginCall1, (MessageReceiver.RetryReceiveAsyncResult thisPtr, IAsyncResult r) => thisPtr.TryReceive = thisPtr.receiver.OnEndTryReceive(r, out thisPtr.messages), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
							}
							else
							{
								str = "ReceiveWithSequences";
								MessageReceiver.RetryReceiveAsyncResult retryReceiveAsyncResult2 = this;
								Transaction transaction = base.AmbientTransaction;
								IteratorAsyncResult<MessageReceiver.RetryReceiveAsyncResult>.BeginCall beginCall2 = (MessageReceiver.RetryReceiveAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.receiver.OnBeginTryReceive(thisPtr.trackingContext, thisPtr.sequenceNumbers, t, c, s);
								yield return retryReceiveAsyncResult2.CallTransactionalAsync(transaction, beginCall2, (MessageReceiver.RetryReceiveAsyncResult thisPtr, IAsyncResult r) => thisPtr.TryReceive = thisPtr.receiver.OnEndTryReceive(r, out thisPtr.messages), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
							}
							if (base.LastAsyncStepException == null)
							{
								Uri address = this.receiver.MessagingFactory.Address;
								num = (this.messages == null ? 0 : this.messages.Count<BrokeredMessage>());
								MessagingPerformanceCounters.IncrementReceiveMessageSuccessPerSec(address, num);
								this.receiver.RetryPolicy.ResetServerBusy();
							}
							else
							{
								MessagingPerformanceCounters.IncrementReceiveMessageFailurePerSec(this.receiver.MessagingFactory.Address, 1);
								MessagingPerformanceCounters.IncrementExceptionPerSec(this.receiver.MessagingFactory.Address, 1, base.LastAsyncStepException);
								flag = (base.TransactionExists ? false : this.receiver.RetryPolicy.ShouldRetry(base.RemainingTime(), num1, base.LastAsyncStepException, out timeSpan2));
								flag1 = flag;
								if (flag1)
								{
									MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRetryPolicyIteration(this.trackingContext.Activity, this.trackingContext.TrackingId, this.receiver.RetryPolicy.GetType().Name, str, num1, timeSpan2.ToString(), this.LastAsyncStepException.GetType().FullName, this.LastAsyncStepException.Message));
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
						string str1 = this.receiver.RetryPolicy.ServerBusyExceptionMessage;
						yield return base.CallAsyncSleep(base.RemainingTime());
						base.Complete(new ServerBusyException(str1, this.trackingContext));
						goto Label0;
					}
				}
				finally
				{
					stopwatch.Stop();
					MessagingPerformanceCounters.IncrementReceiveMessageLatency(this.receiver.MessagingFactory.Address, stopwatch.ElapsedTicks);
				}
				base.Complete(base.LastAsyncStepException);
			Label0:
				yield break;
			}

			public static bool TryReceiveEnd(IAsyncResult r, out IEnumerable<BrokeredMessage> messages)
			{
				bool tryReceive;
				MessageReceiver.RetryReceiveAsyncResult retryReceiveAsyncResult = r as MessageReceiver.RetryReceiveAsyncResult;
				try
				{
					retryReceiveAsyncResult = AsyncResult<MessageReceiver.RetryReceiveAsyncResult>.End(r);
					messages = retryReceiveAsyncResult.Messages;
					tryReceive = retryReceiveAsyncResult.TryReceive;
				}
				finally
				{
					if (retryReceiveAsyncResult != null && retryReceiveAsyncResult.receiver != null)
					{
						MessagingPerformanceCounters.DecrementPendingReceiveMessageCount(retryReceiveAsyncResult.receiver.MessagingFactory.Address, 1);
					}
				}
				return tryReceive;
			}
		}

		private sealed class RetryReceiveEventDataAsyncResult : RetryAsyncResult<MessageReceiver.RetryReceiveEventDataAsyncResult>
		{
			private readonly MessageReceiver receiver;

			private readonly TrackingContext trackingContext;

			private readonly int messageCount;

			private IEnumerable<EventData> messages;

			public IEnumerable<EventData> Messages
			{
				get
				{
					return this.messages;
				}
			}

			public bool TryReceive
			{
				get;
				private set;
			}

			public RetryReceiveEventDataAsyncResult(MessageReceiver receiver, TrackingContext trackingContext, int messageCount, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				if (trackingContext == null)
				{
					throw Fx.Exception.ArgumentNull("trackingContext");
				}
				if (receiver == null)
				{
					throw Fx.Exception.ArgumentNull("receiver");
				}
				this.receiver = receiver;
				this.trackingContext = trackingContext;
				this.messageCount = messageCount;
				MessagingPerformanceCounters.IncrementPendingReceiveMessageCount(this.receiver.MessagingFactory.Address, 1);
			}

			protected override IEnumerator<IteratorAsyncResult<MessageReceiver.RetryReceiveEventDataAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				TimeSpan timeSpan;
				TimeSpan timeSpan1;
				int num;
				bool flag;
				int num1 = 0;
				Stopwatch stopwatch = Stopwatch.StartNew();
				try
				{
					timeSpan = (this.receiver.RetryPolicy.IsServerBusy ? Microsoft.ServiceBus.RetryPolicy.ServerBusyBaseSleepTime : TimeSpan.Zero);
					TimeSpan timeSpan2 = timeSpan;
					timeSpan1 = (base.OriginalTimeout > this.receiver.OperationTimeout ? base.OriginalTimeout : this.receiver.OperationTimeout);
					TimeSpan timeSpan3 = timeSpan1;
					if (!this.receiver.RetryPolicy.IsServerBusy || !(Microsoft.ServiceBus.RetryPolicy.ServerBusyBaseSleepTime >= timeSpan3))
					{
						while (true)
						{
							bool flag1 = false;
							if (timeSpan2 != TimeSpan.Zero)
							{
								yield return base.CallAsyncSleep(timeSpan2);
							}
							MessageReceiver.RetryReceiveEventDataAsyncResult retryReceiveEventDataAsyncResult = this;
							Transaction ambientTransaction = base.AmbientTransaction;
							IteratorAsyncResult<MessageReceiver.RetryReceiveEventDataAsyncResult>.BeginCall beginCall = (MessageReceiver.RetryReceiveEventDataAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.receiver.OnBeginTryReceiveEventData(thisPtr.trackingContext, thisPtr.messageCount, t, c, s);
							yield return retryReceiveEventDataAsyncResult.CallTransactionalAsync(ambientTransaction, beginCall, (MessageReceiver.RetryReceiveEventDataAsyncResult thisPtr, IAsyncResult r) => thisPtr.TryReceive = thisPtr.receiver.OnEndTryReceiveEventData(r, out thisPtr.messages), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
							if (base.LastAsyncStepException == null)
							{
								Uri address = this.receiver.MessagingFactory.Address;
								num = (this.messages == null ? 0 : this.messages.Count<EventData>());
								MessagingPerformanceCounters.IncrementReceiveMessageSuccessPerSec(address, num);
								this.receiver.RetryPolicy.ResetServerBusy();
							}
							else
							{
								MessagingPerformanceCounters.IncrementReceiveMessageFailurePerSec(this.receiver.MessagingFactory.Address, 1);
								MessagingPerformanceCounters.IncrementExceptionPerSec(this.receiver.MessagingFactory.Address, 1, base.LastAsyncStepException);
								flag = (base.TransactionExists ? false : this.receiver.RetryPolicy.ShouldRetry(base.RemainingTime(), num1, base.LastAsyncStepException, out timeSpan2));
								flag1 = flag;
								if (flag1)
								{
									MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRetryPolicyIteration(this.trackingContext.Activity, this.trackingContext.TrackingId, this.receiver.RetryPolicy.GetType().Name, "ReceiveEventData", num1, timeSpan2.ToString(), this.LastAsyncStepException.GetType().FullName, this.LastAsyncStepException.Message));
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
						string str = this.receiver.RetryPolicy.ServerBusyExceptionMessage;
						yield return base.CallAsyncSleep(base.RemainingTime());
						base.Complete(new ServerBusyException(str, this.trackingContext));
						goto Label0;
					}
				}
				finally
				{
					stopwatch.Stop();
					MessagingPerformanceCounters.IncrementReceiveMessageLatency(this.receiver.MessagingFactory.Address, stopwatch.ElapsedTicks);
				}
				base.Complete(base.LastAsyncStepException);
			Label0:
				yield break;
			}

			public static bool TryReceiveEnd(IAsyncResult r, out IEnumerable<EventData> messages)
			{
				bool tryReceive;
				MessageReceiver.RetryReceiveEventDataAsyncResult retryReceiveEventDataAsyncResult = r as MessageReceiver.RetryReceiveEventDataAsyncResult;
				try
				{
					retryReceiveEventDataAsyncResult = AsyncResult<MessageReceiver.RetryReceiveEventDataAsyncResult>.End(r);
					messages = retryReceiveEventDataAsyncResult.Messages;
					tryReceive = retryReceiveEventDataAsyncResult.TryReceive;
				}
				finally
				{
					if (retryReceiveEventDataAsyncResult != null && retryReceiveEventDataAsyncResult.receiver != null)
					{
						MessagingPerformanceCounters.DecrementPendingReceiveMessageCount(retryReceiveEventDataAsyncResult.receiver.MessagingFactory.Address, 1);
					}
				}
				return tryReceive;
			}
		}
	}
}