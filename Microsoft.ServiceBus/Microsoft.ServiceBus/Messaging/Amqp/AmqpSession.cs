using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal class AmqpSession : AmqpObject
	{
		private readonly AmqpConnection connection;

		private readonly AmqpSessionSettings settings;

		private readonly ILinkFactory linkFactory;

		private Dictionary<string, AmqpLink> links;

		private HandleTable<AmqpLink> linksByLocalHandle;

		private HandleTable<AmqpLink> linksByRemoteHandle;

		private AmqpSession.OutgoingSessionChannel outgoingChannel;

		private AmqpSession.IncomingSessionChannel incomingChannel;

		private ushort cachedRemoteChannel;

		private bool upgraded;

		public ushort CachedRemoteChannel
		{
			get
			{
				return this.cachedRemoteChannel;
			}
		}

		public AmqpConnection Connection
		{
			get
			{
				return this.connection;
			}
		}

		public ILinkFactory LinkFactory
		{
			get
			{
				return this.linkFactory;
			}
		}

		protected Dictionary<string, AmqpLink> Links
		{
			get
			{
				return this.links;
			}
		}

		protected HandleTable<AmqpLink> LinksByRemoteHandle
		{
			get
			{
				return this.linksByRemoteHandle;
			}
		}

		public ushort LocalChannel
		{
			get;
			set;
		}

		public ushort? RemoteChannel
		{
			get
			{
				return this.settings.RemoteChannel;
			}
			set
			{
				int? nullable;
				this.settings.RemoteChannel = value;
				ushort? remoteChannel = this.settings.RemoteChannel;
				if (remoteChannel.HasValue)
				{
					nullable = new int?((int)remoteChannel.GetValueOrDefault());
				}
				else
				{
					nullable = null;
				}
				if (nullable.HasValue)
				{
					this.cachedRemoteChannel = this.settings.RemoteChannel.Value;
				}
			}
		}

		public AmqpSessionSettings Settings
		{
			get
			{
				return this.settings;
			}
		}

		protected bool Upgraded
		{
			get
			{
				return this.upgraded;
			}
		}

		public AmqpSession(AmqpConnection connection, AmqpSessionSettings settings, ILinkFactory linkFactory) : base("session")
		{
			uint valueOrDefault;
			uint num;
			this.connection = connection;
			this.settings = settings;
			this.linkFactory = linkFactory;
			base.State = AmqpObjectState.Start;
			this.links = new Dictionary<string, AmqpLink>();
			uint? handleMax = settings.HandleMax;
			if (handleMax.HasValue)
			{
				valueOrDefault = handleMax.GetValueOrDefault();
			}
			else
			{
				valueOrDefault = -1;
			}
			this.linksByLocalHandle = new HandleTable<AmqpLink>(valueOrDefault);
			uint? nullable = settings.HandleMax;
			if (nullable.HasValue)
			{
				num = nullable.GetValueOrDefault();
			}
			else
			{
				num = -1;
			}
			this.linksByRemoteHandle = new HandleTable<AmqpLink>(num);
			this.outgoingChannel = new AmqpSession.OutgoingSessionChannel(this);
			this.incomingChannel = new AmqpSession.IncomingSessionChannel(this);
			this.upgraded = true;
		}

		protected AmqpSession(string type, AmqpConnection connection, AmqpSessionSettings settings) : this(type, connection, settings, null)
		{
		}

		protected AmqpSession(string type, AmqpConnection connection, AmqpSessionSettings settings, ILinkFactory linkFactory) : base(type)
		{
			this.connection = connection;
			this.settings = settings;
			this.linkFactory = linkFactory;
		}

		protected override void AbortInternal()
		{
			this.CloseLinks(true);
		}

		public void AttachLink(AmqpLink link)
		{
			link.Closed += new EventHandler(this.OnLinkClosed);
			lock (base.ThisLock)
			{
				this.links.Add(link.Name, link);
				link.LocalHandle = new uint?(this.linksByLocalHandle.Add(link));
			}
			MessagingClientEtwProvider.TraceClient<AmqpSession, AmqpLink>((AmqpSession source, AmqpLink l) => MessagingClientEtwProvider.Provider.EventWriteAmqpAttachLink(source, l, l.Name, (l.IsReceiver ? "receiver" : "sender")), this, link);
		}

		protected override bool CloseInternal()
		{
			if (base.State == AmqpObjectState.OpenReceived)
			{
				this.SendBegin();
			}
			this.CloseLinks(!this.LinkFrameAllowed());
			return this.SendEnd() == AmqpObjectState.End;
		}

		private void CloseLinks(bool abort)
		{
			IEnumerable<AmqpLink> values = null;
			lock (base.ThisLock)
			{
				values = this.linksByLocalHandle.Values;
				if (abort)
				{
					this.linksByLocalHandle.Clear();
					this.linksByRemoteHandle.Clear();
				}
			}
			foreach (AmqpLink value in values)
			{
				if (!abort)
				{
					value.SafeClose();
				}
				else
				{
					value.Abort();
				}
			}
		}

		public void DisposeDelivery(AmqpLink link, Delivery delivery, bool settled, DeliveryState state, bool noFlush)
		{
			if (link.IsReceiver)
			{
				this.incomingChannel.DisposeDelivery(delivery, settled, state, noFlush);
				return;
			}
			this.outgoingChannel.DisposeDelivery(delivery, settled, state, noFlush);
		}

		public void Flush()
		{
			this.outgoingChannel.Flush();
			this.incomingChannel.Flush();
		}

		private bool LinkFrameAllowed()
		{
			if (base.State == AmqpObjectState.OpenSent)
			{
				return true;
			}
			return base.State == AmqpObjectState.Opened;
		}

		private Error Negotiate(Begin begin)
		{
			this.outgoingChannel.OnBegin(begin);
			if (begin.HandleMax.HasValue)
			{
				AmqpSessionSettings nullable = this.settings;
				uint num = this.settings.HandleMax();
				uint? handleMax = begin.HandleMax;
				nullable.HandleMax = new uint?(Math.Min(num, handleMax.Value));
			}
			return null;
		}

		private void NotifyCreditAvailable(int credit)
		{
			IEnumerable<AmqpLink> values = null;
			lock (base.ThisLock)
			{
				values = this.linksByLocalHandle.Values;
			}
			foreach (AmqpLink value in values)
			{
				if (value.IsReceiver)
				{
					continue;
				}
				value.NotifySessionCredit(credit);
			}
		}

		public bool OnAcceptTransfer(Delivery delivery, Transfer transfer, bool newDelivery)
		{
			bool flag;
			try
			{
				this.incomingChannel.OnAcceptTransfer(delivery, transfer, newDelivery);
				flag = true;
			}
			catch (AmqpException amqpException)
			{
				base.SafeClose(amqpException);
				flag = false;
			}
			return flag;
		}

		private void OnLinkClosed(object sender, EventArgs e)
		{
			AmqpLink amqpLink = (AmqpLink)sender;
			lock (base.ThisLock)
			{
				this.links.Remove(amqpLink.Name);
				if (amqpLink.LocalHandle.HasValue)
				{
					this.linksByLocalHandle.Remove(amqpLink.LocalHandle.Value);
				}
				if (amqpLink.RemoteHandle.HasValue)
				{
					this.linksByRemoteHandle.Remove(amqpLink.RemoteHandle.Value);
				}
			}
			MessagingClientEtwProvider.TraceClient<AmqpSession, AmqpLink>((AmqpSession source, AmqpLink l) => MessagingClientEtwProvider.Provider.EventWriteAmqpRemoveLink(source, l, l.LocalHandle.Value), this, amqpLink);
		}

		private void OnReceiveBegin(Begin begin)
		{
			StateTransition stateTransition = base.TransitState("R:BEGIN", StateTransition.ReceiveOpen);
			this.incomingChannel.OnBegin(begin);
			if (stateTransition.To == AmqpObjectState.OpenReceived)
			{
				base.Open();
				return;
			}
			Exception amqpException = null;
			Error error = this.Negotiate(begin);
			if (error != null)
			{
				amqpException = new AmqpException(error);
			}
			base.CompleteOpen(false, amqpException);
			if (amqpException != null)
			{
				base.SafeClose(amqpException);
			}
		}

		private void OnReceiveDisposition(Disposition disposition)
		{
			if (disposition.Role.Value)
			{
				this.outgoingChannel.OnReceiveDisposition(disposition);
				return;
			}
			this.incomingChannel.OnReceiveDisposition(disposition);
		}

		private void OnReceiveEnd(End end)
		{
			base.OnReceiveCloseCommand("R:END", end.Error);
		}

		private void OnReceiveFlow(Flow flow)
		{
			this.outgoingChannel.OnFlow(flow);
			this.incomingChannel.OnFlow(flow);
			if (!flow.Handle.HasValue)
			{
				if (flow.Echo())
				{
					this.SendFlow();
				}
				return;
			}
			AmqpLink amqpLink = null;
			if (this.linksByRemoteHandle.TryGetObject(flow.Handle.Value, out amqpLink))
			{
				amqpLink.OnFlow(flow);
				return;
			}
			if (this.Settings.IgnoreMissingLinks)
			{
				return;
			}
			Error unattachedHandle = AmqpError.UnattachedHandle;
			uint? handle = flow.Handle;
			base.SafeClose(new AmqpException(unattachedHandle, SRAmqp.AmqpHandleNotFound(handle.Value, this)));
		}

		private void OnReceiveLinkFrame(Frame frame)
		{
			AmqpLink handle = null;
			Performative command = frame.Command;
			if (command.DescriptorCode != Attach.Code)
			{
				LinkPerformative linkPerformative = (LinkPerformative)command;
				if (!this.linksByRemoteHandle.TryGetObject(linkPerformative.Handle.Value, out handle))
				{
					if (this.Settings.IgnoreMissingLinks)
					{
						return;
					}
					if (linkPerformative.DescriptorCode != Detach.Code)
					{
						Error unattachedHandle = AmqpError.UnattachedHandle;
						uint? nullable = linkPerformative.Handle;
						base.SafeClose(new AmqpException(unattachedHandle, SRAmqp.AmqpHandleNotFound(nullable.Value, this)));
					}
					return;
				}
			}
			else
			{
				Attach attach = (Attach)command;
				lock (base.ThisLock)
				{
					this.links.TryGetValue(attach.LinkName, out handle);
				}
				if (handle != null)
				{
					lock (base.ThisLock)
					{
						handle.RemoteHandle = attach.Handle;
						this.linksByRemoteHandle.Add(attach.Handle.Value, handle);
					}
				}
				else if (!this.TryCreateRemoteLink(attach, out handle))
				{
					return;
				}
			}
			handle.ProcessFrame(frame);
		}

		protected override bool OpenInternal()
		{
			return this.SendBegin() == AmqpObjectState.Opened;
		}

		public virtual void ProcessFrame(Frame frame)
		{
			Performative command = frame.Command;
			try
			{
				if (command.DescriptorCode == Begin.Code)
				{
					this.OnReceiveBegin((Begin)command);
				}
				else if (command.DescriptorCode == End.Code)
				{
					this.OnReceiveEnd((End)command);
				}
				else if (command.DescriptorCode == Disposition.Code)
				{
					this.OnReceiveDisposition((Disposition)command);
				}
				else if (command.DescriptorCode != Flow.Code)
				{
					this.OnReceiveLinkFrame(frame);
				}
				else
				{
					this.OnReceiveFlow((Flow)command);
				}
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				MessagingClientEtwProvider.TraceClient<AmqpSession, Exception>((AmqpSession source, Exception ex) => MessagingClientEtwProvider.Provider.EventWriteAmqpLogError(source, "ProcessFrame", ex.Message), this, exception);
				base.SafeClose(exception);
			}
		}

		protected AmqpObjectState SendBegin()
		{
			StateTransition stateTransition = base.TransitState("S:BEGIN", StateTransition.SendOpen);
			this.SendCommand(this.settings);
			return stateTransition.To;
		}

		public void SendCommand(Performative command)
		{
			this.SendCommand(command, null);
		}

		public void SendCommand(Performative command, ArraySegment<byte>[] payload)
		{
			this.connection.SendCommand(command, this.LocalChannel, payload);
		}

		protected AmqpObjectState SendEnd()
		{
			StateTransition stateTransition = base.TransitState("S:END", StateTransition.SendClose);
			End end = new End();
			Exception terminalException = base.TerminalException;
			if (terminalException != null)
			{
				end.Error = AmqpError.FromException(terminalException, true);
			}
			this.SendCommand(end);
			return stateTransition.To;
		}

		public void SendFlow(Flow flow)
		{
			lock (base.ThisLock)
			{
				if (!base.IsClosing())
				{
					this.outgoingChannel.SendFlow(flow);
				}
			}
		}

		private void SendFlow()
		{
			this.SendFlow(new Flow());
		}

		protected bool TryCreateRemoteLink(Attach attach, out AmqpLink link)
		{
			bool flag;
			link = null;
			AmqpLinkSettings amqpLinkSetting = AmqpLinkSettings.Create(attach);
			try
			{
				link = this.LinkFactory.CreateLink(this, amqpLinkSetting);
				link.RemoteHandle = attach.Handle;
				this.linksByRemoteHandle.Add(attach.Handle.Value, link);
				return true;
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				attach.Source = null;
				attach.Target = null;
				this.SendCommand(attach);
				if (link != null)
				{
					link.SafeClose(exception);
				}
				flag = false;
			}
			return flag;
		}

		public bool TrySendTransfer(Delivery delivery, Transfer transfer, ArraySegment<byte>[] payload)
		{
			return this.outgoingChannel.TrySendTransfer(delivery, transfer, payload);
		}

		protected void Upgrade()
		{
			uint valueOrDefault;
			uint num;
			if (!this.upgraded)
			{
				this.links = new Dictionary<string, AmqpLink>();
				uint? handleMax = this.settings.HandleMax;
				if (handleMax.HasValue)
				{
					valueOrDefault = handleMax.GetValueOrDefault();
				}
				else
				{
					valueOrDefault = -1;
				}
				this.linksByLocalHandle = new HandleTable<AmqpLink>(valueOrDefault);
				uint? nullable = this.settings.HandleMax;
				if (nullable.HasValue)
				{
					num = nullable.GetValueOrDefault();
				}
				else
				{
					num = -1;
				}
				this.linksByRemoteHandle = new HandleTable<AmqpLink>(num);
				this.outgoingChannel = new AmqpSession.OutgoingSessionChannel(this);
				this.incomingChannel = new AmqpSession.IncomingSessionChannel(this);
				this.upgraded = true;
			}
		}

		private sealed class IncomingSessionChannel : AmqpSession.SessionChannel
		{
			private SequenceNumber nextIncomingId;

			private uint incomingWindow;

			private uint flowThreshold;

			private uint needFlowCount;

			private bool transferEverReceived;

			public IncomingSessionChannel(AmqpSession session) : base(session)
			{
				this.incomingWindow = session.settings.IncomingWindow();
				this.flowThreshold = this.incomingWindow * 2 / 3;
				base.IsReceiver = true;
			}

			public void AddFlowState(Flow flow, bool reset)
			{
				lock (base.SyncRoot)
				{
					flow.NextIncomingId = new uint?(this.nextIncomingId.Value);
					flow.IncomingWindow = new uint?(this.incomingWindow);
					if (reset)
					{
						this.needFlowCount = 0;
					}
				}
			}

			public void OnAcceptTransfer(Delivery delivery, Transfer transfer, bool newDelivery)
			{
				if (!this.transferEverReceived)
				{
					base.OnReceiveFirstTransfer(transfer);
					this.transferEverReceived = true;
				}
				bool flag = false;
				lock (base.SyncRoot)
				{
					if (this.incomingWindow > 0)
					{
						flag = true;
						if (newDelivery)
						{
							base.AddDelivery(delivery);
						}
						this.nextIncomingId.Increment();
						AmqpSession.IncomingSessionChannel incomingSessionChannel = this;
						incomingSessionChannel.incomingWindow = incomingSessionChannel.incomingWindow - 1;
					}
				}
				if (!flag)
				{
					MessagingClientEtwProvider.TraceClient<AmqpSession.IncomingSessionChannel>((AmqpSession.IncomingSessionChannel source) => {
					}, this);
					throw new AmqpException(AmqpError.WindowViolation);
				}
				if (!newDelivery)
				{
					this.OnWindowMoved(1);
				}
			}

			public void OnBegin(Begin begin)
			{
				lock (base.SyncRoot)
				{
					this.nextIncomingId = begin.NextOutgoingId.Value;
				}
			}

			public void OnFlow(Flow flow)
			{
			}

			protected override void OnWindowMoved(int count)
			{
				bool flag = false;
				lock (base.SyncRoot)
				{
					AmqpSession.IncomingSessionChannel incomingSessionChannel = this;
					incomingSessionChannel.incomingWindow = incomingSessionChannel.incomingWindow + count;
					AmqpSession.IncomingSessionChannel incomingSessionChannel1 = this;
					incomingSessionChannel1.needFlowCount = incomingSessionChannel1.needFlowCount + count;
					if (this.needFlowCount >= this.flowThreshold)
					{
						this.needFlowCount = 0;
						flag = true;
					}
				}
				if (flag)
				{
					base.Session.SendFlow();
				}
			}

			public override string ToString()
			{
				return string.Concat(base.Session.ToString(), "-in");
			}
		}

		private sealed class OutgoingSessionChannel : AmqpSession.SessionChannel
		{
			private SequenceNumber nextOutgoingId;

			private uint outgoingWindow;

			public OutgoingSessionChannel(AmqpSession session) : base(session)
			{
				this.nextOutgoingId = session.settings.NextOutgoingId.Value;
				this.outgoingWindow = session.settings.OutgoingWindow.Value;
				base.IsReceiver = false;
			}

			public void AddFlowState(Flow flow, bool reset)
			{
				lock (base.SyncRoot)
				{
					flow.OutgoingWindow = new uint?(this.outgoingWindow);
					flow.NextOutgoingId = new uint?(this.nextOutgoingId.Value);
				}
			}

			public void OnBegin(Begin begin)
			{
				lock (base.SyncRoot)
				{
					uint? outgoingWindow = base.Session.settings.OutgoingWindow;
					uint value = outgoingWindow.Value - this.outgoingWindow;
					if (value <= begin.IncomingWindow.Value)
					{
						this.outgoingWindow = begin.IncomingWindow.Value - value;
					}
					else
					{
						this.outgoingWindow = 0;
					}
				}
			}

			public void OnFlow(Flow flow)
			{
				uint valueOrDefault;
				uint num = 0;
				lock (base.SyncRoot)
				{
					uint? nextIncomingId = flow.NextIncomingId;
					if (nextIncomingId.HasValue)
					{
						valueOrDefault = nextIncomingId.GetValueOrDefault();
					}
					else
					{
						valueOrDefault = 0;
					}
					uint num1 = valueOrDefault;
					uint? incomingWindow = flow.IncomingWindow;
					this.outgoingWindow = num1 + incomingWindow.Value - this.nextOutgoingId.Value;
					num = this.outgoingWindow;
				}
				if (num > 0)
				{
					base.Session.NotifyCreditAvailable((num > 2147483647 ? 2147483647 : (int)num));
				}
			}

			protected override void OnWindowMoved(int count)
			{
			}

			public void SendFlow(Flow flow)
			{
				lock (base.SyncRoot)
				{
					this.AddFlowState(flow, false);
					base.Session.incomingChannel.AddFlowState(flow, true);
					base.Session.SendCommand(flow, null);
				}
			}

			public override string ToString()
			{
				return string.Concat(base.Session.ToString(), "-out");
			}

			public bool TrySendTransfer(Delivery delivery, Transfer transfer, ArraySegment<byte>[] payload)
			{
				bool flag;
				lock (base.SyncRoot)
				{
					if (this.outgoingWindow != 0)
					{
						this.nextOutgoingId.Increment();
						AmqpSession.OutgoingSessionChannel outgoingSessionChannel = this;
						outgoingSessionChannel.outgoingWindow = outgoingSessionChannel.outgoingWindow - 1;
						if (delivery == null)
						{
							transfer.DeliveryId = null;
						}
						else
						{
							base.AddDelivery(delivery);
							transfer.DeliveryId = new uint?(delivery.DeliveryId.Value);
						}
						base.Session.SendCommand(transfer, payload);
						return true;
					}
					else
					{
						MessagingClientEtwProvider.TraceClient<AmqpSession.OutgoingSessionChannel>((AmqpSession.OutgoingSessionChannel source) => {
						}, this);
						flag = false;
					}
				}
				return flag;
			}
		}

		private abstract class SessionChannel
		{
			private readonly static Action<object> dispositionTimerCallback;

			private readonly AmqpSession session;

			private readonly LinkedList<Delivery> deliveries;

			private readonly object syncRoot;

			private readonly IOThreadTimer dispositionTimer;

			private SequenceNumber nextDeliveryId;

			private int needDispositionCount;

			private bool sendingDisposition;

			private bool timerScheduled;

			protected bool IsReceiver
			{
				get;
				set;
			}

			protected AmqpSession Session
			{
				get
				{
					return this.session;
				}
			}

			protected object SyncRoot
			{
				get
				{
					return this.syncRoot;
				}
			}

			static SessionChannel()
			{
				AmqpSession.SessionChannel.dispositionTimerCallback = new Action<object>(AmqpSession.SessionChannel.DispositionTimerCallback);
			}

			public SessionChannel(AmqpSession session)
			{
				this.session = session;
				this.nextDeliveryId = session.settings.InitialDeliveryId;
				this.syncRoot = new object();
				this.deliveries = new LinkedList<Delivery>();
				if (session.settings.DispositionInterval > TimeSpan.Zero)
				{
					this.dispositionTimer = new IOThreadTimer(AmqpSession.SessionChannel.dispositionTimerCallback, this, false);
				}
			}

			protected void AddDelivery(Delivery delivery)
			{
				delivery.DeliveryId = this.nextDeliveryId;
				this.nextDeliveryId.Increment();
				if (!delivery.Settled)
				{
					this.deliveries.AddLast(delivery);
				}
			}

			private static bool CanBatch(Outcome outcome1, Outcome outcome2)
			{
				if (outcome1 == null || outcome2 == null || outcome1.DescriptorCode != outcome2.DescriptorCode)
				{
					return false;
				}
				if (outcome1.DescriptorCode == Accepted.Code)
				{
					return true;
				}
				return outcome1.DescriptorCode == Released.Code;
			}

			public void DisposeDelivery(Delivery delivery, bool settled, DeliveryState state, bool noFlush)
			{
				if (delivery.Settled)
				{
					this.OnWindowMoved(1);
					return;
				}
				bool flag = false;
				lock (this.syncRoot)
				{
					delivery.StateChanged = true;
					delivery.Settled = settled;
					delivery.State = state;
					if (this.sendingDisposition || noFlush)
					{
						return;
					}
					else
					{
						if (delivery.Batchable && !(this.session.settings.DispositionInterval == TimeSpan.Zero))
						{
							AmqpSession.SessionChannel sessionChannel = this;
							int num = sessionChannel.needDispositionCount + 1;
							int num1 = num;
							sessionChannel.needDispositionCount = num;
							if (num1 >= this.session.settings.DispositionThreshold)
							{
								goto Label2;
							}
							if (!this.timerScheduled)
							{
								this.timerScheduled = true;
								flag = true;
								goto Label1;
							}
							else
							{
								goto Label1;
							}
						}
					Label2:
						this.sendingDisposition = true;
						this.needDispositionCount = 0;
					Label1:
					}
				}
				if (flag)
				{
					this.dispositionTimer.Set(this.session.settings.DispositionInterval);
					return;
				}
				this.SendDisposition();
			}

			private static void DispositionTimerCallback(object state)
			{
				AmqpSession.SessionChannel sessionChannel = (AmqpSession.SessionChannel)state;
				if (sessionChannel.session.State != AmqpObjectState.Opened)
				{
					return;
				}
				MessagingClientEtwProvider.TraceClient<AmqpSession.SessionChannel>((AmqpSession.SessionChannel source) => {
				}, sessionChannel);
				lock (sessionChannel.syncRoot)
				{
					sessionChannel.timerScheduled = false;
					if (!sessionChannel.sendingDisposition)
					{
						sessionChannel.sendingDisposition = true;
					}
					else
					{
						return;
					}
				}
				try
				{
					sessionChannel.SendDisposition();
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					sessionChannel.session.SafeClose(exception);
				}
			}

			public void Flush()
			{
				SpinWait.SpinUntil(() => this.TrySendDisposition());
			}

			public void OnReceiveDisposition(Disposition disposition)
			{
				SequenceNumber valueOrDefault;
				SequenceNumber value = disposition.First.Value;
				uint? last = disposition.Last;
				if (last.HasValue)
				{
					valueOrDefault = last.GetValueOrDefault();
				}
				else
				{
					valueOrDefault = value;
				}
				SequenceNumber sequenceNumber = valueOrDefault;
				if (sequenceNumber < value)
				{
					return;
				}
				LinkedList<Delivery> deliveries = new LinkedList<Delivery>();
				int num = 0;
				lock (this.syncRoot)
				{
					if (value < this.nextDeliveryId)
					{
						if (sequenceNumber > this.nextDeliveryId)
						{
							sequenceNumber = this.nextDeliveryId;
						}
						bool flag = disposition.Settled();
						LinkedListNode<Delivery> first = this.deliveries.First;
						while (first != null)
						{
							SequenceNumber value1 = first.Value.DeliveryId.Value;
							if (value1 >= value)
							{
								if (value1 > sequenceNumber)
								{
									break;
								}
								LinkedListNode<Delivery> linkedListNode = first;
								first = first.Next;
								Delivery state = linkedListNode.Value;
								state.Settled = flag;
								state.State = disposition.State;
								if (!flag)
								{
									deliveries.AddLast(linkedListNode.Value);
								}
								else
								{
									num++;
									this.deliveries.Remove(linkedListNode);
									deliveries.AddLast(linkedListNode);
								}
							}
							else
							{
								first = first.Next;
							}
						}
					}
					else
					{
						return;
					}
				}
				if (deliveries.Count > 0)
				{
					foreach (Delivery delivery in deliveries)
					{
						delivery.Link.OnDisposeDelivery(delivery);
					}
					if (num > 0)
					{
						this.OnWindowMoved(num);
					}
				}
			}

			protected void OnReceiveFirstTransfer(Transfer transfer)
			{
				this.nextDeliveryId = transfer.DeliveryId.Value;
			}

			protected abstract void OnWindowMoved(int count);

			private void SendDisposition()
			{
				List<AmqpSession.SessionChannel.DispositionInfo> dispositionInfos = new List<AmqpSession.SessionChannel.DispositionInfo>();
				int num = 0;
				lock (this.syncRoot)
				{
					LinkedListNode<Delivery> first = this.deliveries.First;
					Delivery value = null;
					uint? nullable = null;
					while (first != null)
					{
						if (!first.Value.StateChanged)
						{
							if (value != null)
							{
								AmqpSession.SessionChannel.DispositionInfo dispositionInfo = new AmqpSession.SessionChannel.DispositionInfo()
								{
									First = value,
									Last = nullable
								};
								dispositionInfos.Add(dispositionInfo);
								value = null;
								nullable = null;
							}
							first = first.Next;
						}
						else
						{
							if (value == null)
							{
								value = first.Value;
							}
							else if (first.Value.Settled != value.Settled || !AmqpSession.SessionChannel.CanBatch(first.Value.State as Outcome, value.State as Outcome))
							{
								AmqpSession.SessionChannel.DispositionInfo dispositionInfo1 = new AmqpSession.SessionChannel.DispositionInfo()
								{
									First = value,
									Last = nullable
								};
								dispositionInfos.Add(dispositionInfo1);
								value = first.Value;
								nullable = null;
							}
							else
							{
								nullable = new uint?(first.Value.DeliveryId.Value);
							}
							if (!first.Value.Settled)
							{
								first.Value.StateChanged = false;
								first = first.Next;
							}
							else
							{
								LinkedListNode<Delivery> linkedListNode = first;
								first = first.Next;
								num++;
								this.deliveries.Remove(linkedListNode);
							}
						}
					}
					if (value != null)
					{
						AmqpSession.SessionChannel.DispositionInfo dispositionInfo2 = new AmqpSession.SessionChannel.DispositionInfo()
						{
							First = value,
							Last = nullable
						};
						dispositionInfos.Add(dispositionInfo2);
					}
					this.sendingDisposition = false;
				}
				if (dispositionInfos.Count > 0)
				{
					foreach (AmqpSession.SessionChannel.DispositionInfo dispositionInfo3 in dispositionInfos)
					{
						Disposition disposition = new Disposition()
						{
							First = new uint?(dispositionInfo3.First.DeliveryId.Value),
							Last = dispositionInfo3.Last,
							Settled = new bool?(dispositionInfo3.First.Settled),
							State = dispositionInfo3.First.State,
							Role = new bool?(this.IsReceiver)
						};
						lock (this.session.ThisLock)
						{
							if (this.session.State < AmqpObjectState.CloseSent)
							{
								this.session.SendCommand(disposition);
							}
						}
					}
				}
				if (num > 0)
				{
					this.OnWindowMoved(num);
				}
			}

			private bool TrySendDisposition()
			{
				bool flag;
				lock (this.syncRoot)
				{
					if (!this.sendingDisposition)
					{
						this.sendingDisposition = true;
						this.needDispositionCount = 0;
						this.SendDisposition();
						return true;
					}
					else
					{
						flag = false;
					}
				}
				return flag;
			}

			private struct DispositionInfo
			{
				public Delivery First;

				public uint? Last;
			}
		}
	}
}