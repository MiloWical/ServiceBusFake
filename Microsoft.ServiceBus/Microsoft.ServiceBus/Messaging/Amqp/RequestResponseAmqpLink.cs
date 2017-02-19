using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal class RequestResponseAmqpLink : AmqpObject
	{
		private readonly static TimeSpan OperationTimeout;

		private readonly static AsyncCallback onSenderOpen;

		private readonly static AsyncCallback onReceiverOpen;

		private readonly static AsyncCallback onSenderClose;

		private readonly static AsyncCallback onReceiverClose;

		private readonly Address replyTo;

		private readonly SendingAmqpLink sender;

		private readonly ReceivingAmqpLink receiver;

		private readonly WorkCollection<MessageId, RequestResponseAmqpLink.RequestAsyncResult, AmqpMessage> inflightRequests;

		private long nextRequestId;

		public string Name
		{
			get;
			private set;
		}

		public AmqpSession Session
		{
			get
			{
				return this.sender.Session;
			}
		}

		static RequestResponseAmqpLink()
		{
			RequestResponseAmqpLink.OperationTimeout = TimeSpan.FromSeconds(60);
			RequestResponseAmqpLink.onSenderOpen = new AsyncCallback(RequestResponseAmqpLink.OnSenderOpen);
			RequestResponseAmqpLink.onReceiverOpen = new AsyncCallback(RequestResponseAmqpLink.OnReceiverOpen);
			RequestResponseAmqpLink.onSenderClose = new AsyncCallback(RequestResponseAmqpLink.OnSenderClose);
			RequestResponseAmqpLink.onReceiverClose = new AsyncCallback(RequestResponseAmqpLink.OnReceiverClose);
		}

		public RequestResponseAmqpLink(AmqpLinkSettings settings) : this(null, settings)
		{
		}

		public RequestResponseAmqpLink(AmqpSession session, AmqpLinkSettings settings) : base("requestresponseamqplink")
		{
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] identifier = new object[] { session.Connection.Identifier, session.Identifier, base.Identifier };
			this.Name = string.Format(invariantCulture, "{0}:{1}:{2}", identifier);
			Source source = (Source)settings.Source;
			string str = source.Address.ToString();
			Guid guid = Guid.NewGuid();
			source.Address = string.Concat(str, "/", guid.ToString("N"));
			this.replyTo = source.Address;
			AmqpLinkSettings amqpLinkSetting = new AmqpLinkSettings()
			{
				Role = new bool?(false),
				LinkName = string.Concat(this.Name, ":sender"),
				SettleType = settings.SettleType,
				Source = new Source(),
				Target = settings.Target,
				Properties = settings.Properties
			};
			this.sender = new SendingAmqpLink(session, amqpLinkSetting);
			this.sender.Closed += new EventHandler(this.OnLinkClosed);
			AmqpLinkSettings amqpLinkSetting1 = new AmqpLinkSettings()
			{
				Role = new bool?(true),
				LinkName = string.Concat(this.Name, ":receiver"),
				SettleType = settings.SettleType,
				Source = settings.Source,
				TotalLinkCredit = settings.TotalLinkCredit,
				AutoSendFlow = settings.AutoSendFlow,
				Target = new Target(),
				Properties = settings.Properties
			};
			this.receiver = new ReceivingAmqpLink(session, amqpLinkSetting1);
			this.receiver.SetTotalLinkCredit(amqpLinkSetting1.TotalLinkCredit, true, false);
			this.receiver.RegisterMessageListener(new Action<AmqpMessage>(this.OnResponseMessage));
			this.receiver.Closed += new EventHandler(this.OnLinkClosed);
			this.inflightRequests = new WorkCollection<MessageId, RequestResponseAmqpLink.RequestAsyncResult, AmqpMessage>();
		}

		protected override void AbortInternal()
		{
			this.sender.Abort();
			this.receiver.Abort();
		}

		public IAsyncResult BeginRequest(AmqpMessage request, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new RequestResponseAmqpLink.RequestAsyncResult(this, request, timeout, callback, state);
		}

		protected override bool CloseInternal()
		{
			IAsyncResult asyncResult = this.sender.BeginClose(RequestResponseAmqpLink.OperationTimeout, RequestResponseAmqpLink.onSenderClose, this);
			IAsyncResult asyncResult1 = this.receiver.BeginClose(RequestResponseAmqpLink.OperationTimeout, RequestResponseAmqpLink.onReceiverClose, this);
			if (!asyncResult.CompletedSynchronously)
			{
				return false;
			}
			return asyncResult1.CompletedSynchronously;
		}

		public AmqpMessage EndRequest(IAsyncResult result)
		{
			return RequestResponseAmqpLink.RequestAsyncResult.End(result);
		}

		private void OnLinkClosed(object sender, EventArgs e)
		{
			base.SafeClose();
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
			RequestResponseAmqpLink asyncState = (RequestResponseAmqpLink)result.AsyncState;
			asyncState.OnOperationComplete(asyncState.receiver, result, false);
		}

		private static void OnReceiverOpen(IAsyncResult result)
		{
			RequestResponseAmqpLink asyncState = (RequestResponseAmqpLink)result.AsyncState;
			asyncState.OnOperationComplete(asyncState.receiver, result, true);
		}

		private void OnResponseMessage(AmqpMessage response)
		{
			this.receiver.DisposeDelivery(response, true, AmqpConstants.AcceptedOutcome);
			if (response.Properties != null && response.Properties.CorrelationId != null)
			{
				this.inflightRequests.CompleteWork(response.Properties.CorrelationId, false, response);
			}
		}

		private static void OnSenderClose(IAsyncResult result)
		{
			RequestResponseAmqpLink asyncState = (RequestResponseAmqpLink)result.AsyncState;
			asyncState.OnOperationComplete(asyncState.sender, result, false);
		}

		private static void OnSenderOpen(IAsyncResult result)
		{
			RequestResponseAmqpLink asyncState = (RequestResponseAmqpLink)result.AsyncState;
			asyncState.OnOperationComplete(asyncState.sender, result, true);
		}

		protected override bool OpenInternal()
		{
			IAsyncResult asyncResult = this.sender.BeginOpen(RequestResponseAmqpLink.OperationTimeout, RequestResponseAmqpLink.onSenderOpen, this);
			IAsyncResult asyncResult1 = this.receiver.BeginOpen(RequestResponseAmqpLink.OperationTimeout, RequestResponseAmqpLink.onReceiverOpen, this);
			if (!asyncResult.CompletedSynchronously)
			{
				return false;
			}
			return asyncResult1.CompletedSynchronously;
		}

		public void SendProperties(Fields fields)
		{
			this.receiver.SendProperties(fields);
			this.sender.SendProperties(fields);
		}

		private sealed class RequestAsyncResult : TimeoutAsyncResult<RequestResponseAmqpLink>, IWork<AmqpMessage>
		{
			private readonly RequestResponseAmqpLink parent;

			private readonly MessageId requestId;

			private AmqpMessage request;

			private AmqpMessage response;

			protected override RequestResponseAmqpLink Target
			{
				get
				{
					return this.parent;
				}
			}

			public RequestAsyncResult(RequestResponseAmqpLink parent, AmqpMessage request, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.parent = parent;
				this.request = request;
				this.requestId = Interlocked.Increment(ref this.parent.nextRequestId);
				this.request.Properties.MessageId = this.requestId;
				this.request.Properties.ReplyTo = this.parent.replyTo;
				this.parent.inflightRequests.StartWork(this.requestId, this);
			}

			public void Cancel(bool completedSynchronously, Exception exception)
			{
				base.CompleteSelf(completedSynchronously, exception);
			}

			protected override void CompleteOnTimer()
			{
				RequestResponseAmqpLink.RequestAsyncResult requestAsyncResult;
				if (this.parent.inflightRequests.TryRemoveWork(this.requestId, out requestAsyncResult))
				{
					base.CompleteOnTimer();
				}
			}

			public void Done(bool completedSynchronously, AmqpMessage response)
			{
				this.response = response;
				base.CompleteSelf(completedSynchronously);
			}

			public static new AmqpMessage End(IAsyncResult result)
			{
				return AsyncResult.End<RequestResponseAmqpLink.RequestAsyncResult>(result).response;
			}

			public void Start()
			{
				this.parent.sender.SendMessageNoWait(this.request, AmqpConstants.EmptyBinary, AmqpConstants.NullBinary);
				this.request = null;
			}
		}
	}
}