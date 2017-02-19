using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal sealed class DuplexAmqpLink : AmqpObject
	{
		private readonly static AsyncCallback onSenderOpen;

		private readonly static AsyncCallback onReceiverOpen;

		private readonly static AsyncCallback onSenderClose;

		private readonly static AsyncCallback onReceiverClose;

		private readonly SendingAmqpLink sender;

		private readonly ReceivingAmqpLink receiver;

		internal SendingAmqpLink SendingLink
		{
			get
			{
				return this.sender;
			}
		}

		static DuplexAmqpLink()
		{
			DuplexAmqpLink.onSenderOpen = new AsyncCallback(DuplexAmqpLink.OnSenderOpen);
			DuplexAmqpLink.onReceiverOpen = new AsyncCallback(DuplexAmqpLink.OnReceiverOpen);
			DuplexAmqpLink.onSenderClose = new AsyncCallback(DuplexAmqpLink.OnSenderClose);
			DuplexAmqpLink.onReceiverClose = new AsyncCallback(DuplexAmqpLink.OnReceiverClose);
		}

		public DuplexAmqpLink(AmqpSession session, AmqpLinkSettings settings) : base("duplex")
		{
			MessagingClientEtwProvider.Provider.EventWriteAmqpLogOperation(this, TraceOperation.Create, "Create");
			AmqpLinkSettings amqpLinkSetting = new AmqpLinkSettings()
			{
				Role = new bool?(false),
				LinkName = string.Concat(settings.LinkName, ":out"),
				SettleType = settings.SettleType,
				Source = new Source(),
				TotalLinkCredit = settings.TotalLinkCredit,
				AutoSendFlow = settings.AutoSendFlow,
				Target = settings.Target,
				Properties = settings.Properties
			};
			this.sender = new SendingAmqpLink(session, amqpLinkSetting);
			AmqpLinkSettings amqpLinkSetting1 = new AmqpLinkSettings()
			{
				Role = new bool?(true),
				LinkName = string.Concat(settings.LinkName, ":in"),
				SettleType = settings.SettleType,
				Source = settings.Source,
				TotalLinkCredit = settings.TotalLinkCredit,
				AutoSendFlow = settings.AutoSendFlow,
				Target = new Target(),
				Properties = settings.Properties
			};
			AmqpLinkSettings amqpLinkSetting2 = amqpLinkSetting1;
			this.receiver = new ReceivingAmqpLink(session, amqpLinkSetting2);
			this.receiver.SetTotalLinkCredit(amqpLinkSetting2.TotalLinkCredit, true, false);
			this.sender.SafeAddClosed(new EventHandler(this.OnLinkClosed));
			this.receiver.SafeAddClosed(new EventHandler(this.OnLinkClosed));
		}

		public DuplexAmqpLink(SendingAmqpLink sender, ReceivingAmqpLink receiver) : base("duplex")
		{
			MessagingClientEtwProvider.Provider.EventWriteAmqpLogOperation(this, TraceOperation.Create, "Create");
			this.sender = sender;
			this.receiver = receiver;
			this.sender.SafeAddClosed(new EventHandler(this.OnLinkClosed));
			this.receiver.SafeAddClosed(new EventHandler(this.OnLinkClosed));
		}

		protected override void AbortInternal()
		{
			this.sender.Abort();
			this.receiver.Abort();
		}

		protected override bool CloseInternal()
		{
			if (base.TerminalException != null)
			{
				this.sender.SafeClose(base.TerminalException);
				this.receiver.SafeClose(base.TerminalException);
				return true;
			}
			IAsyncResult asyncResult = this.sender.BeginClose(base.DefaultCloseTimeout, DuplexAmqpLink.onSenderClose, this);
			IAsyncResult asyncResult1 = this.receiver.BeginClose(base.DefaultCloseTimeout, DuplexAmqpLink.onReceiverClose, this);
			if (!asyncResult.CompletedSynchronously)
			{
				return false;
			}
			return asyncResult1.CompletedSynchronously;
		}

		public void DisposeMessage(AmqpMessage message, DeliveryState deliveryState, bool settled, bool batchable)
		{
			this.receiver.DisposeMessage(message, deliveryState, settled, batchable);
		}

		private void OnLinkClosed(object closedObject, EventArgs e)
		{
			base.SafeClose(((AmqpObject)closedObject).TerminalException);
		}

		private void OnOperationComplete(AmqpObject link, IAsyncResult result, bool isOpen)
		{
			Exception exception = null;
			try
			{
				if (!isOpen)
				{
					link.EndClose(result);
				}
				else
				{
					link.EndOpen(result);
				}
			}
			catch (Exception exception2)
			{
				Exception exception1 = exception2;
				if (Fx.IsFatal(exception1))
				{
					throw;
				}
				exception = exception1;
			}
			bool flag = true;
			if (exception == null)
			{
				AmqpObjectState amqpObjectState = (isOpen ? AmqpObjectState.OpenSent : AmqpObjectState.CloseSent);
				lock (base.ThisLock)
				{
					flag = (this.sender.State == amqpObjectState ? false : this.receiver.State != amqpObjectState);
				}
			}
			if (flag)
			{
				if (isOpen)
				{
					base.CompleteOpen(false, exception);
					return;
				}
				base.CompleteClose(false, exception);
			}
		}

		private static void OnReceiverClose(IAsyncResult result)
		{
			DuplexAmqpLink asyncState = (DuplexAmqpLink)result.AsyncState;
			asyncState.OnOperationComplete(asyncState.receiver, result, false);
		}

		private static void OnReceiverOpen(IAsyncResult result)
		{
			DuplexAmqpLink asyncState = (DuplexAmqpLink)result.AsyncState;
			asyncState.OnOperationComplete(asyncState.receiver, result, true);
		}

		private static void OnSenderClose(IAsyncResult result)
		{
			DuplexAmqpLink asyncState = (DuplexAmqpLink)result.AsyncState;
			asyncState.OnOperationComplete(asyncState.sender, result, false);
		}

		private static void OnSenderOpen(IAsyncResult result)
		{
			DuplexAmqpLink asyncState = (DuplexAmqpLink)result.AsyncState;
			asyncState.OnOperationComplete(asyncState.sender, result, true);
		}

		protected override bool OpenInternal()
		{
			IAsyncResult asyncResult = this.sender.BeginOpen(base.DefaultOpenTimeout, DuplexAmqpLink.onSenderOpen, this);
			IAsyncResult asyncResult1 = this.receiver.BeginOpen(base.DefaultOpenTimeout, DuplexAmqpLink.onReceiverOpen, this);
			if (!asyncResult.CompletedSynchronously)
			{
				return false;
			}
			return asyncResult1.CompletedSynchronously;
		}

		public void RegisterMessageListener(Action<AmqpMessage> messageListener)
		{
			this.receiver.RegisterMessageListener(messageListener);
		}

		public Task<Outcome> SendMessageAsync(AmqpMessage message, ArraySegment<byte> deliveryTag, ArraySegment<byte> txnId, TimeSpan timeout)
		{
			return this.sender.SendMessageAsync(message, deliveryTag, txnId, timeout);
		}

		internal void SendProperties(Fields fields)
		{
			this.receiver.SendProperties(fields);
			this.sender.SendProperties(fields);
		}
	}
}