using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Runtime.CompilerServices;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	internal class AcceptMessageSessionForNamespaceAsyncResult : AsyncResult
	{
		private readonly static AsyncResult.AsyncCompletion operationComplete;

		private readonly CreateControlLinkSettings controlLinkSettings;

		private readonly ReceiveMode receiveMode;

		private MessageSession result;

		private SbmpMessagingFactory MessagingFactory
		{
			get;
			set;
		}

		static AcceptMessageSessionForNamespaceAsyncResult()
		{
			AcceptMessageSessionForNamespaceAsyncResult.operationComplete = new AsyncResult.AsyncCompletion(AcceptMessageSessionForNamespaceAsyncResult.OperationComplete);
		}

		public AcceptMessageSessionForNamespaceAsyncResult(SbmpMessagingFactory messagingFactory, ReceiveMode receiveMode, int prefetchCount, CreateControlLinkSettings controlLinkSettings, TimeSpan serverWaitTime, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
		{
			this.MessagingFactory = messagingFactory;
			this.controlLinkSettings = controlLinkSettings;
			this.receiveMode = receiveMode;
			AcceptMessageSessionForNamespaceCommand acceptMessageSessionForNamespaceCommand = new AcceptMessageSessionForNamespaceCommand()
			{
				PrefetchCount = prefetchCount,
				ReceiveMode = receiveMode,
				Timeout = serverWaitTime,
				OperationTimeout = timeout
			};
			AcceptMessageSessionForNamespaceCommand acceptMessageSessionForNamespaceCommand1 = acceptMessageSessionForNamespaceCommand;
			RequestInfo requestInfo = new RequestInfo()
			{
				ServerTimeout = new TimeSpan?(acceptMessageSessionForNamespaceCommand1.Timeout),
				OperationTimeout = new TimeSpan?(acceptMessageSessionForNamespaceCommand1.OperationTimeout)
			};
			RequestInfo requestInfo1 = requestInfo;
			Message message = this.controlLinkSettings.MessageCreator.CreateWcfMessage("http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpConnection/AcceptMessageSessionForNamespace", acceptMessageSessionForNamespaceCommand1, null, this.MessagingFactory.RetryPolicy, null, requestInfo1);
			if (base.SyncContinue(this.MessagingFactory.Channel.BeginRequest(message, SbmpProtocolDefaults.BufferTimeout(timeout, this.MessagingFactory.GetSettings().EnableAdditionalClientTimeout), base.PrepareAsyncCompletion(AcceptMessageSessionForNamespaceAsyncResult.operationComplete), this)))
			{
				base.Complete(true);
			}
		}

		public static new MessageSession End(IAsyncResult result)
		{
			return AsyncResult.End<AcceptMessageSessionForNamespaceAsyncResult>(result).result;
		}

		private static bool OperationComplete(IAsyncResult asyncResult)
		{
			AcceptMessageSessionForNamespaceAsyncResult asyncState = (AcceptMessageSessionForNamespaceAsyncResult)asyncResult.AsyncState;
			Message message = asyncState.MessagingFactory.Channel.EndRequest(asyncResult);
			AcceptMessageSessionForNamespaceResponseCommand body = message.GetBody<AcceptMessageSessionForNamespaceResponseCommand>();
			SessionState sessionState = body.SessionState;
			MessageCollection messages = body.Messages;
			DateTime lockedUntilUtc = body.LockedUntilUtc;
			LinkInfo linkInfo = new LinkInfo()
			{
				ConnectionId = asyncState.MessagingFactory.ConnectionId,
				EntityName = body.EntityPath,
				IsSessionReceiver = true,
				LinkId = body.LinkId,
				ReceiveMode = asyncState.receiveMode,
				LinkType = LinkType.Receive,
				SessionId = body.SessionId
			};
			LinkInfo linkInfo1 = linkInfo;
			CreateLinkSettings createLinkSetting = new CreateLinkSettings(asyncState.MessagingFactory, body.EntityPath, body.EntityPath, linkInfo1, null);
			SbmpMessageReceiver sbmpMessageReceiver = new SbmpMessageReceiver(createLinkSetting.EntityName, false, createLinkSetting.MessagingFactory, createLinkSetting.MessageCreator, createLinkSetting.ControlMessageCreator, createLinkSetting.LinkInfo, false, asyncState.MessagingFactory.RetryPolicy.Clone(), messages);
			sbmpMessageReceiver.Open();
			asyncState.result = new SbmpMessageSession(body.SessionId, lockedUntilUtc, sessionState, sbmpMessageReceiver);
			return true;
		}
	}
}