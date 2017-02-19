using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using Microsoft.ServiceBus.Messaging.Amqp.Transaction;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal sealed class SendingAmqpLink : AmqpLink, IWorkDelegate<AmqpMessage>
	{
		private readonly static TimeSpan MinRequestCreditWindow;

		private readonly static Action<object> onRequestCredit;

		private readonly SerializedWorker<AmqpMessage> pendingDeliveries;

		private readonly WorkCollection<ArraySegment<byte>, SendingAmqpLink.SendAsyncResult, Outcome> inflightSends;

		private Action<Delivery> dispositionListener;

		private Action<uint, bool, ArraySegment<byte>> creditListener;

		private DateTime lastFlowRequestTime;

		public override uint Available
		{
			get
			{
				return (uint)this.pendingDeliveries.Count;
			}
		}

		static SendingAmqpLink()
		{
			SendingAmqpLink.MinRequestCreditWindow = TimeSpan.FromSeconds(10);
			SendingAmqpLink.onRequestCredit = new Action<object>(SendingAmqpLink.OnRequestCredit);
		}

		public SendingAmqpLink(AmqpLinkSettings settings) : this(null, settings)
		{
		}

		public SendingAmqpLink(AmqpSession session, AmqpLinkSettings settings) : base(session, settings)
		{
			this.pendingDeliveries = new SerializedWorker<AmqpMessage>(this);
			this.inflightSends = new WorkCollection<ArraySegment<byte>, SendingAmqpLink.SendAsyncResult, Outcome>(ByteArrayComparer.Instance);
			this.lastFlowRequestTime = DateTime.UtcNow;
		}

		protected override void AbortInternal()
		{
			this.pendingDeliveries.Abort();
			this.inflightSends.Abort();
			base.AbortInternal();
		}

		public IAsyncResult BeginSendMessage(AmqpMessage message, ArraySegment<byte> deliveryTag, ArraySegment<byte> txnId, TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (this.dispositionListener != null)
			{
				throw new InvalidOperationException(SRClient.DispositionListenerSetNotSupported);
			}
			return new SendingAmqpLink.SendAsyncResult(this, message, deliveryTag, txnId, timeout, callback, state);
		}

		protected override bool CloseInternal()
		{
			this.pendingDeliveries.Abort();
			this.inflightSends.Abort();
			return base.CloseInternal();
		}

		public override bool CreateDelivery(out Delivery delivery)
		{
			delivery = null;
			throw new NotImplementedException();
		}

		public Outcome EndSendMessage(IAsyncResult result)
		{
			return SendingAmqpLink.SendAsyncResult.End(result);
		}

		bool Microsoft.ServiceBus.Messaging.Amqp.IWorkDelegate<Microsoft.ServiceBus.Messaging.Amqp.AmqpMessage>.Invoke(AmqpMessage message)
		{
			bool flag = base.TrySendDelivery(message);
			if (!flag && base.Session.State == AmqpObjectState.Opened && (DateTime.UtcNow - this.lastFlowRequestTime) >= SendingAmqpLink.MinRequestCreditWindow)
			{
				this.lastFlowRequestTime = DateTime.UtcNow;
				ActionItem.Schedule(SendingAmqpLink.onRequestCredit, this);
			}
			return flag;
		}

		protected override void OnCreditAvailable(int session, uint link, bool drain, ArraySegment<byte> txnId)
		{
			if (base.LinkCredit > 0)
			{
				this.pendingDeliveries.ContinueWork();
			}
			if (base.LinkCredit > 0 && this.creditListener != null)
			{
				this.creditListener(link, drain, txnId);
			}
		}

		protected override void OnDisposeDeliveryInternal(Delivery delivery)
		{
			if (this.dispositionListener != null)
			{
				this.dispositionListener(delivery);
				return;
			}
			DeliveryState state = delivery.State;
			if (state.DescriptorCode == Received.Code)
			{
				return;
			}
			TransactionalState transactionalState = state as TransactionalState;
			if (transactionalState != null)
			{
				state = transactionalState.Outcome;
			}
			this.inflightSends.CompleteWork(delivery.DeliveryTag, false, (Outcome)state);
		}

		protected override void OnProcessTransfer(Delivery delivery, Transfer transfer, Frame frame)
		{
			throw new AmqpException(AmqpError.NotAllowed);
		}

		private static void OnRequestCredit(object state)
		{
			try
			{
				SendingAmqpLink sendingAmqpLink = (SendingAmqpLink)state;
				if (sendingAmqpLink.State == AmqpObjectState.OpenSent || sendingAmqpLink.State == AmqpObjectState.Opened)
				{
					sendingAmqpLink.SendFlow(true);
				}
			}
			catch (Exception exception)
			{
				if (Fx.IsFatal(exception))
				{
					throw;
				}
			}
		}

		public void RegisterCreditListener(Action<uint, bool, ArraySegment<byte>> creditListener)
		{
			if (Interlocked.Exchange<Action<uint, bool, ArraySegment<byte>>>(ref this.creditListener, creditListener) != null)
			{
				throw new InvalidOperationException(SRClient.CreditListenerAlreadyRegistered);
			}
		}

		public void RegisterDispositionListener(Action<Delivery> dispositionListener)
		{
			if (Interlocked.Exchange<Action<Delivery>>(ref this.dispositionListener, dispositionListener) != null)
			{
				throw new InvalidOperationException(SRClient.DispositionListenerAlreadyRegistered);
			}
		}

		internal Task<Outcome> SendMessageAsync(AmqpMessage message, ArraySegment<byte> deliveryTag, ArraySegment<byte> txnId, TimeSpan timeout)
		{
			return Task.Factory.FromAsync<Outcome>((AsyncCallback c, object s) => this.BeginSendMessage(message, deliveryTag, txnId, timeout, c, s), new Func<IAsyncResult, Outcome>(this.EndSendMessage), null);
		}

		private void SendMessageInternal(AmqpMessage message, ArraySegment<byte> deliveryTag, ArraySegment<byte> txnId)
		{
			message.DeliveryTag = deliveryTag;
			message.Settled = base.Settings.SettleType == SettleMode.SettleOnSend;
			message.TxnId = txnId;
			this.pendingDeliveries.DoWork(message);
		}

		public void SendMessageNoWait(AmqpMessage message, ArraySegment<byte> deliveryTag, ArraySegment<byte> txnId)
		{
			this.SendMessageInternal(message, deliveryTag, txnId);
		}

		private sealed class SendAsyncResult : AsyncResult, IWork<Outcome>
		{
			private readonly SendingAmqpLink link;

			private readonly AmqpMessage message;

			private readonly ArraySegment<byte> deliveryTag;

			private readonly ArraySegment<byte> txnId;

			private Outcome outcome;

			public SendAsyncResult(SendingAmqpLink link, AmqpMessage message, ArraySegment<byte> deliveryTag, ArraySegment<byte> txnId, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				this.link = link;
				this.message = message;
				this.deliveryTag = deliveryTag;
				this.txnId = txnId;
				this.link.inflightSends.StartWork(deliveryTag, this);
			}

			public void Cancel(bool completedSynchronously, Exception exception)
			{
				Exception operationCanceledException = exception;
				if (exception is OperationCanceledException && this.link.TerminalException != null)
				{
					operationCanceledException = new OperationCanceledException(this.link.TerminalException.Message);
				}
				base.Complete(completedSynchronously, operationCanceledException);
			}

			public void Done(bool completedSynchronously, Outcome outcome)
			{
				this.outcome = outcome;
				base.Complete(completedSynchronously);
			}

			public static new Outcome End(IAsyncResult result)
			{
				return AsyncResult.End<SendingAmqpLink.SendAsyncResult>(result).outcome;
			}

			public void Start()
			{
				this.link.SendMessageInternal(this.message, this.deliveryTag, this.txnId);
			}
		}
	}
}