using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Transactions;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal sealed class AmqpMessageReceiver : MessageReceiver
	{
		private readonly AmqpMessagingFactory messagingFactory;

		private readonly string entityName;

		private readonly bool sessionReceiver;

		private string sessionId;

		private FaultTolerantObject<ReceivingAmqpLink> receiveLink;

		private ConcurrentDictionary<Guid, ArraySegment<byte>> lockedMessages;

		private ActiveClientLinkManager clientLinkManager;

		private TimeSpan batchFlushInterval;

		public override TimeSpan BatchFlushInterval
		{
			get
			{
				return this.batchFlushInterval;
			}
			internal set
			{
				base.ThrowIfDisposedOrImmutable();
				this.batchFlushInterval = value;
			}
		}

		public override string Path
		{
			get
			{
				return this.entityName;
			}
		}

		public override int PrefetchCount
		{
			get
			{
				return base.PrefetchCount;
			}
			set
			{
				if (value != base.PrefetchCount)
				{
					base.PrefetchCount = value;
					ReceivingAmqpLink unsafeInnerObject = this.receiveLink.UnsafeInnerObject;
					if (unsafeInnerObject != null)
					{
						unsafeInnerObject.SetTotalLinkCredit((uint)value, true, true);
					}
				}
			}
		}

		public string SessionId
		{
			get
			{
				return this.sessionId;
			}
		}

		protected internal override bool SupportsGetRuntimeEntityDescription
		{
			get
			{
				return false;
			}
		}

		private AmqpMessageReceiver(AmqpMessagingFactory messagingFactory, string entityName, MessagingEntityType? entityType, string sessionId, bool sessionReceiver, Microsoft.ServiceBus.RetryPolicy retryPolicy, ReceiveMode receiveMode, Microsoft.ServiceBus.Messaging.Filter filter) : base(messagingFactory, retryPolicy, receiveMode, filter)
		{
			this.receiveLink = new FaultTolerantObject<ReceivingAmqpLink>(this, new Action<ReceivingAmqpLink>(this.CloseLink), new Func<TimeSpan, AsyncCallback, object, IAsyncResult>(this.BeginCreateLink), new Func<IAsyncResult, ReceivingAmqpLink>(this.EndCreateLink));
			this.messagingFactory = messagingFactory;
			this.entityName = entityName;
			this.EntityType = entityType;
			this.sessionId = sessionId;
			this.sessionReceiver = sessionReceiver;
			this.PrefetchCount = this.messagingFactory.PrefetchCount;
			this.lockedMessages = new ConcurrentDictionary<Guid, ArraySegment<byte>>();
			this.clientLinkManager = new ActiveClientLinkManager(this.messagingFactory);
			this.batchFlushInterval = this.messagingFactory.TransportSettings.BatchFlushInterval;
		}

		public AmqpMessageReceiver(AmqpMessagingFactory messagingFactory, string entityName, MessagingEntityType? entityType, Microsoft.ServiceBus.RetryPolicy retryPolicy, ReceiveMode receiveMode) : this(messagingFactory, entityName, entityType, null, false, retryPolicy, receiveMode, null)
		{
		}

		public AmqpMessageReceiver(AmqpMessagingFactory messagingFactory, string entityName, MessagingEntityType? entityType, Microsoft.ServiceBus.RetryPolicy retryPolicy, ReceiveMode receiveMode, Microsoft.ServiceBus.Messaging.Filter filter) : this(messagingFactory, entityName, entityType, null, false, retryPolicy, receiveMode, filter)
		{
		}

		public AmqpMessageReceiver(AmqpMessagingFactory messagingFactory, string entityName, MessagingEntityType? entityType, string sessionId, Microsoft.ServiceBus.RetryPolicy retryPolicy, ReceiveMode receiveMode) : this(messagingFactory, entityName, entityType, sessionId, true, retryPolicy, receiveMode, null)
		{
		}

		private IAsyncResult BeginCreateLink(TimeSpan timeout, AsyncCallback callback, object state)
		{
			IList<AmqpDescribed> amqpDescribeds = this.CreateFilters();
			string str = (!string.IsNullOrWhiteSpace(base.PartitionId) ? EntityNameHelper.FormatPartitionReceiverPath(this.entityName, base.PartitionId) : this.entityName);
			return this.messagingFactory.BeginOpenEntity(this, str, this.EntityType, this.PrefetchCount, this.sessionId, this.sessionReceiver, base.Mode, amqpDescribeds, base.Epoch, timeout, callback, state);
		}

		private IAsyncResult BeginDisposeEvents(IEnumerable<ArraySegment<byte>> deliveryTags, Outcome outcome, bool batchable, TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (System.Transactions.Transaction.Current != null)
			{
				throw new NotSupportedException(SRClient.FeatureNotSupported("Transaction"));
			}
			return new AmqpMessageReceiver.DisposeEventAsyncResult(this, deliveryTags, outcome, batchable, timeout, callback, state);
		}

		private IAsyncResult BeginDisposeMessages(IEnumerable<Guid> lockTokens, Outcome outcome, bool batchable, TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (System.Transactions.Transaction.Current != null)
			{
				throw new NotSupportedException(SRClient.FeatureNotSupported("Transaction"));
			}
			return new AmqpMessageReceiver.DisposeMessageAsyncResult(this, lockTokens, outcome, batchable, timeout, callback, state);
		}

		private void CloseLink(ReceivingAmqpLink link)
		{
			link.Session.SafeClose();
		}

		private IList<AmqpDescribed> CreateFilters()
		{
			string str;
			if (string.IsNullOrWhiteSpace(this.StartOffset) && !this.ReceiverStartTime.HasValue && base.Filter == null)
			{
				return null;
			}
			List<AmqpDescribed> amqpDescribeds = null;
			if (base.Filter != null)
			{
				amqpDescribeds = new List<AmqpDescribed>()
				{
					MessageConverter.GetFilter(base.Filter)
				};
			}
			if (base.Mode == ReceiveMode.ReceiveAndDelete && (!string.IsNullOrWhiteSpace(this.StartOffset) || this.ReceiverStartTime.HasValue))
			{
				if (amqpDescribeds == null)
				{
					amqpDescribeds = new List<AmqpDescribed>();
				}
				if (string.IsNullOrWhiteSpace(this.StartOffset))
				{
					CultureInfo invariantCulture = CultureInfo.InvariantCulture;
					object[] milliseconds = new object[1];
					DateTime? receiverStartTime = this.ReceiverStartTime;
					milliseconds[0] = TimeStampEncoding.GetMilliseconds(receiverStartTime.Value);
					str = string.Format(invariantCulture, "amqp.annotation.x-opt-enqueuedtimeutc > {0}", milliseconds);
				}
				else if (this.OffsetInclusive)
				{
					CultureInfo cultureInfo = CultureInfo.InvariantCulture;
					object[] startOffset = new object[] { this.StartOffset };
					str = string.Format(cultureInfo, "amqp.annotation.x-opt-offset >= '{0}'", startOffset);
				}
				else
				{
					CultureInfo invariantCulture1 = CultureInfo.InvariantCulture;
					object[] objArray = new object[] { this.StartOffset };
					str = string.Format(invariantCulture1, "amqp.annotation.x-opt-offset > '{0}'", objArray);
				}
				amqpDescribeds.Add(new AmqpSelectorFilter(str));
			}
			return amqpDescribeds;
		}

		private ReceivingAmqpLink EndCreateLink(IAsyncResult result)
		{
			ActiveClientLink activeClientLink = this.messagingFactory.EndOpenEntity(result);
			this.clientLinkManager.SetActiveLink(activeClientLink);
			return (ReceivingAmqpLink)activeClientLink.Link;
		}

		private void EndDisposeEvents(IAsyncResult result)
		{
			AsyncResult<AmqpMessageReceiver.DisposeAsyncResult>.End(result);
		}

		private void EndDisposeMessages(IAsyncResult result)
		{
			AsyncResult<AmqpMessageReceiver.DisposeAsyncResult>.End(result);
		}

		private Outcome GetAbandonOutcome(bool unlockOnly, IDictionary<string, object> propertiesToModify)
		{
			if (unlockOnly)
			{
				return AmqpConstants.ReleasedOutcome;
			}
			return this.GetModifiedOutcome(propertiesToModify, false);
		}

		private Outcome GetDeferOutcome(IDictionary<string, object> propertiesToModify)
		{
			return this.GetModifiedOutcome(propertiesToModify, true);
		}

		private Outcome GetModifiedOutcome(IDictionary<string, object> propertiesToModify, bool undeliverableHere)
		{
			object obj;
			Modified modified = new Modified();
			if (undeliverableHere)
			{
				modified.UndeliverableHere = new bool?(true);
			}
			if (propertiesToModify != null)
			{
				modified.MessageAnnotations = new Fields();
				foreach (KeyValuePair<string, object> keyValuePair in propertiesToModify)
				{
					if (!MessageConverter.TryGetAmqpObjectFromNetObject(keyValuePair.Value, MappingType.ApplicationProperty, out obj))
					{
						continue;
					}
					modified.MessageAnnotations.Add(keyValuePair.Key, obj);
				}
			}
			return modified;
		}

		private Rejected GetRejectedOutcome(IDictionary<string, object> propertiesToModify, string deadLetterReason, string deadLetterErrorDescription)
		{
			object obj;
			Rejected rejectedOutcome = AmqpConstants.RejectedOutcome;
			if (deadLetterReason != null || deadLetterErrorDescription != null || propertiesToModify != null)
			{
				Rejected rejected = new Rejected();
				Microsoft.ServiceBus.Messaging.Amqp.Framing.Error error = new Microsoft.ServiceBus.Messaging.Amqp.Framing.Error()
				{
					Condition = ClientConstants.DeadLetterName,
					Info = new Fields()
				};
				rejected.Error = error;
				rejectedOutcome = rejected;
				if (deadLetterReason != null)
				{
					rejectedOutcome.Error.Info.Add("DeadLetterReason", deadLetterReason);
				}
				if (deadLetterErrorDescription != null)
				{
					rejectedOutcome.Error.Info.Add("DeadLetterErrorDescription", deadLetterErrorDescription);
				}
				if (propertiesToModify != null)
				{
					foreach (KeyValuePair<string, object> keyValuePair in propertiesToModify)
					{
						if (!MessageConverter.TryGetAmqpObjectFromNetObject(keyValuePair.Value, MappingType.ApplicationProperty, out obj))
						{
							continue;
						}
						rejectedOutcome.Error.Info.Add(keyValuePair.Key, obj);
					}
				}
			}
			return rejectedOutcome;
		}

		private Microsoft.ServiceBus.Messaging.Amqp.Framing.Error GetSessionLockLostError()
		{
			if (this.sessionReceiver)
			{
				AmqpLink unsafeInnerObject = this.receiveLink.UnsafeInnerObject;
				AmqpException amqpException = null;
				if (unsafeInnerObject != null)
				{
					AmqpException terminalException = unsafeInnerObject.TerminalException as AmqpException;
					amqpException = terminalException;
					if (terminalException != null && amqpException.Error.Condition.Equals(ClientConstants.SessionLockLostError))
					{
						return amqpException.Error;
					}
				}
			}
			return null;
		}

		protected override void OnAbort()
		{
			ReceivingAmqpLink receivingAmqpLink = null;
			if (this.receiveLink.TryGetOpenedObject(out receivingAmqpLink))
			{
				this.CloseLink(receivingAmqpLink);
			}
			this.clientLinkManager.Close();
		}

		protected override IAsyncResult OnBeginAbandon(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, IDictionary<string, object> propertiesToModify, bool fromSync, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.BeginDisposeMessages(lockTokens, this.GetAbandonOutcome(false, propertiesToModify), !fromSync, timeout, callback, state);
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			ReceivingAmqpLink receivingAmqpLink = null;
			if (!this.receiveLink.TryGetOpenedObject(out receivingAmqpLink))
			{
				return new CompletedAsyncResult(callback, state);
			}
			return this.messagingFactory.BeginCloseEntity(receivingAmqpLink, timeout, callback, state);
		}

		protected override IAsyncResult OnBeginComplete(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, bool fromSync, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.BeginDisposeMessages(lockTokens, AmqpConstants.AcceptedOutcome, !fromSync, timeout, callback, state);
		}

		protected override IAsyncResult OnBeginComplete(TrackingContext trackingContext, IEnumerable<ArraySegment<byte>> deliveryTags, bool fromSync, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.BeginDisposeEvents(deliveryTags, AmqpConstants.AcceptedOutcome, !fromSync, timeout, callback, state);
		}

		protected override IAsyncResult OnBeginDeadLetter(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, IDictionary<string, object> propertiesToModify, string deadLetterReason, string deadLetterErrorDescription, bool fromSync, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.BeginDisposeMessages(lockTokens, this.GetRejectedOutcome(propertiesToModify, deadLetterReason, deadLetterErrorDescription), !fromSync, timeout, callback, state);
		}

		protected override IAsyncResult OnBeginDefer(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, IDictionary<string, object> propertiesToModify, bool fromSync, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.BeginDisposeMessages(lockTokens, this.GetDeferOutcome(propertiesToModify), !fromSync, timeout, callback, state);
		}

		internal override IAsyncResult OnBeginGetRuntimeEntityDescriptionAsyncResult(TrackingContext trackingContext, TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new NotSupportedException();
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.receiveLink.BeginGetInstance(timeout, callback, state);
		}

		protected override IAsyncResult OnBeginPeek(TrackingContext trackingContext, long fromSequenceNumber, int messageCount, TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new NotImplementedException();
		}

		protected override IAsyncResult OnBeginRenewMessageLocks(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, bool fromSync, TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new NotSupportedException();
		}

		protected override IAsyncResult OnBeginTryReceive(TrackingContext trackingContext, int messageCount, TimeSpan serverWaitTime, AsyncCallback callback, object state)
		{
			if (messageCount > 1)
			{
				throw new NotSupportedException(SRClient.FeatureNotSupported("ReceiveBatch"));
			}
			return new AmqpMessageReceiver.ReceiveMessageAsyncResult(this, false, serverWaitTime, callback, state);
		}

		protected override IAsyncResult OnBeginTryReceive(TrackingContext trackingContext, IEnumerable<long> receipts, TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new NotSupportedException();
		}

		protected override IAsyncResult OnBeginTryReceive2(TrackingContext trackingContext, int messageCount, TimeSpan serverWaitTime, AsyncCallback callback, object state)
		{
			return new AmqpMessageReceiver.ReceiveMessageAsyncResult(this, true, serverWaitTime, callback, state);
		}

		protected override IAsyncResult OnBeginTryReceiveEventData(TrackingContext trackingContext, int messageCount, TimeSpan serverWaitTime, AsyncCallback callback, object state)
		{
			Fx.AssertAndThrow(messageCount > 0, "messageCount needs to be at least 1");
			return new AmqpMessageReceiver.ReceiveEventAsyncResult(this, messageCount, false, serverWaitTime, callback, state);
		}

		protected override void OnEndAbandon(IAsyncResult result)
		{
			this.EndDisposeMessages(result);
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			if (!(result is CompletedAsyncResult))
			{
				this.messagingFactory.EndCloseEntity(result);
			}
			else
			{
				CompletedAsyncResult.End(result);
			}
			this.clientLinkManager.Close();
		}

		protected override void OnEndComplete(IAsyncResult result)
		{
			if (result is AmqpMessageReceiver.DisposeEventAsyncResult)
			{
				this.EndDisposeEvents(result);
				return;
			}
			this.EndDisposeMessages(result);
		}

		protected override void OnEndDeadLetter(IAsyncResult result)
		{
			this.EndDisposeMessages(result);
		}

		protected override void OnEndDefer(IAsyncResult result)
		{
			this.EndDisposeMessages(result);
		}

		internal override Microsoft.ServiceBus.Messaging.RuntimeEntityDescription OnEndGetRuntimeEntityDescriptionAsyncResult(IAsyncResult result)
		{
			throw new NotSupportedException();
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			ReceivingAmqpLink receivingAmqpLink = this.receiveLink.EndGetInstance(result);
			base.Mode = (receivingAmqpLink.Settings.SettleType == SettleMode.SettleOnDispose ? ReceiveMode.PeekLock : ReceiveMode.ReceiveAndDelete);
			if (this.sessionReceiver && !((Source)receivingAmqpLink.Settings.Source).FilterSet.TryGetValue<string>(ClientConstants.SessionFilterName, out this.sessionId))
			{
				receivingAmqpLink.Session.SafeClose();
				throw new MessagingException(SRAmqp.AmqpFieldSessionId);
			}
		}

		protected override IEnumerable<BrokeredMessage> OnEndPeek(IAsyncResult result)
		{
			throw new NotImplementedException();
		}

		protected override IEnumerable<DateTime> OnEndRenewMessageLocks(IAsyncResult result)
		{
			throw new NotSupportedException();
		}

		protected override bool OnEndTryReceive(IAsyncResult result, out IEnumerable<BrokeredMessage> messages)
		{
			return AmqpMessageReceiver.ReceiveMessageAsyncResult.End(result, out messages);
		}

		protected override bool OnEndTryReceive2(IAsyncResult result, out IEnumerable<BrokeredMessage> messages)
		{
			return AmqpMessageReceiver.ReceiveMessageAsyncResult.End(result, out messages);
		}

		protected override bool OnEndTryReceiveEventData(IAsyncResult result, out IEnumerable<EventData> messages)
		{
			IEnumerable<AmqpMessage> amqpMessages;
			List<EventData> eventDatas = new List<EventData>();
			if (AmqpMessageReceiver.ReceiveEventAsyncResult.End(result, out amqpMessages))
			{
				eventDatas.AddRange(
					from amqpMessage in amqpMessages
					select new EventData(amqpMessage));
			}
			messages = eventDatas;
			return eventDatas.Count > 0;
		}

		private abstract class DisposeAsyncResult : IteratorAsyncResult<AmqpMessageReceiver.DisposeAsyncResult>
		{
			private readonly AmqpMessageReceiver parent;

			private readonly Outcome outcome;

			private readonly bool batchable;

			private ReceivingAmqpLink amqpLink;

			private ArraySegment<byte> deliveryTag;

			private Outcome disposeOutcome;

			protected AmqpMessageReceiver Receiver
			{
				get
				{
					return this.parent;
				}
			}

			protected DisposeAsyncResult(AmqpMessageReceiver parent, Outcome outcome, bool batchable, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.parent = parent;
				this.outcome = outcome;
				this.batchable = batchable;
			}

			protected override IEnumerator<IteratorAsyncResult<AmqpMessageReceiver.DisposeAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				Exception messagingContract;
				this.deliveryTag = this.PopulateDeliveryTag();
				if (!this.parent.receiveLink.TryGetOpenedObject(out this.amqpLink))
				{
					Microsoft.ServiceBus.Messaging.Amqp.Framing.Error error1 = this.parent.GetSessionLockLostError();
					if (error1 == null)
					{
						AmqpMessageReceiver.DisposeAsyncResult disposeAsyncResult = this;
						IteratorAsyncResult<AmqpMessageReceiver.DisposeAsyncResult>.BeginCall beginCall = (AmqpMessageReceiver.DisposeAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.parent.receiveLink.BeginGetInstance(t, c, s);
						yield return disposeAsyncResult.CallAsync(beginCall, (AmqpMessageReceiver.DisposeAsyncResult thisPtr, IAsyncResult r) => thisPtr.amqpLink = thisPtr.parent.receiveLink.EndGetInstance(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
						if (base.LastAsyncStepException == null)
						{
							goto Label1;
						}
						base.LastAsyncStepException = this.ProcessException(ExceptionHelper.GetClientException(base.LastAsyncStepException, this.parent.messagingFactory.RemoteContainerId));
						base.Complete(base.LastAsyncStepException);
						goto Label0;
					}
					else
					{
						base.Complete(ExceptionHelper.ToMessagingContract(error1));
						goto Label0;
					}
				}
			Label1:
				AmqpMessageReceiver.DisposeAsyncResult disposeAsyncResult1 = this;
				IteratorAsyncResult<AmqpMessageReceiver.DisposeAsyncResult>.BeginCall beginCall1 = (AmqpMessageReceiver.DisposeAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.amqpLink.BeginDisposeMessage(thisPtr.deliveryTag, thisPtr.outcome, thisPtr.batchable, t, c, s);
				yield return disposeAsyncResult1.CallAsync(beginCall1, (AmqpMessageReceiver.DisposeAsyncResult thisPtr, IAsyncResult r) => thisPtr.disposeOutcome = thisPtr.amqpLink.EndDisposeMessage(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
				if (base.LastAsyncStepException == null)
				{
					Microsoft.ServiceBus.Messaging.Amqp.Framing.Error error = null;
					if (this.disposeOutcome.DescriptorCode == Rejected.Code)
					{
						Microsoft.ServiceBus.Messaging.Amqp.Framing.Error error2 = ((Rejected)this.disposeOutcome).Error;
						Microsoft.ServiceBus.Messaging.Amqp.Framing.Error error3 = error2;
						error = error2;
						if (error3 != null)
						{
							if (!error.Condition.Equals(AmqpError.NotFound.Condition))
							{
								base.LastAsyncStepException = ExceptionHelper.ToMessagingContract(error);
							}
							else if (this.IsDisposeWithoutSideEffect())
							{
								base.LastAsyncStepException = null;
							}
							else if (!this.parent.sessionReceiver)
							{
								base.LastAsyncStepException = new MessageLockLostException(SRClient.MessageLockLost);
							}
							else
							{
								base.LastAsyncStepException = new SessionLockLostException(SRClient.SessionLockExpiredOnMessageSession);
							}
							base.Complete(this.ProcessException(base.LastAsyncStepException));
						}
					}
				}
				else
				{
					if (!(base.LastAsyncStepException is OperationCanceledException) || this.amqpLink.State == AmqpObjectState.Opened)
					{
						base.LastAsyncStepException = ExceptionHelper.GetClientException(base.LastAsyncStepException, this.amqpLink.GetTrackingId());
					}
					else if (!this.parent.sessionReceiver)
					{
						base.LastAsyncStepException = new MessageLockLostException(SRClient.MessageLockLost, base.LastAsyncStepException);
					}
					else
					{
						Microsoft.ServiceBus.Messaging.Amqp.Framing.Error error4 = this.parent.GetSessionLockLostError();
						AmqpMessageReceiver.DisposeAsyncResult disposeAsyncResult2 = this;
						if (error4 != null)
						{
							messagingContract = ExceptionHelper.ToMessagingContract(error4);
						}
						else
						{
							messagingContract = new SessionLockLostException(SRClient.SessionLockExpiredOnMessageSession, base.LastAsyncStepException);
						}
						disposeAsyncResult2.LastAsyncStepException = messagingContract;
					}
					base.Complete(this.ProcessException(base.LastAsyncStepException));
				}
			Label0:
				yield break;
			}

			private bool IsDisposeWithoutSideEffect()
			{
				if (this.outcome.DescriptorCode == Released.Code)
				{
					return true;
				}
				Modified modified = this.outcome as Modified;
				if (modified == null || modified.UndeliverableHere.HasValue)
				{
					return false;
				}
				return modified.MessageAnnotations == null;
			}

			protected abstract ArraySegment<byte> PopulateDeliveryTag();

			protected virtual Exception ProcessException(Exception exception)
			{
				return exception;
			}
		}

		private sealed class DisposeEventAsyncResult : AmqpMessageReceiver.DisposeAsyncResult
		{
			private readonly IEnumerable<ArraySegment<byte>> deliveryTags;

			public DisposeEventAsyncResult(AmqpMessageReceiver parent, IEnumerable<ArraySegment<byte>> deliveryTags, Outcome outcome, bool batchable, TimeSpan timeout, AsyncCallback callback, object state) : base(parent, outcome, batchable, timeout, callback, state)
			{
				this.deliveryTags = deliveryTags;
				Fx.AssertIsNotNull(this.deliveryTags, "offsets collection should not be null");
				base.Start();
			}

			protected override ArraySegment<byte> PopulateDeliveryTag()
			{
				return this.deliveryTags.Last<ArraySegment<byte>>();
			}

			protected override Exception ProcessException(Exception exception)
			{
				Exception exception1 = base.ProcessException(exception);
				if (exception1 != null && exception1 is MessageLockLostException)
				{
					exception1 = null;
				}
				return exception1;
			}
		}

		private sealed class DisposeMessageAsyncResult : AmqpMessageReceiver.DisposeAsyncResult
		{
			private readonly IEnumerable<Guid> lockTokens;

			public DisposeMessageAsyncResult(AmqpMessageReceiver parent, IEnumerable<Guid> lockTokens, Outcome outcome, bool batchable, TimeSpan timeout, AsyncCallback callback, object state) : base(parent, outcome, batchable, timeout, callback, state)
			{
				this.lockTokens = lockTokens;
				Fx.AssertIsNotNull(this.lockTokens, "lockToken collection should not be null");
				base.Start();
			}

			private Guid CheckAndGetLockToken(IEnumerable<Guid> lockTokens)
			{
				Guid empty = Guid.Empty;
				foreach (Guid lockToken in lockTokens)
				{
					if (empty != Guid.Empty)
					{
						throw new NotSupportedException(SRClient.FeatureNotSupported("CompleteBatch"));
					}
					empty = lockToken;
				}
				return empty;
			}

			protected override ArraySegment<byte> PopulateDeliveryTag()
			{
				ArraySegment<byte> nums;
				Guid guid = this.CheckAndGetLockToken(this.lockTokens);
				if (!base.Receiver.lockedMessages.TryRemove(guid, out nums))
				{
					nums = new ArraySegment<byte>(guid.ToByteArray());
				}
				return nums;
			}
		}

		private abstract class ReceiveAsyncResult : IteratorAsyncResult<AmqpMessageReceiver.ReceiveAsyncResult>
		{
			private readonly AmqpMessageReceiver parent;

			private readonly bool shouldThrowTimeout;

			private ReceivingAmqpLink amqpLink;

			protected IEnumerable<AmqpMessage> AmqpMessages
			{
				get;
				set;
			}

			protected ReceivingAmqpLink Link
			{
				get
				{
					return this.amqpLink;
				}
			}

			protected AmqpMessageReceiver Receiver
			{
				get
				{
					return this.parent;
				}
			}

			protected ReceiveAsyncResult(AmqpMessageReceiver parent, bool shouldThrowTimeout, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.parent = parent;
				this.shouldThrowTimeout = shouldThrowTimeout;
			}

			protected abstract IAsyncResult BeginReceive(ReceivingAmqpLink link, TimeSpan timeout, AsyncCallback callback, object state);

			protected abstract IEnumerable<AmqpMessage> EndReceive(ReceivingAmqpLink link, IAsyncResult result);

			protected override IEnumerator<IteratorAsyncResult<AmqpMessageReceiver.ReceiveAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				if (!this.parent.receiveLink.TryGetOpenedObject(out this.amqpLink))
				{
					Microsoft.ServiceBus.Messaging.Amqp.Framing.Error error = this.parent.GetSessionLockLostError();
					if (error == null)
					{
						AmqpMessageReceiver.ReceiveAsyncResult receiveAsyncResult = this;
						IteratorAsyncResult<AmqpMessageReceiver.ReceiveAsyncResult>.BeginCall beginCall = (AmqpMessageReceiver.ReceiveAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.parent.receiveLink.BeginGetInstance(t, c, s);
						yield return receiveAsyncResult.CallAsync(beginCall, (AmqpMessageReceiver.ReceiveAsyncResult thisPtr, IAsyncResult r) => thisPtr.amqpLink = thisPtr.parent.receiveLink.EndGetInstance(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
						if (base.LastAsyncStepException == null)
						{
							goto Label1;
						}
						if (this.shouldThrowTimeout || !(base.LastAsyncStepException is TimeoutException))
						{
							base.Complete(ExceptionHelper.GetClientException(base.LastAsyncStepException, this.parent.messagingFactory.RemoteContainerId));
							goto Label0;
						}
						else
						{
							base.Complete(null);
							goto Label0;
						}
					}
					else
					{
						base.Complete(ExceptionHelper.ToMessagingContract(error));
						goto Label0;
					}
				}
			Label1:
				bool flag = true;
				do
				{
				Label3:
					if (!flag)
					{
						goto Label0;
					}
					AmqpMessageReceiver.ReceiveAsyncResult receiveAsyncResult1 = this;
					IteratorAsyncResult<AmqpMessageReceiver.ReceiveAsyncResult>.BeginCall beginCall1 = (AmqpMessageReceiver.ReceiveAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.BeginReceive(thisPtr.amqpLink, t, c, s);
					yield return receiveAsyncResult1.CallAsync(beginCall1, (AmqpMessageReceiver.ReceiveAsyncResult thisPtr, IAsyncResult r) => thisPtr.AmqpMessages = thisPtr.EndReceive(thisPtr.amqpLink, r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
					if (base.LastAsyncStepException == null)
					{
						try
						{
							flag = false;
							if (this.AmqpMessages != null && this.AmqpMessages.Any<AmqpMessage>())
							{
								flag = !this.ProcessMessage(this.AmqpMessages);
							}
						}
						catch (AmqpException amqpException)
						{
							base.Complete(ExceptionHelper.ToMessagingContract(amqpException.Error));
							goto Label0;
						}
						if (base.RemainingTime() <= TimeSpan.Zero)
						{
							flag = false;
						}
						else
						{
							goto Label3;
						}
					}
					else
					{
						base.Complete(ExceptionHelper.GetClientException(base.LastAsyncStepException, this.amqpLink.GetTrackingId()));
						goto Label0;
					}
				}
				while (!this.shouldThrowTimeout);
				goto Label2;
			Label0:
				yield break;
			Label2:
				base.Complete(new TimeoutException(SRCore.TimeoutOnOperation(base.OriginalTimeout)));
				goto Label0;
			}

			protected abstract bool ProcessMessage(IEnumerable<AmqpMessage> amqpMessages);
		}

		private sealed class ReceiveEventAsyncResult : AmqpMessageReceiver.ReceiveAsyncResult
		{
			private readonly int maxCount;

			public ReceiveEventAsyncResult(AmqpMessageReceiver parent, int maxCount, bool shouldThrowTimeout, TimeSpan timeout, AsyncCallback callback, object state) : base(parent, shouldThrowTimeout, timeout, callback, state)
			{
				this.maxCount = maxCount;
				base.Start();
			}

			protected override IAsyncResult BeginReceive(ReceivingAmqpLink link, TimeSpan timeout, AsyncCallback callback, object state)
			{
				Fx.AssertAndThrow(this.maxCount > 0, "we should not have maxCount = 0");
				return link.BeginReceiveMessages(this.maxCount, timeout, callback, state);
			}

			public static bool End(IAsyncResult result, out IEnumerable<AmqpMessage> messages)
			{
				AmqpMessageReceiver.ReceiveEventAsyncResult receiveEventAsyncResult = AsyncResult.End<AmqpMessageReceiver.ReceiveEventAsyncResult>(result);
				messages = receiveEventAsyncResult.AmqpMessages;
				return receiveEventAsyncResult.AmqpMessages != null;
			}

			protected override IEnumerable<AmqpMessage> EndReceive(ReceivingAmqpLink link, IAsyncResult result)
			{
				IEnumerable<AmqpMessage> amqpMessages;
				link.EndReceiveMessages(result, out amqpMessages);
				return amqpMessages;
			}

			protected override bool ProcessMessage(IEnumerable<AmqpMessage> messages)
			{
				if (base.Receiver.Mode != ReceiveMode.PeekLock)
				{
					foreach (AmqpMessage message in messages)
					{
						base.Link.DisposeDelivery(message, true, AmqpConstants.AcceptedOutcome);
					}
				}
				return true;
			}
		}

		private sealed class ReceiveMessageAsyncResult : AmqpMessageReceiver.ReceiveAsyncResult
		{
			private IEnumerable<BrokeredMessage> messages;

			public ReceiveMessageAsyncResult(AmqpMessageReceiver parent, bool shouldThrowTimeout, TimeSpan timeout, AsyncCallback callback, object state) : base(parent, shouldThrowTimeout, timeout, callback, state)
			{
				base.Start();
			}

			protected override IAsyncResult BeginReceive(ReceivingAmqpLink link, TimeSpan timeout, AsyncCallback callback, object state)
			{
				return link.BeginReceiveRemoteMessage(timeout, callback, state);
			}

			public static bool End(IAsyncResult result, out IEnumerable<BrokeredMessage> messages)
			{
				messages = AsyncResult.End<AmqpMessageReceiver.ReceiveMessageAsyncResult>(result).messages;
				return messages != null;
			}

			protected override IEnumerable<AmqpMessage> EndReceive(ReceivingAmqpLink link, IAsyncResult result)
			{
				IEnumerable<AmqpMessage> amqpMessages;
				link.EndReceiveMessages(result, out amqpMessages);
				return amqpMessages;
			}

			protected override bool ProcessMessage(IEnumerable<AmqpMessage> messages)
			{
				bool flag;
				List<BrokeredMessage> brokeredMessages = new List<BrokeredMessage>();
				using (IEnumerator<AmqpMessage> enumerator = messages.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						AmqpMessage current = enumerator.Current;
						BrokeredMessage empty = MessageConverter.ClientGetMessage(current);
						if (base.Receiver.Mode == ReceiveMode.PeekLock && empty.IsLockTokenSet && empty.LockedUntilUtc <= DateTime.UtcNow)
						{
							base.Link.ReleaseMessage(current);
							if (base.Receiver.PrefetchCount == 0)
							{
								empty.Dispose();
								flag = false;
								return flag;
							}
						}
						if (base.Receiver.Mode == ReceiveMode.ReceiveAndDelete)
						{
							empty.LockToken = Guid.Empty;
							base.Link.DisposeDelivery(current, true, AmqpConstants.AcceptedOutcome);
						}
						else if (base.Receiver.Mode == ReceiveMode.PeekLock && !empty.IsLockTokenSet)
						{
							empty.LockToken = Guid.NewGuid();
							base.Receiver.lockedMessages.TryAdd(empty.LockToken, current.DeliveryTag);
						}
						brokeredMessages.Add(empty);
					}
					this.messages = brokeredMessages.ToArray();
					return true;
				}
				return flag;
			}
		}
	}
}