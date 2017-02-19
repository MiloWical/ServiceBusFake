using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using Microsoft.ServiceBus.Messaging.ScaledEntity;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Transactions;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	internal sealed class SbmpMessageReceiver : MessageReceiver
	{
		private readonly string path;

		private readonly ConcurrentQueue<BrokeredMessage> prefetchedMessages;

		private readonly PrefetchAsyncWaitHandle prefetchWaitHandle;

		private readonly bool abortLinkOnReceiveTimeout;

		private readonly bool embedParentLinkId;

		public override TimeSpan BatchFlushInterval
		{
			get
			{
				return this.BatchManager.FlushInterval;
			}
			internal set
			{
				base.ThrowIfDisposedOrImmutable();
				this.BatchManager.FlushInterval = value;
			}
		}

		internal BatchManager<Guid> BatchManager
		{
			get;
			private set;
		}

		internal Lazy<SbmpMessageCreator> ControlMessageCreator
		{
			get;
			private set;
		}

		internal override MessagingEntityType? EntityType
		{
			get
			{
				return this.LinkInfo.EntityType;
			}
			set
			{
				this.LinkInfo.EntityType = value;
			}
		}

		private Microsoft.ServiceBus.Messaging.Sbmp.LinkInfo LinkInfo
		{
			get;
			set;
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

		internal ConcurrentQueue<BrokeredMessage> PrefetchedMessages
		{
			get
			{
				return this.prefetchedMessages;
			}
		}

		protected internal override DateTime? ReceiverStartTime
		{
			get
			{
				long? fromTimestamp = this.LinkInfo.FromTimestamp;
				return new DateTime?(TimeStampEncoding.ToDateTime(fromTimestamp.GetValueOrDefault((long)0)));
			}
			set
			{
				if (!value.HasValue)
				{
					this.LinkInfo.FromTimestamp = null;
					return;
				}
				this.LinkInfo.FromTimestamp = new long?(TimeStampEncoding.GetMilliseconds(value.Value));
			}
		}

		internal Microsoft.ServiceBus.Messaging.Sbmp.SbmpMessagingFactory SbmpMessagingFactory
		{
			get;
			private set;
		}

		internal bool SbmpReceiverBatchingEnabled
		{
			get;
			private set;
		}

		protected internal override string StartOffset
		{
			get
			{
				return this.LinkInfo.FromOffset;
			}
			set
			{
				this.LinkInfo.FromOffset = value;
			}
		}

		protected internal override bool SupportsGetRuntimeEntityDescription
		{
			get
			{
				return !this.SbmpMessagingFactory.Settings.GatewayMode;
			}
		}

		public SbmpMessageReceiver(string path, bool abortLinkOnReceiveTimeout, Microsoft.ServiceBus.Messaging.Sbmp.SbmpMessagingFactory messagingFactory, SbmpMessageCreator messageCreator, Lazy<SbmpMessageCreator> controlMessageCreator, Microsoft.ServiceBus.Messaging.Sbmp.LinkInfo linkInfo, Microsoft.ServiceBus.RetryPolicy retryPolicy) : this(path, abortLinkOnReceiveTimeout, messagingFactory, messageCreator, controlMessageCreator, linkInfo, false, retryPolicy, null)
		{
		}

		public SbmpMessageReceiver(string path, bool abortLinkOnReceiveTimeout, Microsoft.ServiceBus.Messaging.Sbmp.SbmpMessagingFactory messagingFactory, SbmpMessageCreator messageCreator, Lazy<SbmpMessageCreator> controlMessageCreator, Microsoft.ServiceBus.Messaging.Sbmp.LinkInfo linkInfo, bool embedParentLinkId, Microsoft.ServiceBus.RetryPolicy retryPolicy, MessageCollection prefetchMessages) : base(messagingFactory, retryPolicy, linkInfo.ReceiveMode, null)
		{
			bool flag;
			this.SbmpMessagingFactory = messagingFactory;
			this.abortLinkOnReceiveTimeout = abortLinkOnReceiveTimeout;
			this.path = path;
			this.MessageCreator = messageCreator;
			this.embedParentLinkId = embedParentLinkId;
			this.ControlMessageCreator = controlMessageCreator;
			base.InstanceTrackingContext = TrackingContext.GetInstance(Guid.NewGuid(), this.path);
			this.LinkInfo = linkInfo;
			this.BatchManager = new BatchManager<Guid>((TrackingContext trackingContext, IEnumerable<Guid> lockTokens, string transactionId, TimeSpan timeout, AsyncCallback callback, object state) => this.BeginUpdateCommand(trackingContext, lockTokens, null, DispositionStatus.Completed, null, timeout, callback, state), (IAsyncResult result, bool forceCleanUp) => this.EndUpdateCommand(result), (IEnumerable<Guid> lockTokens) => lockTokens.Count<Guid>(), new OnRetryDelegate<Guid>(SbmpMessageReceiver.IsCompleteRetryable), null, null, (long)Constants.FlushBatchThreshold, (long)Constants.FlushBatchThreshold)
			{
				FlushInterval = this.SbmpMessagingFactory.Settings.BatchFlushInterval
			};
			if (this.SbmpMessagingFactory.Settings.GatewayMode)
			{
				flag = true;
			}
			else
			{
				flag = (this.SbmpMessagingFactory.Settings.GatewayMode ? false : this.SbmpMessagingFactory.Settings.EnableRedirect);
			}
			this.SbmpReceiverBatchingEnabled = flag;
			this.PrefetchCount = Constants.DefaultPrefetchCount;
			this.prefetchWaitHandle = new PrefetchAsyncWaitHandle();
			this.prefetchWaitHandle.Set();
			if (base.Mode == ReceiveMode.PeekLock)
			{
				this.PrefetchCount = Constants.DefaultPrefetchCount;
			}
			if (prefetchMessages == null || prefetchMessages.Count == 0)
			{
				this.prefetchedMessages = new ConcurrentQueue<BrokeredMessage>();
				return;
			}
			foreach (BrokeredMessage prefetchMessage in prefetchMessages)
			{
				prefetchMessage.IsFromCache = true;
			}
			this.PrefetchCount = prefetchMessages.Count;
			this.prefetchedMessages = new ConcurrentQueue<BrokeredMessage>(prefetchMessages);
		}

		private static void AbortCallback(IAsyncResult result)
		{
			AsyncResult<CloseOrAbortLinkAsyncResult>.End(result);
		}

		private void AbortLink(bool receiverAborting)
		{
			ICommunicationObject batchManager;
			AsyncCallback asyncCallback;
			if (!this.prefetchedMessages.IsEmpty)
			{
				this.DisposePrefetchedMessages();
			}
			string empty = string.Empty;
			if (this.embedParentLinkId && this.ControlMessageCreator != null && !string.IsNullOrWhiteSpace(this.ControlMessageCreator.Value.LinkInfo.LinkId))
			{
				empty = this.ControlMessageCreator.Value.LinkInfo.LinkId;
			}
			SbmpMessageCreator messageCreator = this.MessageCreator;
			IRequestSessionChannel channel = this.SbmpMessagingFactory.Channel;
			if (!receiverAborting || !base.BatchingEnabled)
			{
				batchManager = null;
			}
			else
			{
				batchManager = this.BatchManager;
			}
			string str = empty;
			TimeSpan operationTimeout = this.OperationTimeout;
			if (receiverAborting)
			{
				asyncCallback = new AsyncCallback(SbmpMessageReceiver.AbortCallback);
			}
			else
			{
				asyncCallback = null;
			}
			CloseOrAbortLinkAsyncResult closeOrAbortLinkAsyncResult = new CloseOrAbortLinkAsyncResult(messageCreator, channel, batchManager, str, operationTimeout, true, asyncCallback, null);
			if (receiverAborting)
			{
				closeOrAbortLinkAsyncResult.Schedule();
				return;
			}
			closeOrAbortLinkAsyncResult.Start();
			AsyncResult<CloseOrAbortLinkAsyncResult>.End(closeOrAbortLinkAsyncResult);
		}

		private IAsyncResult BeginCloseLink(TimeSpan timeout, AsyncCallback callback, object state)
		{
			ICommunicationObject batchManager;
			string empty = string.Empty;
			if (this.embedParentLinkId && this.ControlMessageCreator != null && !string.IsNullOrWhiteSpace(this.ControlMessageCreator.Value.LinkInfo.LinkId))
			{
				empty = this.ControlMessageCreator.Value.LinkInfo.LinkId;
			}
			SbmpMessageCreator messageCreator = this.MessageCreator;
			IRequestSessionChannel channel = this.SbmpMessagingFactory.Channel;
			if (base.BatchingEnabled)
			{
				batchManager = this.BatchManager;
			}
			else
			{
				batchManager = null;
			}
			return (new CloseOrAbortLinkAsyncResult(messageCreator, channel, batchManager, empty, timeout, false, callback, state)).Start();
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

		private IAsyncResult BeginReceiveCommand(TrackingContext trackingContext, int messageCount, TimeSpan serverWaitTime, string partitionId, TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult asyncResult;
			try
			{
				TryReceiveCommand tryReceiveCommand = new TryReceiveCommand()
				{
					MessageCount = messageCount,
					Timeout = serverWaitTime,
					OperationTimeout = timeout,
					MessageVersion = BrokeredMessage.MessageVersion
				};
				TryReceiveCommand tryReceiveCommand1 = tryReceiveCommand;
				RequestInfo requestInfo = new RequestInfo()
				{
					ServerTimeout = new TimeSpan?(tryReceiveCommand1.Timeout),
					OperationTimeout = new TimeSpan?(tryReceiveCommand1.OperationTimeout)
				};
				RequestInfo requestInfo1 = requestInfo;
				Message message = this.MessageCreator.CreateWcfMessage("http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageReceiver/TryReceive", tryReceiveCommand1, this.GetParentLinkId(), base.RetryPolicy, trackingContext, requestInfo1);
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

		private IAsyncResult BeginUpdateCommand(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, IDictionary<string, object> propertiesToModify, DispositionStatus disposition, DeadLetterInfo deadLetterInfo, TimeSpan timeout, AsyncCallback callback, object asyncState)
		{
			IAsyncResult updateAsyncResult;
			try
			{
				updateAsyncResult = new SbmpMessageReceiver.UpdateAsyncResult(this, trackingContext, lockTokens, propertiesToModify, disposition, deadLetterInfo, timeout, callback, asyncState);
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
			return updateAsyncResult;
		}

		private void DisposePrefetchedMessages()
		{
			BrokeredMessage brokeredMessage;
			while (this.prefetchedMessages.TryDequeue(out brokeredMessage))
			{
				brokeredMessage.Dispose();
			}
		}

		private void EndCloseLink(IAsyncResult result)
		{
			AsyncResult<CloseOrAbortLinkAsyncResult>.End(result);
		}

		private IEnumerable<BrokeredMessage> EndPeekCommand(IAsyncResult result)
		{
			IEnumerable<BrokeredMessage> brokeredMessages;
			IEnumerable<BrokeredMessage> messages = null;
			try
			{
				Message message = this.SbmpMessagingFactory.Channel.EndRequest(result);
				messages = message.GetBody<PeekResponseCommand>().Messages;
				if (messages == null)
				{
					messages = new List<BrokeredMessage>();
				}
				brokeredMessages = messages;
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
			return brokeredMessages;
		}

		private bool EndReceiveCommand(IAsyncResult result, out IEnumerable<BrokeredMessage> messages)
		{
			bool flag;
			try
			{
				Message message = this.SbmpMessagingFactory.Channel.EndRequest(result);
				TryReceiveResponseCommand body = message.GetBody<TryReceiveResponseCommand>();
				bool flag1 = body.Result;
				messages = body.Messages;
				if (flag1)
				{
					TrackingContext.GetInstance(message, this.Path, false, null);
				}
				flag = flag1;
			}
			catch (CommunicationException communicationException)
			{
				Exception exception = MessagingExceptionHelper.Unwrap(communicationException, base.IsClosedOrClosing);
				if (!(exception is TimeoutException))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(exception, null);
				}
				if (this.abortLinkOnReceiveTimeout)
				{
					this.AbortLink(false);
				}
				throw exception;
			}
			catch (TimeoutException timeoutException)
			{
				if (this.abortLinkOnReceiveTimeout)
				{
					this.AbortLink(false);
				}
				throw;
			}
			catch (MessagingCommunicationException messagingCommunicationException)
			{
				if (this.abortLinkOnReceiveTimeout)
				{
					this.AbortLink(false);
				}
				throw;
			}
			catch (Exception exception2)
			{
				Exception exception1 = exception2;
				if (!Fx.IsFatal(exception1) && base.IsClosedOrClosing)
				{
					throw new OperationCanceledException(SRClient.EntityClosedOrAborted, exception1);
				}
				throw;
			}
			return flag;
		}

		private void EndUpdateCommand(IAsyncResult result)
		{
			try
			{
				AsyncResult<SbmpMessageReceiver.UpdateAsyncResult>.End(result);
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
		}

		private static string GetFormattedLockToken(IEnumerable<Guid> lockTokens)
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (Guid lockToken in lockTokens)
			{
				CultureInfo invariantCulture = CultureInfo.InvariantCulture;
				object[] objArray = new object[] { lockToken };
				stringBuilder.AppendLine(string.Format(invariantCulture, "<LockToken>{0}</LockToken>", objArray));
			}
			return stringBuilder.ToString();
		}

		private string GetFormattedMessageId(IEnumerable<BrokeredMessage> messages)
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (BrokeredMessage message in messages)
			{
				if (base.Mode != ReceiveMode.PeekLock)
				{
					CultureInfo invariantCulture = CultureInfo.InvariantCulture;
					object[] messageId = new object[] { message.MessageId };
					stringBuilder.AppendLine(string.Format(invariantCulture, "<MessageId>{0}</MessageId>", messageId));
				}
				else
				{
					CultureInfo cultureInfo = CultureInfo.InvariantCulture;
					object[] objArray = new object[] { message.MessageId, message.LockToken };
					stringBuilder.AppendLine(string.Format(cultureInfo, "<MessageId>{0}</MessageId>, <LockToken>{1}</LockToken>", objArray));
				}
			}
			return stringBuilder.ToString();
		}

		private string GetParentLinkId()
		{
			if (!this.embedParentLinkId || this.ControlMessageCreator == null || string.IsNullOrWhiteSpace(this.ControlMessageCreator.Value.LinkInfo.LinkId))
			{
				return null;
			}
			return this.ControlMessageCreator.Value.LinkInfo.LinkId;
		}

		private void InsertInCache(BrokeredMessage message)
		{
			this.prefetchedMessages.Enqueue(message);
		}

		private static bool IsCompleteRetryable(IEnumerable<Guid> lockTokens, Exception exception, bool isMultiBatchCommand)
		{
			return false;
		}

		internal IComparable MessagePartitioningGroupByKeySelector(IEnumerable<Guid> batchedObjects)
		{
			return ScaledEntityPartitionResolver.LockTokenToEntityLogicalPartition(batchedObjects.First<Guid>());
		}

		protected sealed override void OnAbort()
		{
			this.AbortLink(true);
		}

		protected override IAsyncResult OnBeginAbandon(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, IDictionary<string, object> propertiesToModify, bool fromSync, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.BeginUpdateCommand(trackingContext, lockTokens, propertiesToModify, DispositionStatus.Abandoned, null, timeout, callback, state);
		}

		protected sealed override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult asyncResult;
			try
			{
				asyncResult = (new SbmpMessageReceiver.AbandonPrefetchedMessagesCloseAbortAsyncResult(this, timeout, callback, state)).Start();
			}
			catch (CommunicationException communicationException1)
			{
				CommunicationException communicationException = communicationException1;
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(MessagingExceptionHelper.Unwrap(communicationException, false), null);
			}
			return asyncResult;
		}

		protected override IAsyncResult OnBeginComplete(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, bool fromSync, TimeSpan timeout, AsyncCallback callback, object asyncState)
		{
			IAsyncResult batchManagerAsyncResult;
			try
			{
				if (!(Transaction.Current == null) || fromSync || !base.BatchingEnabled || !this.SbmpReceiverBatchingEnabled)
				{
					return this.BeginUpdateCommand(trackingContext, lockTokens, null, DispositionStatus.Completed, null, timeout, callback, asyncState);
				}
				else
				{
					batchManagerAsyncResult = new BatchManagerAsyncResult<Guid>(trackingContext, this.BatchManager, lockTokens, timeout, callback, asyncState);
				}
			}
			catch (CommunicationObjectAbortedException communicationObjectAbortedException)
			{
				throw new OperationCanceledException(SRClient.EntityClosedOrAborted, communicationObjectAbortedException);
			}
			return batchManagerAsyncResult;
		}

		protected override IAsyncResult OnBeginComplete(TrackingContext trackingContext, IEnumerable<ArraySegment<byte>> deliveryTags, bool fromSync, TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new NotSupportedException("The NetMessaging protocol does not support EventData related operations.");
		}

		protected override IAsyncResult OnBeginDeadLetter(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, IDictionary<string, object> propertiesToModify, string deadLetterReason, string deadLetterErrorDescription, bool fromSync, TimeSpan timeout, AsyncCallback callback, object state)
		{
			DeadLetterInfo deadLetterInfo = null;
			if (deadLetterReason != null || deadLetterErrorDescription != null)
			{
				deadLetterInfo = new DeadLetterInfo(deadLetterReason, deadLetterErrorDescription);
			}
			return this.BeginUpdateCommand(trackingContext, lockTokens, propertiesToModify, DispositionStatus.Suspended, deadLetterInfo, timeout, callback, state);
		}

		protected override IAsyncResult OnBeginDefer(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, IDictionary<string, object> propertiesToModify, bool fromSync, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.BeginUpdateCommand(trackingContext, lockTokens, propertiesToModify, DispositionStatus.Defered, null, timeout, callback, state);
		}

		internal override IAsyncResult OnBeginGetRuntimeEntityDescriptionAsyncResult(TrackingContext trackingContext, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return (new GetRuntimeEntityDescriptionAsyncResult(trackingContext, this, string.Concat(this.SbmpMessagingFactory.BaseAddress.AbsoluteUri, this.Path), this.SbmpMessagingFactory, this.MessageCreator, true, timeout, callback, state)).Start();
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (!base.BatchingEnabled)
			{
				return new CompletedAsyncResult(callback, state);
			}
			return this.BatchManager.BeginOpen(timeout, callback, state);
		}

		protected override IAsyncResult OnBeginPeek(TrackingContext trackingContext, long fromSequenceNumber, int messageCount, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.BeginPeekCommand(trackingContext, fromSequenceNumber, messageCount, timeout, callback, state);
		}

		protected override IAsyncResult OnBeginRenewMessageLocks(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, bool fromSync, TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult renewLockAsyncResult;
			try
			{
				renewLockAsyncResult = new SbmpMessageReceiver.RenewLockAsyncResult(this, trackingContext, lockTokens, timeout, callback, state);
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
			return renewLockAsyncResult;
		}

		protected override IAsyncResult OnBeginTryReceive(TrackingContext trackingContext, int messageCount, TimeSpan serverWaitTime, AsyncCallback callback, object state)
		{
			SbmpMessageReceiver.ReceiveAsyncResult receiveAsyncResult = new SbmpMessageReceiver.ReceiveAsyncResult(this, trackingContext, messageCount, this.abortLinkOnReceiveTimeout, false, serverWaitTime, (serverWaitTime > this.OperationTimeout ? serverWaitTime : this.OperationTimeout), callback, state);
			receiveAsyncResult.Start();
			return receiveAsyncResult;
		}

		protected override IAsyncResult OnBeginTryReceive(TrackingContext trackingContext, IEnumerable<long> receipts, TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult asyncResult;
			long? nullable;
			try
			{
				TryReceiveCommand tryReceiveCommand = new TryReceiveCommand()
				{
					AppMessageIds = receipts,
					Timeout = timeout,
					OperationTimeout = timeout,
					MessageVersion = BrokeredMessage.MessageVersion
				};
				TryReceiveCommand tryReceiveCommand1 = tryReceiveCommand;
				RequestInfo requestInfo = new RequestInfo()
				{
					ServerTimeout = new TimeSpan?(tryReceiveCommand1.Timeout),
					OperationTimeout = new TimeSpan?(tryReceiveCommand1.OperationTimeout)
				};
				RequestInfo requestInfo1 = requestInfo;
				if (tryReceiveCommand1.AppMessageIds == null || !tryReceiveCommand1.AppMessageIds.Any<long>())
				{
					nullable = null;
				}
				else
				{
					nullable = new long?(tryReceiveCommand1.AppMessageIds.FirstOrDefault<long>());
				}
				requestInfo1.SequenceNumber = nullable;
				RequestInfo requestInfo2 = requestInfo;
				Message message = this.MessageCreator.CreateWcfMessage("http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageReceiver/TryReceive", tryReceiveCommand1, this.GetParentLinkId(), base.RetryPolicy, trackingContext, requestInfo2);
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

		protected override IAsyncResult OnBeginTryReceive2(TrackingContext trackingContext, int messageCount, TimeSpan serverWaitTime, AsyncCallback callback, object state)
		{
			SbmpMessageReceiver.ReceiveAsyncResult receiveAsyncResult = new SbmpMessageReceiver.ReceiveAsyncResult(this, trackingContext, messageCount, this.abortLinkOnReceiveTimeout, true, serverWaitTime, (serverWaitTime > this.OperationTimeout ? serverWaitTime : this.OperationTimeout), callback, state);
			receiveAsyncResult.Start();
			return receiveAsyncResult;
		}

		protected override void OnEndAbandon(IAsyncResult result)
		{
			this.EndUpdateCommand(result);
		}

		protected sealed override void OnEndClose(IAsyncResult result)
		{
			try
			{
				AsyncResult<SbmpMessageReceiver.AbandonPrefetchedMessagesCloseAbortAsyncResult>.End(result);
			}
			catch (CommunicationException communicationException1)
			{
				CommunicationException communicationException = communicationException1;
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(MessagingExceptionHelper.Unwrap(communicationException, false), null);
			}
		}

		protected override void OnEndComplete(IAsyncResult result)
		{
			try
			{
				if (!(result is BatchManagerAsyncResult<Guid>))
				{
					this.EndUpdateCommand(result);
				}
				else
				{
					BatchManagerAsyncResult<Guid>.End(result);
				}
			}
			catch (CommunicationObjectAbortedException communicationObjectAbortedException)
			{
				throw new OperationCanceledException(SRClient.EntityClosedOrAborted, communicationObjectAbortedException);
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
		}

		protected override void OnEndDeadLetter(IAsyncResult result)
		{
			this.EndUpdateCommand(result);
		}

		protected override void OnEndDefer(IAsyncResult result)
		{
			this.EndUpdateCommand(result);
		}

		internal override Microsoft.ServiceBus.Messaging.RuntimeEntityDescription OnEndGetRuntimeEntityDescriptionAsyncResult(IAsyncResult result)
		{
			return GetRuntimeEntityDescriptionAsyncResult.End(result);
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			if (!base.BatchingEnabled)
			{
				CompletedAsyncResult.End(result);
			}
			else
			{
				this.BatchManager.EndOpen(result);
			}
			this.SbmpMessagingFactory.ScheduleGetRuntimeEntityDescription(TrackingContext.GetInstance(Guid.NewGuid()), this, this.Path, this.MessageCreator);
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

		protected override IEnumerable<DateTime> OnEndRenewMessageLocks(IAsyncResult result)
		{
			IEnumerable<DateTime> lockedUntilUtcCollection;
			try
			{
				lockedUntilUtcCollection = AsyncResult<SbmpMessageReceiver.RenewLockAsyncResult>.End(result).LockedUntilUtcCollection;
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
			return lockedUntilUtcCollection;
		}

		protected override bool OnEndTryReceive(IAsyncResult result, out IEnumerable<BrokeredMessage> messages)
		{
			if (result is SbmpMessageReceiver.ReceiveAsyncResult)
			{
				messages = AsyncResult<SbmpMessageReceiver.ReceiveAsyncResult>.End(result).Messages;
				if (messages == null)
				{
					messages = new List<BrokeredMessage>(0);
				}
				return messages.Any<BrokeredMessage>();
			}
			bool flag = false;
			messages = null;
			try
			{
				this.EndReceiveCommand(result, out messages);
			}
			catch (TimeoutException timeoutException)
			{
				flag = true;
			}
			if (flag || messages == null)
			{
				messages = new List<BrokeredMessage>(0);
			}
			return messages.Any<BrokeredMessage>();
		}

		protected override bool OnEndTryReceive2(IAsyncResult result, out IEnumerable<BrokeredMessage> messages)
		{
			messages = AsyncResult<SbmpMessageReceiver.ReceiveAsyncResult>.End(result).Messages;
			if (messages == null)
			{
				return false;
			}
			return messages.Any<BrokeredMessage>();
		}

		internal override void OnRuntimeDescriptionChanged(Microsoft.ServiceBus.Messaging.RuntimeEntityDescription newValue)
		{
			if (newValue == null)
			{
				this.SbmpReceiverBatchingEnabled = false;
			}
			else if (newValue.EnableSubscriptionPartitioning)
			{
				this.BatchManager.GroupByKeySelector = null;
				this.SbmpReceiverBatchingEnabled = true;
			}
			else if (!newValue.EnableMessagePartitioning)
			{
				this.BatchManager.GroupByKeySelector = null;
				this.SbmpReceiverBatchingEnabled = true;
			}
			else
			{
				this.BatchManager.GroupByKeySelector = new GroupByKeySelectorDelegate<Guid>(this.MessagePartitioningGroupByKeySelector);
				this.SbmpReceiverBatchingEnabled = true;
			}
			base.OnRuntimeDescriptionChanged(newValue);
		}

		[Conditional("CLIENT")]
		private static void TraceAbandon(TrackingContext requestTracker, IEnumerable<Guid> lockTokens)
		{
			SbmpMessageReceiver.GetFormattedLockToken(lockTokens);
			MessagingClientEtwProvider.TraceClient(() => {
			});
		}

		[Conditional("CLIENT")]
		private static void TraceComplete(TrackingContext requestTracker, IEnumerable<Guid> lockTokens)
		{
			SbmpMessageReceiver.GetFormattedLockToken(lockTokens);
			MessagingClientEtwProvider.TraceClient(() => {
			});
		}

		[Conditional("CLIENT")]
		private static void TraceDefer(TrackingContext requestTracker, IEnumerable<Guid> lockTokens)
		{
			SbmpMessageReceiver.GetFormattedLockToken(lockTokens);
			MessagingClientEtwProvider.TraceClient(() => {
			});
		}

		[Conditional("CLIENT")]
		private void TraceReceive(TrackingContext requestTracker, IEnumerable<BrokeredMessage> messages)
		{
			this.GetFormattedMessageId(messages);
			MessagingClientEtwProvider.TraceClient(() => {
			});
		}

		[Conditional("CLIENT")]
		private static void TraceRenewed(TrackingContext requestTracker, IEnumerable<Guid> lockTokens)
		{
			SbmpMessageReceiver.GetFormattedLockToken(lockTokens);
			MessagingClientEtwProvider.TraceClient(() => {
			});
		}

		[Conditional("CLIENT")]
		private static void TraceSuspend(TrackingContext requestTracker, IEnumerable<Guid> lockTokens)
		{
			SbmpMessageReceiver.GetFormattedLockToken(lockTokens);
			MessagingClientEtwProvider.TraceClient(() => {
			});
		}

		private bool TryRemoveFromCache(out BrokeredMessage message)
		{
			return this.prefetchedMessages.TryDequeue(out message);
		}

		private sealed class AbandonPrefetchedMessagesCloseAbortAsyncResult : IteratorAsyncResult<SbmpMessageReceiver.AbandonPrefetchedMessagesCloseAbortAsyncResult>
		{
			private readonly SbmpMessageReceiver receiver;

			public AbandonPrefetchedMessagesCloseAbortAsyncResult(SbmpMessageReceiver receiver, TimeSpan closeTimeout, AsyncCallback callback, object state) : base(closeTimeout, callback, state)
			{
				this.receiver = receiver;
			}

			protected override IEnumerator<IteratorAsyncResult<SbmpMessageReceiver.AbandonPrefetchedMessagesCloseAbortAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				BrokeredMessage brokeredMessage;
				if (!this.receiver.prefetchedMessages.IsEmpty)
				{
					if (this.receiver.Mode == ReceiveMode.PeekLock)
					{
						List<Guid> guids = new List<Guid>();
						while (this.receiver.prefetchedMessages.TryDequeue(out brokeredMessage))
						{
							guids.Add(brokeredMessage.LockToken);
						}
						SbmpMessageReceiver.AbandonPrefetchedMessagesCloseAbortAsyncResult abandonPrefetchedMessagesCloseAbortAsyncResult = this;
						IteratorAsyncResult<SbmpMessageReceiver.AbandonPrefetchedMessagesCloseAbortAsyncResult>.BeginCall beginCall = (SbmpMessageReceiver.AbandonPrefetchedMessagesCloseAbortAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.receiver.BeginAbandon(guids, t, c, s);
						yield return abandonPrefetchedMessagesCloseAbortAsyncResult.CallAsync(beginCall, (SbmpMessageReceiver.AbandonPrefetchedMessagesCloseAbortAsyncResult thisPtr, IAsyncResult a) => thisPtr.receiver.EndAbandon(a), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
					}
					this.receiver.DisposePrefetchedMessages();
				}
				SbmpMessageReceiver.AbandonPrefetchedMessagesCloseAbortAsyncResult abandonPrefetchedMessagesCloseAbortAsyncResult1 = this;
				IteratorAsyncResult<SbmpMessageReceiver.AbandonPrefetchedMessagesCloseAbortAsyncResult>.BeginCall beginCall1 = (SbmpMessageReceiver.AbandonPrefetchedMessagesCloseAbortAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.receiver.BeginCloseLink(t, c, s);
				yield return abandonPrefetchedMessagesCloseAbortAsyncResult1.CallAsync(beginCall1, (SbmpMessageReceiver.AbandonPrefetchedMessagesCloseAbortAsyncResult thisPtr, IAsyncResult r) => thisPtr.receiver.EndCloseLink(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
			}
		}

		private sealed class ReceiveAsyncResult : IteratorAsyncResult<SbmpMessageReceiver.ReceiveAsyncResult>
		{
			private readonly static TimeSpan MinimumSemaphoreWaitTime;

			private readonly SbmpMessageReceiver receiver;

			private readonly int messageCount;

			private readonly TrackingContext trackingContext;

			private readonly bool abortLinkOnReceiveTimeout;

			private readonly bool shouldThrowTimeout;

			private IEnumerable<BrokeredMessage> messages;

			private bool hasReceivedMessages;

			private readonly TimeSpan serverWaitTime;

			public IEnumerable<BrokeredMessage> Messages
			{
				get
				{
					return this.messages;
				}
			}

			static ReceiveAsyncResult()
			{
				SbmpMessageReceiver.ReceiveAsyncResult.MinimumSemaphoreWaitTime = TimeSpan.FromMilliseconds(500);
			}

			public ReceiveAsyncResult(SbmpMessageReceiver receiver, TrackingContext trackingContext, int messageCount, bool abortLinkOnReceiveTimeout, bool shouldThrowTimeout, TimeSpan serverWaitTime, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.receiver = receiver;
				this.trackingContext = trackingContext;
				this.messageCount = messageCount;
				this.abortLinkOnReceiveTimeout = abortLinkOnReceiveTimeout;
				this.shouldThrowTimeout = shouldThrowTimeout;
				this.serverWaitTime = serverWaitTime;
			}

			protected override IEnumerator<IteratorAsyncResult<SbmpMessageReceiver.ReceiveAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				int num;
				if (this.receiver.PrefetchCount <= 0)
				{
					SbmpMessageReceiver.ReceiveAsyncResult receiveAsyncResult = this;
					IteratorAsyncResult<SbmpMessageReceiver.ReceiveAsyncResult>.BeginCall beginCall = (SbmpMessageReceiver.ReceiveAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.receiver.BeginReceiveCommand(thisPtr.trackingContext, thisPtr.receiver.PrefetchCount + thisPtr.messageCount, this.serverWaitTime, thisPtr.receiver.PartitionId, t, c, s);
					yield return receiveAsyncResult.CallAsync(beginCall, (SbmpMessageReceiver.ReceiveAsyncResult thisPtr, IAsyncResult a) => thisPtr.receiver.EndReceiveCommand(a, out thisPtr.messages), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
					if (this.shouldThrowTimeout || !(base.LastAsyncStepException is TimeoutException))
					{
						base.Complete(base.LastAsyncStepException);
					}
				}
				else
				{
					TimeSpan minimumSemaphoreWaitTime = base.RemainingTime();
					if (minimumSemaphoreWaitTime < SbmpMessageReceiver.ReceiveAsyncResult.MinimumSemaphoreWaitTime)
					{
						minimumSemaphoreWaitTime = SbmpMessageReceiver.ReceiveAsyncResult.MinimumSemaphoreWaitTime;
					}
					SbmpMessageReceiver.ReceiveAsyncResult receiveAsyncResult1 = this;
					IteratorAsyncResult<SbmpMessageReceiver.ReceiveAsyncResult>.BeginCall beginCall1 = (SbmpMessageReceiver.ReceiveAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.receiver.prefetchWaitHandle.BeginWait(minimumSemaphoreWaitTime, c, s);
					yield return receiveAsyncResult1.CallAsync(beginCall1, (SbmpMessageReceiver.ReceiveAsyncResult thisPtr, IAsyncResult a) => thisPtr.receiver.prefetchWaitHandle.EndWait(a), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
					if (base.LastAsyncStepException == null)
					{
						try
						{
							BrokeredMessage brokeredMessage = null;
							if (!this.receiver.TryRemoveFromCache(out brokeredMessage))
							{
								SbmpMessageReceiver.ReceiveAsyncResult receiveAsyncResult2 = this;
								IteratorAsyncResult<SbmpMessageReceiver.ReceiveAsyncResult>.BeginCall beginCall2 = (SbmpMessageReceiver.ReceiveAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.receiver.BeginReceiveCommand(thisPtr.trackingContext, thisPtr.receiver.PrefetchCount + thisPtr.messageCount, this.serverWaitTime, thisPtr.receiver.PartitionId, t, c, s);
								yield return receiveAsyncResult2.CallAsync(beginCall2, (SbmpMessageReceiver.ReceiveAsyncResult thisPtr, IAsyncResult a) => thisPtr.hasReceivedMessages = thisPtr.receiver.EndReceiveCommand(a, out thisPtr.messages), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
								if (!this.hasReceivedMessages || this.messages.Count<BrokeredMessage>() <= this.messageCount)
								{
									goto Label0;
								}
								List<BrokeredMessage> brokeredMessages = new List<BrokeredMessage>(this.messages);
								int num1 = Math.Min(this.messageCount, brokeredMessages.Count);
								this.messages = brokeredMessages.GetRange(0, num1);
								for (int i = num1; i < brokeredMessages.Count; i++)
								{
									this.receiver.InsertInCache(brokeredMessages[i]);
								}
							}
							else
							{
								List<BrokeredMessage> brokeredMessages1 = new List<BrokeredMessage>();
								int num2 = 0;
								do
								{
									brokeredMessages1.Add(brokeredMessage);
									brokeredMessage.IsFromCache = true;
									num = num2 + 1;
									num2 = num;
								}
								while (num < this.messageCount && this.receiver.TryRemoveFromCache(out brokeredMessage));
								this.messages = brokeredMessages1;
							}
						}
						finally
						{
							this.receiver.prefetchWaitHandle.Set();
						}
					}
				Label0:
					if ((base.LastAsyncStepException is TimeoutException || base.LastAsyncStepException is MessagingCommunicationException) && this.abortLinkOnReceiveTimeout)
					{
						this.receiver.AbortLink(false);
					}
					if (this.shouldThrowTimeout || !(base.LastAsyncStepException is TimeoutException))
					{
						base.Complete(base.LastAsyncStepException);
					}
				}
			}
		}

		private sealed class RenewLockAsyncResult : IteratorAsyncResult<SbmpMessageReceiver.RenewLockAsyncResult>
		{
			private readonly SbmpMessageReceiver receiver;

			private readonly TrackingContext trackingContext;

			private readonly IEnumerable<Guid> lockTokens;

			private Message wcfMessage;

			private Message response;

			public List<DateTime> LockedUntilUtcCollection
			{
				get;
				private set;
			}

			public RenewLockAsyncResult(SbmpMessageReceiver sbmpMessageReceiver, TrackingContext trackingContext, IEnumerable<Guid> lockTokens, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.receiver = sbmpMessageReceiver;
				this.trackingContext = trackingContext;
				this.lockTokens = lockTokens;
				this.LockedUntilUtcCollection = new List<DateTime>();
				base.Start();
			}

			protected override IEnumerator<IteratorAsyncResult<SbmpMessageReceiver.RenewLockAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				Guid? nullable;
				MessageRenewLockCommand messageRenewLockCommand = new MessageRenewLockCommand()
				{
					LockTokens = this.lockTokens,
					Timeout = base.RemainingTime()
				};
				MessageRenewLockCommand messageRenewLockCommand1 = messageRenewLockCommand;
				RequestInfo requestInfo1 = new RequestInfo()
				{
					ServerTimeout = new TimeSpan?(messageRenewLockCommand1.Timeout)
				};
				RequestInfo requestInfo2 = requestInfo1;
				if (this.receiver.MessageCreator.LinkInfo.IsSessionReceiver || messageRenewLockCommand1.LockTokens == null)
				{
					nullable = null;
				}
				else
				{
					nullable = new Guid?(messageRenewLockCommand1.LockTokens.FirstOrDefault<Guid>());
				}
				requestInfo2.LockToken = nullable;
				RequestInfo requestInfo = requestInfo1;
				this.wcfMessage = this.receiver.MessageCreator.CreateWcfMessage("http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageReceiver/MessageRenewLock", messageRenewLockCommand1, this.receiver.GetParentLinkId(), this.receiver.RetryPolicy, this.trackingContext, requestInfo);
				TrackingContext trackingContext = this.trackingContext;
				SbmpMessageReceiver.RenewLockAsyncResult renewLockAsyncResult = this;
				IteratorAsyncResult<SbmpMessageReceiver.RenewLockAsyncResult>.BeginCall beginCall = (SbmpMessageReceiver.RenewLockAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.receiver.SbmpMessagingFactory.Channel.BeginRequest(thisPtr.wcfMessage, SbmpProtocolDefaults.BufferTimeout(t, this.receiver.SbmpMessagingFactory.GetSettings().EnableAdditionalClientTimeout), c, s);
				yield return renewLockAsyncResult.CallAsync(beginCall, (SbmpMessageReceiver.RenewLockAsyncResult thisPtr, IAsyncResult a) => thisPtr.response = thisPtr.receiver.SbmpMessagingFactory.Channel.EndRequest(a), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				if (this.response != null)
				{
					MessageRenewLockResponseCommand body = this.response.GetBody<MessageRenewLockResponseCommand>();
					if (body.LockedUntilUtcs != null)
					{
						foreach (DateTime lockedUntilUtc in body.LockedUntilUtcs)
						{
							this.LockedUntilUtcCollection.Add(lockedUntilUtc);
						}
					}
				}
			}
		}

		private sealed class UpdateAsyncResult : SbmpTransactionalAsyncResult<SbmpMessageReceiver.UpdateAsyncResult>
		{
			private readonly IEnumerable<Guid> lockTokens;

			private readonly DispositionStatus messageDisposition;

			private readonly SbmpMessageReceiver messageReceiver;

			private readonly TrackingContext trackingContext;

			private readonly DeadLetterInfo deadLetterInfo;

			private readonly IDictionary<string, object> propertiesToModify;

			public UpdateAsyncResult(SbmpMessageReceiver messageReceiver, TrackingContext trackingContext, IEnumerable<Guid> lockTokens, IDictionary<string, object> propertiesToModify, DispositionStatus messageDisposition, DeadLetterInfo deadLetterInfo, TimeSpan timeout, AsyncCallback callback, object state) : base(messageReceiver.SbmpMessagingFactory, messageReceiver.MessageCreator, messageReceiver.ControlMessageCreator, timeout, callback, state)
			{
				this.messageReceiver = messageReceiver;
				this.trackingContext = trackingContext;
				this.lockTokens = lockTokens;
				this.messageDisposition = messageDisposition;
				this.deadLetterInfo = deadLetterInfo;
				this.propertiesToModify = propertiesToModify;
				base.Start();
			}

			protected override Message CreateWcfMessage()
			{
				string localIdentifier;
				Guid? nullable;
				Transaction transaction = base.Transaction;
				UpdateMessageStateCommand updateMessageStateCommand = new UpdateMessageStateCommand()
				{
					LockTokens = this.lockTokens,
					MessageDisposition = this.messageDisposition,
					Timeout = base.RemainingTime()
				};
				UpdateMessageStateCommand updateMessageStateCommand1 = updateMessageStateCommand;
				if (transaction != null)
				{
					localIdentifier = transaction.TransactionInformation.LocalIdentifier;
				}
				else
				{
					localIdentifier = null;
				}
				updateMessageStateCommand1.TransactionId = localIdentifier;
				updateMessageStateCommand.DeadLetterInfo = this.deadLetterInfo;
				updateMessageStateCommand.PropertiesToModify = this.propertiesToModify;
				UpdateMessageStateCommand updateMessageStateCommand2 = updateMessageStateCommand;
				RequestInfo requestInfo = new RequestInfo()
				{
					ServerTimeout = new TimeSpan?(updateMessageStateCommand2.Timeout)
				};
				RequestInfo requestInfo1 = requestInfo;
				if (this.messageReceiver.MessageCreator.LinkInfo.IsSessionReceiver || updateMessageStateCommand2.LockTokens == null)
				{
					nullable = null;
				}
				else
				{
					nullable = new Guid?(updateMessageStateCommand2.LockTokens.FirstOrDefault<Guid>());
				}
				requestInfo1.LockToken = nullable;
				requestInfo.TransactionId = updateMessageStateCommand2.TransactionId;
				RequestInfo requestInfo2 = requestInfo;
				Message message = this.messageReceiver.MessageCreator.CreateWcfMessage("http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageReceiver/UpdateMessageState", updateMessageStateCommand2, this.messageReceiver.GetParentLinkId(), this.messageReceiver.RetryPolicy, this.trackingContext, requestInfo2);
				if (this.trackingContext != null)
				{
					switch (this.messageDisposition)
					{

					}
				}
				return message;
			}

			protected override void PartitionInfoSetter(RequestInfo requestInfo)
			{
				requestInfo.LockToken = new Guid?(this.lockTokens.FirstOrDefault<Guid>());
			}
		}
	}
}