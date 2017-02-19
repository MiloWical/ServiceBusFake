using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.PerformanceCounters;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging
{
	internal abstract class MessageBrowser : MessageClientEntity, IMessageBrowser
	{
		private readonly OpenOnceManager openOnceManager;

		private readonly TimeSpan operationTimeout;

		private long lastPeekedSequenceNumber;

		internal TrackingContext InstanceTrackingContext
		{
			get;
			set;
		}

		public virtual long LastPeekedSequenceNumber
		{
			get
			{
				return this.lastPeekedSequenceNumber;
			}
			internal set
			{
				if (value < (long)0)
				{
					throw FxTrace.Exception.ArgumentOutOfRange("LastPeekedSequenceNumber", value, SRClient.ArgumentOutOfRange(0, 9223372036854775807L));
				}
				this.lastPeekedSequenceNumber = value;
			}
		}

		internal Microsoft.ServiceBus.Messaging.MessagingFactory MessagingFactory
		{
			get;
			private set;
		}

		internal override TimeSpan OperationTimeout
		{
			get
			{
				return this.operationTimeout;
			}
		}

		public abstract string Path
		{
			get;
		}

		internal MessageBrowser(Microsoft.ServiceBus.Messaging.MessagingFactory messagingFactory, Microsoft.ServiceBus.RetryPolicy retryPolicy)
		{
			this.MessagingFactory = messagingFactory;
			this.operationTimeout = messagingFactory.OperationTimeout;
			this.lastPeekedSequenceNumber = Constants.DefaultLastPeekedSequenceNumber;
			base.RetryPolicy = retryPolicy ?? messagingFactory.RetryPolicy.Clone();
			this.openOnceManager = new OpenOnceManager(this);
		}

		public IAsyncResult BeginPeek(AsyncCallback callback, object state)
		{
			return this.BeginPeek(this.LastPeekedSequenceNumber + (long)1, callback, state);
		}

		public IAsyncResult BeginPeek(long fromSequenceNumber, AsyncCallback callback, object state)
		{
			return this.BeginPeekBatch(null, fromSequenceNumber, 1, this.OperationTimeout, callback, state);
		}

		public IAsyncResult BeginPeekBatch(int messageCount, AsyncCallback callback, object state)
		{
			return this.BeginPeekBatch(this.LastPeekedSequenceNumber + (long)1, messageCount, callback, state);
		}

		public IAsyncResult BeginPeekBatch(long fromSequenceNumber, int messageCount, AsyncCallback callback, object state)
		{
			return this.BeginPeekBatch(null, fromSequenceNumber, messageCount, this.OperationTimeout, callback, state);
		}

		internal IAsyncResult BeginPeekBatch(TrackingContext trackingContext, long fromSequenceNumber, int messageCount, TimeSpan timeout, AsyncCallback callback, object state)
		{
			TrackingContext instance = trackingContext;
			base.ThrowIfDisposed();
			TimeoutHelper.ThrowIfNegativeArgument(timeout);
			if (instance == null)
			{
				instance = TrackingContext.GetInstance(Guid.NewGuid(), this.Path);
			}
			MessageBrowser.TracePeek(EventTraceActivity.CreateFromThread(), instance);
			if (!this.openOnceManager.ShouldOpen)
			{
				MessageBrowser.RetryBrowserAsyncResult retryBrowserAsyncResult1 = new MessageBrowser.RetryBrowserAsyncResult(this, instance, messageCount, fromSequenceNumber, timeout, callback, state);
				retryBrowserAsyncResult1.Start();
				return retryBrowserAsyncResult1;
			}
			return this.openOnceManager.Begin<IEnumerable<BrokeredMessage>>(callback, state, (AsyncCallback c, object s) => {
				MessageBrowser.RetryBrowserAsyncResult retryBrowserAsyncResult = new MessageBrowser.RetryBrowserAsyncResult(this, instance, messageCount, fromSequenceNumber, timeout, c, s);
				retryBrowserAsyncResult.Start();
				return retryBrowserAsyncResult;
			}, new Func<IAsyncResult, IEnumerable<BrokeredMessage>>(MessageBrowser.RetryBrowserAsyncResult.End));
		}

		public BrokeredMessage EndPeek(IAsyncResult result)
		{
			return MessageBrowser.GetTopMessage(this.EndPeekBatch(result));
		}

		public IEnumerable<BrokeredMessage> EndPeekBatch(IAsyncResult result)
		{
			IEnumerable<BrokeredMessage> brokeredMessages = null;
			brokeredMessages = (!OpenOnceManager.ShouldEnd<IEnumerable<BrokeredMessage>>(result) ? MessageBrowser.RetryBrowserAsyncResult.End(result) : OpenOnceManager.End<IEnumerable<BrokeredMessage>>(result));
			return brokeredMessages;
		}

		private static BrokeredMessage GetTopMessage(IEnumerable<BrokeredMessage> messages)
		{
			BrokeredMessage current;
			if (messages != null)
			{
				using (IEnumerator<BrokeredMessage> enumerator = messages.GetEnumerator())
				{
					if (!enumerator.MoveNext())
					{
						return null;
					}
					else
					{
						current = enumerator.Current;
					}
				}
				return current;
			}
			return null;
		}

		protected abstract IAsyncResult OnBeginPeek(TrackingContext trackingContext, long fromSequenceNumber, int messageCount, TimeSpan timeout, AsyncCallback callback, object state);

		protected abstract IEnumerable<BrokeredMessage> OnEndPeek(IAsyncResult result);

		protected virtual IEnumerable<BrokeredMessage> OnPeek(TrackingContext trackingContext, long fromSequenceNumber, int messageCount, TimeSpan timeout)
		{
			MessageBrowser.RetryBrowserAsyncResult retryBrowserAsyncResult = new MessageBrowser.RetryBrowserAsyncResult(this, trackingContext, messageCount, fromSequenceNumber, timeout, null, null);
			retryBrowserAsyncResult.RunSynchronously();
			return retryBrowserAsyncResult.Messages;
		}

		public BrokeredMessage Peek()
		{
			return this.Peek(this.LastPeekedSequenceNumber + (long)1);
		}

		public BrokeredMessage Peek(long fromSequenceNumber)
		{
			return this.Peek(null, fromSequenceNumber, this.OperationTimeout);
		}

		internal BrokeredMessage Peek(TrackingContext trackingContext, long fromSequenceNumber, TimeSpan timeout)
		{
			IEnumerable<BrokeredMessage> brokeredMessages = this.PeekBatch(trackingContext, fromSequenceNumber, 1, timeout);
			return MessageBrowser.GetTopMessage(brokeredMessages);
		}

		public IEnumerable<BrokeredMessage> PeekBatch(int messageCount)
		{
			return this.PeekBatch(this.LastPeekedSequenceNumber + (long)1, messageCount);
		}

		public IEnumerable<BrokeredMessage> PeekBatch(long fromSequenceNumber, int messageCount)
		{
			return this.PeekBatch(null, fromSequenceNumber, messageCount, this.OperationTimeout);
		}

		internal IEnumerable<BrokeredMessage> PeekBatch(TrackingContext trackingContext, long fromSequenceNumber, int messageCount, TimeSpan timeout)
		{
			base.ThrowIfDisposed();
			TimeoutHelper.ThrowIfNegativeArgument(timeout);
			if (trackingContext == null)
			{
				trackingContext = TrackingContext.GetInstance(Guid.NewGuid(), this.Path);
			}
			MessageBrowser.TracePeek(EventTraceActivity.CreateFromThread(), trackingContext);
			if (this.openOnceManager.ShouldOpen)
			{
				this.openOnceManager.Open();
			}
			return this.OnPeek(trackingContext, fromSequenceNumber, messageCount, timeout);
		}

		private static void TracePeek(EventTraceActivity activity, TrackingContext trackingContext)
		{
			if (activity != null && activity != EventTraceActivity.Empty && trackingContext != null)
			{
				MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageReceiveTransfer(activity, trackingContext.Activity));
			}
		}

		private sealed class RetryBrowserAsyncResult : RetryAsyncResult<MessageBrowser.RetryBrowserAsyncResult>
		{
			private readonly MessageBrowser browser;

			private readonly TrackingContext trackingContext;

			private readonly int messageCount;

			private readonly long fromSequenceNumber;

			public IEnumerable<BrokeredMessage> Messages
			{
				get;
				private set;
			}

			public RetryBrowserAsyncResult(MessageBrowser browser, TrackingContext trackingContext, int messageCount, long fromSequenceNumber, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.browser = browser;
				this.messageCount = messageCount;
				this.fromSequenceNumber = fromSequenceNumber;
				this.trackingContext = trackingContext;
			}

			public static new IEnumerable<BrokeredMessage> End(IAsyncResult r)
			{
				return AsyncResult<MessageBrowser.RetryBrowserAsyncResult>.End(r).Messages;
			}

			protected override IEnumerator<IteratorAsyncResult<MessageBrowser.RetryBrowserAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				TimeSpan timeSpan;
				bool flag;
				bool flag1;
				int num = 0;
				timeSpan = (this.browser.RetryPolicy.IsServerBusy ? Microsoft.ServiceBus.RetryPolicy.ServerBusyBaseSleepTime : TimeSpan.Zero);
				TimeSpan timeSpan1 = timeSpan;
				if (!this.browser.RetryPolicy.IsServerBusy || !(Microsoft.ServiceBus.RetryPolicy.ServerBusyBaseSleepTime >= base.OriginalTimeout))
				{
					do
					{
						flag1 = false;
						if (timeSpan1 != TimeSpan.Zero)
						{
							yield return base.CallAsyncSleep(timeSpan1);
						}
						MessageBrowser.RetryBrowserAsyncResult retryBrowserAsyncResult = this;
						IteratorAsyncResult<MessageBrowser.RetryBrowserAsyncResult>.BeginCall beginCall = (MessageBrowser.RetryBrowserAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.browser.OnBeginPeek(thisPtr.trackingContext, thisPtr.fromSequenceNumber, thisPtr.messageCount, t, c, s);
						yield return retryBrowserAsyncResult.CallAsync(beginCall, (MessageBrowser.RetryBrowserAsyncResult thisPtr, IAsyncResult r) => thisPtr.Messages = thisPtr.browser.OnEndPeek(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
						if (base.LastAsyncStepException == null)
						{
							this.browser.RetryPolicy.ResetServerBusy();
						}
						else
						{
							MessagingPerformanceCounters.IncrementExceptionPerSec(this.browser.MessagingFactory.Address, 1, base.LastAsyncStepException);
							flag = (base.TransactionExists ? false : this.browser.RetryPolicy.ShouldRetry(base.RemainingTime(), num, base.LastAsyncStepException, out timeSpan1));
							flag1 = flag;
							if (!flag1)
							{
								continue;
							}
							MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRetryPolicyIteration(this.trackingContext.Activity, this.trackingContext.TrackingId, this.browser.RetryPolicy.GetType().Name, "Peek", num, timeSpan1.ToString(), this.LastAsyncStepException.GetType().FullName, this.LastAsyncStepException.Message));
							num++;
						}
					}
					while (flag1);
					base.Complete(base.LastAsyncStepException);
				}
				else
				{
					string str = this.browser.RetryPolicy.ServerBusyExceptionMessage;
					yield return base.CallAsyncSleep(base.RemainingTime());
					base.Complete(new ServerBusyException(str, this.trackingContext));
				}
			}
		}
	}
}