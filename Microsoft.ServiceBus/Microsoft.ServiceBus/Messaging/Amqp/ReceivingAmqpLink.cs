using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using Microsoft.ServiceBus.Messaging.Amqp.Transaction;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal sealed class ReceivingAmqpLink : AmqpLink
	{
		private const int MaxCreditForOnDemandReceive = 200;

		private const int CreditBatchThreshold = 20;

		private readonly static TimeSpan MinReceiveTimeout;

		private readonly object syncRoot;

		private Action<AmqpMessage> messageListener;

		private Queue<AmqpMessage> messageQueue;

		private WorkCollection<ArraySegment<byte>, ReceivingAmqpLink.DisposeAsyncResult, Outcome> pendingDispositions;

		private AmqpMessage currentMessage;

		private LinkedList<ReceivingAmqpLink.ReceiveAsyncResult> waiterList;

		static ReceivingAmqpLink()
		{
			ReceivingAmqpLink.MinReceiveTimeout = TimeSpan.FromSeconds(10);
		}

		public ReceivingAmqpLink(AmqpLinkSettings settings) : this(null, settings)
		{
		}

		public ReceivingAmqpLink(AmqpSession session, AmqpLinkSettings settings) : base(session, settings)
		{
			this.syncRoot = new object();
		}

		protected override void AbortInternal()
		{
			Queue<AmqpMessage> amqpMessages = null;
			this.CancelPendingOperations(true, out amqpMessages);
			if (amqpMessages != null)
			{
				foreach (AmqpMessage amqpMessage in amqpMessages)
				{
					amqpMessage.Dispose();
				}
			}
			AmqpMessage amqpMessage1 = this.currentMessage;
			if (amqpMessage1 != null)
			{
				amqpMessage1.Dispose();
			}
			base.AbortInternal();
		}

		public void AcceptMessage(AmqpMessage message, bool batchable)
		{
			bool settleType = base.Settings.SettleType != SettleMode.SettleOnDispose;
			this.AcceptMessage(message, settleType, batchable);
		}

		public void AcceptMessage(AmqpMessage message, bool settled, bool batchable)
		{
			this.DisposeMessage(message, AmqpConstants.AcceptedOutcome, settled, batchable);
		}

		public IAsyncResult BeginDisposeMessage(ArraySegment<byte> deliveryTag, Outcome outcome, bool batchable, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new ReceivingAmqpLink.DisposeAsyncResult(this, deliveryTag, outcome, batchable, timeout, callback, state);
		}

		public IAsyncResult BeginReceiveMessage(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.BeginReceiveMessages(1, timeout, callback, state);
		}

		public IAsyncResult BeginReceiveMessages(int messageCount, TimeSpan timeout, AsyncCallback callback, object state)
		{
			List<AmqpMessage> amqpMessages = new List<AmqpMessage>();
			lock (this.syncRoot)
			{
				if (this.messageQueue != null && this.messageQueue.Count > 0)
				{
					for (int i = 0; i < messageCount && this.messageQueue.Count > 0; i++)
					{
						amqpMessages.Add(this.messageQueue.Dequeue());
					}
				}
			}
			if (amqpMessages.Any<AmqpMessage>() || !(timeout > TimeSpan.Zero))
			{
				return new CompletedAsyncResult<IEnumerable<AmqpMessage>>(amqpMessages, callback, state);
			}
			ReceivingAmqpLink.ReceiveAsyncResult receiveAsyncResult = new ReceivingAmqpLink.ReceiveAsyncResult(this, timeout, callback, state);
			bool flag = true;
			lock (this.syncRoot)
			{
				if (this.messageQueue != null)
				{
					if (this.messageQueue.Count <= 0)
					{
						receiveAsyncResult.Initialize(this.waiterList.AddLast(receiveAsyncResult));
						flag = false;
						int num = (base.Settings.AutoSendFlow ? 0 : this.GetOnDemandReceiveCredit());
						if (num > 0)
						{
							base.IssueCredit((uint)num, false, AmqpConstants.NullBinary);
						}
					}
					else
					{
						int num1 = 0;
						while (num1 < messageCount)
						{
							if (this.messageQueue.Count > 0)
							{
								amqpMessages.Add(this.messageQueue.Dequeue());
								num1++;
							}
							else
							{
								break;
							}
						}
					}
				}
			}
			if (flag)
			{
				receiveAsyncResult.Signal(amqpMessages, true);
			}
			return receiveAsyncResult;
		}

		public IAsyncResult BeginReceiveRemoteMessage(TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (timeout == TimeSpan.Zero)
			{
				timeout = ReceivingAmqpLink.MinReceiveTimeout;
			}
			return this.BeginReceiveMessage(timeout, callback, state);
		}

		private void CancelPendingOperations(bool aborted, out Queue<AmqpMessage> messagesToRelease)
		{
			messagesToRelease = null;
			LinkedList<ReceivingAmqpLink.ReceiveAsyncResult> receiveAsyncResults = null;
			lock (this.syncRoot)
			{
				messagesToRelease = this.messageQueue;
				receiveAsyncResults = this.waiterList;
				this.messageQueue = null;
				this.waiterList = null;
			}
			if (receiveAsyncResults != null)
			{
				ActionItem.Schedule((object o) => {
					Tuple<LinkedList<ReceivingAmqpLink.ReceiveAsyncResult>, bool> tuple = (Tuple<LinkedList<ReceivingAmqpLink.ReceiveAsyncResult>, bool>)o;
					foreach (ReceivingAmqpLink.ReceiveAsyncResult item1 in tuple.Item1)
					{
						if (!tuple.Item2)
						{
							item1.Signal(null, false, null);
						}
						else
						{
							item1.Cancel();
						}
					}
				}, new Tuple<LinkedList<ReceivingAmqpLink.ReceiveAsyncResult>, bool>(receiveAsyncResults, aborted));
			}
			if (this.pendingDispositions != null)
			{
				this.pendingDispositions.Abort();
			}
		}

		protected override bool CloseInternal()
		{
			Queue<AmqpMessage> amqpMessages = null;
			this.CancelPendingOperations(false, out amqpMessages);
			if (amqpMessages != null)
			{
				foreach (AmqpMessage amqpMessage in amqpMessages)
				{
					this.ReleaseMessage(amqpMessage);
					amqpMessage.Dispose();
				}
			}
			AmqpMessage amqpMessage1 = this.currentMessage;
			if (amqpMessage1 != null)
			{
				amqpMessage1.Dispose();
			}
			return base.CloseInternal();
		}

		public override bool CreateDelivery(out Delivery delivery)
		{
			if (this.currentMessage != null)
			{
				delivery = this.currentMessage;
				return false;
			}
			AmqpMessage amqpMessage = AmqpMessage.CreateReceivedMessage();
			AmqpMessage amqpMessage1 = amqpMessage;
			this.currentMessage = amqpMessage;
			delivery = amqpMessage1;
			return true;
		}

		public void DisposeMessage(AmqpMessage message, DeliveryState state, bool settled, bool batchable)
		{
			message.Batchable = batchable;
			base.DisposeDelivery(message, settled, state);
		}

		public Outcome EndDisposeMessage(IAsyncResult result)
		{
			return ReceivingAmqpLink.DisposeAsyncResult.End(result);
		}

		public bool EndReceiveMessage(IAsyncResult result, out AmqpMessage message)
		{
			IEnumerable<AmqpMessage> amqpMessages;
			if (!(result is ReceivingAmqpLink.ReceiveAsyncResult))
			{
				message = CompletedAsyncResult<IEnumerable<AmqpMessage>>.End(result).FirstOrDefault<AmqpMessage>();
				return true;
			}
			bool flag = ReceivingAmqpLink.ReceiveAsyncResult.End(result, out amqpMessages);
			message = amqpMessages.FirstOrDefault<AmqpMessage>();
			return flag;
		}

		public bool EndReceiveMessages(IAsyncResult result, out IEnumerable<AmqpMessage> messages)
		{
			if (result is ReceivingAmqpLink.ReceiveAsyncResult)
			{
				return ReceivingAmqpLink.ReceiveAsyncResult.End(result, out messages);
			}
			messages = CompletedAsyncResult<IEnumerable<AmqpMessage>>.End(result);
			return true;
		}

		private int GetOnDemandReceiveCredit()
		{
			int num = 0;
			int linkCredit = (int)base.LinkCredit;
			if (this.waiterList.Count > linkCredit && linkCredit < 200)
			{
				int num1 = Math.Min(this.waiterList.Count, 200) - linkCredit;
				if (this.waiterList.Count <= 20 || num1 % 20 == 0)
				{
					num = linkCredit + num1;
				}
			}
			return num;
		}

		public void ModifyMessage(AmqpMessage message, bool deliveryFailed, bool deliverElseWhere, Fields messageAttributes)
		{
			Modified modified = new Modified()
			{
				DeliveryFailed = new bool?(deliveryFailed),
				UndeliverableHere = new bool?(deliverElseWhere),
				MessageAnnotations = messageAttributes
			};
			this.DisposeMessage(message, modified, true, false);
		}

		protected override void OnCreditAvailable(int session, uint link, bool drain, ArraySegment<byte> txnId)
		{
		}

		protected override void OnDisposeDeliveryInternal(Delivery delivery)
		{
			MessagingClientEtwProvider.TraceClient<ReceivingAmqpLink, Delivery>((ReceivingAmqpLink source, Delivery deliv) => {
			}, this, delivery);
			DeliveryState state = delivery.State;
			if (delivery.Transactional())
			{
				state = ((TransactionalState)delivery.State).Outcome;
			}
			if (state != null)
			{
				this.pendingDispositions.CompleteWork(delivery.DeliveryTag, false, (Outcome)state);
			}
		}

		protected override void OnProcessTransfer(Delivery delivery, Transfer transfer, Frame frame)
		{
			if (base.Settings.MaxMessageSize.HasValue)
			{
				ulong bytesTransfered = (ulong)(this.currentMessage.BytesTransfered + (long)frame.Payload.Count);
				if (bytesTransfered > base.Settings.MaxMessageSize.Value)
				{
					if (!base.IsClosing())
					{
						Microsoft.ServiceBus.Messaging.Amqp.Framing.Error messageSizeExceeded = AmqpError.MessageSizeExceeded;
						object value = this.currentMessage.DeliveryId.Value;
						object obj = bytesTransfered;
						ulong? maxMessageSize = base.Settings.MaxMessageSize;
						throw new AmqpException(messageSizeExceeded, SRAmqp.AmqpMessageSizeExceeded(value, obj, maxMessageSize.Value));
					}
					return;
				}
			}
			ArraySegment<byte> payload = frame.Payload;
			frame.RawByteBuffer.AdjustPosition(payload.Offset, payload.Count);
			frame.RawByteBuffer.Clone();
			this.currentMessage.AddPayload(frame.RawByteBuffer, !transfer.More());
			if (!transfer.More())
			{
				AmqpMessage amqpMessage = this.currentMessage;
				this.currentMessage = null;
				Action<ReceivingAmqpLink, uint, int> action = (ReceivingAmqpLink source, uint id, int count) => {
				};
				SequenceNumber deliveryId = amqpMessage.DeliveryId;
				MessagingClientEtwProvider.TraceClient<ReceivingAmqpLink, uint, int>(action, this, deliveryId.Value, amqpMessage.RawByteBuffers.Count);
				this.OnReceiveMessage(amqpMessage);
			}
		}

		private void OnReceiveMessage(AmqpMessage message)
		{
			if (this.messageListener != null)
			{
				this.messageListener(message);
				return;
			}
			ReceivingAmqpLink.ReceiveAsyncResult value = null;
			int num = 0;
			bool flag = false;
			lock (this.syncRoot)
			{
				if (this.waiterList != null && this.waiterList.Count > 0)
				{
					value = this.waiterList.First.Value;
					this.waiterList.RemoveFirst();
					value.OnRemoved();
					num = (base.Settings.AutoSendFlow ? 0 : this.GetOnDemandReceiveCredit());
				}
				else if (!base.Settings.AutoSendFlow && base.Settings.SettleType != SettleMode.SettleOnSend)
				{
					flag = true;
				}
				else if (this.messageQueue != null)
				{
					this.messageQueue.Enqueue(message);
				}
			}
			if (flag)
			{
				this.ReleaseMessage(message);
				message.Dispose();
			}
			if (num > 0)
			{
				base.IssueCredit((uint)num, false, AmqpConstants.NullBinary);
			}
			if (value != null)
			{
				Action<object> action = (object o) => {
					Tuple<ReceivingAmqpLink.ReceiveAsyncResult, IEnumerable<AmqpMessage>> tuple = (Tuple<ReceivingAmqpLink.ReceiveAsyncResult, IEnumerable<AmqpMessage>>)o;
					tuple.Item1.Signal(tuple.Item2, false);
				};
				AmqpMessage[] amqpMessageArray = new AmqpMessage[] { message };
				ActionItem.Schedule(action, new Tuple<ReceivingAmqpLink.ReceiveAsyncResult, IEnumerable<AmqpMessage>>(value, amqpMessageArray));
			}
		}

		protected override bool OpenInternal()
		{
			this.messageQueue = new Queue<AmqpMessage>();
			this.waiterList = new LinkedList<ReceivingAmqpLink.ReceiveAsyncResult>();
			this.pendingDispositions = new WorkCollection<ArraySegment<byte>, ReceivingAmqpLink.DisposeAsyncResult, Outcome>(ByteArrayComparer.Instance);
			bool flag = base.OpenInternal();
			if (base.LinkCredit > 0)
			{
				base.SendFlow(false);
			}
			return flag;
		}

		public void RegisterMessageListener(Action<AmqpMessage> messageListener)
		{
			if (Interlocked.Exchange<Action<AmqpMessage>>(ref this.messageListener, messageListener) != null)
			{
				throw new InvalidOperationException(SRClient.MessageListenerAlreadyRegistered);
			}
		}

		public void RejectMessage(AmqpMessage message, Exception exception)
		{
			Rejected rejected = new Rejected()
			{
				Error = AmqpError.FromException(exception, true)
			};
			this.DisposeMessage(message, rejected, true, false);
		}

		public void ReleaseMessage(AmqpMessage message)
		{
			this.DisposeMessage(message, AmqpConstants.ReleasedOutcome, true, false);
		}

		private sealed class DisposeAsyncResult : AsyncResult, IWork<Outcome>
		{
			private readonly ReceivingAmqpLink link;

			private readonly ArraySegment<byte> deliveryTag;

			private readonly bool batchable;

			private Outcome outcome;

			public DisposeAsyncResult(ReceivingAmqpLink link, ArraySegment<byte> deliveryTag, Outcome outcome, bool batchable, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				this.link = link;
				this.deliveryTag = deliveryTag;
				this.batchable = batchable;
				this.outcome = outcome;
				this.link.pendingDispositions.StartWork(deliveryTag, this);
			}

			public void Cancel(bool completedSynchronously, Exception exception)
			{
				base.Complete(completedSynchronously, exception);
			}

			public void Done(bool completedSynchronously, Outcome outcome)
			{
				this.outcome = outcome;
				base.Complete(completedSynchronously);
			}

			public static new Outcome End(IAsyncResult result)
			{
				return AsyncResult.End<ReceivingAmqpLink.DisposeAsyncResult>(result).outcome;
			}

			public void Start()
			{
				if (!this.link.DisposeDelivery(this.deliveryTag, false, this.outcome, this.batchable))
				{
					WorkCollection<ArraySegment<byte>, ReceivingAmqpLink.DisposeAsyncResult, Outcome> workCollection = this.link.pendingDispositions;
					ArraySegment<byte> nums = this.deliveryTag;
					Rejected rejected = new Rejected()
					{
						Error = AmqpError.NotFound
					};
					workCollection.CompleteWork(nums, true, rejected);
				}
			}
		}

		private sealed class ReceiveAsyncResult : AsyncResult
		{
			private static Action<object> onTimer;

			private readonly ReceivingAmqpLink parent;

			private readonly TimeSpan timeout;

			private IOThreadTimer timer;

			private LinkedListNode<ReceivingAmqpLink.ReceiveAsyncResult> node;

			private int completed;

			private IEnumerable<AmqpMessage> messages;

			static ReceiveAsyncResult()
			{
				ReceivingAmqpLink.ReceiveAsyncResult.onTimer = new Action<object>(ReceivingAmqpLink.ReceiveAsyncResult.OnTimer);
			}

			public ReceiveAsyncResult(ReceivingAmqpLink parent, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				this.parent = parent;
				this.timeout = timeout;
			}

			public void Cancel()
			{
				this.Signal(null, false, new OperationCanceledException());
			}

			private void CompleteInternal(IEnumerable<AmqpMessage> messages, bool syncComplete, int code, Exception exception)
			{
				if (Interlocked.CompareExchange(ref this.completed, code, 0) == 0)
				{
					this.messages = messages;
					if (messages == null)
					{
						this.messages = Enumerable.Empty<AmqpMessage>();
					}
					if (exception != null)
					{
						base.Complete(syncComplete, exception);
						return;
					}
					base.Complete(syncComplete);
				}
			}

			public static bool End(IAsyncResult result, out IEnumerable<AmqpMessage> messages)
			{
				ReceivingAmqpLink.ReceiveAsyncResult receiveAsyncResult = AsyncResult.End<ReceivingAmqpLink.ReceiveAsyncResult>(result);
				messages = receiveAsyncResult.messages;
				return receiveAsyncResult.completed == 1;
			}

			public void Initialize(LinkedListNode<ReceivingAmqpLink.ReceiveAsyncResult> node)
			{
				this.node = node;
				if (this.timeout != TimeSpan.MaxValue)
				{
					this.timer = new IOThreadTimer(ReceivingAmqpLink.ReceiveAsyncResult.onTimer, this, false);
					this.timer.Set(this.timeout);
				}
			}

			public void OnRemoved()
			{
				this.node = null;
			}

			private static void OnTimer(object state)
			{
				ReceivingAmqpLink.ReceiveAsyncResult receiveAsyncResult = (ReceivingAmqpLink.ReceiveAsyncResult)state;
				lock (receiveAsyncResult.parent.syncRoot)
				{
					if (receiveAsyncResult.parent.waiterList == null || receiveAsyncResult.node == null)
					{
						return;
					}
					else
					{
						receiveAsyncResult.parent.waiterList.Remove(receiveAsyncResult.node);
						receiveAsyncResult.node = null;
					}
				}
				receiveAsyncResult.CompleteInternal(null, false, 2, null);
			}

			public void Signal(IEnumerable<AmqpMessage> messages, bool syncComplete)
			{
				this.Signal(messages, syncComplete, null);
			}

			public void Signal(IEnumerable<AmqpMessage> messages, bool syncComplete, Exception exception)
			{
				IOThreadTimer oThreadTimer = this.timer;
				if (oThreadTimer != null)
				{
					oThreadTimer.Cancel();
				}
				this.CompleteInternal(messages, syncComplete, 1, exception);
			}
		}
	}
}