using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Runtime.CompilerServices;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	internal sealed class AcceptMessageSessionAsyncResult : AsyncResult
	{
		private readonly static AsyncResult.AsyncCompletion operationComplete;

		private readonly CreateLinkSettings createLinkSettings;

		private readonly int prefetchCount;

		private readonly string sessionId;

		private RetryPolicy retryPolicy;

		private MessageSession result;

		private SbmpMessagingFactory MessagingFactory
		{
			get;
			set;
		}

		static AcceptMessageSessionAsyncResult()
		{
			AcceptMessageSessionAsyncResult.operationComplete = new AsyncResult.AsyncCompletion(AcceptMessageSessionAsyncResult.OperationComplete);
		}

		public AcceptMessageSessionAsyncResult(SbmpMessagingFactory messagingFactory, string entityName, string sessionId, MessagingEntityType? entityType, ReceiveMode receiveMode, int prefetchCount, Lazy<SbmpMessageCreator> controlMessageCreator, TimeSpan serverWaitTime, TimeSpan timeout, AsyncCallback callback, object state) : this(messagingFactory, entityName, sessionId, entityType, receiveMode, prefetchCount, controlMessageCreator, null, serverWaitTime, timeout, callback, state)
		{
		}

		public AcceptMessageSessionAsyncResult(SbmpMessagingFactory messagingFactory, string entityName, string sessionId, MessagingEntityType? entityType, ReceiveMode receiveMode, int prefetchCount, Lazy<SbmpMessageCreator> controlMessageCreator, RetryPolicy retryPolicy, TimeSpan serverWaitTime, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
		{
			this.prefetchCount = prefetchCount;
			this.sessionId = sessionId;
			this.MessagingFactory = messagingFactory;
			this.retryPolicy = retryPolicy ?? messagingFactory.RetryPolicy.Clone();
			LinkInfo linkInfo = new LinkInfo()
			{
				LinkId = messagingFactory.GetNextLinkId(),
				ConnectionId = messagingFactory.ConnectionId,
				LinkType = LinkType.Receive,
				IsSessionReceiver = true,
				ReceiveMode = receiveMode,
				EntityName = entityName,
				EntityType = entityType
			};
			this.createLinkSettings = new CreateLinkSettings(messagingFactory, entityName, entityName, linkInfo, controlMessageCreator);
			AcceptMessageSessionCommand acceptMessageSessionCommand = new AcceptMessageSessionCommand()
			{
				SessionId = this.sessionId,
				Timeout = serverWaitTime,
				OperationTimeout = timeout,
				PrefetchCount = this.prefetchCount
			};
			AcceptMessageSessionCommand acceptMessageSessionCommand1 = acceptMessageSessionCommand;
			string linkId = null;
			if (this.createLinkSettings.ControlMessageCreator != null && !string.IsNullOrWhiteSpace(this.createLinkSettings.ControlMessageCreator.Value.LinkInfo.LinkId))
			{
				linkId = this.createLinkSettings.ControlMessageCreator.Value.LinkInfo.LinkId;
			}
			RequestInfo requestInfo = new RequestInfo()
			{
				ServerTimeout = new TimeSpan?(acceptMessageSessionCommand1.Timeout),
				SessionId = acceptMessageSessionCommand1.SessionId
			};
			RequestInfo requestInfo1 = requestInfo;
			Message message = this.createLinkSettings.MessageCreator.CreateWcfMessage("http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageReceiver/AcceptMessageSession", acceptMessageSessionCommand1, linkId, this.retryPolicy, null, requestInfo1);
			if (base.SyncContinue(this.MessagingFactory.Channel.BeginRequest(message, SbmpProtocolDefaults.BufferTimeout(timeout, this.MessagingFactory.GetSettings().EnableAdditionalClientTimeout), base.PrepareAsyncCompletion(AcceptMessageSessionAsyncResult.operationComplete), this)))
			{
				base.Complete(true);
			}
		}

		public static new MessageSession End(IAsyncResult result)
		{
			return AsyncResult.End<AcceptMessageSessionAsyncResult>(result).result;
		}

		private static bool OperationComplete(IAsyncResult asyncResult)
		{
			AcceptMessageSessionAsyncResult asyncState = (AcceptMessageSessionAsyncResult)asyncResult.AsyncState;
			Message message = asyncState.MessagingFactory.Channel.EndRequest(asyncResult);
			AcceptMessageSessionResponseCommand body = message.GetBody<AcceptMessageSessionResponseCommand>();
			SessionState sessionState = body.SessionState;
			MessageCollection messages = body.Messages;
			DateTime lockedUntilUtc = body.LockedUntilUtc;
			asyncState.createLinkSettings.LinkInfo.LinkId = (body.LinkId != null ? body.LinkId : asyncState.createLinkSettings.LinkInfo.LinkId);
			asyncState.createLinkSettings.LinkInfo.SessionId = body.SessionId;
			SbmpMessageReceiver sbmpMessageReceiver = new SbmpMessageReceiver(asyncState.createLinkSettings.EntityName, false, asyncState.createLinkSettings.MessagingFactory, asyncState.createLinkSettings.MessageCreator, asyncState.createLinkSettings.ControlMessageCreator, asyncState.createLinkSettings.LinkInfo, true, asyncState.retryPolicy, messages);
			sbmpMessageReceiver.Open();
			asyncState.result = new SbmpMessageSession(body.SessionId, lockedUntilUtc, sessionState, sbmpMessageReceiver)
			{
				PrefetchCount = asyncState.prefetchCount
			};
			return true;
		}
	}
}