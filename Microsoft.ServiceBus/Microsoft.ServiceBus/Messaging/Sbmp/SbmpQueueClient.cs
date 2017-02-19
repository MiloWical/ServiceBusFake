using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.ServiceModel;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	internal sealed class SbmpQueueClient : QueueClient
	{
		private Lazy<SbmpMessageCreator> ControlMessageCreator
		{
			get;
			set;
		}

		public SbmpQueueClient(SbmpMessagingFactory messagingFactory, string name, ReceiveMode receiveMode) : base(messagingFactory, name, receiveMode)
		{
			this.ControlMessageCreator = new Lazy<SbmpMessageCreator>(new Func<SbmpMessageCreator>(this.InitializeControlLink));
		}

		private void BaseAbort()
		{
			base.OnAbort();
		}

		private IAsyncResult BaseBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return base.OnBeginClose(timeout, callback, state);
		}

		private void EndBaseClose(IAsyncResult result)
		{
			base.OnEndClose(result);
		}

		private SbmpMessageCreator InitializeControlLink()
		{
			CreateControlLinkSettings createControlLinkSetting = new CreateControlLinkSettings((SbmpMessagingFactory)base.MessagingFactory, base.Path, base.Path, MessagingEntityType.Queue, null);
			return createControlLinkSetting.MessageCreator;
		}

		protected override void OnAbort()
		{
			(new SbmpQueueClient.CloseAsyncResult(this, true, this.OperationTimeout, (IAsyncResult r) => {
				try
				{
					AsyncResult<SbmpQueueClient.CloseAsyncResult>.End(r);
				}
				catch (Exception exception)
				{
					if (Fx.IsFatal(exception))
					{
						throw;
					}
				}
			}, null)).Start();
		}

		protected override IAsyncResult OnBeginAcceptMessageSession(string sessionId, ReceiveMode receiveMode, TimeSpan serverWaitTime, TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult acceptMessageSessionAsyncResult;
			try
			{
				acceptMessageSessionAsyncResult = new AcceptMessageSessionAsyncResult((SbmpMessagingFactory)base.MessagingFactory, base.Path, sessionId, new MessagingEntityType?(MessagingEntityType.Queue), receiveMode, base.PrefetchCount, this.ControlMessageCreator, base.RetryPolicy, serverWaitTime, timeout, callback, state);
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
			return acceptMessageSessionAsyncResult;
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult asyncResult;
			try
			{
				asyncResult = (new SbmpQueueClient.CloseAsyncResult(this, false, timeout, callback, state)).Start();
			}
			catch (CommunicationException communicationException1)
			{
				CommunicationException communicationException = communicationException1;
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(MessagingExceptionHelper.Unwrap(communicationException, false), null);
			}
			return asyncResult;
		}

		internal override IAsyncResult OnBeginCreateBrowser(TimeSpan timeout, AsyncCallback callback, object state)
		{
			CreateBrowserLinkSettings createBrowserLinkSetting = new CreateBrowserLinkSettings((SbmpMessagingFactory)base.MessagingFactory, base.Path, base.Path, new MessagingEntityType?(MessagingEntityType.Queue), this.ControlMessageCreator, base.RetryPolicy, false);
			return new CompletedAsyncResult<SbmpMessageBrowser>(createBrowserLinkSetting.MessageBrowser, callback, state);
		}

		protected override IAsyncResult OnBeginCreateReceiver(ReceiveMode receiveMode, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.OnBeginCreateReceiver(base.Path, receiveMode, timeout, callback, state);
		}

		protected override IAsyncResult OnBeginCreateReceiver(string subQueueName, ReceiveMode receiveMode, TimeSpan timeout, AsyncCallback callback, object state)
		{
			CreateReceiverLinkSettings createReceiverLinkSetting = new CreateReceiverLinkSettings((SbmpMessagingFactory)base.MessagingFactory, subQueueName, subQueueName, new MessagingEntityType?(MessagingEntityType.Queue), receiveMode, this.ControlMessageCreator, base.RetryPolicy, false);
			return new CompletedAsyncResult<SbmpMessageReceiver>(createReceiverLinkSetting.MessageReceiver, callback, state);
		}

		protected override IAsyncResult OnBeginCreateSender(TimeSpan timeout, AsyncCallback callback, object state)
		{
			CreateSenderLinkSettings createSenderLinkSetting = new CreateSenderLinkSettings((SbmpMessagingFactory)base.MessagingFactory, base.Path, new MessagingEntityType?(MessagingEntityType.Queue), base.RetryPolicy);
			return new CompletedAsyncResult<SbmpMessageSender>(createSenderLinkSetting.MessageSender, callback, state);
		}

		protected override IAsyncResult OnBeginGetMessageSessions(DateTime lastUpdateTime, AsyncCallback callback, object state)
		{
			IAsyncResult getMessageSessionsAsyncResult;
			try
			{
				getMessageSessionsAsyncResult = new GetMessageSessionsAsyncResult((SbmpMessagingFactory)base.MessagingFactory, base.Path, lastUpdateTime, this.ControlMessageCreator.Value, base.RetryPolicy, MessagingEntityType.Queue, this.OperationTimeout, callback, state);
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
			return getMessageSessionsAsyncResult;
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult(callback, state);
		}

		protected override MessageSession OnEndAcceptMessageSession(IAsyncResult result)
		{
			MessageSession messageSession;
			try
			{
				messageSession = AcceptMessageSessionAsyncResult.End(result);
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
			return messageSession;
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			try
			{
				AsyncResult<SbmpQueueClient.CloseAsyncResult>.End(result);
			}
			catch (CommunicationException communicationException1)
			{
				CommunicationException communicationException = communicationException1;
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(MessagingExceptionHelper.Unwrap(communicationException, false), null);
			}
		}

		internal override MessageBrowser OnEndCreateBrowser(IAsyncResult result)
		{
			return CompletedAsyncResult<SbmpMessageBrowser>.End(result);
		}

		protected override MessageReceiver OnEndCreateReceiver(IAsyncResult result)
		{
			return CompletedAsyncResult<SbmpMessageReceiver>.End(result);
		}

		protected override MessageSender OnEndCreateSender(IAsyncResult result)
		{
			return CompletedAsyncResult<SbmpMessageSender>.End(result);
		}

		protected override IEnumerable<MessageSession> OnEndGetMessageSessions(IAsyncResult result)
		{
			IEnumerable<MessageSession> messageSessions;
			try
			{
				messageSessions = GetMessageSessionsAsyncResult.End(result);
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
			return messageSessions;
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
		}

		protected override void OnOpen(TimeSpan timeout)
		{
		}

		private sealed class CloseAsyncResult : IteratorAsyncResult<SbmpQueueClient.CloseAsyncResult>
		{
			private readonly bool aborting;

			private readonly SbmpQueueClient parent;

			public CloseAsyncResult(SbmpQueueClient parent, bool aborting, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.aborting = aborting;
				this.parent = parent;
			}

			protected override IEnumerator<IteratorAsyncResult<SbmpQueueClient.CloseAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				if (!this.aborting)
				{
					if (this.parent.ControlMessageCreator.IsValueCreated)
					{
						SbmpQueueClient.CloseAsyncResult closeAsyncResult = this;
						IteratorAsyncResult<SbmpQueueClient.CloseAsyncResult>.BeginCall beginCall = (SbmpQueueClient.CloseAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => (new CloseOrAbortLinkAsyncResult(thisPtr.parent.ControlMessageCreator.Value, ((SbmpMessagingFactory)thisPtr.parent.MessagingFactory).Channel, null, t, false, c, s)).Start();
						yield return closeAsyncResult.CallAsync(beginCall, (SbmpQueueClient.CloseAsyncResult thisPtr, IAsyncResult r) => AsyncResult<CloseOrAbortLinkAsyncResult>.End(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
					}
					SbmpQueueClient.CloseAsyncResult closeAsyncResult1 = this;
					IteratorAsyncResult<SbmpQueueClient.CloseAsyncResult>.BeginCall beginCall1 = (SbmpQueueClient.CloseAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.parent.BaseBeginClose(t, c, s);
					yield return closeAsyncResult1.CallAsync(beginCall1, (SbmpQueueClient.CloseAsyncResult thisPtr, IAsyncResult r) => thisPtr.parent.EndBaseClose(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				}
				else
				{
					if (this.parent.ControlMessageCreator.IsValueCreated)
					{
						CloseOrAbortLinkAsyncResult closeOrAbortLinkAsyncResult = new CloseOrAbortLinkAsyncResult(this.parent.ControlMessageCreator.Value, ((SbmpMessagingFactory)this.parent.MessagingFactory).Channel, null, base.RemainingTime(), true, null, null);
						closeOrAbortLinkAsyncResult.Schedule();
					}
					this.parent.BaseAbort();
				}
			}
		}
	}
}