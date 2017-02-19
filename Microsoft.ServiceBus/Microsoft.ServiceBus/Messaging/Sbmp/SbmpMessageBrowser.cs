using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	internal class SbmpMessageBrowser : MessageBrowser
	{
		private readonly string path;

		internal Lazy<SbmpMessageCreator> ControlMessageCreator
		{
			get;
			private set;
		}

		public SbmpMessageCreator MessageCreator
		{
			get;
			private set;
		}

		public override string Path
		{
			get
			{
				return this.path;
			}
		}

		internal Microsoft.ServiceBus.Messaging.Sbmp.SbmpMessagingFactory SbmpMessagingFactory
		{
			get;
			private set;
		}

		public SbmpMessageBrowser(string path, Microsoft.ServiceBus.Messaging.Sbmp.SbmpMessagingFactory messagingFactory, SbmpMessageCreator messageCreator, Lazy<SbmpMessageCreator> controlMessageCreator, Microsoft.ServiceBus.RetryPolicy retryPolicy, bool embedParentLinkId) : base(messagingFactory, retryPolicy)
		{
			this.SbmpMessagingFactory = messagingFactory;
			this.path = path;
			this.MessageCreator = messageCreator;
			this.ControlMessageCreator = controlMessageCreator;
			base.InstanceTrackingContext = TrackingContext.GetInstance(Guid.NewGuid(), this.path);
		}

		private static void AbortCallback(IAsyncResult result)
		{
			AsyncResult<CloseOrAbortLinkAsyncResult>.End(result);
		}

		private IAsyncResult BeginCloseLink(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return (new CloseOrAbortLinkAsyncResult(this.MessageCreator, this.SbmpMessagingFactory.Channel, null, string.Empty, timeout, false, callback, state)).Start();
		}

		private IAsyncResult BeginPeekCommand(TrackingContext trackingContext, long fromSequenceNumber, int messageCount, TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult asyncResult;
			try
			{
				PeekCommand peekCommand = new PeekCommand()
				{
					FromSequenceNumber = fromSequenceNumber,
					MessageCount = messageCount,
					Timeout = timeout,
					MessageVersion = BrokeredMessage.MessageVersion
				};
				PeekCommand peekCommand1 = peekCommand;
				RequestInfo requestInfo = new RequestInfo()
				{
					ServerTimeout = new TimeSpan?(peekCommand1.Timeout),
					SequenceNumber = new long?(peekCommand1.FromSequenceNumber),
					MessageCount = new int?(peekCommand1.MessageCount)
				};
				RequestInfo requestInfo1 = requestInfo;
				Message message = this.MessageCreator.CreateWcfMessage("http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageBrowser/Peek", peekCommand1, null, base.RetryPolicy, trackingContext, requestInfo1);
				asyncResult = this.SbmpMessagingFactory.Channel.BeginRequest(message, SbmpProtocolDefaults.BufferTimeout(timeout, this.SbmpMessagingFactory.GetSettings().EnableAdditionalClientTimeout), callback, state);
			}
			catch (CommunicationException communicationException1)
			{
				CommunicationException communicationException = communicationException1;
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(MessagingExceptionHelper.Unwrap(communicationException, base.IsClosedOrClosing), null);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception) && base.IsClosedOrClosing)
				{
					throw new OperationCanceledException(SRClient.EntityClosedOrAborted, exception);
				}
				throw;
			}
			return asyncResult;
		}

		private void EndCloseLink(IAsyncResult result)
		{
			AsyncResult<CloseOrAbortLinkAsyncResult>.End(result);
		}

		private IEnumerable<BrokeredMessage> EndPeekCommand(IAsyncResult result)
		{
			IEnumerable<BrokeredMessage> messages;
			try
			{
				Message message = this.SbmpMessagingFactory.Channel.EndRequest(result);
				messages = message.GetBody<PeekResponseCommand>().Messages;
			}
			catch (CommunicationException communicationException1)
			{
				CommunicationException communicationException = communicationException1;
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(MessagingExceptionHelper.Unwrap(communicationException, base.IsClosedOrClosing), null);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception) && base.IsClosedOrClosing)
				{
					throw new OperationCanceledException(SRClient.EntityClosedOrAborted, exception);
				}
				throw;
			}
			return messages;
		}

		protected sealed override void OnAbort()
		{
			CloseOrAbortLinkAsyncResult closeOrAbortLinkAsyncResult = new CloseOrAbortLinkAsyncResult(this.MessageCreator, this.SbmpMessagingFactory.Channel, null, string.Empty, this.OperationTimeout, true, new AsyncCallback(SbmpMessageBrowser.AbortCallback), null);
			closeOrAbortLinkAsyncResult.Schedule();
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.BeginCloseLink(timeout, callback, state);
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult(callback, state);
		}

		protected override IAsyncResult OnBeginPeek(TrackingContext trackingContext, long fromSequenceNumber, int messageCount, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.BeginPeekCommand(trackingContext, fromSequenceNumber, messageCount, timeout, callback, state);
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			this.EndCloseLink(result);
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
		}

		protected override IEnumerable<BrokeredMessage> OnEndPeek(IAsyncResult result)
		{
			IEnumerable<BrokeredMessage> brokeredMessages = this.EndPeekCommand(result);
			if (brokeredMessages == null)
			{
				brokeredMessages = new List<BrokeredMessage>(0);
			}
			if (brokeredMessages != null && brokeredMessages.Any<BrokeredMessage>())
			{
				this.LastPeekedSequenceNumber = brokeredMessages.Last<BrokeredMessage>().SequenceNumber;
			}
			return brokeredMessages;
		}
	}
}