using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.ServiceModel.Channels;
using System.Threading;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	internal sealed class SbmpBrowsableMessageSession : MessageSession
	{
		private readonly string path;

		private readonly MessagingEntityType entityType;

		private AutoResetEvent resetEvent;

		public override string Path
		{
			get
			{
				return this.path;
			}
		}

		public override int PrefetchCount
		{
			get
			{
				return 0;
			}
		}

		public SbmpBrowsableMessageSession(string path, string sessionId, MessagingEntityType entityType, SbmpMessagingFactory messagingFactory, Microsoft.ServiceBus.RetryPolicy retryPolicy) : base(ReceiveMode.PeekLock, sessionId, DateTime.MinValue, messagingFactory, retryPolicy)
		{
			this.path = path;
			this.entityType = entityType;
			this.resetEvent = new AutoResetEvent(true);
		}

		protected override void OnAbandon(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, IDictionary<string, object> propertiesToModify, TimeSpan timeout)
		{
			throw new InvalidOperationException(SRClient.InvalidOperationOnSessionBrowser);
		}

		protected override IAsyncResult OnBeginAbandon(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, IDictionary<string, object> propertiesToModify, bool fromSync, TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new InvalidOperationException(SRClient.InvalidOperationOnSessionBrowser);
		}

		protected override IAsyncResult OnBeginComplete(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, bool fromSync, TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new InvalidOperationException(SRClient.InvalidOperationOnSessionBrowser);
		}

		protected override IAsyncResult OnBeginDeadLetter(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, IDictionary<string, object> propertiesToModify, string deadLetterReason, string deadLetterErrorDescription, bool fromSync, TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new InvalidOperationException(SRClient.InvalidOperationOnSessionBrowser);
		}

		protected override IAsyncResult OnBeginDefer(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, IDictionary<string, object> propertiesToModify, bool fromSync, TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new InvalidOperationException(SRClient.InvalidOperationOnSessionBrowser);
		}

		protected override IAsyncResult OnBeginGetState(TrackingContext trackingContext, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new SbmpBrowsableMessageSession.GetStateAsyncResult(this, trackingContext, timeout, callback, state);
		}

		protected override IAsyncResult OnBeginPeek(TrackingContext trackingContext, long fromSequenceNumber, int messageCount, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new SbmpBrowsableMessageSession.PeekMessagesAsyncResult(this, trackingContext, fromSequenceNumber, messageCount, timeout, callback, state);
		}

		protected override IAsyncResult OnBeginRenewLock(TrackingContext trackingContext, TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new InvalidOperationException(SRClient.InvalidOperationOnSessionBrowser);
		}

		protected override IAsyncResult OnBeginSetState(TrackingContext trackingContext, Stream stream, TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new InvalidOperationException(SRClient.InvalidOperationOnSessionBrowser);
		}

		protected override IAsyncResult OnBeginTryReceive(TrackingContext trackingContext, int messageCount, TimeSpan serverWaitTime, AsyncCallback callback, object state)
		{
			throw new InvalidOperationException(SRClient.InvalidOperationOnSessionBrowser);
		}

		protected override IAsyncResult OnBeginTryReceive(TrackingContext trackingContext, IEnumerable<long> receipts, TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new InvalidOperationException(SRClient.InvalidOperationOnSessionBrowser);
		}

		protected override IAsyncResult OnBeginTryReceive2(TrackingContext trackingContext, int messageCount, TimeSpan serverWaitTime, AsyncCallback callback, object state)
		{
			throw new InvalidOperationException(SRClient.InvalidOperationOnSessionBrowser);
		}

		protected override void OnComplete(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, TimeSpan timeout)
		{
			throw new InvalidOperationException(SRClient.InvalidOperationOnSessionBrowser);
		}

		protected override void OnDeadLetter(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, IDictionary<string, object> propertiesToModify, string deadLetterReason, string deadLetterErrorDescription, TimeSpan timeout)
		{
			throw new InvalidOperationException(SRClient.InvalidOperationOnSessionBrowser);
		}

		protected override void OnDefer(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, IDictionary<string, object> propertiesToModify, TimeSpan timeout)
		{
			throw new InvalidOperationException(SRClient.InvalidOperationOnSessionBrowser);
		}

		protected override void OnEndAbandon(IAsyncResult result)
		{
			throw new InvalidOperationException(SRClient.InvalidOperationOnSessionBrowser);
		}

		protected override void OnEndComplete(IAsyncResult result)
		{
			throw new InvalidOperationException(SRClient.InvalidOperationOnSessionBrowser);
		}

		protected override void OnEndDeadLetter(IAsyncResult result)
		{
			throw new InvalidOperationException(SRClient.InvalidOperationOnSessionBrowser);
		}

		protected override void OnEndDefer(IAsyncResult result)
		{
			throw new InvalidOperationException(SRClient.InvalidOperationOnSessionBrowser);
		}

		protected override Stream OnEndGetState(IAsyncResult result)
		{
			SessionState sessionState = AsyncResult<SbmpBrowsableMessageSession.GetStateAsyncResult>.End(result).SessionState;
			if (sessionState == null)
			{
				return null;
			}
			return sessionState.Stream;
		}

		protected override IEnumerable<BrokeredMessage> OnEndPeek(IAsyncResult result)
		{
			return AsyncResult<SbmpBrowsableMessageSession.PeekMessagesAsyncResult>.End(result).Messages;
		}

		protected override DateTime OnEndRenewLock(IAsyncResult result)
		{
			throw new InvalidOperationException(SRClient.InvalidOperationOnSessionBrowser);
		}

		protected override void OnEndSetState(IAsyncResult result)
		{
			throw new InvalidOperationException(SRClient.InvalidOperationOnSessionBrowser);
		}

		protected override bool OnEndTryReceive(IAsyncResult result, out IEnumerable<BrokeredMessage> messages)
		{
			messages = null;
			throw new InvalidOperationException(SRClient.InvalidOperationOnSessionBrowser);
		}

		protected override bool OnEndTryReceive2(IAsyncResult result, out IEnumerable<BrokeredMessage> messages)
		{
			messages = null;
			throw new InvalidOperationException(SRClient.InvalidOperationOnSessionBrowser);
		}

		protected override bool OnTryReceive(TrackingContext trackingContext, IEnumerable<long> receipts, TimeSpan timeout, out IEnumerable<BrokeredMessage> messages)
		{
			messages = null;
			throw new InvalidOperationException(SRClient.InvalidOperationOnSessionBrowser);
		}

		protected override bool OnTryReceive(TrackingContext trackingContext, int messageCount, TimeSpan timeout, out IEnumerable<BrokeredMessage> messages)
		{
			messages = null;
			throw new InvalidOperationException(SRClient.InvalidOperationOnSessionBrowser);
		}

		private sealed class GetStateAsyncResult : IteratorAsyncResult<SbmpBrowsableMessageSession.GetStateAsyncResult>
		{
			private readonly TrackingContext trackingContext;

			private SbmpBrowsableMessageSession sbmpMessageSession;

			private Message wcfMessage;

			public Message Response
			{
				get;
				private set;
			}

			public SessionState SessionState
			{
				get;
				private set;
			}

			public GetStateAsyncResult(SbmpBrowsableMessageSession sbmpMessageSession, TrackingContext trackingContext, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.sbmpMessageSession = sbmpMessageSession;
				this.trackingContext = trackingContext;
				base.Start();
			}

			private IAsyncResult BeginAcceptMessageSessionBrowser(TimeSpan timeout, AsyncCallback callback, object state)
			{
				return new AcceptMessageSessionBrowserAsyncResult(this.sbmpMessageSession, (SbmpMessagingFactory)this.sbmpMessageSession.MessagingFactory, new MessagingEntityType?(this.sbmpMessageSession.entityType), ReceiveMode.PeekLock, 0, timeout, callback, state);
			}

			private void EndAcceptMessageSessionBrowser(IAsyncResult result)
			{
				AcceptMessageSessionBrowserAsyncResult acceptMessageSessionBrowserAsyncResult = (AcceptMessageSessionBrowserAsyncResult)result;
				this.sbmpMessageSession = (SbmpBrowsableMessageSession)AcceptMessageSessionBrowserAsyncResult.End(result);
				this.SessionState = acceptMessageSessionBrowserAsyncResult.SessionState;
			}

			protected override IEnumerator<IteratorAsyncResult<SbmpBrowsableMessageSession.GetStateAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				object obj = null;
				bool flag = false;
				if (this.sbmpMessageSession.InnerMessageReceiver == null)
				{
					bool flag1 = false;
					try
					{
						object thisLock = this.sbmpMessageSession.ThisLock;
						object obj1 = thisLock;
						obj = thisLock;
						Monitor.Enter(obj1, ref flag1);
						this.sbmpMessageSession.resetEvent.WaitOne();
					}
					finally
					{
						if (flag1)
						{
							Monitor.Exit(obj);
						}
					}
					try
					{
						if (this.sbmpMessageSession.InnerMessageReceiver != null)
						{
							goto Label0;
						}
						flag = true;
						SbmpBrowsableMessageSession.GetStateAsyncResult getStateAsyncResult = this;
						IteratorAsyncResult<SbmpBrowsableMessageSession.GetStateAsyncResult>.BeginCall beginCall = (SbmpBrowsableMessageSession.GetStateAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.BeginAcceptMessageSessionBrowser(t, c, s);
						yield return getStateAsyncResult.CallAsync(beginCall, (SbmpBrowsableMessageSession.GetStateAsyncResult thisPtr, IAsyncResult r) => thisPtr.EndAcceptMessageSessionBrowser(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
					}
					finally
					{
						this.sbmpMessageSession.resetEvent.Set();
					}
				}
			Label0:
				if (this.sbmpMessageSession.InnerMessageReceiver != null && !flag)
				{
					GetSessionStateCommand getSessionStateCommand = new GetSessionStateCommand()
					{
						SessionId = this.sbmpMessageSession.SessionId,
						Timeout = base.RemainingTime(),
						TransactionId = null,
						IsBrowseMode = true
					};
					GetSessionStateCommand getSessionStateCommand1 = getSessionStateCommand;
					RequestInfo requestInfo1 = new RequestInfo()
					{
						ServerTimeout = new TimeSpan?(getSessionStateCommand1.Timeout)
					};
					RequestInfo requestInfo = requestInfo1;
					SbmpMessageReceiver innerMessageReceiver = (SbmpMessageReceiver)this.sbmpMessageSession.InnerMessageReceiver;
					this.wcfMessage = innerMessageReceiver.MessageCreator.CreateWcfMessage("http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageReceiver/GetSessionState", getSessionStateCommand1, null, this.sbmpMessageSession.RetryPolicy, this.trackingContext, requestInfo);
					yield return base.CallAsync((SbmpBrowsableMessageSession.GetStateAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => innerMessageReceiver.SbmpMessagingFactory.Channel.BeginRequest(thisPtr.wcfMessage, SbmpProtocolDefaults.BufferTimeout(t, innerMessageReceiver.MessagingFactory.GetSettings().EnableAdditionalClientTimeout), c, s), (SbmpBrowsableMessageSession.GetStateAsyncResult thisPtr, IAsyncResult a) => thisPtr.Response = innerMessageReceiver.SbmpMessagingFactory.Channel.EndRequest(a), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
					if (base.LastAsyncStepException == null)
					{
						this.SessionState = this.Response.GetBody<GetSessionStateResponseCommand>().SessionState;
					}
				}
			}
		}

		private sealed class PeekMessagesAsyncResult : IteratorAsyncResult<SbmpBrowsableMessageSession.PeekMessagesAsyncResult>
		{
			private readonly TrackingContext trackingContext;

			private readonly long fromSequenceNumber;

			private readonly int messageCount;

			private SbmpBrowsableMessageSession sbmpMessageSession;

			public IEnumerable<BrokeredMessage> Messages;

			public PeekMessagesAsyncResult(SbmpBrowsableMessageSession sbmpMessageSession, TrackingContext trackingContext, long fromSequenceNumber, int messageCount, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.sbmpMessageSession = sbmpMessageSession;
				this.trackingContext = trackingContext;
				this.fromSequenceNumber = fromSequenceNumber;
				this.messageCount = messageCount;
				base.Start();
			}

			private IAsyncResult BeginAcceptMessageSessionBrowser(TimeSpan timeout, AsyncCallback callback, object state)
			{
				return new AcceptMessageSessionBrowserAsyncResult(this.sbmpMessageSession, (SbmpMessagingFactory)this.sbmpMessageSession.MessagingFactory, new MessagingEntityType?(this.sbmpMessageSession.entityType), ReceiveMode.PeekLock, 0, timeout, callback, state);
			}

			private void EndAcceptMessageSessionBrowser(IAsyncResult result)
			{
				this.sbmpMessageSession = (SbmpBrowsableMessageSession)AcceptMessageSessionBrowserAsyncResult.End(result);
			}

			protected override IEnumerator<IteratorAsyncResult<SbmpBrowsableMessageSession.PeekMessagesAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				object obj = null;
				if (this.sbmpMessageSession.InnerMessageReceiver == null)
				{
					bool flag = false;
					try
					{
						object thisLock = this.sbmpMessageSession.ThisLock;
						object obj1 = thisLock;
						obj = thisLock;
						Monitor.Enter(obj1, ref flag);
						this.sbmpMessageSession.resetEvent.WaitOne();
					}
					finally
					{
						if (flag)
						{
							Monitor.Exit(obj);
						}
					}
					try
					{
						if (this.sbmpMessageSession.InnerMessageReceiver != null)
						{
							goto Label0;
						}
						SbmpBrowsableMessageSession.PeekMessagesAsyncResult peekMessagesAsyncResult = this;
						IteratorAsyncResult<SbmpBrowsableMessageSession.PeekMessagesAsyncResult>.BeginCall beginCall = (SbmpBrowsableMessageSession.PeekMessagesAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.BeginAcceptMessageSessionBrowser(t, c, s);
						yield return peekMessagesAsyncResult.CallAsync(beginCall, (SbmpBrowsableMessageSession.PeekMessagesAsyncResult thisPtr, IAsyncResult r) => thisPtr.EndAcceptMessageSessionBrowser(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
					}
					finally
					{
						this.sbmpMessageSession.resetEvent.Set();
					}
				}
			Label0:
				if (this.sbmpMessageSession.InnerMessageReceiver != null)
				{
					SbmpBrowsableMessageSession.PeekMessagesAsyncResult peekMessagesAsyncResult1 = this;
					IteratorAsyncResult<SbmpBrowsableMessageSession.PeekMessagesAsyncResult>.BeginCall beginCall1 = (SbmpBrowsableMessageSession.PeekMessagesAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.sbmpMessageSession.InnerMessageReceiver.BeginPeekBatch(thisPtr.trackingContext, thisPtr.fromSequenceNumber, thisPtr.messageCount, SbmpProtocolDefaults.BufferTimeout(t, thisPtr.sbmpMessageSession.MessagingFactory.GetSettings().EnableAdditionalClientTimeout), c, s);
					yield return peekMessagesAsyncResult1.CallAsync(beginCall1, (SbmpBrowsableMessageSession.PeekMessagesAsyncResult thisPtr, IAsyncResult a) => thisPtr.Messages = thisPtr.sbmpMessageSession.InnerMessageReceiver.EndPeekBatch(a), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				}
			}
		}
	}
}