using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Transactions;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal sealed class AmqpMessageSender : MessageSender
	{
		private readonly AmqpMessagingFactory messagingFactory;

		private readonly string entityName;

		private FaultTolerantObject<SendingAmqpLink> sendLink;

		private int deliveryCount;

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

		public AmqpMessageSender(AmqpMessagingFactory factory, string entityName, MessagingEntityType? entityType, Microsoft.ServiceBus.RetryPolicy retryPolicy) : base(factory, retryPolicy)
		{
			this.sendLink = new FaultTolerantObject<SendingAmqpLink>(this, new Action<SendingAmqpLink>(this.CloseLink), new Func<TimeSpan, AsyncCallback, object, IAsyncResult>(this.BeginCreateLink), new Func<IAsyncResult, SendingAmqpLink>(this.EndCreateLink));
			this.messagingFactory = factory;
			this.entityName = entityName;
			this.EntityType = entityType;
			this.clientLinkManager = new ActiveClientLinkManager(this.messagingFactory);
			this.batchFlushInterval = this.messagingFactory.TransportSettings.BatchFlushInterval;
		}

		private IAsyncResult BeginCreateLink(TimeSpan timeout, AsyncCallback callback, object state)
		{
			string str = (!string.IsNullOrWhiteSpace(base.PartitionId) ? EntityNameHelper.FormatPartitionSenderPath(this.entityName, base.PartitionId) : this.entityName);
			return this.messagingFactory.BeginOpenEntity(this, str, this.EntityType, timeout, callback, state);
		}

		private void CloseLink(SendingAmqpLink link)
		{
			link.Session.SafeClose();
		}

		private SendingAmqpLink EndCreateLink(IAsyncResult result)
		{
			ActiveClientLink activeClientLink = this.messagingFactory.EndOpenEntity(result);
			this.clientLinkManager.SetActiveLink(activeClientLink);
			return (SendingAmqpLink)activeClientLink.Link;
		}

		private ArraySegment<byte> GetDeliveryTag()
		{
			int num = Interlocked.Increment(ref this.deliveryCount);
			return new ArraySegment<byte>(BitConverter.GetBytes(num));
		}

		protected override void OnAbort()
		{
			SendingAmqpLink sendingAmqpLink = null;
			if (this.sendLink.TryGetOpenedObject(out sendingAmqpLink))
			{
				this.CloseLink(sendingAmqpLink);
			}
			this.clientLinkManager.Close();
		}

		protected override IAsyncResult OnBeginCancelScheduledMessage(TrackingContext trackingContext, IEnumerable<long> sequenceNumbers, TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new NotImplementedException();
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			SendingAmqpLink sendingAmqpLink = null;
			if (!this.sendLink.TryGetOpenedObject(out sendingAmqpLink))
			{
				return new CompletedAsyncResult(callback, state);
			}
			return this.messagingFactory.BeginCloseEntity(sendingAmqpLink, timeout, callback, state);
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.sendLink.BeginGetInstance(timeout, callback, state);
		}

		protected override IAsyncResult OnBeginScheduleMessage(TrackingContext trackingContext, IEnumerable<BrokeredMessage> messages, TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new NotImplementedException();
		}

		protected override IAsyncResult OnBeginSend(TrackingContext trackingContext, IEnumerable<BrokeredMessage> messages, bool fromSync, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.OnBeginSend(trackingContext, messages, timeout, !fromSync, callback, state);
		}

		private IAsyncResult OnBeginSend(TrackingContext trackingContext, IEnumerable<BrokeredMessage> messages, TimeSpan timeout, bool batchable, AsyncCallback callback, object state)
		{
			if (System.Transactions.Transaction.Current != null)
			{
				throw new NotSupportedException(SRClient.FeatureNotSupported("Transaction"));
			}
			return new AmqpMessageSender.SendBrokeredMessageAsyncResult(this, messages, batchable, timeout, callback, state);
		}

		protected override IAsyncResult OnBeginSendEventData(TrackingContext trackingContext, IEnumerable<EventData> eventDatas, TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (System.Transactions.Transaction.Current != null)
			{
				throw new NotSupportedException(SRClient.FeatureNotSupported("Transaction"));
			}
			return new AmqpMessageSender.SendEventDataAsyncResult(this, eventDatas, true, timeout, callback, state);
		}

		protected override void OnEndCancelScheduledMessage(IAsyncResult result)
		{
			throw new NotImplementedException();
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

		protected override void OnEndOpen(IAsyncResult result)
		{
			this.sendLink.EndGetInstance(result);
		}

		protected override IEnumerable<long> OnEndScheduleMessage(IAsyncResult result)
		{
			throw new NotImplementedException();
		}

		protected override void OnEndSend(IAsyncResult result)
		{
			AsyncResult<AmqpMessageSender.SendAsyncResult>.End(result);
		}

		protected override void OnEndSendEventData(IAsyncResult result)
		{
			AsyncResult<AmqpMessageSender.SendAsyncResult>.End(result);
		}

		private abstract class SendAsyncResult : IteratorAsyncResult<AmqpMessageSender.SendAsyncResult>
		{
			private readonly AmqpMessageSender parent;

			private SendingAmqpLink amqpLink;

			private AmqpMessage amqpMessage;

			private Outcome outcome;

			protected bool Batchable
			{
				get;
				private set;
			}

			protected SendAsyncResult(AmqpMessageSender parent, bool batchable, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.parent = parent;
				this.Batchable = batchable;
			}

			protected abstract AmqpMessage CreateAmqpMessage();

			protected override IEnumerator<IteratorAsyncResult<AmqpMessageSender.SendAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				try
				{
					this.amqpMessage = this.CreateAmqpMessage();
				}
				catch (Exception exception)
				{
					base.Complete(exception);
					goto Label0;
				}
				if (!this.parent.sendLink.TryGetOpenedObject(out this.amqpLink))
				{
					AmqpMessageSender.SendAsyncResult sendAsyncResult = this;
					IteratorAsyncResult<AmqpMessageSender.SendAsyncResult>.BeginCall beginCall = (AmqpMessageSender.SendAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.parent.sendLink.BeginGetInstance(t, c, s);
					yield return sendAsyncResult.CallAsync(beginCall, (AmqpMessageSender.SendAsyncResult thisPtr, IAsyncResult r) => thisPtr.amqpLink = thisPtr.parent.sendLink.EndGetInstance(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
					if (base.LastAsyncStepException == null)
					{
						goto Label1;
					}
					base.Complete(ExceptionHelper.GetClientException(base.LastAsyncStepException, this.parent.messagingFactory.RemoteContainerId));
					goto Label0;
				}
			Label1:
				if (this.amqpLink.Settings.MaxMessageSize.HasValue)
				{
					ulong serializedMessageSize = (ulong)this.amqpMessage.SerializedMessageSize;
					if (serializedMessageSize <= Convert.ToUInt64(this.amqpLink.Settings.MaxMessageSize))
					{
						goto Label2;
					}
					AmqpMessageSender.SendAsyncResult sendAsyncResult1 = this;
					object value = this.amqpMessage.DeliveryId.Value;
					object obj = serializedMessageSize;
					ulong? maxMessageSize = this.amqpLink.Settings.MaxMessageSize;
					sendAsyncResult1.Complete(new MessageSizeExceededException(SRAmqp.AmqpMessageSizeExceeded(value, obj, maxMessageSize.Value)));
					goto Label0;
				}
			Label2:
				AmqpMessageSender.SendAsyncResult sendAsyncResult2 = this;
				IteratorAsyncResult<AmqpMessageSender.SendAsyncResult>.BeginCall beginCall1 = (AmqpMessageSender.SendAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.amqpLink.BeginSendMessage(thisPtr.amqpMessage, thisPtr.parent.GetDeliveryTag(), AmqpConstants.NullBinary, t, c, s);
				yield return sendAsyncResult2.CallAsync(beginCall1, (AmqpMessageSender.SendAsyncResult thisPtr, IAsyncResult r) => thisPtr.outcome = thisPtr.amqpLink.EndSendMessage(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
				if (base.LastAsyncStepException != null)
				{
					base.Complete(ExceptionHelper.GetClientException(base.LastAsyncStepException, this.amqpLink.GetTrackingId()));
				}
				else if (this.outcome.DescriptorCode == Rejected.Code)
				{
					Rejected rejected = (Rejected)this.outcome;
					base.Complete(ExceptionHelper.ToMessagingContract(rejected.Error));
				}
			Label0:
				yield break;
			}
		}

		private sealed class SendBrokeredMessageAsyncResult : AmqpMessageSender.SendAsyncResult
		{
			private readonly IEnumerable<BrokeredMessage> messages;

			public SendBrokeredMessageAsyncResult(AmqpMessageSender parent, IEnumerable<BrokeredMessage> messages, bool batchable, TimeSpan timeout, AsyncCallback callback, object state) : base(parent, batchable, timeout, callback, state)
			{
				this.messages = messages;
				base.Start();
			}

			protected override AmqpMessage CreateAmqpMessage()
			{
				AmqpMessage batchable = null;
				if (this.messages != null)
				{
					foreach (BrokeredMessage message in this.messages)
					{
						if (batchable != null)
						{
							throw new NotSupportedException(SRClient.FeatureNotSupported("SendBatch"));
						}
						batchable = MessageConverter.ClientGetMessage(message);
						batchable.Batchable = base.Batchable;
					}
				}
				return batchable;
			}
		}

		private sealed class SendEventDataAsyncResult : AmqpMessageSender.SendAsyncResult
		{
			private const SectionFlag ClientAmqpPropsSetOnSendToEventHub = SectionFlag.DeliveryAnnotations | SectionFlag.MessageAnnotations | SectionFlag.Properties | SectionFlag.ApplicationProperties;

			private readonly IEnumerable<EventData> eventDatas;

			public SendEventDataAsyncResult(AmqpMessageSender parent, IEnumerable<EventData> eventDatas, bool batchable, TimeSpan timeout, AsyncCallback callback, object state) : base(parent, batchable, timeout, callback, state)
			{
				this.eventDatas = eventDatas;
				base.Start();
			}

			protected override AmqpMessage CreateAmqpMessage()
			{
				AmqpMessage nullable = null;
				int num = this.eventDatas.Count<EventData>();
				if (this.eventDatas != null && num > 1)
				{
					IList<Data> datas = new List<Data>();
					EventData eventDatum = null;
					foreach (EventData eventData in this.eventDatas)
					{
						if (eventDatum != null)
						{
							if (eventDatum.PartitionKey != eventData.PartitionKey)
							{
								throw Fx.Exception.AsError(new InvalidOperationException(SRClient.EventHubSendBatchMismatchPartitionKey(eventDatum.PartitionKey ?? "(null)", eventData.PartitionKey ?? "(null)")), null);
							}
							if (eventDatum.Publisher != eventData.Publisher)
							{
								throw Fx.Exception.AsError(new InvalidOperationException(SRClient.EventHubSendBatchMismatchPublisher(eventDatum.Publisher ?? "(null)", eventData.Publisher ?? "(null)")), null);
							}
						}
						else
						{
							eventDatum = eventData;
						}
						AmqpMessage amqpMessage = eventData.ToAmqpMessage();
						amqpMessage.Batchable = base.Batchable;
						if ((int)(amqpMessage.Sections & (SectionFlag.DeliveryAnnotations | SectionFlag.MessageAnnotations | SectionFlag.Properties | SectionFlag.ApplicationProperties)) == 0 && (eventData.BodyStream == null || eventData.BodyStream == Stream.Null))
						{
							throw new InvalidOperationException(SRClient.CannotSendAnEmptyEvent(eventData.GetType().Name));
						}
						ArraySegment<byte> nums = MessageConverter.ReadStream(amqpMessage.ToStream());
						datas.Add(new Data()
						{
							Value = nums
						});
					}
					nullable = AmqpMessage.Create(datas);
					nullable.Batchable = true;
					nullable.MessageFormat = new uint?(-2147404032);
					MessageConverter.UpdateAmqpMessageHeadersAndProperties(nullable, eventDatum, false);
				}
				else if (this.eventDatas != null && num == 1)
				{
					EventData eventDatum1 = this.eventDatas.First<EventData>();
					nullable = eventDatum1.ToAmqpMessage();
					nullable.Batchable = base.Batchable;
					if ((int)(nullable.Sections & (SectionFlag.DeliveryAnnotations | SectionFlag.MessageAnnotations | SectionFlag.Properties | SectionFlag.ApplicationProperties)) == 0 && (eventDatum1.BodyStream == null || eventDatum1.BodyStream == Stream.Null))
					{
						throw new InvalidOperationException(SRClient.CannotSendAnEmptyEvent(eventDatum1.GetType().Name));
					}
				}
				return nullable;
			}
		}
	}
}