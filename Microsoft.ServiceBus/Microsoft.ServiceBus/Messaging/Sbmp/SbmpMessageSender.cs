using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Transactions;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	internal sealed class SbmpMessageSender : MessageSender
	{
		private const int sendBatchSizeThreshold = 204800;

		private const int maximumAllowedSendBatchSize = 225280;

		private readonly string path;

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

		internal BatchManager<BrokeredMessage> BatchManager
		{
			get;
			private set;
		}

		internal bool EnableMessagePartitioning
		{
			get;
			private set;
		}

		internal bool EnableSubscriptionPartitioning
		{
			get;
			private set;
		}

		internal SbmpMessageCreator MessageCreator
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

		internal bool RequiresDuplicateDetection
		{
			get;
			private set;
		}

		internal Microsoft.ServiceBus.Messaging.Sbmp.SbmpMessagingFactory SbmpMessagingFactory
		{
			get;
			private set;
		}

		internal bool SbmpSenderBatchingEnabled
		{
			get;
			private set;
		}

		internal bool ShouldAddPartitioningHeaders
		{
			get;
			private set;
		}

		internal bool ViaSender
		{
			get;
			private set;
		}

		public SbmpMessageSender(string path, Microsoft.ServiceBus.Messaging.Sbmp.SbmpMessagingFactory messagingFactory, SbmpMessageCreator messageCreator, Microsoft.ServiceBus.RetryPolicy retryPolicy) : base(messagingFactory, retryPolicy)
		{
			this.SbmpMessagingFactory = messagingFactory;
			this.path = path;
			this.MessageCreator = messageCreator;
			this.ViaSender = !string.IsNullOrWhiteSpace(messageCreator.LinkInfo.TransferDestinationEntityAddress);
			this.BatchManager = new BatchManager<BrokeredMessage>((TrackingContext trackingContext, IEnumerable<BrokeredMessage> messages, string transactionId, TimeSpan timeout, AsyncCallback callback, object state) => this.BeginSendCommand(trackingContext, messages, timeout, callback, state), (IAsyncResult result, bool forceCleanUp) => this.EndSendCommand(result), null, new OnRetryDelegate<BrokeredMessage>(SbmpMessageSender.IsSendCommandRetryable), null, null, (long)204800, (long)225280, this.GetOverheadSize())
			{
				FlushInterval = this.SbmpMessagingFactory.Settings.BatchFlushInterval
			};
			if (!this.SbmpMessagingFactory.Settings.GatewayMode && (this.SbmpMessagingFactory.Settings.GatewayMode || !this.SbmpMessagingFactory.Settings.EnableRedirect))
			{
				this.ShouldAddPartitioningHeaders = true;
				return;
			}
			this.BatchManager.CalculateBatchSize = new CalculateBatchSizeDelegate<BrokeredMessage>(SbmpMessageSender.DefaultCalculateSizeOfMessages);
			this.SbmpSenderBatchingEnabled = true;
			this.ShouldAddPartitioningHeaders = false;
		}

		private static void AbortCallback(IAsyncResult result)
		{
			AsyncResult<CloseOrAbortLinkAsyncResult>.End(result);
		}

		internal void AddRequestInfoHeader(RequestInfo requestInfo, IEnumerable<BrokeredMessage> messages)
		{
			if (this.EnableSubscriptionPartitioning)
			{
				requestInfo.SessionId = (
					from bm in messages
					where bm.SessionId != null
					select bm.SessionId).FirstOrDefault<string>();
				requestInfo.PartitionKey = (
					from bm in messages
					where bm.PartitionKey != null
					select bm.PartitionKey).FirstOrDefault<string>();
				requestInfo.Destination = (
					from bm in messages
					where bm.Destination != null
					select bm.Destination).FirstOrDefault<string>();
				return;
			}
			if (!this.EnableMessagePartitioning)
			{
				if (this.ShouldAddPartitioningHeaders)
				{
					if (this.ViaSender)
					{
						requestInfo.ViaPartitionKey = (
							from bm in messages
							where bm.ViaPartitionKey != null
							select bm.ViaPartitionKey).FirstOrDefault<string>();
						return;
					}
					requestInfo.SessionId = (
						from bm in messages
						where bm.SessionId != null
						select bm.SessionId).FirstOrDefault<string>();
					requestInfo.PartitionKey = (
						from bm in messages
						where bm.PartitionKey != null
						select bm.PartitionKey).FirstOrDefault<string>();
					requestInfo.MessageId = (
						from bm in messages
						where bm.MessageId != null
						select bm.MessageId).FirstOrDefault<string>();
					requestInfo.Destination = (
						from bm in messages
						where bm.Destination != null
						select bm.Destination).FirstOrDefault<string>();
				}
				return;
			}
			if (this.ViaSender)
			{
				requestInfo.ViaPartitionKey = (
					from bm in messages
					where bm.ViaPartitionKey != null
					select bm.ViaPartitionKey).FirstOrDefault<string>();
				return;
			}
			if (!this.RequiresDuplicateDetection)
			{
				requestInfo.SessionId = (
					from bm in messages
					where bm.SessionId != null
					select bm.SessionId).FirstOrDefault<string>();
				requestInfo.PartitionKey = (
					from bm in messages
					where bm.PartitionKey != null
					select bm.PartitionKey).FirstOrDefault<string>();
				return;
			}
			requestInfo.SessionId = (
				from bm in messages
				where bm.SessionId != null
				select bm.SessionId).FirstOrDefault<string>();
			requestInfo.PartitionKey = (
				from bm in messages
				where bm.PartitionKey != null
				select bm.PartitionKey).FirstOrDefault<string>();
			requestInfo.MessageId = (
				from bm in messages
				where bm.MessageId != null
				select bm.MessageId).FirstOrDefault<string>();
		}

		private IAsyncResult BeginCancelScheduledMessageCommand(TrackingContext trackingContext, IEnumerable<long> sequenceNumbers, TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult cancelScheduledMessageCommandAsyncResult;
			try
			{
				cancelScheduledMessageCommandAsyncResult = new SbmpMessageSender.CancelScheduledMessageCommandAsyncResult(this, trackingContext, sequenceNumbers, timeout, callback, state);
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
			return cancelScheduledMessageCommandAsyncResult;
		}

		private IAsyncResult BeginSendCommand(TrackingContext trackingContext, IEnumerable<BrokeredMessage> messages, TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult sendCommandAsyncResult;
			try
			{
				sendCommandAsyncResult = new SbmpMessageSender.SendCommandAsyncResult(this, trackingContext, messages, timeout, callback, state);
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
			return sendCommandAsyncResult;
		}

		internal static int DeDupMessagePartitioningCalculateSizeOfMessages(IEnumerable<BrokeredMessage> messages)
		{
			long serializedSize = (long)0;
			foreach (BrokeredMessage message in messages)
			{
				serializedSize = serializedSize + message.GetSerializedSize(SerializationTarget.Communication);
				if (message.SessionId != null)
				{
					serializedSize = serializedSize + (long)((int)Encoding.UTF8.GetBytes(message.SessionId).Length);
				}
				else if (message.PartitionKey == null)
				{
					if (message.MessageId == null)
					{
						continue;
					}
					serializedSize = serializedSize + (long)((int)Encoding.UTF8.GetBytes(message.MessageId).Length);
				}
				else
				{
					serializedSize = serializedSize + (long)((int)Encoding.UTF8.GetBytes(message.PartitionKey).Length);
				}
			}
			return (int)serializedSize;
		}

		internal IComparable DeDupMessagePartitioningKeyGroupByKeySelector(IEnumerable<BrokeredMessage> batchedObjects)
		{
			BrokeredMessage brokeredMessage = batchedObjects.First<BrokeredMessage>();
			return brokeredMessage.SessionId ?? brokeredMessage.PartitionKey ?? brokeredMessage.MessageId;
		}

		internal static int DefaultCalculateSizeOfMessages(IEnumerable<BrokeredMessage> messages)
		{
			long serializedSize = (long)0;
			foreach (BrokeredMessage message in messages)
			{
				serializedSize = serializedSize + message.GetSerializedSize(SerializationTarget.Communication);
			}
			return (int)serializedSize;
		}

		private void EndCancelScheduledMessageCommand(IAsyncResult result)
		{
			try
			{
				AsyncResult<SbmpMessageSender.CancelScheduledMessageCommandAsyncResult>.End(result);
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

		private void EndSendCommand(IAsyncResult result)
		{
			try
			{
				AsyncResult<SbmpMessageSender.SendCommandAsyncResult>.End(result);
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

		private static string GetFormattedMessageId(IEnumerable<BrokeredMessage> messages)
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (BrokeredMessage message in messages)
			{
				CultureInfo invariantCulture = CultureInfo.InvariantCulture;
				object[] messageId = new object[] { message.MessageId };
				stringBuilder.AppendLine(string.Format(invariantCulture, "<MessageId>{0}</MessageId>", messageId));
			}
			return stringBuilder.ToString();
		}

		private static string GetFormattedSequenceNumbers(IEnumerable<long> sequenceNumbers)
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (long sequenceNumber in sequenceNumbers)
			{
				CultureInfo invariantCulture = CultureInfo.InvariantCulture;
				object[] objArray = new object[] { sequenceNumber };
				stringBuilder.AppendLine(string.Format(invariantCulture, "<SequenceNumber>{0}</SequenceNumber>", objArray));
			}
			return stringBuilder.ToString();
		}

		private int GetOverheadSize()
		{
			long length = (long)0;
			using (MemoryStream memoryStream = new MemoryStream(1024))
			{
				BrokeredMessage brokeredMessage = new BrokeredMessage();
				BrokeredMessage brokeredMessage1 = new BrokeredMessage();
				SendCommand sendCommand = new SendCommand();
				BrokeredMessage[] brokeredMessageArray = new BrokeredMessage[] { brokeredMessage, brokeredMessage1 };
				sendCommand.Messages = new MessageCollection(brokeredMessageArray);
				sendCommand.Timeout = TimeSpan.MaxValue;
				sendCommand.TransactionId = new string('A', 32);
				SendCommand sendCommand1 = sendCommand;
				RequestInfo requestInfo = new RequestInfo()
				{
					ServerTimeout = new TimeSpan?(sendCommand1.Timeout),
					OperationTimeout = new TimeSpan?(sendCommand1.Timeout),
					MessageId = Guid.NewGuid().ToString(),
					SessionId = Guid.NewGuid().ToString(),
					PartitionKey = Guid.NewGuid().ToString(),
					ViaPartitionKey = Guid.NewGuid().ToString(),
					TransactionId = Guid.NewGuid().ToString(),
					Destination = Guid.NewGuid().ToString()
				};
				RequestInfo requestInfo1 = requestInfo;
				using (Message message = this.MessageCreator.CreateWcfMessage("http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageSender/Send", sendCommand1, null, base.RetryPolicy, TrackingContext.GetInstance(Guid.NewGuid(), null), requestInfo1))
				{
					(new BinaryMessageEncodingBindingElement()).CreateMessageEncoderFactory().Encoder.WriteMessage(message, memoryStream);
				}
				memoryStream.Flush();
				length = memoryStream.Length;
				length = length - (brokeredMessage.Size + brokeredMessage1.Size);
				length = length + (long)512;
				brokeredMessage.Dispose();
				brokeredMessage1.Dispose();
			}
			return (int)length;
		}

		private static bool IsSendCommandRetryable(IEnumerable<BrokeredMessage> messages, Exception exception, bool isMultiCommandBatch)
		{
			bool flag;
			using (IEnumerator<BrokeredMessage> enumerator = messages.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					Stream bodyStream = enumerator.Current.BodyStream;
					if (bodyStream == null || bodyStream.CanSeek)
					{
						if (bodyStream == null)
						{
							continue;
						}
						bodyStream.Position = (long)0;
					}
					else
					{
						flag = false;
						return flag;
					}
				}
				return true;
			}
			return flag;
		}

		internal static int MessagePartitioningCalculateSizeOfMessages(IEnumerable<BrokeredMessage> messages)
		{
			long serializedSize = (long)0;
			foreach (BrokeredMessage message in messages)
			{
				serializedSize = serializedSize + message.GetSerializedSize(SerializationTarget.Communication);
				if (message.SessionId == null)
				{
					if (message.PartitionKey == null)
					{
						continue;
					}
					serializedSize = serializedSize + (long)((int)Encoding.UTF8.GetBytes(message.PartitionKey).Length);
				}
				else
				{
					serializedSize = serializedSize + (long)((int)Encoding.UTF8.GetBytes(message.SessionId).Length);
				}
			}
			return (int)serializedSize;
		}

		internal IComparable MessagePartitioningKeyGroupByKeySelector(IEnumerable<BrokeredMessage> batchedObjects)
		{
			BrokeredMessage brokeredMessage = batchedObjects.First<BrokeredMessage>();
			return brokeredMessage.SessionId ?? brokeredMessage.PartitionKey;
		}

		internal static int MessagePartitioningViaSenderCalculateSizeOfMessages(IEnumerable<BrokeredMessage> messages)
		{
			long serializedSize = (long)0;
			foreach (BrokeredMessage message in messages)
			{
				serializedSize = serializedSize + message.GetSerializedSize(SerializationTarget.Communication);
				if (message.ViaPartitionKey == null)
				{
					continue;
				}
				serializedSize = serializedSize + (long)((int)Encoding.UTF8.GetBytes(message.ViaPartitionKey).Length);
			}
			return (int)serializedSize;
		}

		protected override void OnAbort()
		{
			ICommunicationObject batchManager;
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
			CloseOrAbortLinkAsyncResult closeOrAbortLinkAsyncResult = new CloseOrAbortLinkAsyncResult(messageCreator, channel, batchManager, this.OperationTimeout, true, new AsyncCallback(SbmpMessageSender.AbortCallback), null);
			closeOrAbortLinkAsyncResult.Schedule();
		}

		protected override IAsyncResult OnBeginCancelScheduledMessage(TrackingContext trackingContext, IEnumerable<long> sequenceNumbers, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.BeginCancelScheduledMessageCommand(trackingContext, sequenceNumbers, timeout, callback, state);
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult asyncResult;
			ICommunicationObject batchManager;
			try
			{
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
				asyncResult = (new CloseOrAbortLinkAsyncResult(messageCreator, channel, batchManager, timeout, false, callback, state)).Start();
			}
			catch (CommunicationException communicationException1)
			{
				CommunicationException communicationException = communicationException1;
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(MessagingExceptionHelper.Unwrap(communicationException, false), null);
			}
			return asyncResult;
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (!base.BatchingEnabled)
			{
				return new CompletedAsyncResult(callback, state);
			}
			return this.BatchManager.BeginOpen(timeout, callback, state);
		}

		protected override IAsyncResult OnBeginScheduleMessage(TrackingContext trackingContext, IEnumerable<BrokeredMessage> messages, TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult scheduleMessageCommandAsyncResult;
			try
			{
				scheduleMessageCommandAsyncResult = new SbmpMessageSender.ScheduleMessageCommandAsyncResult(this, trackingContext, messages, timeout, callback, state);
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
			return scheduleMessageCommandAsyncResult;
		}

		protected override IAsyncResult OnBeginSend(TrackingContext trackingContext, IEnumerable<BrokeredMessage> messages, bool fromSync, TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (!(Transaction.Current == null) || fromSync || !base.BatchingEnabled || !this.SbmpSenderBatchingEnabled)
			{
				return this.BeginSendCommand(trackingContext, messages, timeout, callback, state);
			}
			return new BatchManagerAsyncResult<BrokeredMessage>(trackingContext, this.BatchManager, messages, timeout, callback, state);
		}

		protected override IAsyncResult OnBeginSendEventData(TrackingContext trackingContext, IEnumerable<EventData> eventDatas, TimeSpan timeout, AsyncCallback callback, object state)
		{
			List<BrokeredMessage> brokeredMessages = new List<BrokeredMessage>(
				from data in eventDatas
				select data.ToBrokeredMessage());
			return this.OnBeginSend(trackingContext, brokeredMessages, false, timeout, callback, state);
		}

		protected override void OnEndCancelScheduledMessage(IAsyncResult result)
		{
			try
			{
				this.EndCancelScheduledMessageCommand(result);
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

		protected override void OnEndClose(IAsyncResult result)
		{
			try
			{
				AsyncResult<CloseOrAbortLinkAsyncResult>.End(result);
			}
			catch (CommunicationException communicationException1)
			{
				CommunicationException communicationException = communicationException1;
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(MessagingExceptionHelper.Unwrap(communicationException, false), null);
			}
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

		protected override IEnumerable<long> OnEndScheduleMessage(IAsyncResult result)
		{
			IEnumerable<long> sequenceNumbers;
			try
			{
				sequenceNumbers = AsyncResult<SbmpMessageSender.ScheduleMessageCommandAsyncResult>.End(result).SequenceNumbers;
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
			return sequenceNumbers;
		}

		protected override void OnEndSend(IAsyncResult result)
		{
			try
			{
				if (!(result is BatchManagerAsyncResult<BrokeredMessage>))
				{
					this.EndSendCommand(result);
				}
				else
				{
					BatchManagerAsyncResult<BrokeredMessage>.End(result);
				}
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

		protected override void OnEndSendEventData(IAsyncResult result)
		{
			this.OnEndSend(result);
		}

		internal override void OnRuntimeDescriptionChanged(Microsoft.ServiceBus.Messaging.RuntimeEntityDescription newValue)
		{
			if (newValue == null)
			{
				this.SbmpSenderBatchingEnabled = false;
			}
			else
			{
				this.RequiresDuplicateDetection = newValue.RequiresDuplicateDetection;
				if (newValue.EnableSubscriptionPartitioning)
				{
					this.EnableSubscriptionPartitioning = true;
					this.EnableMessagePartitioning = false;
					this.ShouldAddPartitioningHeaders = true;
					this.BatchManager.GroupByKeySelector = new GroupByKeySelectorDelegate<BrokeredMessage>(this.SubscriptionPartitioningGroupByKeySelector);
					this.BatchManager.CalculateBatchSize = new CalculateBatchSizeDelegate<BrokeredMessage>(SbmpMessageSender.SubscriptionPartitioningCalculateSizeOfMessages);
					this.SbmpSenderBatchingEnabled = true;
				}
				else if (!newValue.EnableMessagePartitioning)
				{
					this.EnableMessagePartitioning = false;
					this.EnableSubscriptionPartitioning = false;
					this.ShouldAddPartitioningHeaders = false;
					this.BatchManager.GroupByKeySelector = null;
					this.BatchManager.CalculateBatchSize = new CalculateBatchSizeDelegate<BrokeredMessage>(SbmpMessageSender.DefaultCalculateSizeOfMessages);
					this.SbmpSenderBatchingEnabled = true;
				}
				else
				{
					this.EnableMessagePartitioning = true;
					this.EnableSubscriptionPartitioning = false;
					this.ShouldAddPartitioningHeaders = true;
					if (!newValue.RequiresDuplicateDetection)
					{
						this.BatchManager.GroupByKeySelector = new GroupByKeySelectorDelegate<BrokeredMessage>(this.MessagePartitioningKeyGroupByKeySelector);
						this.BatchManager.CalculateBatchSize = (this.ViaSender ? new CalculateBatchSizeDelegate<BrokeredMessage>(SbmpMessageSender.MessagePartitioningViaSenderCalculateSizeOfMessages) : new CalculateBatchSizeDelegate<BrokeredMessage>(SbmpMessageSender.MessagePartitioningCalculateSizeOfMessages));
					}
					else
					{
						this.BatchManager.GroupByKeySelector = new GroupByKeySelectorDelegate<BrokeredMessage>(this.DeDupMessagePartitioningKeyGroupByKeySelector);
						this.BatchManager.CalculateBatchSize = (this.ViaSender ? new CalculateBatchSizeDelegate<BrokeredMessage>(SbmpMessageSender.MessagePartitioningViaSenderCalculateSizeOfMessages) : new CalculateBatchSizeDelegate<BrokeredMessage>(SbmpMessageSender.DeDupMessagePartitioningCalculateSizeOfMessages));
					}
					this.SbmpSenderBatchingEnabled = true;
				}
			}
			base.OnRuntimeDescriptionChanged(newValue);
		}

		internal static int SubscriptionPartitioningCalculateSizeOfMessages(IEnumerable<BrokeredMessage> messages)
		{
			long serializedSize = (long)0;
			foreach (BrokeredMessage message in messages)
			{
				serializedSize = serializedSize + message.GetSerializedSize(SerializationTarget.Communication);
				if (message.Destination == null)
				{
					continue;
				}
				serializedSize = serializedSize + (long)((int)Encoding.UTF8.GetBytes(message.Destination).Length);
			}
			return (int)serializedSize;
		}

		internal IComparable SubscriptionPartitioningGroupByKeySelector(IEnumerable<BrokeredMessage> batchedObjects)
		{
			BrokeredMessage brokeredMessage = batchedObjects.First<BrokeredMessage>();
			return brokeredMessage.Destination ?? brokeredMessage.SessionId ?? brokeredMessage.PartitionKey;
		}

		private static void TraceCancel(EventTraceActivity fromActivity, TrackingContext requestTracker, IEnumerable<long> sequenceNumbers)
		{
			if (fromActivity != null && fromActivity != EventTraceActivity.Empty)
			{
				MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageSendingTransfer(fromActivity, requestTracker.Activity));
			}
			SbmpMessageSender.GetFormattedSequenceNumbers(sequenceNumbers);
			MessagingClientEtwProvider.TraceClient(() => {
			});
		}

		private static void TraceSend(EventTraceActivity fromActivity, TrackingContext requestTracker, IEnumerable<BrokeredMessage> messages)
		{
			if (fromActivity != null && fromActivity != EventTraceActivity.Empty)
			{
				MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageSendingTransfer(fromActivity, requestTracker.Activity));
			}
			SbmpMessageSender.GetFormattedMessageId(messages);
			MessagingClientEtwProvider.TraceClient(() => {
			});
		}

		private sealed class CancelScheduledMessageCommandAsyncResult : IteratorAsyncResult<SbmpMessageSender.CancelScheduledMessageCommandAsyncResult>
		{
			private readonly IEnumerable<long> sequenceNumbers;

			private readonly SbmpMessageSender messageSender;

			private readonly TrackingContext trackingContext;

			private readonly EventTraceActivity relatedActivity;

			private Message wcfMessage;

			public CancelScheduledMessageCommandAsyncResult(SbmpMessageSender messageSender, TrackingContext trackingContext, IEnumerable<long> sequenceNumbers, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.messageSender = messageSender;
				this.trackingContext = trackingContext;
				this.sequenceNumbers = sequenceNumbers;
				this.relatedActivity = EventTraceActivity.CreateFromThread();
				base.Start();
			}

			protected override IEnumerator<IteratorAsyncResult<SbmpMessageSender.CancelScheduledMessageCommandAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				CancelScheduledMessageCommand cancelScheduledMessageCommand = new CancelScheduledMessageCommand()
				{
					SequenceNumbers = this.sequenceNumbers,
					Timeout = base.RemainingTime(),
					TransactionId = null
				};
				CancelScheduledMessageCommand cancelScheduledMessageCommand1 = cancelScheduledMessageCommand;
				RequestInfo requestInfo1 = new RequestInfo()
				{
					ServerTimeout = new TimeSpan?(cancelScheduledMessageCommand1.Timeout),
					TransactionId = cancelScheduledMessageCommand1.TransactionId
				};
				RequestInfo nullable = requestInfo1;
				nullable.SequenceNumber = new long?(this.sequenceNumbers.First<long>());
				if (this.trackingContext != null)
				{
					SbmpMessageSender.TraceCancel(this.relatedActivity, this.trackingContext, this.sequenceNumbers);
				}
				this.wcfMessage = this.messageSender.MessageCreator.CreateWcfMessage("http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageSender/CancelScheduledMessage", cancelScheduledMessageCommand1, null, this.messageSender.RetryPolicy, this.trackingContext, nullable);
				SbmpMessageSender.CancelScheduledMessageCommandAsyncResult cancelScheduledMessageCommandAsyncResult = this;
				IteratorAsyncResult<SbmpMessageSender.CancelScheduledMessageCommandAsyncResult>.BeginCall beginCall = (SbmpMessageSender.CancelScheduledMessageCommandAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.messageSender.SbmpMessagingFactory.Channel.BeginRequest(thisPtr.wcfMessage, thisPtr.OriginalTimeout, c, s);
				yield return cancelScheduledMessageCommandAsyncResult.CallAsync(beginCall, (SbmpMessageSender.CancelScheduledMessageCommandAsyncResult thisPtr, IAsyncResult r) => thisPtr.messageSender.SbmpMessagingFactory.Channel.EndRequest(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
			}
		}

		private sealed class ScheduleMessageCommandAsyncResult : IteratorAsyncResult<SbmpMessageSender.ScheduleMessageCommandAsyncResult>
		{
			private readonly IEnumerable<BrokeredMessage> messages;

			private readonly SbmpMessageSender messageSender;

			private readonly TrackingContext trackingContext;

			private Message wcfMessage;

			private Message response;

			private readonly EventTraceActivity relatedActivity;

			public List<long> SequenceNumbers
			{
				get;
				private set;
			}

			public ScheduleMessageCommandAsyncResult(SbmpMessageSender messageSender, TrackingContext trackingContext, IEnumerable<BrokeredMessage> messages, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.messageSender = messageSender;
				this.trackingContext = trackingContext;
				this.messages = messages;
				this.relatedActivity = EventTraceActivity.CreateFromThread();
				this.SequenceNumbers = new List<long>();
				base.Start();
			}

			protected override IEnumerator<IteratorAsyncResult<SbmpMessageSender.ScheduleMessageCommandAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				ScheduleMessageCommand scheduleMessageCommand = new ScheduleMessageCommand()
				{
					Messages = MessageCollection.Wrap(this.messages),
					Timeout = base.RemainingTime(),
					TransactionId = null
				};
				ScheduleMessageCommand scheduleMessageCommand1 = scheduleMessageCommand;
				RequestInfo requestInfo1 = new RequestInfo()
				{
					ServerTimeout = new TimeSpan?(scheduleMessageCommand1.Timeout),
					TransactionId = scheduleMessageCommand1.TransactionId
				};
				RequestInfo requestInfo = requestInfo1;
				this.messageSender.AddRequestInfoHeader(requestInfo, this.messages);
				if (this.trackingContext != null)
				{
					SbmpMessageSender.TraceSend(this.relatedActivity, this.trackingContext, this.messages);
				}
				this.wcfMessage = this.messageSender.MessageCreator.CreateWcfMessage("http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageSender/ScheduleMessage", scheduleMessageCommand1, null, this.messageSender.RetryPolicy, this.trackingContext, requestInfo);
				SbmpMessageSender.ScheduleMessageCommandAsyncResult scheduleMessageCommandAsyncResult = this;
				IteratorAsyncResult<SbmpMessageSender.ScheduleMessageCommandAsyncResult>.BeginCall beginCall = (SbmpMessageSender.ScheduleMessageCommandAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.messageSender.SbmpMessagingFactory.Channel.BeginRequest(thisPtr.wcfMessage, thisPtr.OriginalTimeout, c, s);
				yield return scheduleMessageCommandAsyncResult.CallAsync(beginCall, (SbmpMessageSender.ScheduleMessageCommandAsyncResult thisPtr, IAsyncResult a) => thisPtr.response = thisPtr.messageSender.SbmpMessagingFactory.Channel.EndRequest(a), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				ScheduleMessageResponseCommand body = this.response.GetBody<ScheduleMessageResponseCommand>();
				if (body.SequenceNumbers != null)
				{
					foreach (long sequenceNumber in body.SequenceNumbers)
					{
						this.SequenceNumbers.Add(sequenceNumber);
					}
				}
			}
		}

		private sealed class SendCommandAsyncResult : SbmpTransactionalAsyncResult<SbmpMessageSender.SendCommandAsyncResult>
		{
			private readonly IEnumerable<BrokeredMessage> messages;

			private readonly SbmpMessageSender messageSender;

			private readonly TrackingContext trackingContext;

			private readonly EventTraceActivity relatedActivity;

			public SendCommandAsyncResult(SbmpMessageSender messageSender, TrackingContext trackingContext, IEnumerable<BrokeredMessage> messages, TimeSpan timeout, AsyncCallback callback, object state) : base(messageSender.SbmpMessagingFactory, messageSender.MessageCreator, null, timeout, callback, state)
			{
				this.messageSender = messageSender;
				this.trackingContext = trackingContext;
				this.messages = messages;
				this.relatedActivity = EventTraceActivity.CreateFromThread();
				base.Start();
			}

			protected override Message CreateWcfMessage()
			{
				string localIdentifier;
				Transaction transaction = base.Transaction;
				SendCommand sendCommand = new SendCommand()
				{
					Messages = MessageCollection.Wrap(this.messages),
					Timeout = base.RemainingTime()
				};
				SendCommand sendCommand1 = sendCommand;
				if (transaction != null)
				{
					localIdentifier = transaction.TransactionInformation.LocalIdentifier;
				}
				else
				{
					localIdentifier = null;
				}
				sendCommand1.TransactionId = localIdentifier;
				SendCommand sendCommand2 = sendCommand;
				RequestInfo requestInfo = new RequestInfo()
				{
					ServerTimeout = new TimeSpan?(sendCommand2.Timeout),
					TransactionId = sendCommand2.TransactionId
				};
				RequestInfo requestInfo1 = requestInfo;
				this.messageSender.AddRequestInfoHeader(requestInfo1, this.messages);
				if (this.trackingContext != null)
				{
					SbmpMessageSender.TraceSend(this.relatedActivity, this.trackingContext, this.messages);
				}
				Message message = base.MessageCreator.CreateWcfMessage("http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpMessageSender/Send", sendCommand2, null, this.messageSender.RetryPolicy, this.trackingContext, requestInfo1);
				return message;
			}

			protected override void PartitionInfoSetter(RequestInfo requestInfo)
			{
				if (this.messageSender.ViaSender)
				{
					requestInfo.ViaPartitionKey = (
						from bm in this.messages
						where bm.ViaPartitionKey != null
						select bm.ViaPartitionKey).FirstOrDefault<string>();
					return;
				}
				requestInfo.SessionId = (
					from bm in this.messages
					where bm.SessionId != null
					select bm.SessionId).FirstOrDefault<string>();
				requestInfo.PartitionKey = (
					from bm in this.messages
					where bm.PartitionKey != null
					select bm.PartitionKey).FirstOrDefault<string>();
				requestInfo.MessageId = (
					from bm in this.messages
					where bm.MessageId != null
					select bm.MessageId).FirstOrDefault<string>();
			}
		}
	}
}