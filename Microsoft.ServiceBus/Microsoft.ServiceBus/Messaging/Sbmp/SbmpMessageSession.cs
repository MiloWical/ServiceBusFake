using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Tracing;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.ServiceModel.Channels;
using System.Transactions;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	internal sealed class SbmpMessageSession : MessageSession
	{
		private readonly SbmpMessagingFactory messagingFactory;

		private readonly string parentLinkId;

		private SessionState sessionState;

		internal bool CachedState
		{
			get;
			private set;
		}

		public SbmpMessageSession(string sessionId, DateTime lockedUntilUtc, SessionState sessionState, SbmpMessageReceiver innerReceiver) : base(innerReceiver.Mode, sessionId, lockedUntilUtc, innerReceiver)
		{
			this.sessionState = sessionState;
			this.parentLinkId = (innerReceiver.ControlMessageCreator == null ? string.Empty : innerReceiver.ControlMessageCreator.Value.LinkInfo.LinkId);
			this.CachedState = sessionState != null;
			this.messagingFactory = (SbmpMessagingFactory)innerReceiver.MessagingFactory;
		}

		protected override IAsyncResult OnBeginGetState(TrackingContext trackingContext, TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (this.CachedState)
			{
				return new CompletedAsyncResult(callback, state);
			}
			return new SbmpMessageSession.GetStateAsyncResult(this, trackingContext, timeout, callback, state);
		}

		protected override IAsyncResult OnBeginRenewLock(TrackingContext trackingContext, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new SbmpMessageSession.RenewLockAsyncResult(this, trackingContext, timeout, callback, state);
		}

		protected override IAsyncResult OnBeginSetState(TrackingContext trackingContext, Stream stream, TimeSpan timeout, AsyncCallback callback, object state)
		{
			this.sessionState = null;
			this.CachedState = false;
			return new SbmpMessageSession.SetStateAsyncResult(this, trackingContext, stream, timeout, callback, state);
		}

		protected override Stream OnEndGetState(IAsyncResult result)
		{
			if (!this.CachedState)
			{
				GetSessionStateResponseCommand body = AsyncResult<SbmpMessageSession.GetStateAsyncResult>.End(result).Response.GetBody<GetSessionStateResponseCommand>();
				this.CachedState = body.SessionState.Stream == null;
				return body.SessionState.Stream;
			}
			CompletedAsyncResult.End(result);
			Stream stream = null;
			if (this.sessionState != null)
			{
				stream = this.sessionState.Stream;
				this.sessionState = null;
				this.CachedState = false;
			}
			return stream;
		}

		protected override DateTime OnEndRenewLock(IAsyncResult result)
		{
			return AsyncResult<SbmpMessageSession.RenewLockAsyncResult>.End(result).Response.GetBody<SessionRenewLockResponseCommand>().LockedUntilUtc;
		}

		protected override void OnEndSetState(IAsyncResult result)
		{
			AsyncResult<SbmpMessageSession.SetStateAsyncResult>.End(result);
		}

		private sealed class GetStateAsyncResult : SbmpTransactionalAsyncResult<SbmpMessageSession.GetStateAsyncResult>
		{
			private readonly SbmpMessageSession sbmpMessageSession;

			private readonly TrackingContext trackingContext;

			public GetStateAsyncResult(SbmpMessageSession sbmpMessageSession, TrackingContext trackingContext, TimeSpan timeout, AsyncCallback callback, object state) : base((SbmpMessagingFactory)sbmpMessageSession.MessagingFactory, ((SbmpMessageReceiver)sbmpMessageSession.InnerMessageReceiver).MessageCreator, ((SbmpMessageReceiver)sbmpMessageSession.InnerMessageReceiver).ControlMessageCreator, timeout, callback, state)
			{
				this.sbmpMessageSession = sbmpMessageSession;
				this.trackingContext = trackingContext;
				base.Start();
			}

			protected override Message CreateWcfMessage()
			{
				string localIdentifier;
				if (base.Transaction != null)
				{
					localIdentifier = base.Transaction.TransactionInformation.LocalIdentifier;
				}
				else
				{
					localIdentifier = null;
				}
				string str = localIdentifier;
				MessagingClientEtwProvider.TraceClient(() => {
				});
				GetSessionStateCommand getSessionStateCommand = new GetSessionStateCommand()
				{
					SessionId = this.sbmpMessageSession.SessionId,
					Timeout = base.RemainingTime(),
					TransactionId = str
				};
				GetSessionStateCommand getSessionStateCommand1 = getSessionStateCommand;
				RequestInfo requestInfo = new RequestInfo()
				{
					ServerTimeout = new TimeSpan?(getSessionStateCommand1.Timeout),
					TransactionId = getSessionStateCommand1.TransactionId
				};
				RequestInfo requestInfo1 = requestInfo;
				Message message = base.MessageCreator.CreateWcfMessage("http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageReceiver/GetSessionState", getSessionStateCommand1, this.sbmpMessageSession.parentLinkId, this.sbmpMessageSession.RetryPolicy, this.trackingContext, requestInfo1);
				return message;
			}

			protected override void PartitionInfoSetter(RequestInfo requestInfo)
			{
				requestInfo.SessionId = this.sbmpMessageSession.SessionId;
			}
		}

		private sealed class RenewLockAsyncResult : SbmpTransactionalAsyncResult<SbmpMessageSession.RenewLockAsyncResult>
		{
			private readonly SbmpMessageSession sbmpMessageSession;

			private readonly TrackingContext trackingContext;

			public RenewLockAsyncResult(SbmpMessageSession sbmpMessageSession, TrackingContext trackingContext, TimeSpan timeout, AsyncCallback callback, object state) : base((SbmpMessagingFactory)sbmpMessageSession.MessagingFactory, ((SbmpMessageReceiver)sbmpMessageSession.InnerMessageReceiver).MessageCreator, ((SbmpMessageReceiver)sbmpMessageSession.InnerMessageReceiver).ControlMessageCreator, timeout, callback, state)
			{
				this.sbmpMessageSession = sbmpMessageSession;
				this.trackingContext = trackingContext;
				base.Start();
			}

			protected override Message CreateWcfMessage()
			{
				string localIdentifier;
				MessagingClientEtwProvider.TraceClient(() => {
				});
				Transaction transaction = base.Transaction;
				SessionRenewLockCommand sessionRenewLockCommand = new SessionRenewLockCommand()
				{
					SessionId = this.sbmpMessageSession.SessionId,
					Timeout = base.RemainingTime()
				};
				SessionRenewLockCommand sessionRenewLockCommand1 = sessionRenewLockCommand;
				RequestInfo requestInfo = new RequestInfo()
				{
					ServerTimeout = new TimeSpan?(sessionRenewLockCommand1.Timeout)
				};
				RequestInfo requestInfo1 = requestInfo;
				if (transaction != null)
				{
					localIdentifier = transaction.TransactionInformation.LocalIdentifier;
				}
				else
				{
					localIdentifier = null;
				}
				requestInfo1.TransactionId = localIdentifier;
				RequestInfo requestInfo2 = requestInfo;
				Message message = base.MessageCreator.CreateWcfMessage("http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageReceiver/SessionRenewLock", sessionRenewLockCommand1, this.sbmpMessageSession.parentLinkId, this.sbmpMessageSession.RetryPolicy, this.trackingContext, requestInfo2);
				return message;
			}

			protected override void PartitionInfoSetter(RequestInfo requestInfo)
			{
				requestInfo.SessionId = this.sbmpMessageSession.SessionId;
			}
		}

		private sealed class SetStateAsyncResult : SbmpTransactionalAsyncResult<SbmpMessageSession.SetStateAsyncResult>
		{
			private readonly SbmpMessageSession sbmpMessageSession;

			private readonly TrackingContext trackingContext;

			private readonly Stream stream;

			public SetStateAsyncResult(SbmpMessageSession sbmpMessageSession, TrackingContext trackingContext, Stream stream, TimeSpan timeout, AsyncCallback callback, object state) : base(sbmpMessageSession.messagingFactory, ((SbmpMessageReceiver)sbmpMessageSession.InnerMessageReceiver).MessageCreator, ((SbmpMessageReceiver)sbmpMessageSession.InnerMessageReceiver).ControlMessageCreator, timeout, callback, state)
			{
				this.sbmpMessageSession = sbmpMessageSession;
				this.trackingContext = trackingContext;
				this.stream = stream;
				base.Start();
			}

			protected override Message CreateWcfMessage()
			{
				string localIdentifier;
				if (base.Transaction != null)
				{
					localIdentifier = base.Transaction.TransactionInformation.LocalIdentifier;
				}
				else
				{
					localIdentifier = null;
				}
				string str = localIdentifier;
				MessagingClientEtwProvider.TraceClient(() => {
				});
				SetSessionStateCommand setSessionStateCommand = new SetSessionStateCommand()
				{
					SessionId = this.sbmpMessageSession.SessionId,
					SessionState = new SessionState(this.stream),
					Timeout = base.RemainingTime(),
					TransactionId = str
				};
				SetSessionStateCommand setSessionStateCommand1 = setSessionStateCommand;
				RequestInfo requestInfo = new RequestInfo()
				{
					ServerTimeout = new TimeSpan?(setSessionStateCommand1.Timeout),
					TransactionId = setSessionStateCommand1.TransactionId
				};
				RequestInfo requestInfo1 = requestInfo;
				Message message = base.MessageCreator.CreateWcfMessage("http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageReceiver/SetSessionState", setSessionStateCommand1, this.sbmpMessageSession.parentLinkId, this.sbmpMessageSession.RetryPolicy, this.trackingContext, requestInfo1);
				return message;
			}

			protected override void PartitionInfoSetter(RequestInfo requestInfo)
			{
				requestInfo.SessionId = this.sbmpMessageSession.SessionId;
			}
		}
	}
}