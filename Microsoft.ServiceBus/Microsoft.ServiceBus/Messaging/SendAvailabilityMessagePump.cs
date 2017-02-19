using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.ServiceBus.Messaging
{
	internal class SendAvailabilityMessagePump
	{
		private readonly static TimeSpan DefaultCloseTimeout;

		internal static TimeSpan SyphonReadTimeout;

		private static int LatestSyphonId;

		private readonly SendAvailabilityPairedNamespaceOptions options;

		private readonly IOThreadTimer[] timers;

		private readonly IAsyncResult[] queuesInProcess;

		private readonly int SyphonId;

		private long closed;

		private IOThreadTimer closeTimer;

		private bool Closed
		{
			get
			{
				return Interlocked.Read(ref this.closed) != (long)0;
			}
			set
			{
				object obj;
				if (value)
				{
					obj = 1;
				}
				else
				{
					obj = null;
				}
				long num = (long)obj;
				Interlocked.Exchange(ref this.closed, num);
			}
		}

		private TrackingContext InstanceTrackingContext
		{
			get;
			set;
		}

		static SendAvailabilityMessagePump()
		{
			SendAvailabilityMessagePump.DefaultCloseTimeout = TimeSpan.FromSeconds(5);
			SendAvailabilityMessagePump.SyphonReadTimeout = TimeSpan.FromMinutes(15);
			SendAvailabilityMessagePump.LatestSyphonId = 0;
		}

		public SendAvailabilityMessagePump(SendAvailabilityPairedNamespaceOptions options)
		{
			if (options == null)
			{
				throw Fx.Exception.AsError(new ArgumentNullException("options"), null);
			}
			if (options.PrimaryMessagingFactory == null)
			{
				throw Fx.Exception.AsError(new ArgumentNullException("options.PrimaryMessagingFactory"), null);
			}
			if (options.SecondaryMessagingFactory == null)
			{
				throw Fx.Exception.AsError(new ArgumentNullException("options.MessagingFactory"), null);
			}
			this.options = options;
			this.InstanceTrackingContext = TrackingContext.GetInstance(Guid.NewGuid(), this.options.SbNamespace);
			this.SyphonId = SendAvailabilityMessagePump.NextSyphonId();
			this.timers = new IOThreadTimer[this.options.BacklogQueueCount];
			this.queuesInProcess = new IAsyncResult[this.options.BacklogQueueCount];
		}

		public IAsyncResult BeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new SendAvailabilityMessagePump.CloseAsyncResult(this, timeout, callback, state);
		}

		private void BeginProcessQueue(object state)
		{
			int num = (int)state;
			this.queuesInProcess[num] = new SendAvailabilityMessagePump.ProcessQueueAsyncResult(this, num, SendAvailabilityMessagePump.SyphonReadTimeout, new AsyncCallback(this.EndProcessQueue), (object)num);
		}

		private bool CanPump()
		{
			MessagingFactory primaryMessagingFactory = this.options.PrimaryMessagingFactory;
			MessagingFactory secondaryMessagingFactory = this.options.SecondaryMessagingFactory;
			bool flag = (primaryMessagingFactory.IsClosedOrClosing ? false : !primaryMessagingFactory.IsFaulted);
			bool flag1 = (secondaryMessagingFactory.IsClosedOrClosing ? false : !secondaryMessagingFactory.IsFaulted);
			if (flag)
			{
				return flag1;
			}
			return false;
		}

		public void Close()
		{
			this.EndClose(this.BeginClose(SendAvailabilityMessagePump.DefaultCloseTimeout, null, null));
		}

		public void EndClose(IAsyncResult result)
		{
			AsyncResult<SendAvailabilityMessagePump.CloseAsyncResult>.End(result);
		}

		private void EndProcessQueue(IAsyncResult result)
		{
			TimeSpan zero = TimeSpan.Zero;
			try
			{
				try
				{
					AsyncResult<SendAvailabilityMessagePump.ProcessQueueAsyncResult>.End(result);
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWritePairedNamespaceMessagePumpProcessQueueFailed(exception));
				}
			}
			finally
			{
				if (!this.CanPump())
				{
					this.HandleUnplannedShutdown();
				}
				else
				{
					int asyncState = (int)result.AsyncState;
					if ((int)this.timers.Length > asyncState && !this.Closed)
					{
						IOThreadTimer oThreadTimer = this.timers[asyncState];
						oThreadTimer.Set((this.options.SyphonRetryInterval > zero ? this.options.SyphonRetryInterval : zero));
						this.queuesInProcess[asyncState] = null;
					}
				}
			}
		}

		private static void FaultingClose(object state)
		{
			Tuple<MessagingFactory, MessagingFactory, SendAvailabilityMessagePump> tuple = (Tuple<MessagingFactory, MessagingFactory, SendAvailabilityMessagePump>)state;
			MessagingFactory item1 = tuple.Item1;
			MessagingFactory item2 = tuple.Item2;
			SendAvailabilityMessagePump item3 = tuple.Item3;
			if (item1.IsOpened)
			{
				Exception pairedMessagingFactoryException = new PairedMessagingFactoryException(SRClient.FaultingPairedMessagingFactory(item1.Address.ToString(), item2.Address.ToString()));
				item1.Fault(pairedMessagingFactoryException);
			}
			if (!item3.Closed)
			{
				item3.Close();
			}
		}

		private void HandleUnplannedShutdown()
		{
			MessagingFactory primaryMessagingFactory = this.options.PrimaryMessagingFactory;
			MessagingFactory secondaryMessagingFactory = this.options.SecondaryMessagingFactory;
			this.HandleUnplannedShutdown(primaryMessagingFactory, secondaryMessagingFactory);
			this.HandleUnplannedShutdown(secondaryMessagingFactory, primaryMessagingFactory);
		}

		private void HandleUnplannedShutdown(MessagingFactory factory1, MessagingFactory factory2)
		{
			if (!this.Closed && (factory1.IsFaulted || factory1.IsClosedOrClosing) && (factory2.IsClosedOrClosing || factory2.IsFaulted))
			{
				this.Close();
				return;
			}
			if (factory1.IsFaulted && factory2.IsOpened)
			{
				Exception pairedMessagingFactoryException = new PairedMessagingFactoryException(SRClient.FaultingPairedMessagingFactory(factory2.Address.ToString(), factory1.Address.ToString()));
				factory2.Fault(pairedMessagingFactoryException);
				return;
			}
			if (factory1.IsClosedOrClosing && factory2.IsOpened)
			{
				IOThreadTimer oThreadTimer = new IOThreadTimer(new Action<object>(SendAvailabilityMessagePump.FaultingClose), new Tuple<MessagingFactory, MessagingFactory, SendAvailabilityMessagePump>(factory2, factory1, this), false);
				if (Interlocked.CompareExchange<IOThreadTimer>(ref this.closeTimer, oThreadTimer, null) == null)
				{
					oThreadTimer.Set(SendAvailabilityMessagePump.DefaultCloseTimeout);
				}
			}
		}

		private static int NextSyphonId()
		{
			return Interlocked.Increment(ref SendAvailabilityMessagePump.LatestSyphonId);
		}

		public void Start()
		{
			MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWritePairedNamespaceStartSyphon(this.InstanceTrackingContext.Activity, this.SyphonId, this.options.PrimaryMessagingFactory.Address.ToString(), this.options.SecondaryMessagingFactory.Address.ToString(), this.options.BacklogQueueCount));
			for (int i = 0; i < this.options.BacklogQueueCount; i++)
			{
				IOThreadTimer oThreadTimer = new IOThreadTimer(new Action<object>(this.BeginProcessQueue), (object)i, false);
				oThreadTimer.Set(this.options.SyphonRetryInterval);
				this.timers[i] = oThreadTimer;
			}
		}

		private class CloseAsyncResult : IteratorAsyncResult<SendAvailabilityMessagePump.CloseAsyncResult>
		{
			private SendAvailabilityMessagePump pump;

			public CloseAsyncResult(SendAvailabilityMessagePump pump, TimeSpan timespan, AsyncCallback callback, object state) : base(timespan, callback, state)
			{
				this.pump = pump;
				this.pump.Closed = true;
				SendAvailabilityMessagePump.CloseAsyncResult closeAsyncResult = this;
				closeAsyncResult.OnCompleting = (Action<AsyncResult, Exception>)Delegate.Combine(closeAsyncResult.OnCompleting, new Action<AsyncResult, Exception>(SendAvailabilityMessagePump.CloseAsyncResult.OnFinally));
				base.Start();
			}

			protected override IEnumerator<IteratorAsyncResult<SendAvailabilityMessagePump.CloseAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				IOThreadTimer[] oThreadTimerArray = this.pump.timers;
				for (int i = 0; i < (int)oThreadTimerArray.Length; i++)
				{
					oThreadTimerArray[i].Cancel();
				}
				IAsyncResult[] asyncResultArray = this.pump.queuesInProcess;
				IEnumerable<IAsyncResult> asyncResults = 
					from result in (IEnumerable<IAsyncResult>)asyncResultArray
					where result != null
					select result;
				WaitHandle[] array = (
					from result in asyncResults
					select result.AsyncWaitHandle).ToArray<WaitHandle>();
				if ((int)array.Length != 0)
				{
					WaitHandle.WaitAll(array, base.RemainingTime());
				}
				yield break;
			}

			private static void OnFinally(AsyncResult asyncResult, Exception exception)
			{
				SendAvailabilityMessagePump.CloseAsyncResult closeAsyncResult = (SendAvailabilityMessagePump.CloseAsyncResult)asyncResult;
				MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWritePairedNamespaceStopSyphon(closeAsyncResult.pump.InstanceTrackingContext.Activity, closeAsyncResult.pump.SyphonId, closeAsyncResult.pump.options.PrimaryMessagingFactory.Address.ToString(), closeAsyncResult.pump.options.SecondaryMessagingFactory.Address.ToString()));
			}
		}

		private class ProcessQueueAsyncResult : IteratorAsyncResult<SendAvailabilityMessagePump.ProcessQueueAsyncResult>
		{
			private readonly static TimeSpan MinTimeToProcessLoop;

			private readonly static TimeSpan DefaultReadTimeout;

			private readonly SendAvailabilityMessagePump pump;

			private readonly MessageReceiver messageReceiver;

			private List<BrokeredMessage> messagesToDispose;

			static ProcessQueueAsyncResult()
			{
				SendAvailabilityMessagePump.ProcessQueueAsyncResult.MinTimeToProcessLoop = TimeSpan.FromMinutes(4);
				SendAvailabilityMessagePump.ProcessQueueAsyncResult.DefaultReadTimeout = TimeSpan.FromMinutes(2);
			}

			public ProcessQueueAsyncResult(SendAvailabilityMessagePump pump, int index, TimeSpan timespan, AsyncCallback callback, object state) : base(timespan, callback, state)
			{
				this.pump = pump;
				SendAvailabilityMessagePump.ProcessQueueAsyncResult processQueueAsyncResult = this;
				processQueueAsyncResult.OnCompleting = (Action<AsyncResult, Exception>)Delegate.Combine(processQueueAsyncResult.OnCompleting, new Action<AsyncResult, Exception>(this.OnFinally));
				if (!this.pump.CanPump())
				{
					base.Complete(true);
					return;
				}
				string str = this.pump.options.FetchBacklogQueueName(index);
				this.messageReceiver = this.pump.options.SecondaryMessagingFactory.CreateMessageReceiver(str);
				this.messageReceiver.PrefetchCount = 100;
				this.messageReceiver.RetryPolicy = this.pump.options.SecondaryMessagingFactory.RetryPolicy;
				this.messageReceiver.ShouldLinkRetryPolicy = true;
				base.Start();
			}

			private static void EndCloseMessageReceiver(IAsyncResult result)
			{
				MessageReceiver asyncState = (MessageReceiver)result.AsyncState;
				try
				{
					asyncState.EndClose(result);
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWritePairedNamespaceMessagePumpProcessQueueFailed(exception));
				}
			}

			protected override IEnumerator<IteratorAsyncResult<SendAvailabilityMessagePump.ProcessQueueAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				object obj;
				List<BrokeredMessage> brokeredMessages;
				MessageSender retryPolicy;
				bool flag = false;
				HashSet<string> strs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				do
				{
					if (base.RemainingTime() < SendAvailabilityMessagePump.ProcessQueueAsyncResult.MinTimeToProcessLoop)
					{
						break;
					}
					base.LastAsyncStepException = null;
					IEnumerable<BrokeredMessage> brokeredMessages1 = null;
					bool flag1 = false;
					if (!this.pump.CanPump())
					{
						break;
					}
					SendAvailabilityMessagePump.ProcessQueueAsyncResult processQueueAsyncResult = this;
					yield return processQueueAsyncResult.CallAsync((SendAvailabilityMessagePump.ProcessQueueAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.messageReceiver.BeginTryReceive(100, SendAvailabilityMessagePump.ProcessQueueAsyncResult.DefaultReadTimeout, c, s), (SendAvailabilityMessagePump.ProcessQueueAsyncResult thisPtr, IAsyncResult r) => flag1 = this.messageReceiver.EndTryReceive(r, out brokeredMessages1), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
					if (base.LastAsyncStepException != null)
					{
						MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWritePairedNamespaceMessagePumpReceiveFailed(this.messageReceiver.MessagingFactory.Address.ToString(), this.messageReceiver.Path, base.LastAsyncStepException));
						if (!Fx.IsFatal(base.LastAsyncStepException) && !(base.LastAsyncStepException is UnauthorizedAccessException) && !(base.LastAsyncStepException is MessagingEntityNotFoundException))
						{
							break;
						}
						base.Complete(base.LastAsyncStepException);
						break;
					}
					else if (!flag1 || brokeredMessages1 == null)
					{
						flag = true;
					}
					else
					{
						this.messagesToDispose.AddRange(brokeredMessages1);
						Dictionary<string, List<BrokeredMessage>> strs1 = new Dictionary<string, List<BrokeredMessage>>();
						foreach (BrokeredMessage brokeredMessage in brokeredMessages1)
						{
							MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWritePairedNamespaceReceiveMessageFromSecondary(brokeredMessage.SequenceNumber, this.messageReceiver.Path));
							base.LastAsyncStepException = null;
							if (!brokeredMessage.Properties.TryGetValue("x-ms-path", out obj) || !(obj is string))
							{
								BrokeredMessage brokeredMessage1 = brokeredMessage;
								string messageId = brokeredMessage.MessageId;
								MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWritePairedNamespaceMessageNoPathInBacklog(this.messageReceiver.MessagingFactory.Address.ToString(), this.messageReceiver.Path, messageId));
								yield return base.CallAsync((SendAvailabilityMessagePump.ProcessQueueAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => brokeredMessage1.BeginDeadLetter(SRClient.BacklogDeadletterReasonNoQueuePath, SRClient.BacklogDeadletterDescriptionNoQueuePath, c, s), (SendAvailabilityMessagePump.ProcessQueueAsyncResult thisPtr, IAsyncResult r) => brokeredMessage1.EndDeadLetter(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
							}
							else
							{
								string str = (string)obj;
								if (!strs.Contains(str))
								{
									if (!strs1.TryGetValue(str, out brokeredMessages))
									{
										brokeredMessages = new List<BrokeredMessage>();
										strs1.Add(str, brokeredMessages);
									}
									brokeredMessages.Add(brokeredMessage);
								}
							}
						}
						Dictionary<string, List<BrokeredMessage>>.KeyCollection.Enumerator enumerator = strs1.Keys.GetEnumerator();
						try
						{
							while (enumerator.MoveNext())
							{
								string current = enumerator.Current;
								base.LastAsyncStepException = null;
								string str1 = current;
								List<BrokeredMessage> item = strs1[current];
								retryPolicy = null;
								yield return base.CallAsync((SendAvailabilityMessagePump.ProcessQueueAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.pump.options.PrimaryMessagingFactory.BeginCreateMessageSender(null, str1, false, t, c, s), (SendAvailabilityMessagePump.ProcessQueueAsyncResult thisPtr, IAsyncResult r) => retryPolicy = thisPtr.pump.options.PrimaryMessagingFactory.EndCreateMessageSender(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
								if (base.LastAsyncStepException == null)
								{
									retryPolicy.ShouldLinkRetryPolicy = true;
									retryPolicy.RetryPolicy = this.pump.options.PrimaryMessagingFactory.RetryPolicy;
									if (!this.pump.CanPump())
									{
										goto Label1;
									}
									SendAvailabilityMessagePump.ProcessQueueAsyncResult processQueueAsyncResult1 = this;
									List<BrokeredMessage> brokeredMessages2 = item;
									IteratorAsyncResult<SendAvailabilityMessagePump.ProcessQueueAsyncResult>.BeginCall<BrokeredMessage> sendAsyncResult = (SendAvailabilityMessagePump.ProcessQueueAsyncResult thisPtr, BrokeredMessage i, TimeSpan t, AsyncCallback c, object s) => new SendAvailabilityMessagePump.SendAsyncResult(retryPolicy, i, this.pump, this.pump.options.PrimaryMessagingFactory.OperationTimeout, c, s);
									yield return processQueueAsyncResult1.CallParallelAsync<BrokeredMessage>(brokeredMessages2, sendAsyncResult, (SendAvailabilityMessagePump.ProcessQueueAsyncResult thisPtr, BrokeredMessage i, IAsyncResult r) => AsyncResult<SendAvailabilityMessagePump.SendAsyncResult>.End(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
									if (base.LastAsyncStepException != null && !strs.Contains(current))
									{
										strs.Add(current);
									}
									if (!retryPolicy.IsOpened)
									{
										continue;
									}
									SendAvailabilityMessagePump.ProcessQueueAsyncResult processQueueAsyncResult2 = this;
									IteratorAsyncResult<SendAvailabilityMessagePump.ProcessQueueAsyncResult>.BeginCall beginCall = (SendAvailabilityMessagePump.ProcessQueueAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => retryPolicy.BeginClose(thisPtr.RemainingTime(), c, s);
									yield return processQueueAsyncResult2.CallAsync(beginCall, (SendAvailabilityMessagePump.ProcessQueueAsyncResult thisPtr, IAsyncResult r) => retryPolicy.EndClose(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
									if (base.LastAsyncStepException == null)
									{
										continue;
									}
									MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWritePairedNamespaceMessagePumpProcessCloseSenderFailed(base.LastAsyncStepException));
								}
								else
								{
									MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWritePairedNamespaceCouldNotCreateMessageSender(this.messageReceiver.MessagingFactory.Address.ToString(), this.messageReceiver.Path, base.LastAsyncStepException));
								}
							}
							goto Label0;
						Label1:
							retryPolicy.Abort();
							break;
						}
						finally
						{
							((IDisposable)enumerator).Dispose();
						}
					}
				Label0:
				}
				while (!flag);
			}

			private void OnFinally(AsyncResult asyncResult, Exception exception)
			{
				if (exception != null)
				{
					MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWritePairedNamespaceMessagePumpProcessQueueFailed(exception));
				}
				try
				{
					if (this.messageReceiver != null && this.messageReceiver.IsOpened)
					{
						this.messageReceiver.BeginClose(base.RemainingTime(), new AsyncCallback(SendAvailabilityMessagePump.ProcessQueueAsyncResult.EndCloseMessageReceiver), this.messageReceiver);
					}
					foreach (BrokeredMessage brokeredMessage in this.messagesToDispose)
					{
						brokeredMessage.Dispose();
					}
				}
				catch (Exception exception2)
				{
					Exception exception1 = exception2;
					MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWritePairedNamespaceMessagePumpProcessQueueFailed(exception1));
					if (this.messageReceiver != null)
					{
						this.messageReceiver.Abort();
					}
				}
			}
		}

		private class SendAsyncResult : IteratorAsyncResult<SendAvailabilityMessagePump.SendAsyncResult>
		{
			private readonly MessageSender messageSender;

			private readonly BrokeredMessage message;

			private readonly SendAvailabilityMessagePump pump;

			private readonly long sequenceNumber;

			public SendAsyncResult(MessageSender sender, BrokeredMessage message, SendAvailabilityMessagePump pump, TimeSpan timespan, AsyncCallback callback, object state) : base(timespan, callback, state)
			{
				this.messageSender = sender;
				this.message = message;
				this.pump = pump;
				this.sequenceNumber = message.SequenceNumber;
				SendAvailabilityMessagePump.SendAsyncResult sendAsyncResult = this;
				sendAsyncResult.OnCompleting = (Action<AsyncResult, Exception>)Delegate.Combine(sendAsyncResult.OnCompleting, new Action<AsyncResult, Exception>(this.OnFinally));
				base.Start();
			}

			protected override IEnumerator<IteratorAsyncResult<SendAvailabilityMessagePump.SendAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				using (BrokeredMessage brokeredMessage = SendAvailabilityPairedNamespaceMessageSender.ConvertMessageForPrimary(this.message, this.messageSender.Path))
				{
					if (brokeredMessage == null || !this.pump.CanPump())
					{
					}
					else
					{
						MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWritePairedNamespaceSendingMessage(this.sequenceNumber, this.messageSender.Path));
						SendAvailabilityMessagePump.SendAsyncResult sendAsyncResult = this;
						IteratorAsyncResult<SendAvailabilityMessagePump.SendAsyncResult>.BeginCall beginCall = (SendAvailabilityMessagePump.SendAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.messageSender.BeginSend(brokeredMessage, t, c, s);
						yield return sendAsyncResult.CallAsync(beginCall, (SendAvailabilityMessagePump.SendAsyncResult thisPtr, IAsyncResult r) => thisPtr.messageSender.EndSend(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
						if (base.LastAsyncStepException != null)
						{
							MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWritePairedNamespaceDestinationSendException(this.pump.options.PrimaryMessagingFactory.Address.ToString(), this.messageSender.Path, base.LastAsyncStepException));
							MessagingException lastAsyncStepException = base.LastAsyncStepException as MessagingException;
							if (lastAsyncStepException != null)
							{
								if (!lastAsyncStepException.IsTransient && !(lastAsyncStepException is QuotaExceededException))
								{
									MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWritePairedNamespaceDeadletterException(this.messageSender.MessagingFactory.Address.ToString(), this.messageSender.Path, lastAsyncStepException));
									this.message.DeadLetter(SRClient.BacklogDeadletterReasonNotRetryable, SRClient.BacklogDeadletterDescriptionNotRetryable(this.messageSender.Path, lastAsyncStepException.GetType().Name, lastAsyncStepException.Message));
								}
								base.Complete(base.LastAsyncStepException);
							}
							else
							{
								base.Complete(base.LastAsyncStepException);
							}
						}
						else
						{
							SendAvailabilityMessagePump.SendAsyncResult sendAsyncResult1 = this;
							IteratorAsyncResult<SendAvailabilityMessagePump.SendAsyncResult>.BeginCall beginCall1 = (SendAvailabilityMessagePump.SendAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.message.BeginComplete(t, c, s);
							yield return sendAsyncResult1.CallAsync(beginCall1, (SendAvailabilityMessagePump.SendAsyncResult thisPtr, IAsyncResult r) => thisPtr.message.EndComplete(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
							if (base.LastAsyncStepException == null)
							{
								goto Label0;
							}
							MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWritePairedNamespaceDestinationSendException(this.pump.options.PrimaryMessagingFactory.Address.ToString(), this.messageSender.Path, base.LastAsyncStepException));
							base.Complete(base.LastAsyncStepException);
						}
					}
				}
			Label0:
				yield break;
			}

			private void OnFinally(AsyncResult result, Exception exception)
			{
				if (exception == null)
				{
					MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWritePairedNamespaceSendMessageSuccess(this.sequenceNumber, this.messageSender.Path));
					return;
				}
				MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWritePairedNamespaceSendMessageFailure(this.sequenceNumber, this.messageSender.Path, exception));
			}
		}
	}
}