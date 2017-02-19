using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Messaging.Channels;
using Microsoft.ServiceBus.Properties;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using System.Xml;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	internal sealed class DuplexRequestBindingElement : BindingElement
	{
		public bool ClientMode
		{
			get;
			set;
		}

		public bool IncludeExceptionDetails
		{
			get;
			set;
		}

		public DuplexRequestBindingElement()
		{
			this.ClientMode = true;
		}

		private DuplexRequestBindingElement(DuplexRequestBindingElement other)
		{
			this.ClientMode = other.ClientMode;
			this.IncludeExceptionDetails = other.IncludeExceptionDetails;
		}

		public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
		{
			if (!this.CanBuildChannelFactory<TChannel>(context))
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.Argument("TChannel", SRClient.ChannelTypeNotSupported(typeof(TChannel)));
			}
			return (IChannelFactory<TChannel>)(new DuplexRequestBindingElement.DuplexRequestChannelFactory(context, this));
		}

		public override bool CanBuildChannelFactory<TChannel>(BindingContext context)
		{
			if (context == null)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("context");
			}
			if (typeof(TChannel) != typeof(IRequestSessionChannel))
			{
				return false;
			}
			return base.CanBuildChannelFactory<IDuplexSessionChannel>(context);
		}

		public override BindingElement Clone()
		{
			return new DuplexRequestBindingElement(this);
		}

		public override T GetProperty<T>(BindingContext context)
		where T : class
		{
			return context.GetInnerProperty<T>();
		}

		private class DuplexRequestChannelFactory : ChannelFactoryBase<IRequestSessionChannel>
		{
			private readonly IChannelFactory<IDuplexSessionChannel> innerFactory;

			public DuplexRequestBindingElement BindingElement
			{
				get;
				private set;
			}

			public DuplexRequestChannelFactory(BindingContext context, DuplexRequestBindingElement bindingElement) : base(context.Binding)
			{
				this.BindingElement = bindingElement;
				this.innerFactory = context.BuildInnerChannelFactory<IDuplexSessionChannel>();
			}

			public override T GetProperty<T>()
			where T : class
			{
				return this.innerFactory.GetProperty<T>();
			}

			protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
			{
				return this.innerFactory.BeginClose(timeout, callback, state);
			}

			protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
			{
				return this.innerFactory.BeginOpen(timeout, callback, state);
			}

			protected override void OnClose(TimeSpan timeout)
			{
				this.innerFactory.Close(timeout);
			}

			protected override IRequestSessionChannel OnCreateChannel(EndpointAddress address, Uri via)
			{
				IDuplexSessionChannel duplexSessionChannel = this.innerFactory.CreateChannel(address, via);
				MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRuntimeChannelCreated(duplexSessionChannel.GetType().Name, duplexSessionChannel.LocalAddress.Uri.AbsoluteUri, duplexSessionChannel.RemoteAddress.Uri.AbsoluteUri, duplexSessionChannel.Via.AbsoluteUri, duplexSessionChannel.Session.Id));
				return new DuplexRequestBindingElement.DuplexRequestSessionChannel(this, duplexSessionChannel);
			}

			protected override void OnEndClose(IAsyncResult result)
			{
				this.innerFactory.EndClose(result);
			}

			protected override void OnEndOpen(IAsyncResult result)
			{
				this.innerFactory.EndOpen(result);
			}

			private void OnInnerFactoryFaulted(object sender, EventArgs e)
			{
				base.Fault();
			}

			protected override void OnOpen(TimeSpan timeout)
			{
				this.innerFactory.Open();
			}

			protected override void OnOpening()
			{
				base.OnOpening();
				this.innerFactory.SafeAddFaulted(new EventHandler(this.OnInnerFactoryFaulted));
			}
		}

		private sealed class DuplexRequestSessionChannel : ChannelBase, IRequestSessionChannel, IRequestChannel, IChannel, ICommunicationObject, ISessionChannel<IOutputSession>
		{
			private readonly static Action<object> messageReceivePump;

			private readonly static AsyncCallback onMessageReceived;

			private readonly static Action<object> beginPing;

			private readonly static AsyncCallback endPing;

			private readonly EventHandler onInnerChannelFaulted;

			private readonly IDuplexSessionChannel innerChannel;

			private readonly ConcurrentDictionary<UniqueId, DuplexRequestBindingElement.DuplexRequestSessionChannel.DuplexCorrelationAsyncResult> inflightRequests;

			private readonly bool includeExceptionDetails;

			private readonly IOThreadTimer pingTimer;

			private readonly ManualResetEvent serverShutdownEvent;

			private int interlockedSync;

			private bool clientMode;

			private MessageVersion messageVersion;

			protected override TimeSpan DefaultCloseTimeout
			{
				get
				{
					return Microsoft.ServiceBus.Messaging.Channels.TransportDefaults.CloseTimeout;
				}
			}

			protected override TimeSpan DefaultOpenTimeout
			{
				get
				{
					return Microsoft.ServiceBus.Messaging.Channels.TransportDefaults.OpenTimeout;
				}
			}

			public bool IsMaxMessageSizeExceededFault
			{
				get;
				private set;
			}

			public EndpointAddress RemoteAddress
			{
				get
				{
					return this.innerChannel.RemoteAddress;
				}
			}

			public IOutputSession Session
			{
				get
				{
					return this.innerChannel.Session;
				}
			}

			public Uri Via
			{
				get
				{
					return this.innerChannel.Via;
				}
			}

			static DuplexRequestSessionChannel()
			{
				DuplexRequestBindingElement.DuplexRequestSessionChannel.messageReceivePump = new Action<object>(DuplexRequestBindingElement.DuplexRequestSessionChannel.MessageReceivePump);
				DuplexRequestBindingElement.DuplexRequestSessionChannel.onMessageReceived = new AsyncCallback(DuplexRequestBindingElement.DuplexRequestSessionChannel.OnMessageReceived);
				DuplexRequestBindingElement.DuplexRequestSessionChannel.beginPing = new Action<object>(DuplexRequestBindingElement.DuplexRequestSessionChannel.BeginPing);
				DuplexRequestBindingElement.DuplexRequestSessionChannel.endPing = new AsyncCallback(DuplexRequestBindingElement.DuplexRequestSessionChannel.EndPing);
			}

			public DuplexRequestSessionChannel(DuplexRequestBindingElement.DuplexRequestChannelFactory channelFactory, IDuplexSessionChannel innerChannel) : base(channelFactory)
			{
				this.innerChannel = innerChannel;
				this.includeExceptionDetails = channelFactory.BindingElement.IncludeExceptionDetails;
				this.inflightRequests = new ConcurrentDictionary<UniqueId, DuplexRequestBindingElement.DuplexRequestSessionChannel.DuplexCorrelationAsyncResult>();
				this.onInnerChannelFaulted = new EventHandler(this.OnInnerChannelFaulted);
				this.clientMode = channelFactory.BindingElement.ClientMode;
				this.messageVersion = innerChannel.GetProperty<MessageVersion>();
				this.serverShutdownEvent = new ManualResetEvent(false);
				if (this.clientMode)
				{
					this.pingTimer = new IOThreadTimer(DuplexRequestBindingElement.DuplexRequestSessionChannel.beginPing, this, true);
				}
			}

			private static void BeginPing(object state)
			{
				DuplexRequestBindingElement.DuplexRequestSessionChannel duplexRequestSessionChannel = (DuplexRequestBindingElement.DuplexRequestSessionChannel)state;
				CommunicationState communicationState = duplexRequestSessionChannel.State;
				if (communicationState != CommunicationState.Opened)
				{
					MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRuntimeChannelPingIncorrectState(duplexRequestSessionChannel.innerChannel.GetType().Name, duplexRequestSessionChannel.innerChannel.LocalAddress.Uri.AbsoluteUri, (duplexRequestSessionChannel.innerChannel.RemoteAddress == null ? "Null" : duplexRequestSessionChannel.innerChannel.RemoteAddress.Uri.AbsoluteUri), (duplexRequestSessionChannel.innerChannel.Via == null ? "Null" : duplexRequestSessionChannel.innerChannel.Via.AbsoluteUri), duplexRequestSessionChannel.innerChannel.Session.Id, communicationState.ToString()));
					return;
				}
				Message message = Message.CreateMessage(duplexRequestSessionChannel.messageVersion, "http://schemas.microsoft.com/servicebus/2010/08/protocol/Ping", new Microsoft.ServiceBus.Messaging.Channels.PingMessage());
				try
				{
					duplexRequestSessionChannel.innerChannel.BeginSend(message, SbmpConstants.ConnectionPingOperationTimeout, DuplexRequestBindingElement.DuplexRequestSessionChannel.endPing, duplexRequestSessionChannel);
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRuntimeChannelPingFailed(duplexRequestSessionChannel.innerChannel.GetType().Name, duplexRequestSessionChannel.innerChannel.LocalAddress.Uri.AbsoluteUri, (duplexRequestSessionChannel.innerChannel.RemoteAddress == null ? "Null" : duplexRequestSessionChannel.innerChannel.RemoteAddress.Uri.AbsoluteUri), (duplexRequestSessionChannel.innerChannel.Via == null ? "Null" : duplexRequestSessionChannel.innerChannel.Via.AbsoluteUri), duplexRequestSessionChannel.innerChannel.Session.Id, exception.ToString()));
				}
			}

			public IAsyncResult BeginRequest(Message wcfRequest, AsyncCallback callback, object state)
			{
				return this.BeginRequest(wcfRequest, base.DefaultSendTimeout, callback, state);
			}

			public IAsyncResult BeginRequest(Message wcfRequest, TimeSpan timeout, AsyncCallback callback, object state)
			{
				base.ThrowIfDisposedOrNotOpen();
				return new DuplexRequestBindingElement.DuplexRequestSessionChannel.DuplexCorrelationAsyncResult(this, wcfRequest, timeout, callback, state);
			}

			private void CancelPendingOperations(bool isFaulted)
			{
				this.CancelPing();
				if (Interlocked.Exchange(ref this.interlockedSync, 1) == 0)
				{
					foreach (DuplexRequestBindingElement.DuplexRequestSessionChannel.DuplexCorrelationAsyncResult value in this.inflightRequests.Values)
					{
						DuplexRequestBindingElement.DuplexRequestSessionChannel.DuplexCorrelationAsyncResult duplexCorrelationAsyncResult = value;
						string str = SRClient.TrackableExceptionMessageFormat(SRClient.ServerDidNotReply, duplexCorrelationAsyncResult.CreateClientTrackingExceptionInfo());
						Exception communicationObjectAbortedException = new CommunicationObjectAbortedException(str);
						if (isFaulted)
						{
							communicationObjectAbortedException = new CommunicationObjectFaultedException(str);
						}
						ActionItem.Schedule((object s) => duplexCorrelationAsyncResult.TryComplete(false, communicationObjectAbortedException), null);
					}
					this.inflightRequests.Clear();
				}
			}

			private void CancelPing()
			{
				if (this.clientMode)
				{
					lock (this.pingTimer)
					{
						this.pingTimer.Cancel();
					}
				}
			}

			private static void EndPing(IAsyncResult result)
			{
				DuplexRequestBindingElement.DuplexRequestSessionChannel asyncState = (DuplexRequestBindingElement.DuplexRequestSessionChannel)result.AsyncState;
				try
				{
					asyncState.innerChannel.EndSend(result);
					lock (asyncState.pingTimer)
					{
						CommunicationState state = asyncState.State;
						if (state == CommunicationState.Opened && asyncState.inflightRequests.Count > 0)
						{
							asyncState.pingTimer.Set(SbmpConstants.ConnectionPingTimeout);
						}
						else if (state != CommunicationState.Opened)
						{
							MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRuntimeChannelStopPingWithIncorrectState(asyncState.innerChannel.GetType().Name, asyncState.innerChannel.LocalAddress.Uri.AbsoluteUri, (asyncState.innerChannel.RemoteAddress == null ? "Null" : asyncState.innerChannel.RemoteAddress.Uri.AbsoluteUri), (asyncState.innerChannel.Via == null ? "Null" : asyncState.innerChannel.Via.AbsoluteUri), asyncState.innerChannel.Session.Id, state.ToString(), asyncState.inflightRequests.Count));
						}
					}
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRuntimeChannelPingFailed(asyncState.innerChannel.GetType().Name, asyncState.innerChannel.LocalAddress.Uri.AbsoluteUri, (asyncState.innerChannel.RemoteAddress == null ? "Null" : asyncState.innerChannel.RemoteAddress.Uri.AbsoluteUri), (asyncState.innerChannel.Via == null ? "Null" : asyncState.innerChannel.Via.AbsoluteUri), asyncState.innerChannel.Session.Id, exception.ToString()));
				}
			}

			public Message EndRequest(IAsyncResult result)
			{
				return DuplexRequestBindingElement.DuplexRequestSessionChannel.DuplexCorrelationAsyncResult.End(result);
			}

			public override TProperty GetProperty<TProperty>()
			where TProperty : class
			{
				return this.innerChannel.GetProperty<TProperty>();
			}

			private bool HandleMessageReceived(IAsyncResult result)
			{
				Exception exception = null;
				DuplexRequestBindingElement.DuplexRequestSessionChannel.DuplexCorrelationAsyncResult duplexCorrelationAsyncResult = null;
				try
				{
					Message message = null;
					if (this.innerChannel.EndTryReceive(result, out message))
					{
						if (message != null)
						{
							UniqueId relatesTo = message.Headers.RelatesTo;
							if (!this.inflightRequests.TryRemove(relatesTo, out duplexCorrelationAsyncResult))
							{
								MessagingClientEtwProvider.TraceClient(() => {
								});
							}
							else
							{
								duplexCorrelationAsyncResult.ReplyMessage = message;
								this.ThrowIfFaultMessage(duplexCorrelationAsyncResult.ReplyMessage);
							}
						}
						else if (base.State != CommunicationState.Opened)
						{
							this.serverShutdownEvent.Set();
							return false;
						}
						else
						{
							MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRuntimeChannelFaulting(this.innerChannel.GetType().Name, this.innerChannel.LocalAddress.Uri.AbsoluteUri, this.innerChannel.RemoteAddress.Uri.AbsoluteUri, this.innerChannel.Via.AbsoluteUri, this.innerChannel.Session.Id, "HandleMessageReceived: Received null message."));
							base.Fault();
						}
					}
				}
				catch (Exception exception2)
				{
					Exception exception1 = exception2;
					if (Fx.IsFatal(exception1))
					{
						throw;
					}
					if (duplexCorrelationAsyncResult == null && (!base.IsDisposed || !(exception1 is CommunicationObjectAbortedException)))
					{
						MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRuntimeChannelFaulting(this.innerChannel.GetType().Name, this.innerChannel.LocalAddress.Uri.AbsoluteUri, this.innerChannel.RemoteAddress.Uri.AbsoluteUri, this.innerChannel.Via.AbsoluteUri, this.innerChannel.Session.Id, string.Concat("HandleMessageReceived: ", exception1.ToString())));
						base.Fault();
					}
					exception = exception1;
				}
				if (duplexCorrelationAsyncResult != null)
				{
					ActionItem.Schedule((object s) => duplexCorrelationAsyncResult.TryComplete(false, exception), null);
				}
				return true;
			}

			private static void MessageReceivePump(object state)
			{
				bool flag;
				DuplexRequestBindingElement.DuplexRequestSessionChannel duplexRequestSessionChannel = (DuplexRequestBindingElement.DuplexRequestSessionChannel)state;
				try
				{
					do
					{
						flag = false;
						if (duplexRequestSessionChannel.State != CommunicationState.Opened && duplexRequestSessionChannel.State != CommunicationState.Closing)
						{
							continue;
						}
						IAsyncResult asyncResult = duplexRequestSessionChannel.innerChannel.BeginTryReceive(TimeSpan.MaxValue, DuplexRequestBindingElement.DuplexRequestSessionChannel.onMessageReceived, duplexRequestSessionChannel);
						if (!asyncResult.CompletedSynchronously)
						{
							continue;
						}
						flag = duplexRequestSessionChannel.HandleMessageReceived(asyncResult);
					}
					while (flag);
				}
				catch (CommunicationException communicationException1)
				{
					CommunicationException communicationException = communicationException1;
					MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRuntimeChannelFaulting(duplexRequestSessionChannel.innerChannel.GetType().Name, duplexRequestSessionChannel.innerChannel.LocalAddress.Uri.AbsoluteUri, duplexRequestSessionChannel.innerChannel.RemoteAddress.Uri.AbsoluteUri, duplexRequestSessionChannel.innerChannel.Via.AbsoluteUri, duplexRequestSessionChannel.innerChannel.Session.Id, string.Concat("MessageReceivePump: ", communicationException.ToString())));
					duplexRequestSessionChannel.Fault();
				}
			}

			protected override void OnAbort()
			{
				this.CancelPendingOperations(false);
				MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRuntimeChannelAborting(this.innerChannel.GetType().Name, this.innerChannel.LocalAddress.Uri.AbsoluteUri, this.innerChannel.RemoteAddress.Uri.AbsoluteUri, this.innerChannel.Via.AbsoluteUri, this.innerChannel.Session.Id));
				this.innerChannel.Abort();
			}

			protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
			{
				return new DuplexRequestBindingElement.DuplexRequestSessionChannel.CloseAsyncResult(this, timeout, callback, state);
			}

			protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
			{
				this.innerChannel.SafeAddFaulted(this.onInnerChannelFaulted);
				return this.innerChannel.BeginOpen(timeout, callback, state);
			}

			private void OnBeginRequest()
			{
				if (this.clientMode)
				{
					lock (this.pingTimer)
					{
						this.pingTimer.Set(SbmpConstants.ConnectionPingTimeout);
					}
				}
			}

			protected override void OnClose(TimeSpan timeout)
			{
				this.OnEndClose(this.OnBeginClose(timeout, null, null));
			}

			protected override void OnClosed()
			{
				base.OnClosed();
				this.UnregisterInnerChannelEvents();
			}

			protected override void OnEndClose(IAsyncResult result)
			{
				DuplexRequestBindingElement.DuplexRequestSessionChannel.CloseAsyncResult.End(result);
			}

			protected override void OnEndOpen(IAsyncResult result)
			{
				this.innerChannel.EndOpen(result);
			}

			private void OnEndRequest()
			{
				if (this.clientMode && this.inflightRequests.Count == 0)
				{
					lock (this.pingTimer)
					{
						if (this.inflightRequests.Count == 0)
						{
							this.pingTimer.Cancel();
						}
					}
				}
			}

			protected override void OnFaulted()
			{
				this.CancelPendingOperations(true);
				base.OnFaulted();
			}

			private void OnInnerChannelFaulted(object sender, EventArgs args)
			{
				MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRuntimeChannelFaulting(this.innerChannel.GetType().Name, this.innerChannel.LocalAddress.Uri.AbsoluteUri, this.innerChannel.RemoteAddress.Uri.AbsoluteUri, this.innerChannel.Via.AbsoluteUri, this.innerChannel.Session.Id, "OnInnerChannelFaulted"));
				try
				{
					FieldInfo field = this.innerChannel.GetType().GetField("decoder", BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic);
					if (field != null)
					{
						object value = field.GetValue(this.innerChannel);
						PropertyInfo property = value.GetType().GetProperty("Fault");
						if (property != null)
						{
							string str = (string)property.GetValue(value, null);
							this.IsMaxMessageSizeExceededFault = (string.IsNullOrWhiteSpace(str) ? false : str.Contains("http://schemas.microsoft.com/ws/2006/05/framing/faults/MaxMessageSizeExceededFault"));
						}
					}
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteExceptionAsInformation(exception.Message));
				}
				base.Fault();
			}

			private static void OnMessageReceived(IAsyncResult result)
			{
				if (!result.CompletedSynchronously)
				{
					DuplexRequestBindingElement.DuplexRequestSessionChannel asyncState = (DuplexRequestBindingElement.DuplexRequestSessionChannel)result.AsyncState;
					if (asyncState.HandleMessageReceived(result))
					{
						DuplexRequestBindingElement.DuplexRequestSessionChannel.MessageReceivePump(asyncState);
					}
				}
			}

			protected override void OnOpen(TimeSpan timeout)
			{
				this.OnEndOpen(this.OnBeginOpen(timeout, null, null));
			}

			protected override void OnOpened()
			{
				base.OnOpened();
				ActionItem.Schedule(DuplexRequestBindingElement.DuplexRequestSessionChannel.messageReceivePump, this);
			}

			public Message Request(Message wcfRequest)
			{
				return this.Request(wcfRequest, base.DefaultSendTimeout);
			}

			public Message Request(Message wcfRequest, TimeSpan timeout)
			{
				return this.EndRequest(this.BeginRequest(wcfRequest, timeout, null, null));
			}

			private void ThrowIfFaultMessage(Message wcfMessage)
			{
				Exception exception;
				if (wcfMessage.IsFault)
				{
					MessagingClientEtwProvider.TraceClient(() => {
					});
					string action = wcfMessage.Headers.Action;
					MessageFault messageFault = MessageFault.CreateFault(wcfMessage, 65536);
					FaultConverter property = this.innerChannel.GetProperty<FaultConverter>();
					if (property == null || !property.TryCreateException(wcfMessage, messageFault, out exception))
					{
						if (!messageFault.HasDetail)
						{
							throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsWarning(new FaultException(messageFault, action), null);
						}
						ExceptionDetail detail = messageFault.GetDetail<ExceptionDetail>();
						if (!this.clientMode && string.Equals(detail.Type, typeof(CommunicationException).FullName, StringComparison.Ordinal))
						{
							MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRuntimeChannelFaulting(this.innerChannel.GetType().Name, this.innerChannel.LocalAddress.Uri.AbsoluteUri, this.innerChannel.RemoteAddress.Uri.AbsoluteUri, this.innerChannel.Via.AbsoluteUri, this.innerChannel.Session.Id, string.Concat("ThrowIfFaultMessage: Received CommunicationException as fault message. ", detail.ToString())));
							base.Fault();
						}
						if (!this.includeExceptionDetails)
						{
							throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsInformation(new FaultException<ExceptionDetailNoStackTrace>(new ExceptionDetailNoStackTrace(detail, true), messageFault.Reason, messageFault.Code, action), null);
						}
						throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsInformation(new FaultException<ExceptionDetail>(detail, messageFault.Reason, messageFault.Code, action), null);
					}
					throw Fx.Exception.AsWarning(exception, null);
				}
			}

			private void UnregisterInnerChannelEvents()
			{
				this.innerChannel.Faulted -= this.onInnerChannelFaulted;
			}

			private class CloseAsyncResult : AsyncResult
			{
				private readonly static AsyncCallback closeOutputSessionCallback;

				private readonly static AsyncCallback closeChannelCallback;

				private readonly static WaitOrTimerCallback serverShutdownCallback;

				private Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper;

				private readonly DuplexRequestBindingElement.DuplexRequestSessionChannel correlator;

				private readonly RegisteredWaitHandle shutdownWaitHandle;

				private int completed;

				static CloseAsyncResult()
				{
					DuplexRequestBindingElement.DuplexRequestSessionChannel.CloseAsyncResult.closeOutputSessionCallback = new AsyncCallback(DuplexRequestBindingElement.DuplexRequestSessionChannel.CloseAsyncResult.CloseOutputSessionCallback);
					DuplexRequestBindingElement.DuplexRequestSessionChannel.CloseAsyncResult.closeChannelCallback = new AsyncCallback(DuplexRequestBindingElement.DuplexRequestSessionChannel.CloseAsyncResult.CloseChannelCallback);
					DuplexRequestBindingElement.DuplexRequestSessionChannel.CloseAsyncResult.serverShutdownCallback = new WaitOrTimerCallback(DuplexRequestBindingElement.DuplexRequestSessionChannel.CloseAsyncResult.ServerShutdownCallback);
				}

				public CloseAsyncResult(DuplexRequestBindingElement.DuplexRequestSessionChannel correlator, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
				{
					this.correlator = correlator;
					this.timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
					this.shutdownWaitHandle = ThreadPool.RegisterWaitForSingleObject(this.correlator.serverShutdownEvent, DuplexRequestBindingElement.DuplexRequestSessionChannel.CloseAsyncResult.serverShutdownCallback, this, Microsoft.ServiceBus.Common.TimeoutHelper.ToMilliseconds(this.timeoutHelper.RemainingTime()), true);
					this.TryOperation(true, (DuplexRequestBindingElement.DuplexRequestSessionChannel.CloseAsyncResult t) => {
						IAsyncResult asyncResult = t.correlator.innerChannel.Session.BeginCloseOutputSession(t.timeoutHelper.RemainingTime(), DuplexRequestBindingElement.DuplexRequestSessionChannel.CloseAsyncResult.closeOutputSessionCallback, t);
						if (asyncResult.CompletedSynchronously)
						{
							t.correlator.innerChannel.Session.EndCloseOutputSession(asyncResult);
						}
					});
				}

				private static void CloseChannelCallback(IAsyncResult asyncResult)
				{
					if (!asyncResult.CompletedSynchronously)
					{
						((DuplexRequestBindingElement.DuplexRequestSessionChannel.CloseAsyncResult)asyncResult.AsyncState).TryOperation(false, (DuplexRequestBindingElement.DuplexRequestSessionChannel.CloseAsyncResult t) => {
							t.correlator.innerChannel.EndClose(asyncResult);
							t.TryComplete(false, null);
						});
					}
				}

				private static void CloseOutputSessionCallback(IAsyncResult asyncResult)
				{
					if (!asyncResult.CompletedSynchronously)
					{
						((DuplexRequestBindingElement.DuplexRequestSessionChannel.CloseAsyncResult)asyncResult.AsyncState).TryOperation(false, (DuplexRequestBindingElement.DuplexRequestSessionChannel.CloseAsyncResult t) => t.correlator.innerChannel.Session.EndCloseOutputSession(asyncResult));
					}
				}

				public static new void End(IAsyncResult asyncResult)
				{
					AsyncResult.End<DuplexRequestBindingElement.DuplexRequestSessionChannel.CloseAsyncResult>(asyncResult);
				}

				private static void ServerShutdownCallback(object state, bool timedOut)
				{
					DuplexRequestBindingElement.DuplexRequestSessionChannel.CloseAsyncResult closeAsyncResult = (DuplexRequestBindingElement.DuplexRequestSessionChannel.CloseAsyncResult)state;
					if (timedOut)
					{
						closeAsyncResult.TryComplete(false, new TimeoutException());
						return;
					}
					closeAsyncResult.TryOperation(false, (DuplexRequestBindingElement.DuplexRequestSessionChannel.CloseAsyncResult t) => {
						IAsyncResult asyncResult = t.correlator.innerChannel.BeginClose(t.timeoutHelper.RemainingTime(), DuplexRequestBindingElement.DuplexRequestSessionChannel.CloseAsyncResult.closeChannelCallback, t);
						if (asyncResult.CompletedSynchronously)
						{
							t.correlator.innerChannel.EndClose(asyncResult);
							t.TryComplete(false, null);
						}
					});
				}

				private new void TryComplete(bool completedSync, Exception completeException)
				{
					if (Interlocked.Exchange(ref this.completed, 1) == 0)
					{
						this.correlator.CancelPendingOperations(false);
						this.shutdownWaitHandle.Unregister(null);
						base.Complete(completedSync, completeException);
					}
				}

				private void TryOperation(bool completeSync, Action<DuplexRequestBindingElement.DuplexRequestSessionChannel.CloseAsyncResult> operation)
				{
					Exception exception = null;
					try
					{
						operation(this);
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
					if (exception != null)
					{
						this.TryComplete(completeSync, exception);
					}
				}
			}

			private sealed class DuplexCorrelationAsyncResult : AsyncResult
			{
				private readonly static AsyncResult.AsyncCompletion sendCompletion;

				private readonly static Action<object> timerCallback;

				private DuplexRequestBindingElement.DuplexRequestSessionChannel parent;

				private Message wcfRequest;

				private string clientTrackingId;

				private IOThreadTimer timer;

				private int completed;

				private string requestAction;

				private TimeSpan OriginalTimeout
				{
					get;
					set;
				}

				internal Message ReplyMessage
				{
					get;
					set;
				}

				internal UniqueId RequestMessageId
				{
					get;
					private set;
				}

				static DuplexCorrelationAsyncResult()
				{
					DuplexRequestBindingElement.DuplexRequestSessionChannel.DuplexCorrelationAsyncResult.sendCompletion = new AsyncResult.AsyncCompletion(DuplexRequestBindingElement.DuplexRequestSessionChannel.DuplexCorrelationAsyncResult.SendCompletion);
					DuplexRequestBindingElement.DuplexRequestSessionChannel.DuplexCorrelationAsyncResult.timerCallback = new Action<object>(DuplexRequestBindingElement.DuplexRequestSessionChannel.DuplexCorrelationAsyncResult.TimerCallback);
				}

				public DuplexCorrelationAsyncResult(DuplexRequestBindingElement.DuplexRequestSessionChannel parent, Message wcfRequest, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
				{
					TrackingIdHeader trackingIdHeader;
					DuplexRequestBindingElement.DuplexRequestSessionChannel.DuplexCorrelationAsyncResult duplexCorrelationAsyncResult;
					this.parent = parent;
					this.OriginalTimeout = timeout;
					this.wcfRequest = wcfRequest;
					this.RequestMessageId = this.wcfRequest.Headers.MessageId;
					if (this.RequestMessageId == null)
					{
						this.RequestMessageId = new UniqueId();
						this.wcfRequest.Headers.MessageId = this.RequestMessageId;
					}
					this.requestAction = this.wcfRequest.Headers.Action;
					if (!TrackingIdHeader.TryRead(this.wcfRequest.Headers, out trackingIdHeader))
					{
						this.clientTrackingId = this.RequestMessageId.ToString();
					}
					else
					{
						this.clientTrackingId = trackingIdHeader.Id;
					}
					this.parent.inflightRequests[this.RequestMessageId] = this;
					this.parent.OnBeginRequest();
					if (timeout != TimeSpan.MaxValue)
					{
						this.timer = new IOThreadTimer(DuplexRequestBindingElement.DuplexRequestSessionChannel.DuplexCorrelationAsyncResult.timerCallback, this, false);
						this.timer.Set(timeout);
					}
					lock (base.ThisLock)
					{
						try
						{
							if (base.SyncContinue(this.parent.innerChannel.BeginSend(this.wcfRequest, timeout, base.PrepareAsyncCompletion(DuplexRequestBindingElement.DuplexRequestSessionChannel.DuplexCorrelationAsyncResult.sendCompletion), this)))
							{
								this.TryComplete(true, null);
							}
						}
						catch (Exception exception1)
						{
							Exception exception = exception1;
							if (Fx.IsFatal(exception))
							{
								throw;
							}
							this.parent.inflightRequests.TryRemove(this.RequestMessageId, out duplexCorrelationAsyncResult);
							this.TryComplete(true, exception);
						}
					}
				}

				internal string CreateClientTrackingExceptionInfo()
				{
					DateTime utcNow = DateTime.UtcNow;
					return SRClient.TrackingIdAndTimestampFormat(this.clientTrackingId, utcNow);
				}

				public static new Message End(IAsyncResult result)
				{
					return AsyncResult.End<DuplexRequestBindingElement.DuplexRequestSessionChannel.DuplexCorrelationAsyncResult>(result).ReplyMessage;
				}

				private static bool SendCompletion(IAsyncResult result)
				{
					DuplexRequestBindingElement.DuplexRequestSessionChannel.DuplexCorrelationAsyncResult duplexCorrelationAsyncResult;
					bool flag = false;
					bool flag1 = true;
					Exception exception = null;
					DuplexRequestBindingElement.DuplexRequestSessionChannel.DuplexCorrelationAsyncResult asyncState = (DuplexRequestBindingElement.DuplexRequestSessionChannel.DuplexCorrelationAsyncResult)result.AsyncState;
					try
					{
						try
						{
							asyncState.parent.innerChannel.EndSend(result);
						}
						catch (Exception exception2)
						{
							Exception exception1 = exception2;
							if (Fx.IsFatal(exception1))
							{
								throw;
							}
							exception = exception1;
							flag = true;
							if (ReconnectBindingElement.IsRetryable(asyncState.wcfRequest, exception))
							{
								flag1 = false;
							}
						}
					}
					finally
					{
						if (flag1 && asyncState.wcfRequest != null)
						{
							asyncState.wcfRequest.SafeClose();
						}
						asyncState.wcfRequest = null;
					}
					if (flag)
					{
						asyncState.parent.inflightRequests.TryRemove(asyncState.RequestMessageId, out duplexCorrelationAsyncResult);
						asyncState.TryComplete(result.CompletedSynchronously, exception);
					}
					return false;
				}

				private static void TimerCallback(object state)
				{
					DuplexRequestBindingElement.DuplexRequestSessionChannel.DuplexCorrelationAsyncResult duplexCorrelationAsyncResult = (DuplexRequestBindingElement.DuplexRequestSessionChannel.DuplexCorrelationAsyncResult)state;
					if (duplexCorrelationAsyncResult.parent.inflightRequests.TryRemove(duplexCorrelationAsyncResult.RequestMessageId, out duplexCorrelationAsyncResult))
					{
						string timeoutOnRequest = Resources.TimeoutOnRequest;
						object[] originalTimeout = new object[] { duplexCorrelationAsyncResult.OriginalTimeout };
						string str = SRClient.TrackableExceptionMessageFormat(Microsoft.ServiceBus.SR.GetString(timeoutOnRequest, originalTimeout), duplexCorrelationAsyncResult.CreateClientTrackingExceptionInfo());
						MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteLogOperationWarning("DuplexMessageCorrelation.TimerCallback", string.Format(CultureInfo.InvariantCulture, "In IO Thread Time callback , removing Request with Message Id:{0} and terminating the request with error: {1}", new object[] { duplexCorrelationAsyncResult.RequestMessageId.ToString(), str })));
						duplexCorrelationAsyncResult.TryComplete(false, new TimeoutException(str));
					}
				}

				public new void TryComplete(bool completedSynchronously, Exception exception)
				{
					bool flag;
					bool flag1 = false;
					lock (base.ThisLock)
					{
						if (Interlocked.Exchange(ref this.completed, 1) == 0)
						{
							if (this.timer != null)
							{
								this.timer.Cancel();
								this.timer = null;
							}
							flag1 = true;
						}
					}
					if (flag1)
					{
						if (exception != null)
						{
							if (string.IsNullOrWhiteSpace(this.requestAction))
							{
								flag = false;
							}
							else
							{
								flag = (this.requestAction.Equals("http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageSender/Send", StringComparison.OrdinalIgnoreCase) ? true : this.requestAction.Equals("http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageReceiver/SetSessionState", StringComparison.OrdinalIgnoreCase));
							}
							if (flag && this.parent.IsMaxMessageSizeExceededFault)
							{
								string str = SRClient.TrackableExceptionMessageFormat(SRClient.MessageSizeExceeded, this.CreateClientTrackingExceptionInfo());
								ExceptionDetail exceptionDetail = new ExceptionDetail(new MessageSizeExceededException(str));
								FaultCode faultCode = FaultCode.CreateReceiverFaultCode("InternalServiceFault", "http://schemas.microsoft.com/netservices/2011/06/servicebus");
								FaultReason faultReason = new FaultReason(new FaultReasonText(str, CultureInfo.CurrentCulture));
								if (!this.parent.includeExceptionDetails)
								{
									exception = new FaultException<ExceptionDetailNoStackTrace>(new ExceptionDetailNoStackTrace(exceptionDetail, true), faultReason, faultCode);
								}
								else
								{
									exception = new FaultException<ExceptionDetail>(exceptionDetail, faultReason, faultCode);
								}
							}
						}
						this.parent.OnEndRequest();
						base.Complete(completedSynchronously, exception);
					}
				}
			}
		}
	}
}