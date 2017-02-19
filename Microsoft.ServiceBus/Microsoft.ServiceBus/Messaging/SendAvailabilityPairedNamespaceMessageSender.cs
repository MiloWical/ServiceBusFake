using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging
{
	internal class SendAvailabilityPairedNamespaceMessageSender : MessageSender
	{
		private MessageSender backlog;

		private object backlogLock = new object();

		private MessageSender Backlog
		{
			get
			{
				if (this.backlog == null)
				{
					lock (this.backlogLock)
					{
						if (this.backlog == null)
						{
							string str = this.Options.FetchBacklogQueueName(ConcurrentRandom.Next(0, this.Options.BacklogQueueCount));
							this.backlog = this.Options.SecondaryMessagingFactory.CreateMessageSender(str);
							this.backlog.ShouldLinkRetryPolicy = true;
							this.backlog.RetryPolicy = this.Options.SecondaryMessagingFactory.RetryPolicy;
						}
					}
				}
				return this.backlog;
			}
			set
			{
				this.backlog = value;
			}
		}

		private SendAvailabilityPairedNamespaceOptions Options
		{
			get
			{
				return (SendAvailabilityPairedNamespaceOptions)base.MessagingFactory.Options;
			}
		}

		public override string Path
		{
			get
			{
				return this.Primary.Path;
			}
		}

		private MessageSender Primary
		{
			get;
			set;
		}

		public SendAvailabilityPairedNamespaceMessageSender(MessageSender primary) : base(primary.MessagingFactory, Microsoft.ServiceBus.RetryPolicy.NoRetry)
		{
			if (primary == null)
			{
				throw Fx.Exception.AsError(new ArgumentNullException("primary"), null);
			}
			this.Primary = primary;
			base.ShouldLinkRetryPolicy = false;
		}

		private bool CanUsePrimary()
		{
			return this.Options.IsPathAvailable(this.Primary.Path);
		}

		internal static BrokeredMessage ConvertMessageForPrimary(BrokeredMessage message, string path)
		{
			object obj;
			BrokeredMessage brokeredMessage = message.Clone();
			if (message.Properties.TryGetValue("x-ms-scheduledenqueuetimeutc", out obj))
			{
				if (!(obj is DateTime))
				{
					SendAvailabilityPairedNamespaceMessageSender.DeadLetterMessageWithInvalidProperty("x-ms-scheduledenqueuetimeutc", obj, typeof(TimeSpan), message);
					return null;
				}
				brokeredMessage.ScheduledEnqueueTimeUtc = (DateTime)obj;
				brokeredMessage.Properties.Remove("x-ms-scheduledenqueuetimeutc");
			}
			if (message.Properties.TryGetValue("x-ms-sessionid", out obj))
			{
				string str = obj as string;
				if (str == null)
				{
					SendAvailabilityPairedNamespaceMessageSender.DeadLetterMessageWithInvalidProperty("x-ms-sessionid", obj, typeof(string), message);
					return null;
				}
				brokeredMessage.SessionId = str;
				brokeredMessage.Properties.Remove("x-ms-sessionid");
			}
			if (message.Properties.TryGetValue("x-ms-timetolive", out obj))
			{
				if (!(obj is TimeSpan))
				{
					SendAvailabilityPairedNamespaceMessageSender.DeadLetterMessageWithInvalidProperty("x-ms-timetolive", obj, typeof(TimeSpan), message);
					return null;
				}
				brokeredMessage.TimeToLive = (TimeSpan)obj;
				brokeredMessage.Properties.Remove("x-ms-timetolive");
			}
			if (message.Properties.TryGetValue("x-ms-path", out obj))
			{
				brokeredMessage.Properties.Remove("x-ms-path");
			}
			return brokeredMessage;
		}

		private static void ConvertMessagesForBacklog(IEnumerable<BrokeredMessage> messages, string path)
		{
			foreach (BrokeredMessage message in messages)
			{
				TimeSpan timeToLive = message.TimeToLive;
				if (message.SessionId != null)
				{
					message.Properties.Add("x-ms-sessionid", message.SessionId);
					message.SessionId = null;
				}
				if (!TimeSpan.Equals(timeToLive, TimeSpan.MaxValue))
				{
					message.Properties.Add("x-ms-timetolive", timeToLive);
					message.TimeToLive = TimeSpan.MaxValue;
				}
				if (!DateTime.Equals(DateTime.MinValue, message.ScheduledEnqueueTimeUtc))
				{
					message.Properties.Add("x-ms-scheduledenqueuetimeutc", message.ScheduledEnqueueTimeUtc);
					message.ScheduledEnqueueTimeUtc = DateTime.MinValue;
				}
				message.Properties.Add("x-ms-path", path);
			}
		}

		private static void DeadLetterMessageWithInvalidProperty(string propertyName, object temp, Type type, BrokeredMessage message)
		{
			string str = SRClient.PairedNamespacePropertyExtractionDlqDescription(propertyName, message.MessageId, type, (temp == null ? SRClient.NullAsString : temp.GetType().FullName), (temp == null ? SRClient.NullAsString : temp.ToString()));
			message.BeginDeadLetter(SRClient.PairedNamespacePropertyExtractionDlqReason, str, new AsyncCallback(SendAvailabilityPairedNamespaceMessageSender.EndDeadLetterMessageWithInvalidProperty), message);
		}

		private static void EndDeadLetterMessageWithInvalidProperty(IAsyncResult result)
		{
			BrokeredMessage asyncState = (BrokeredMessage)result.AsyncState;
			try
			{
				asyncState.EndDeadLetter(result);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWritePairedNamespaceDeadletterException(asyncState.ReceiveContext.MessageReceiver.MessagingFactory.Address.ToString(), asyncState.ReceiveContext.MessageReceiver.Path, exception));
			}
		}

		private void FindNewMessageSender(string path)
		{
			if (!string.Equals(this.Backlog.Path, path, StringComparison.OrdinalIgnoreCase) || this.Options.BacklogQueueCount <= 1)
			{
				return;
			}
			lock (this.backlogLock)
			{
				if (string.Equals(this.Backlog.Path, path, StringComparison.OrdinalIgnoreCase))
				{
					int num = ConcurrentRandom.Next(0, this.Options.BacklogQueueCount);
					string str = this.Options.FetchBacklogQueueName(num);
					if (string.Equals(path, str, StringComparison.OrdinalIgnoreCase))
					{
						num++;
						if (num >= this.Options.BacklogQueueCount)
						{
							num = 0;
						}
						str = this.Options.FetchBacklogQueueName(num);
					}
					this.Backlog = this.Options.SecondaryMessagingFactory.CreateMessageSender(str);
					this.Backlog.ShouldLinkRetryPolicy = true;
					this.Backlog.RetryPolicy = this.Options.SecondaryMessagingFactory.RetryPolicy;
				}
			}
		}

		protected override void OnAbort()
		{
			this.Primary.Abort();
			if (this.backlog != null)
			{
				this.Backlog.Abort();
			}
		}

		protected override IAsyncResult OnBeginCancelScheduledMessage(TrackingContext trackingContext, IEnumerable<long> sequenceNumbers, TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new NotImplementedException();
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			MessageSender[] primary = new MessageSender[] { this.Primary, this.backlog };
			return new SendAvailabilityPairedNamespaceMessageSender.CloseAsyncResult(primary, timeout, callback, state);
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new SendAvailabilityPairedNamespaceMessageSender.OpenAsyncResult(this.Primary, timeout, callback, state);
		}

		protected override IAsyncResult OnBeginScheduleMessage(TrackingContext trackingContext, IEnumerable<BrokeredMessage> message, TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new NotImplementedException();
		}

		protected override IAsyncResult OnBeginSend(TrackingContext trackingContext, IEnumerable<BrokeredMessage> messages, bool fromSync, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new SendAvailabilityPairedNamespaceMessageSender.SendAsyncResult(this, trackingContext, messages, timeout, callback, state);
		}

		protected override IAsyncResult OnBeginSendEventData(TrackingContext trackingContext, IEnumerable<EventData> eventDatas, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new SendAvailabilityPairedNamespaceMessageSender.SendAsyncResult(this, trackingContext, 
				from data in eventDatas
				select data.ToBrokeredMessage(), timeout, callback, state);
		}

		protected override void OnEndCancelScheduledMessage(IAsyncResult result)
		{
			throw new NotImplementedException();
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			AsyncResult<SendAvailabilityPairedNamespaceMessageSender.CloseAsyncResult>.End(result);
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			AsyncResult<SendAvailabilityPairedNamespaceMessageSender.OpenAsyncResult>.End(result);
		}

		protected override IEnumerable<long> OnEndScheduleMessage(IAsyncResult result)
		{
			throw new NotImplementedException();
		}

		protected override void OnEndSend(IAsyncResult result)
		{
			AsyncResult<SendAvailabilityPairedNamespaceMessageSender.SendAsyncResult>.End(result);
		}

		protected override void OnEndSendEventData(IAsyncResult result)
		{
			this.OnEndSend(result);
		}

		private class CloseAsyncResult : IteratorAsyncResult<SendAvailabilityPairedNamespaceMessageSender.CloseAsyncResult>
		{
			private readonly ICollection<MessageSender> senders;

			public CloseAsyncResult(ICollection<MessageSender> senders, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				List<MessageSender> messageSenders = new List<MessageSender>();
				foreach (MessageSender sender in senders)
				{
					if (sender == null || !sender.IsOpened)
					{
						continue;
					}
					messageSenders.Add(sender);
				}
				this.senders = messageSenders;
				base.Start();
			}

			protected override IEnumerator<IteratorAsyncResult<SendAvailabilityPairedNamespaceMessageSender.CloseAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				SendAvailabilityPairedNamespaceMessageSender.CloseAsyncResult closeAsyncResult = this;
				ICollection<MessageSender> messageSenders = this.senders;
				IteratorAsyncResult<SendAvailabilityPairedNamespaceMessageSender.CloseAsyncResult>.BeginCall<MessageSender> beginCall = (SendAvailabilityPairedNamespaceMessageSender.CloseAsyncResult thisPtr, MessageSender i, TimeSpan t, AsyncCallback c, object s) => i.BeginClose(t, c, s);
				yield return closeAsyncResult.CallParallelAsync<MessageSender>(messageSenders, beginCall, (SendAvailabilityPairedNamespaceMessageSender.CloseAsyncResult thisPtr, MessageSender i, IAsyncResult r) => i.EndClose(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
			}
		}

		private class OpenAsyncResult : IteratorAsyncResult<SendAvailabilityPairedNamespaceMessageSender.OpenAsyncResult>
		{
			private readonly MessageSender sender;

			public OpenAsyncResult(MessageSender sender, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.sender = sender;
				base.Start();
			}

			protected override IEnumerator<IteratorAsyncResult<SendAvailabilityPairedNamespaceMessageSender.OpenAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				SendAvailabilityPairedNamespaceMessageSender.OpenAsyncResult openAsyncResult = this;
				IteratorAsyncResult<SendAvailabilityPairedNamespaceMessageSender.OpenAsyncResult>.BeginCall beginCall = (SendAvailabilityPairedNamespaceMessageSender.OpenAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.sender.BeginOpen(t, c, s);
				yield return openAsyncResult.CallAsync(beginCall, (SendAvailabilityPairedNamespaceMessageSender.OpenAsyncResult thisPtr, IAsyncResult r) => thisPtr.sender.EndOpen(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
			}
		}

		private class SendAsyncResult : IteratorAsyncResult<SendAvailabilityPairedNamespaceMessageSender.SendAsyncResult>
		{
			private readonly SendAvailabilityPairedNamespaceMessageSender sender;

			private readonly TrackingContext trackingContext;

			private readonly IEnumerable<BrokeredMessage> messages;

			private readonly TimeSpan SafeBacklogAttempt;

			private List<BrokeredMessage> messageBuffer;

			public SendAsyncResult(SendAvailabilityPairedNamespaceMessageSender sender, TrackingContext trackingContext, IEnumerable<BrokeredMessage> messages, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.sender = sender;
				this.trackingContext = trackingContext;
				this.messages = messages;
				this.messageBuffer = new List<BrokeredMessage>();
				try
				{
					foreach (BrokeredMessage message in this.messages)
					{
						message.IsConsumed = false;
						this.messageBuffer.Add(message.Clone());
					}
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRetryPolicyStreamNotClonable(this.trackingContext.Activity, this.trackingContext.TrackingId, "PairnedNamespaceSender", "Send", exception.GetType().FullName, exception.Message));
					foreach (BrokeredMessage brokeredMessage in this.messageBuffer)
					{
						brokeredMessage.Dispose();
					}
					this.messageBuffer.Clear();
				}
				base.Start();
			}

			protected override IEnumerator<IteratorAsyncResult<SendAvailabilityPairedNamespaceMessageSender.SendAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				Exception exception = this.sender.Options.RetrieveNontransientException(this.sender.Primary.Path);
				if (exception != null)
				{
					throw Fx.Exception.AsError(new SendAvailabilityMessagingException(exception), null);
				}
				if (this.sender.CanUsePrimary())
				{
					SendAvailabilityPairedNamespaceMessageSender.SendAsyncResult sendAsyncResult = this;
					IteratorAsyncResult<SendAvailabilityPairedNamespaceMessageSender.SendAsyncResult>.BeginCall beginCall = (SendAvailabilityPairedNamespaceMessageSender.SendAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.sender.Primary.BeginSend(thisPtr.trackingContext, thisPtr.messages, t, c, s);
					yield return sendAsyncResult.CallAsync(beginCall, (SendAvailabilityPairedNamespaceMessageSender.SendAsyncResult thisPtr, IAsyncResult r) => thisPtr.sender.Primary.EndSend(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
					if (base.LastAsyncStepException != null)
					{
						if (!this.sender.Options.ExceptionInspector.CausesFailover(base.LastAsyncStepException))
						{
							goto Label1;
						}
						this.sender.Options.NotifyPrimarySendResult(this.sender.Primary.Path, false);
					}
					else
					{
						this.sender.Options.MarkPathHealthy(this.sender.Path);
						goto Label0;
					}
				}
				if (!this.sender.CanUsePrimary())
				{
					if (this.messageBuffer.Count > 0)
					{
						SendAvailabilityPairedNamespaceMessageSender.ConvertMessagesForBacklog(this.messageBuffer, this.sender.Primary.Path);
						while (base.RemainingTime() > this.SafeBacklogAttempt || base.RemainingTime() > TimeSpan.FromTicks(base.OriginalTimeout.Ticks / (long)2))
						{
							List<BrokeredMessage> list = null;
							List<BrokeredMessage> brokeredMessages = this.messageBuffer;
							list = (
								from msg in brokeredMessages
								select msg.Clone()).ToList<BrokeredMessage>();
							try
							{
								SendAvailabilityPairedNamespaceMessageSender.SendAsyncResult sendAsyncResult1 = this;
								IteratorAsyncResult<SendAvailabilityPairedNamespaceMessageSender.SendAsyncResult>.BeginCall beginCall1 = (SendAvailabilityPairedNamespaceMessageSender.SendAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.sender.Backlog.BeginSend(list, t, c, s);
								yield return sendAsyncResult1.CallAsync(beginCall1, (SendAvailabilityPairedNamespaceMessageSender.SendAsyncResult thisPtr, IAsyncResult r) => thisPtr.sender.Backlog.EndSend(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
							}
							finally
							{
								if (list != null)
								{
									list.ForEach((BrokeredMessage msg) => msg.Dispose());
									list.Clear();
								}
							}
							if (base.LastAsyncStepException == null)
							{
								break;
							}
							MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWritePairedNamespaceSendToBacklogFailed(this.sender.MessagingFactory.Address.ToString(), this.sender.Backlog.Path, base.LastAsyncStepException));
							if (!(base.RemainingTime() > TimeSpan.Zero) || this.sender.Options.BacklogQueueCount <= 1)
							{
								break;
							}
							this.sender.FindNewMessageSender(this.sender.Backlog.Path);
						}
					}
					List<BrokeredMessage> brokeredMessages1 = this.messageBuffer;
					brokeredMessages1.ForEach((BrokeredMessage msg) => msg.Dispose());
					this.messageBuffer.Clear();
					base.Complete(base.LastAsyncStepException);
				}
				else
				{
					base.Complete(base.LastAsyncStepException);
				}
			Label0:
				yield break;
			Label1:
				base.Complete(base.LastAsyncStepException);
				goto Label0;
			}
		}
	}
}