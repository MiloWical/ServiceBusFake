using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	internal sealed class GetMessageSessionsAsyncResult : IteratorAsyncResult<GetMessageSessionsAsyncResult>
	{
		private readonly List<MessageSession> messageSessions;

		private readonly DateTime lastUpdatedTime;

		private readonly SbmpMessageCreator messageCreator;

		private readonly string entityName;

		private readonly int sessionsPageSize;

		private readonly MessagingEntityType entityType;

		private readonly RetryPolicy retryPolicy;

		private string LastSessionId
		{
			get;
			set;
		}

		public SbmpMessagingFactory MessagingFactory
		{
			get;
			private set;
		}

		private int Skip
		{
			get;
			set;
		}

		public GetMessageSessionsAsyncResult(SbmpMessagingFactory messagingFactory, string entityName, DateTime lastUpdatedTime, SbmpMessageCreator messageCreator, RetryPolicy retryPolicy, MessagingEntityType entityType, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
		{
			this.lastUpdatedTime = lastUpdatedTime;
			this.MessagingFactory = messagingFactory;
			this.messageCreator = messageCreator;
			this.entityName = entityName;
			this.messageSessions = new List<MessageSession>();
			this.entityType = entityType;
			this.sessionsPageSize = 100;
			this.retryPolicy = retryPolicy;
			base.Start();
		}

		private Message CreateRequestMessage()
		{
			GetMessageSessionsCommand getMessageSessionsCommand = new GetMessageSessionsCommand()
			{
				Skip = this.Skip,
				Top = this.sessionsPageSize,
				LastUpdatedTime = this.lastUpdatedTime,
				Timeout = base.RemainingTime()
			};
			GetMessageSessionsCommand getMessageSessionsCommand1 = getMessageSessionsCommand;
			RequestInfo requestInfo = new RequestInfo()
			{
				ServerTimeout = new TimeSpan?(getMessageSessionsCommand1.Timeout),
				SessionId = this.LastSessionId,
				Skip = new int?(this.Skip),
				Top = new int?(getMessageSessionsCommand1.Top),
				LastUpdatedTime = new DateTime?(this.lastUpdatedTime)
			};
			RequestInfo requestInfo1 = requestInfo;
			Message message = this.messageCreator.CreateWcfMessage("http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageReceiver/GetMessageSessions", getMessageSessionsCommand1, null, this.retryPolicy, null, requestInfo1);
			return message;
		}

		public static new IEnumerable<MessageSession> End(IAsyncResult result)
		{
			return AsyncResult<GetMessageSessionsAsyncResult>.End(result).messageSessions;
		}

		protected override IEnumerator<IteratorAsyncResult<GetMessageSessionsAsyncResult>.AsyncStep> GetAsyncSteps()
		{
			Message message = null;
			do
			{
				Message message1 = this.CreateRequestMessage();
				GetMessageSessionsAsyncResult getMessageSessionsAsyncResult = this;
				IteratorAsyncResult<GetMessageSessionsAsyncResult>.BeginCall beginCall = (GetMessageSessionsAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.MessagingFactory.Channel.BeginRequest(message1, SbmpProtocolDefaults.BufferTimeout(t, thisPtr.MessagingFactory.GetSettings().EnableAdditionalClientTimeout), c, s);
				yield return getMessageSessionsAsyncResult.CallAsync(beginCall, (GetMessageSessionsAsyncResult thisPtr, IAsyncResult r) => message = thisPtr.MessagingFactory.Channel.EndRequest(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
			}
			while (base.LastAsyncStepException == null && message != null && this.ShouldContinue(message));
			base.Complete(base.LastAsyncStepException);
		}

		private bool ShouldContinue(Message responseMessage)
		{
			int num = 0;
			GetMessageSessionsResponseCommand body = responseMessage.GetBody<GetMessageSessionsResponseCommand>();
			this.Skip = body.Skip;
			foreach (Tuple<string, SessionState> session in body.Sessions)
			{
				string item1 = session.Item1;
				this.messageSessions.Add(new SbmpBrowsableMessageSession(this.entityName, item1, this.entityType, this.MessagingFactory, this.retryPolicy));
				this.LastSessionId = item1;
				num++;
			}
			return num > 0;
		}
	}
}