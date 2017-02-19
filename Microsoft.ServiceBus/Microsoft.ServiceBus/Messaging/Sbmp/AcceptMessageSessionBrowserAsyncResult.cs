using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Runtime.CompilerServices;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	internal sealed class AcceptMessageSessionBrowserAsyncResult : AsyncResult
	{
		private readonly static AsyncResult.AsyncCompletion operationComplete;

		private readonly CreateLinkSettings createLinkSettings;

		private readonly string sessionId;

		private MessageSession messageSession;

		public SbmpMessageReceiver MessageReceiver
		{
			get;
			private set;
		}

		private SbmpMessagingFactory MessagingFactory
		{
			get;
			set;
		}

		public Microsoft.ServiceBus.Messaging.Sbmp.SessionState SessionState
		{
			get;
			private set;
		}

		static AcceptMessageSessionBrowserAsyncResult()
		{
			AcceptMessageSessionBrowserAsyncResult.operationComplete = new AsyncResult.AsyncCompletion(AcceptMessageSessionBrowserAsyncResult.OperationComplete);
		}

		public AcceptMessageSessionBrowserAsyncResult(SbmpBrowsableMessageSession messageSession, SbmpMessagingFactory messagingFactory, MessagingEntityType? entityType, ReceiveMode receiveMode, int prefetchCount, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
		{
			this.messageSession = messageSession;
			this.sessionId = messageSession.SessionId;
			this.MessagingFactory = messagingFactory;
			string path = messageSession.Path;
			LinkInfo linkInfo = new LinkInfo()
			{
				LinkId = messagingFactory.GetNextLinkId(),
				ConnectionId = messagingFactory.ConnectionId,
				LinkType = LinkType.Receive,
				IsSessionReceiver = true,
				ReceiveMode = receiveMode,
				EntityName = path,
				EntityType = entityType,
				SessionId = messageSession.SessionId
			};
			this.createLinkSettings = new CreateLinkSettings(messagingFactory, path, path, linkInfo, null);
			AcceptMessageSessionCommand acceptMessageSessionCommand = new AcceptMessageSessionCommand()
			{
				SessionId = this.sessionId,
				Timeout = timeout,
				PrefetchCount = prefetchCount,
				IsSessionBrowser = true
			};
			AcceptMessageSessionCommand acceptMessageSessionCommand1 = acceptMessageSessionCommand;
			RequestInfo requestInfo = new RequestInfo()
			{
				ServerTimeout = new TimeSpan?(acceptMessageSessionCommand1.Timeout),
				SessionId = acceptMessageSessionCommand1.SessionId
			};
			RequestInfo requestInfo1 = requestInfo;
			Message message = this.createLinkSettings.MessageCreator.CreateWcfMessage("http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageReceiver/AcceptMessageSession", acceptMessageSessionCommand1, null, this.messageSession.RetryPolicy, null, requestInfo1);
			if (base.SyncContinue(this.MessagingFactory.Channel.BeginRequest(message, SbmpProtocolDefaults.BufferTimeout(timeout, this.MessagingFactory.GetSettings().EnableAdditionalClientTimeout), base.PrepareAsyncCompletion(AcceptMessageSessionBrowserAsyncResult.operationComplete), this)))
			{
				base.Complete(true);
			}
		}

		public static new MessageSession End(IAsyncResult result)
		{
			return AsyncResult.End<AcceptMessageSessionBrowserAsyncResult>(result).messageSession;
		}

		private static bool OperationComplete(IAsyncResult asyncResult)
		{
			AcceptMessageSessionBrowserAsyncResult asyncState = (AcceptMessageSessionBrowserAsyncResult)asyncResult.AsyncState;
			Message message = asyncState.MessagingFactory.Channel.EndRequest(asyncResult);
			asyncState.SessionState = message.GetBody<AcceptMessageSessionResponseCommand>().SessionState;
			asyncState.MessageReceiver = new SbmpMessageReceiver(asyncState.createLinkSettings.EntityName, false, asyncState.createLinkSettings.MessagingFactory, asyncState.createLinkSettings.MessageCreator, null, asyncState.createLinkSettings.LinkInfo, asyncState.messageSession.RetryPolicy);
			asyncState.MessageReceiver.Open();
			if (asyncState.messageSession != null)
			{
				asyncState.messageSession.InnerMessageReceiver = asyncState.MessageReceiver;
				asyncState.messageSession.InstanceTrackingContext = asyncState.MessageReceiver.InstanceTrackingContext;
				asyncState.messageSession.PrefetchCount = 0;
			}
			return true;
		}
	}
}