using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using Microsoft.ServiceBus.Messaging.Amqp.Transaction;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal abstract class AmqpLink : AmqpObject, IWorkDelegate<Delivery>
	{
		private readonly AmqpLinkSettings settings;

		private readonly Outcome defaultOutcome;

		private readonly Dictionary<ArraySegment<byte>, Delivery> unsettledMap;

		private readonly SerializedWorker<Delivery> inflightDeliveries;

		private readonly object syncRoot;

		private SequenceNumber deliveryCount;

		private uint available;

		private uint linkCredit;

		private bool drain;

		private uint needFlowCount;

		private int sendingFlow;

		private uint? tempTotalCredit;

		private uint bufferedCredit;

		public virtual uint Available
		{
			get
			{
				return this.available;
			}
		}

		public bool IsReceiver
		{
			get
			{
				return this.settings.Role.Value;
			}
		}

		public uint LinkCredit
		{
			get
			{
				return this.linkCredit;
			}
		}

		public uint? LocalHandle
		{
			get
			{
				return this.settings.Handle;
			}
			set
			{
				this.settings.Handle = value;
			}
		}

		public uint MaxFrameSize
		{
			get;
			set;
		}

		public string Name
		{
			get
			{
				return this.settings.LinkName;
			}
		}

		public uint? RemoteHandle
		{
			get;
			set;
		}

		public AmqpSession Session
		{
			get;
			private set;
		}

		public AmqpLinkSettings Settings
		{
			get
			{
				return this.settings;
			}
		}

		protected AmqpLink(AmqpSession session, AmqpLinkSettings linkSettings) : this("link", session, linkSettings)
		{
			this.inflightDeliveries = new SerializedWorker<Delivery>(this);
		}

		protected AmqpLink(string type, AmqpSession session, AmqpLinkSettings linkSettings) : base(type)
		{
			if (linkSettings == null)
			{
				throw new ArgumentNullException("linkSettings");
			}
			this.settings = linkSettings;
			this.linkCredit = this.settings.TotalLinkCredit;
			this.syncRoot = new object();
			Source source = (Source)this.settings.Source;
			if (source != null)
			{
				this.defaultOutcome = source.DefaultOutcome;
			}
			if (this.defaultOutcome == null)
			{
				this.defaultOutcome = AmqpConstants.ReleasedOutcome;
			}
			this.unsettledMap = new Dictionary<ArraySegment<byte>, Delivery>(ByteArrayComparer.Instance);
			if (session != null)
			{
				this.AttachTo(session);
			}
		}

		protected override void AbortInternal()
		{
			if (this.inflightDeliveries != null)
			{
				this.inflightDeliveries.Abort();
			}
		}

		private bool ApplyTempTotalLinkCredit()
		{
			if (!this.tempTotalCredit.HasValue || this.tempTotalCredit.Value == this.settings.TotalLinkCredit)
			{
				return false;
			}
			uint totalLinkCredit = this.settings.TotalLinkCredit;
			uint value = this.tempTotalCredit.Value;
			this.settings.TotalLinkCredit = value;
			this.tempTotalCredit = null;
			if (value == -1)
			{
				this.linkCredit = value;
				this.bufferedCredit = 0;
			}
			else if (value > totalLinkCredit)
			{
				uint num = value - totalLinkCredit;
				AmqpLink amqpLink = this;
				amqpLink.linkCredit = amqpLink.linkCredit + num;
				AmqpLink amqpLink1 = this;
				amqpLink1.bufferedCredit = amqpLink1.bufferedCredit - Math.Min(this.bufferedCredit, num);
			}
			else if (totalLinkCredit >= 2147483647)
			{
				this.linkCredit = value;
				this.bufferedCredit = 0;
			}
			else
			{
				uint num1 = totalLinkCredit - value;
				AmqpLink amqpLink2 = this;
				amqpLink2.linkCredit = amqpLink2.linkCredit - Math.Min(this.linkCredit, num1);
				AmqpLink amqpLink3 = this;
				amqpLink3.bufferedCredit = amqpLink3.bufferedCredit + num1;
			}
			return true;
		}

		public void AttachTo(AmqpSession session)
		{
			this.MaxFrameSize = session.Connection.Settings.MaxFrameSize();
			this.Session = session;
			session.AttachLink(this);
		}

		protected override bool CloseInternal()
		{
			if (base.State == AmqpObjectState.OpenReceived)
			{
				this.settings.Source = null;
				this.settings.Target = null;
				this.SendAttach();
			}
			if (this.inflightDeliveries != null)
			{
				this.inflightDeliveries.Abort();
			}
			this.Session.Flush();
			return this.SendDetach() == AmqpObjectState.End;
		}

		public void CompleteDelivery(ArraySegment<byte> deliveryTag)
		{
			Delivery delivery = null;
			if (this.unsettledMap.TryGetValue(deliveryTag, out delivery))
			{
				this.DisposeDelivery(delivery, true, delivery.State);
			}
		}

		public abstract bool CreateDelivery(out Delivery delivery);

		public void DisposeDelivery(Delivery delivery, bool settled, DeliveryState state)
		{
			this.DisposeDelivery(delivery, settled, state, false);
		}

		public void DisposeDelivery(Delivery delivery, bool settled, DeliveryState state, bool noFlush)
		{
			Action<AmqpLink, uint, bool, DeliveryState> action = (AmqpLink source, uint id, bool isSettled, DeliveryState detail) => {
			};
			SequenceNumber deliveryId = delivery.DeliveryId;
			MessagingClientEtwProvider.TraceClient<AmqpLink, uint, bool, DeliveryState>(action, this, deliveryId.Value, settled, state);
			if (settled && !delivery.Settled)
			{
				lock (this.syncRoot)
				{
					this.unsettledMap.Remove(delivery.DeliveryTag);
				}
			}
			this.Session.DisposeDelivery(this, delivery, settled, state, noFlush);
			if (delivery.Settled)
			{
				this.OnDeliverySettled();
			}
		}

		public bool DisposeDelivery(ArraySegment<byte> deliveryTag, bool settled, DeliveryState state, bool batchable)
		{
			Delivery delivery = null;
			if (this.unsettledMap.TryGetValue(deliveryTag, out delivery))
			{
				delivery.Batchable = batchable;
				this.DisposeDelivery(delivery, settled, state);
				return true;
			}
			MessagingClientEtwProvider.TraceClient<AmqpLink, string>((AmqpLink source, string tag) => MessagingClientEtwProvider.Provider.EventWriteAmqpDeliveryNotFound(source, tag), this, deliveryTag.GetString());
			return false;
		}

		private ArraySegment<byte> GetTxnIdFromFlow(Flow flow)
		{
			if (flow.Properties != null)
			{
				object item = flow.Properties["txn-id"];
				object obj = item;
				if (item != null)
				{
					return (ArraySegment<byte>)obj;
				}
			}
			return AmqpConstants.NullBinary;
		}

		public void IssueCredit(uint credit, bool drain, ArraySegment<byte> txnId)
		{
			if (!this.settings.AutoSendFlow)
			{
				lock (this.syncRoot)
				{
					this.settings.TotalLinkCredit = credit;
					this.linkCredit = credit;
				}
				this.SendFlow(false, drain, txnId);
			}
		}

		bool Microsoft.ServiceBus.Messaging.Amqp.IWorkDelegate<Microsoft.ServiceBus.Messaging.Amqp.Delivery>.Invoke(Delivery delivery)
		{
			return this.SendDelivery(delivery);
		}

		private Error Negotiate(Attach attach)
		{
			if (attach.MaxMessageSize() != (long)0)
			{
				this.settings.MaxMessageSize = new ulong?(Math.Min(this.settings.MaxMessageSize(), attach.MaxMessageSize()));
			}
			return null;
		}

		public void NotifySessionCredit(int credit)
		{
			if (this.inflightDeliveries != null)
			{
				this.inflightDeliveries.ContinueWork();
				return;
			}
			this.OnCreditAvailable(credit, 0, false, AmqpConstants.NullBinary);
		}

		protected abstract void OnCreditAvailable(int session, uint link, bool drain, ArraySegment<byte> txnId);

		private void OnDeliverySettled()
		{
			if (this.IsReceiver && this.settings.AutoSendFlow && this.linkCredit < -1)
			{
				bool flag = false;
				lock (this.syncRoot)
				{
					if (this.linkCredit < this.settings.TotalLinkCredit)
					{
						AmqpLink amqpLink = this;
						amqpLink.linkCredit = amqpLink.linkCredit + 1;
					}
					AmqpLink amqpLink1 = this;
					amqpLink1.needFlowCount = amqpLink1.needFlowCount + 1;
					if ((ulong)this.needFlowCount >= (long)this.settings.FlowThreshold)
					{
						flag = true;
						this.needFlowCount = 0;
					}
				}
				if (flag)
				{
					this.SendFlow(false, false, null);
				}
			}
		}

		public void OnDisposeDelivery(Delivery delivery)
		{
			if (delivery.Settled)
			{
				lock (this.syncRoot)
				{
					this.unsettledMap.Remove(delivery.DeliveryTag);
				}
				this.OnDeliverySettled();
			}
			this.OnDisposeDeliveryInternal(delivery);
			if (!this.IsReceiver && this.settings.SettleType != SettleMode.SettleOnDispose && !delivery.Settled && !delivery.Transactional())
			{
				this.CompleteDelivery(delivery.DeliveryTag);
			}
		}

		protected abstract void OnDisposeDeliveryInternal(Delivery delivery);

		public void OnFlow(Flow flow)
		{
			this.OnReceiveFlow(flow);
		}

		private void OnLinkOpenFailed(Exception exception)
		{
			if (base.State == AmqpObjectState.OpenReceived)
			{
				this.settings.Source = null;
				this.settings.Target = null;
				this.SendAttach();
			}
			base.SafeClose(exception);
		}

		protected abstract void OnProcessTransfer(Delivery delivery, Transfer transfer, Frame rawFrame);

		private void OnProviderLinkOpened(IAsyncResult result)
		{
			MessagingClientEtwProvider.TraceClient<string>((string source) => {
			}, this.Name);
			Exception exception = null;
			try
			{
				this.Session.LinkFactory.EndOpenLink(result);
			}
			catch (Exception exception2)
			{
				Exception exception1 = exception2;
				if (Fx.IsFatal(exception1))
				{
					throw;
				}
				MessagingClientEtwProvider.TraceClient<AmqpLink, Exception>((AmqpLink source, Exception ex) => MessagingClientEtwProvider.Provider.EventWriteAmqpLogError(source, "EndOpenLink", ex.Message), this, exception1);
				exception = exception1;
			}
			try
			{
				if (exception == null)
				{
					base.Open();
				}
				else
				{
					this.OnLinkOpenFailed(exception);
				}
			}
			catch (Exception exception4)
			{
				Exception exception3 = exception4;
				if (Fx.IsFatal(exception3))
				{
					throw;
				}
				MessagingClientEtwProvider.TraceClient<AmqpLink, Exception>((AmqpLink source, Exception ex) => MessagingClientEtwProvider.Provider.EventWriteAmqpLogError(source, "CompleteOpenLink", ex.Message), this, exception3);
				base.Abort();
			}
		}

		private void OnReceiveAttach(Attach attach)
		{
			StateTransition stateTransition = base.TransitState("R:ATTACH", StateTransition.ReceiveOpen);
			Error error = this.Negotiate(attach);
			if (error != null)
			{
				this.OnLinkOpenFailed(new AmqpException(error));
				return;
			}
			if (stateTransition.From == AmqpObjectState.OpenSent)
			{
				if (!this.IsReceiver)
				{
					Target target = this.settings.Target as Target;
					if (target != null && target.Dynamic())
					{
						target.Address = ((Target)attach.Target).Address;
					}
				}
				else
				{
					Source source = this.settings.Source as Source;
					if (source != null && source.Dynamic())
					{
						source.Address = ((Source)attach.Source).Address;
					}
				}
				if (attach.Properties != null)
				{
					if (this.Settings.Properties != null)
					{
						this.settings.Properties.Merge(attach.Properties);
					}
					else
					{
						this.settings.Properties = attach.Properties;
					}
				}
			}
			if (stateTransition.To != AmqpObjectState.Opened)
			{
				if (stateTransition.To == AmqpObjectState.OpenReceived)
				{
					try
					{
						this.Session.LinkFactory.BeginOpenLink(this, base.DefaultOpenTimeout, new AsyncCallback(this.OnProviderLinkOpened), null);
					}
					catch (Exception exception1)
					{
						Exception exception = exception1;
						if (Fx.IsFatal(exception))
						{
							throw;
						}
						this.OnLinkOpenFailed(exception);
					}
				}
				return;
			}
			if (this.IsReceiver && attach.Source == null || !this.IsReceiver && attach.Target == null)
			{
				return;
			}
			if (!this.IsReceiver)
			{
				this.settings.Target = attach.Target;
			}
			else
			{
				this.deliveryCount = attach.InitialDeliveryCount.Value;
				this.settings.Source = attach.Source;
			}
			this.settings.SettleType = attach.SettleType();
			base.CompleteOpen(false, null);
		}

		private void OnReceiveDetach(Detach detach)
		{
			base.OnReceiveCloseCommand("R:DETACH", detach.Error);
		}

		protected virtual void OnReceiveFlow(Flow flow)
		{
			this.ProcessFlow(flow);
		}

		private void OnReceiveTransfer(Transfer transfer, Frame rawFrame)
		{
			Delivery value = null;
			bool flag = this.CreateDelivery(out value);
			if (flag)
			{
				value.Link = this;
				value.DeliveryId = transfer.DeliveryId.Value;
				value.DeliveryTag = transfer.DeliveryTag;
				value.Settled = transfer.Settled();
				value.Batchable = transfer.Batchable();
				value.MessageFormat = transfer.MessageFormat;
				TransactionalState state = transfer.State as TransactionalState;
				if (state != null)
				{
					value.TxnId = state.TxnId;
				}
			}
			if (!this.Session.OnAcceptTransfer(value, transfer, flag))
			{
				return;
			}
			this.ProcessTransfer(transfer, rawFrame, value, flag);
		}

		protected override bool OpenInternal()
		{
			AmqpObjectState amqpObjectState = this.SendAttach();
			if (this.IsReceiver)
			{
				this.ApplyTempTotalLinkCredit();
			}
			return amqpObjectState == AmqpObjectState.Opened;
		}

		public void ProcessFlow(Flow flow)
		{
			uint valueOrDefault;
			uint num;
			uint valueOrDefault1;
			if (flow.Properties != null)
			{
				EventHandler eventHandler = this.PropertyReceived;
				if (eventHandler != null)
				{
					eventHandler(flow.Properties, EventArgs.Empty);
					return;
				}
			}
			uint num1 = 0;
			uint num2 = flow.LinkCredit();
			lock (this.syncRoot)
			{
				if (!this.IsReceiver)
				{
					bool? drain = flow.Drain;
					this.drain = (drain.HasValue ? drain.GetValueOrDefault() : false);
					if (num2 == -1)
					{
						this.linkCredit = -1;
						num1 = -1;
					}
					else if (this.linkCredit != -1)
					{
						uint? deliveryCount = flow.DeliveryCount;
						if (deliveryCount.HasValue)
						{
							valueOrDefault = deliveryCount.GetValueOrDefault();
						}
						else
						{
							valueOrDefault = 0;
						}
						SequenceNumber sequenceNumber = valueOrDefault + num2;
						SequenceNumber value = this.deliveryCount.Value + this.linkCredit;
						int num3 = sequenceNumber - value;
						if (num3 > 0)
						{
							AmqpLink amqpLink = this;
							amqpLink.linkCredit = amqpLink.linkCredit + num3;
							num1 = (uint)num3;
						}
						else if (num3 < 0)
						{
							uint num4 = (uint)(-num3);
							if (num4 > this.linkCredit)
							{
								num = 0;
							}
							else
							{
								num = this.linkCredit - num4;
							}
							this.linkCredit = num;
						}
					}
					else
					{
						this.linkCredit = num2;
						num1 = 0;
					}
				}
				else
				{
					uint? available = flow.Available;
					if (available.HasValue)
					{
						valueOrDefault1 = available.GetValueOrDefault();
					}
					else
					{
						valueOrDefault1 = -1;
					}
					this.available = valueOrDefault1;
					this.ApplyTempTotalLinkCredit();
				}
			}
			if (num1 > 0)
			{
				ArraySegment<byte> txnIdFromFlow = this.GetTxnIdFromFlow(flow);
				this.OnCreditAvailable(0, num1, this.drain, txnIdFromFlow);
			}
			if (flow.Echo())
			{
				this.SendFlow(false, false, null);
			}
		}

		public void ProcessFrame(Frame frame)
		{
			Performative command = frame.Command;
			try
			{
				if (command.DescriptorCode == Attach.Code)
				{
					this.OnReceiveAttach((Attach)command);
				}
				else if (command.DescriptorCode == Detach.Code)
				{
					this.OnReceiveDetach((Detach)command);
				}
				else if (command.DescriptorCode != Transfer.Code)
				{
					if (command.DescriptorCode != Flow.Code)
					{
						throw new AmqpException(AmqpError.InvalidField, SRAmqp.AmqpInvalidPerformativeCode(command.DescriptorCode));
					}
					this.OnReceiveFlow((Flow)command);
				}
				else
				{
					this.OnReceiveTransfer((Transfer)command, frame);
				}
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				MessagingClientEtwProvider.TraceClient<AmqpLink, Exception>((AmqpLink source, Exception ex) => MessagingClientEtwProvider.Provider.EventWriteAmqpLogError(source, "ProcessFrame", ex.Message), this, exception);
				base.SafeClose(exception);
			}
		}

		protected void ProcessTransfer(Transfer transfer, Frame rawFrame, Delivery delivery, bool newDelivery)
		{
			if (newDelivery)
			{
				bool flag = true;
				lock (this.syncRoot)
				{
					if (this.tempTotalCredit.HasValue && this.ApplyTempTotalLinkCredit())
					{
						this.SendFlow(false, false, null);
					}
					if (this.linkCredit != 0 || this.bufferedCredit != 0)
					{
						this.deliveryCount.Increment();
						if (this.bufferedCredit > 0)
						{
							AmqpLink amqpLink = this;
							amqpLink.bufferedCredit = amqpLink.bufferedCredit - 1;
						}
						else if (this.linkCredit < -1)
						{
							AmqpLink amqpLink1 = this;
							amqpLink1.linkCredit = amqpLink1.linkCredit - 1;
						}
					}
					else
					{
						flag = false;
					}
				}
				if (!flag)
				{
					Error transferLimitExceeded = AmqpError.TransferLimitExceeded;
					SequenceNumber deliveryId = delivery.DeliveryId;
					throw new AmqpException(transferLimitExceeded, SRAmqp.AmqpTransferLimitExceeded(deliveryId.Value));
				}
				if (!delivery.Settled)
				{
					lock (this.syncRoot)
					{
						this.unsettledMap.Add(delivery.DeliveryTag, delivery);
					}
				}
			}
			this.OnProcessTransfer(delivery, transfer, rawFrame);
		}

		private AmqpObjectState SendAttach()
		{
			StateTransition stateTransition = base.TransitState("S:ATTACH", StateTransition.SendOpen);
			this.Session.SendCommand(this.settings);
			return stateTransition.To;
		}

		private bool SendDelivery(Delivery delivery)
		{
			uint maxFrameSize;
			Delivery delivery1;
			uint valueOrDefault;
			bool flag = true;
			while (flag)
			{
				bool bytesTransfered = delivery.BytesTransfered == (long)0;
				Transfer transfer = new Transfer()
				{
					Handle = this.LocalHandle,
					More = new bool?(flag)
				};
				if (bytesTransfered)
				{
					transfer.DeliveryId = new uint?(-1);
					transfer.DeliveryTag = delivery.DeliveryTag;
					Transfer nullable = transfer;
					uint? messageFormat = delivery.MessageFormat;
					if (messageFormat.HasValue)
					{
						valueOrDefault = messageFormat.GetValueOrDefault();
					}
					else
					{
						valueOrDefault = 0;
					}
					nullable.MessageFormat = new uint?(valueOrDefault);
					transfer.Batchable = new bool?(delivery.Batchable);
					if (delivery.Settled)
					{
						transfer.Settled = new bool?(true);
					}
					if (delivery.TxnId.Array != null)
					{
						transfer.State = new TransactionalState()
						{
							TxnId = delivery.TxnId
						};
					}
				}
				if (this.MaxFrameSize == -1)
				{
					maxFrameSize = 65536;
				}
				else
				{
					maxFrameSize = this.MaxFrameSize;
				}
				uint num = maxFrameSize;
				int encodeSize = 8 + transfer.EncodeSize;
				if ((long)encodeSize > (ulong)num)
				{
					throw new AmqpException(AmqpError.FrameSizeTooSmall);
				}
				int count = (int)(num - encodeSize);
				ArraySegment<byte>[] payload = delivery.GetPayload(count, out flag);
				transfer.More = new bool?(flag);
				if (!bytesTransfered && payload == null)
				{
					break;
				}
				AmqpSession session = this.Session;
				if (bytesTransfered)
				{
					delivery1 = delivery;
				}
				else
				{
					delivery1 = null;
				}
				if (session.TrySendTransfer(delivery1, transfer, payload))
				{
					count = 0;
					for (int i = 0; i < (int)payload.Length; i++)
					{
						count = count + payload[i].Count;
					}
					delivery.CompletePayload(count);
				}
				else
				{
					flag = true;
					break;
				}
			}
			if (!flag && delivery.Settled)
			{
				delivery.State = AmqpConstants.AcceptedOutcome;
				this.OnDisposeDeliveryInternal(delivery);
			}
			return !flag;
		}

		private AmqpObjectState SendDetach()
		{
			StateTransition stateTransition = base.TransitState("S:DETACH", StateTransition.SendClose);
			Detach detach = new Detach()
			{
				Handle = this.LocalHandle,
				Closed = new bool?(true)
			};
			Exception terminalException = base.TerminalException;
			if (terminalException != null)
			{
				detach.Error = AmqpError.FromException(terminalException, true);
			}
			this.Session.SendCommand(detach);
			return stateTransition.To;
		}

		protected void SendFlow(bool echo)
		{
			this.SendFlow(echo, false, null);
		}

		private void SendFlow(bool echo, bool drain, ArraySegment<byte> txnId)
		{
			Fields field = null;
			if (txnId.Array != null)
			{
				field = new Fields();
				field["txn-id"] = txnId;
			}
			this.SendFlow(echo, drain, field);
		}

		private void SendFlow(bool echo, bool drain, Fields properties)
		{
			this.drain = drain;
			bool flag = (echo ? true : properties != null);
			if (!flag && Interlocked.Increment(ref this.sendingFlow) != 1)
			{
				return;
			}
			do
			{
				Flow flow = new Flow()
				{
					Handle = this.LocalHandle
				};
				lock (this.syncRoot)
				{
					flow.LinkCredit = new uint?(this.linkCredit);
					flow.Available = new uint?(this.Available);
					flow.DeliveryCount = new uint?(this.deliveryCount.Value);
					if (this.drain)
					{
						flow.Drain = new bool?(true);
					}
				}
				flow.Echo = new bool?(echo);
				flow.Properties = properties;
				if (base.IsClosing())
				{
					continue;
				}
				this.Session.SendFlow(flow);
			}
			while (!flag && Interlocked.Decrement(ref this.sendingFlow) > 0);
		}

		public void SendProperties(Fields properties)
		{
			this.SendFlow(false, false, properties);
		}

		public void SetTotalLinkCredit(uint totalCredit, bool applyNow, bool updateAutoFlow = false)
		{
			lock (this.syncRoot)
			{
				bool flag = (this.tempTotalCredit.HasValue || this.linkCredit != 0 ? false : totalCredit != 0);
				this.tempTotalCredit = new uint?(totalCredit);
				if (updateAutoFlow)
				{
					this.settings.AutoSendFlow = totalCredit != 0;
				}
				if ((applyNow || flag) && this.ApplyTempTotalLinkCredit() && base.State == AmqpObjectState.Opened)
				{
					this.SendFlow(false, false, null);
				}
			}
		}

		public bool TrySendDelivery(Delivery delivery)
		{
			bool settleType = this.settings.SettleType == SettleMode.SettleOnSend;
			bool flag = false;
			lock (this.syncRoot)
			{
				if (this.linkCredit > 0)
				{
					flag = true;
					this.deliveryCount.Increment();
					if (this.linkCredit != -1)
					{
						AmqpLink amqpLink = this;
						amqpLink.linkCredit = amqpLink.linkCredit - 1;
					}
				}
			}
			if (!flag)
			{
				MessagingClientEtwProvider.TraceClient<AmqpLink>((AmqpLink source) => {
				}, this);
				return false;
			}
			delivery.PrepareForSend();
			delivery.Settled = settleType;
			if (!delivery.Settled)
			{
				lock (this.syncRoot)
				{
					this.unsettledMap.Add(delivery.DeliveryTag, delivery);
				}
			}
			delivery.Link = this;
			this.inflightDeliveries.DoWork(delivery);
			return true;
		}

		internal event EventHandler PropertyReceived;
	}
}