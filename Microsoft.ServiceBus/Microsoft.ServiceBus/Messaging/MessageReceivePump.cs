using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus.Messaging
{
	internal sealed class MessageReceivePump
	{
		private const int MaxInitialReceiveRetryCount = 3;

		private readonly static AsyncCallback StaticOnRenewLoopLoopCompleted;

		private readonly static TimeSpan ServerBusyExceptionBackoffAmount;

		private readonly static TimeSpan OtherExceptionBackoffAmount;

		private readonly static TimeSpan MessageCompletionOperationTimeout;

		private readonly MessageReceiver receiver;

		private readonly Action<BrokeredMessage> syncCallback;

		private readonly Func<BrokeredMessage, Task> taskCallback;

		private readonly Func<BrokeredMessage, AsyncCallback, object, IAsyncResult> beginCallback;

		private readonly Action<IAsyncResult> endCallback;

		private readonly AsyncSemaphore semaphore;

		private readonly List<Guid> completionList;

		private bool renewSupported;

		private volatile bool completionInProgress;

		public Microsoft.ServiceBus.Messaging.OnMessageOptions OnMessageOptions
		{
			get;
			private set;
		}

		static MessageReceivePump()
		{
			MessageReceivePump.StaticOnRenewLoopLoopCompleted = new AsyncCallback(MessageReceivePump.OnRenewLockLoopCompleted);
			MessageReceivePump.ServerBusyExceptionBackoffAmount = TimeSpan.FromSeconds(10);
			MessageReceivePump.OtherExceptionBackoffAmount = TimeSpan.FromSeconds(1);
			MessageReceivePump.MessageCompletionOperationTimeout = Constants.DefaultOperationTimeout;
		}

		public MessageReceivePump(MessageReceiver receiver, Microsoft.ServiceBus.Messaging.OnMessageOptions onMessageOptions, Action<BrokeredMessage> syncCallback) : this(receiver, onMessageOptions)
		{
			this.syncCallback = syncCallback;
			this.beginCallback = new Func<BrokeredMessage, AsyncCallback, object, IAsyncResult>(this.BeginSyncCallback);
			this.endCallback = new Action<IAsyncResult>(this.EndSyncCallback);
		}

		public MessageReceivePump(MessageReceiver receiver, Microsoft.ServiceBus.Messaging.OnMessageOptions onMessageOptions, Func<BrokeredMessage, Task> taskCallback) : this(receiver, onMessageOptions)
		{
			this.taskCallback = taskCallback;
			this.beginCallback = new Func<BrokeredMessage, AsyncCallback, object, IAsyncResult>(this.BeginTaskCallback);
			this.endCallback = new Action<IAsyncResult>(this.EndTaskCallback);
		}

		private MessageReceivePump(MessageReceiver receiver, Microsoft.ServiceBus.Messaging.OnMessageOptions onMessageOptions)
		{
			if (receiver == null)
			{
				throw new ArgumentNullException("receiver");
			}
			this.receiver = receiver;
			this.OnMessageOptions = onMessageOptions;
			this.semaphore = new AsyncSemaphore(onMessageOptions.MaxConcurrentCalls);
			this.renewSupported = true;
			this.completionList = new List<Guid>();
		}

		private IAsyncResult BeginRenewLockLoop(TrackingContext trackingContext, BrokeredMessage message, CancellationToken cancellationToken, AsyncCallback callback, object state)
		{
			MessageReceivePump.RenewLockLoopAsyncResult renewLockLoopAsyncResult = new MessageReceivePump.RenewLockLoopAsyncResult(this, trackingContext, message, cancellationToken, callback, state);
			return renewLockLoopAsyncResult.Start();
		}

		private IAsyncResult BeginSyncCallback(BrokeredMessage message, AsyncCallback callback, object state)
		{
			return new MessageReceivePump.SyncCallbackAsyncResult(this, message, callback, state);
		}

		private IAsyncResult BeginTaskCallback(BrokeredMessage message, AsyncCallback callback, object state)
		{
			return new TaskAsyncResult(this.taskCallback(message), callback, state);
		}

		private void EndRenewLockLoop(IAsyncResult result)
		{
			AsyncResult<MessageReceivePump.RenewLockLoopAsyncResult>.End(result);
		}

		private void EndSyncCallback(IAsyncResult asyncResult)
		{
			AsyncResult<MessageReceivePump.SyncCallbackAsyncResult>.End(asyncResult);
		}

		private void EndTaskCallback(IAsyncResult asyncResult)
		{
			AsyncResult<TaskAsyncResult>.End(asyncResult);
		}

		private static void OnCompleteMessageCompletion(IAsyncResult result)
		{
			MessageReceivePump.BatchCompleteAsyncResult batchCompleteAsyncResult = (MessageReceivePump.BatchCompleteAsyncResult)result;
			MessageReceivePump pump = batchCompleteAsyncResult.Pump;
			TrackingContext trackingContext = batchCompleteAsyncResult.TrackingContext;
			lock (pump.completionList)
			{
				try
				{
					try
					{
						AsyncResult<MessageReceivePump.BatchCompleteAsyncResult>.End(batchCompleteAsyncResult);
					}
					catch (Exception exception1)
					{
						Exception exception = exception1;
						MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageReceivePumpFailedToComplete(trackingContext.Activity, trackingContext.TrackingId, trackingContext.SystemTracker, exception.ToStringSlim()));
						pump.RaiseExceptionReceivedEvent(exception, "Complete");
					}
				}
				finally
				{
					pump.completionInProgress = false;
				}
			}
			pump.ScheduleMessageCompletion(null, trackingContext);
		}

		private static void OnRenewLockLoopCompleted(IAsyncResult result)
		{
			MessageReceivePump asyncState = (MessageReceivePump)result.AsyncState;
			try
			{
				asyncState.EndRenewLockLoop(result);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), asyncState.receiver.Path);
				MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageReceivePumpUnexpectedException(instance.Activity, instance.TrackingId, instance.SystemTracker, exception.ToString()));
				throw;
			}
		}

		private void RaiseExceptionReceivedEvent(Exception exception, string action)
		{
			try
			{
				this.OnMessageOptions.RaiseExceptionReceived(new ExceptionReceivedEventArgs(exception, action));
			}
			catch (Exception exception1)
			{
				Environment.FailFast(exception1.ToString());
			}
		}

		private void ScheduleMessageCompletion(BrokeredMessage message, TrackingContext trackingContext)
		{
			MessageReceivePump.BatchCompleteAsyncResult batchCompleteAsyncResult = null;
			lock (this.completionList)
			{
				if (message != null)
				{
					this.completionList.Add(message.LockToken);
				}
				if (!this.completionInProgress && this.completionList.Count > 0)
				{
					this.completionInProgress = true;
					batchCompleteAsyncResult = new MessageReceivePump.BatchCompleteAsyncResult(this, trackingContext, MessageReceivePump.MessageCompletionOperationTimeout, new AsyncCallback(MessageReceivePump.OnCompleteMessageCompletion), null);
				}
			}
			if (batchCompleteAsyncResult != null)
			{
				IOThreadScheduler.ScheduleCallbackNoFlow((object o) => ((MessageReceivePump.BatchCompleteAsyncResult)o).Start(), batchCompleteAsyncResult);
			}
		}

		private void ScheduleRenewLockLoop(TrackingContext trackingContext, BrokeredMessage message, CancellationToken cancellationToken)
		{
			try
			{
				this.BeginRenewLockLoop(trackingContext, message, cancellationToken, MessageReceivePump.StaticOnRenewLoopLoopCompleted, this);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageReceivePumpUnexpectedException(trackingContext.Activity, trackingContext.TrackingId, trackingContext.SystemTracker, exception.ToString()));
				throw;
			}
		}

		private static bool ShouldBackoff(Exception exception, out TimeSpan amount)
		{
			if (exception is ServerBusyException || exception is MessagingEntityNotFoundException)
			{
				amount = MessageReceivePump.ServerBusyExceptionBackoffAmount;
				return true;
			}
			MessagingException messagingException = exception as MessagingException;
			if (messagingException != null && messagingException.IsTransient)
			{
				amount = TimeSpan.Zero;
				return false;
			}
			amount = MessageReceivePump.OtherExceptionBackoffAmount;
			return true;
		}

		public void Start()
		{
			(new MessageReceivePump.StartAsyncResult(this, null, null)).RunSynchronously();
		}

		private sealed class BatchCompleteAsyncResult : IteratorAsyncResult<MessageReceivePump.BatchCompleteAsyncResult>
		{
			private readonly static TimeSpan completionBufferWaitTime;

			private readonly MessageReceivePump owner;

			private readonly List<Guid> lockTokens;

			private readonly TrackingContext trackingContext;

			public MessageReceivePump Pump
			{
				get
				{
					return this.owner;
				}
			}

			public TrackingContext TrackingContext
			{
				get
				{
					return this.trackingContext;
				}
			}

			static BatchCompleteAsyncResult()
			{
				MessageReceivePump.BatchCompleteAsyncResult.completionBufferWaitTime = TimeSpan.FromMilliseconds(500);
			}

			public BatchCompleteAsyncResult(MessageReceivePump owner, TrackingContext trackingContext, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.owner = owner;
				this.lockTokens = new List<Guid>();
				this.trackingContext = trackingContext;
			}

			protected override IEnumerator<IteratorAsyncResult<MessageReceivePump.BatchCompleteAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				List<Guid> guids = null;
				yield return base.CallAsyncSleep(MessageReceivePump.BatchCompleteAsyncResult.completionBufferWaitTime);
				bool flag = false;
				try
				{
					List<Guid> guids1 = this.owner.completionList;
					List<Guid> guids2 = guids1;
					guids = guids1;
					Monitor.Enter(guids2, ref flag);
					this.lockTokens.AddRange(this.owner.completionList);
					this.owner.completionList.Clear();
				}
				finally
				{
					if (flag)
					{
						Monitor.Exit(guids);
					}
				}
				if (this.lockTokens.Count != 0)
				{
					MessageReceivePump.BatchCompleteAsyncResult batchCompleteAsyncResult = this;
					List<Guid> guids3 = this.lockTokens;
					IteratorAsyncResult<MessageReceivePump.BatchCompleteAsyncResult>.BeginCall<Guid> beginCall = (MessageReceivePump.BatchCompleteAsyncResult thisPtr, Guid i, TimeSpan t, AsyncCallback c, object s) => thisPtr.owner.receiver.BeginComplete(i, c, s);
					yield return batchCompleteAsyncResult.CallParallelAsync<Guid>(guids3, beginCall, (MessageReceivePump.BatchCompleteAsyncResult thisPtr, Guid i, IAsyncResult r) => thisPtr.owner.receiver.EndComplete(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
					if (base.LastAsyncStepException != null)
					{
						MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageReceivePumpFailedToComplete(this.trackingContext.Activity, this.trackingContext.TrackingId, this.trackingContext.SystemTracker, base.LastAsyncStepException.ToString()));
						this.owner.RaiseExceptionReceivedEvent(base.LastAsyncStepException, "Complete");
					}
				}
			}
		}

		private sealed class DispatchAsyncResult : IteratorAsyncResult<MessageReceivePump.DispatchAsyncResult>
		{
			private readonly static Action<object> StaticOnProcessingTimeout;

			private readonly static Action<AsyncResult, Exception> CompletingAction;

			private readonly MessageReceivePump owner;

			private readonly BrokeredMessage message;

			private readonly TrackingContext trackingContext;

			private readonly CancellationTokenSource renewCancellationTokenSource;

			private readonly IOThreadTimer processingTimer;

			static DispatchAsyncResult()
			{
				MessageReceivePump.DispatchAsyncResult.StaticOnProcessingTimeout = new Action<object>(MessageReceivePump.DispatchAsyncResult.OnProcessingTimeout);
				MessageReceivePump.DispatchAsyncResult.CompletingAction = new Action<AsyncResult, Exception>(MessageReceivePump.DispatchAsyncResult.Finally);
			}

			public DispatchAsyncResult(MessageReceivePump owner, TrackingContext trackingContext, BrokeredMessage message, AsyncCallback callback, object state) : base(TimeSpan.MaxValue, callback, state)
			{
				this.owner = owner;
				this.message = message;
				this.trackingContext = trackingContext;
				this.processingTimer = new IOThreadTimer(MessageReceivePump.DispatchAsyncResult.StaticOnProcessingTimeout, this, true);
				this.renewCancellationTokenSource = new CancellationTokenSource();
				MessageReceivePump.DispatchAsyncResult dispatchAsyncResult = this;
				dispatchAsyncResult.OnCompleting = (Action<AsyncResult, Exception>)Delegate.Combine(dispatchAsyncResult.OnCompleting, MessageReceivePump.DispatchAsyncResult.CompletingAction);
			}

			private static void Finally(AsyncResult asyncResult, Exception exception)
			{
				((MessageReceivePump.DispatchAsyncResult)asyncResult).message.Dispose();
			}

			protected override IEnumerator<IteratorAsyncResult<MessageReceivePump.DispatchAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				TimeSpan timeSpan;
				if (this.ShouldRenewLock())
				{
					this.owner.ScheduleRenewLockLoop(this.trackingContext, this.message, this.renewCancellationTokenSource.Token);
					this.processingTimer.Set(this.owner.OnMessageOptions.AutoRenewTimeout);
				}
				MessageReceivePump.DispatchAsyncResult dispatchAsyncResult = this;
				IteratorAsyncResult<MessageReceivePump.DispatchAsyncResult>.BeginCall beginCall = (MessageReceivePump.DispatchAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.owner.beginCallback(thisPtr.message, c, s);
				yield return dispatchAsyncResult.CallAsync(beginCall, (MessageReceivePump.DispatchAsyncResult thisPtr, IAsyncResult r) => thisPtr.owner.endCallback(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
				if (this.ShouldRenewLock())
				{
					this.processingTimer.Cancel();
					this.renewCancellationTokenSource.Cancel();
					this.renewCancellationTokenSource.Dispose();
				}
				if (base.LastAsyncStepException != null)
				{
					MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageReceivePumpUserCallbackException(this.trackingContext.Activity, this.trackingContext.TrackingId, this.trackingContext.SystemTracker, base.LastAsyncStepException.ToString()));
					this.owner.RaiseExceptionReceivedEvent(base.LastAsyncStepException, "UserCallback");
				}
				if (this.owner.receiver.Mode == ReceiveMode.PeekLock)
				{
					if (base.LastAsyncStepException != null)
					{
						MessageReceivePump.DispatchAsyncResult dispatchAsyncResult1 = this;
						IteratorAsyncResult<MessageReceivePump.DispatchAsyncResult>.BeginCall beginCall1 = (MessageReceivePump.DispatchAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.owner.receiver.BeginAbandon(thisPtr.message.LockToken, c, s);
						yield return dispatchAsyncResult1.CallAsync(beginCall1, (MessageReceivePump.DispatchAsyncResult thisPtr, IAsyncResult r) => thisPtr.owner.receiver.EndAbandon(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
						if (base.LastAsyncStepException != null)
						{
							MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageReceivePumpFailedToAbandon(this.trackingContext.Activity, this.trackingContext.TrackingId, this.trackingContext.SystemTracker, base.LastAsyncStepException.ToString()));
							this.owner.RaiseExceptionReceivedEvent(base.LastAsyncStepException, "Abandon");
						}
					}
					else if (this.owner.OnMessageOptions.AutoComplete)
					{
						this.owner.ScheduleMessageCompletion(this.message, this.trackingContext);
					}
					bool lastAsyncStepException = base.LastAsyncStepException != null;
					if (lastAsyncStepException && MessageReceivePump.ShouldBackoff(base.LastAsyncStepException, out timeSpan))
					{
						MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageReceivePumpBackoff(this.trackingContext.Activity, this.trackingContext.TrackingId, this.trackingContext.SystemTracker, (int)MessageReceivePump.ServerBusyExceptionBackoffAmount.TotalMilliseconds, base.LastAsyncStepException.ToString()));
						yield return base.CallAsyncSleep(timeSpan);
					}
				}
			}

			private static void OnProcessingTimeout(object state)
			{
				MessageReceivePump.DispatchAsyncResult dispatchAsyncResult = (MessageReceivePump.DispatchAsyncResult)state;
				MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageReceivePumpUserCallTimedOut(dispatchAsyncResult.trackingContext.Activity, dispatchAsyncResult.trackingContext.TrackingId, dispatchAsyncResult.trackingContext.SystemTracker, dispatchAsyncResult.message.MessageId));
				try
				{
					dispatchAsyncResult.renewCancellationTokenSource.Cancel();
				}
				catch (ObjectDisposedException objectDisposedException)
				{
				}
			}

			private bool ShouldRenewLock()
			{
				if (this.owner.receiver.Mode != ReceiveMode.PeekLock || !this.owner.renewSupported)
				{
					return false;
				}
				return this.owner.OnMessageOptions.AutoRenewLock;
			}
		}

		private sealed class PumpAsyncResult : IteratorAsyncResult<MessageReceivePump.PumpAsyncResult>
		{
			private readonly static Action<AsyncResult, Exception> CompletingAction;

			private readonly static AsyncCallback onDispatchCompleted;

			private readonly TrackingContext trackingContext;

			private readonly MessageReceivePump owner;

			private BrokeredMessage firstMessage;

			private BrokeredMessage message;

			static PumpAsyncResult()
			{
				MessageReceivePump.PumpAsyncResult.CompletingAction = new Action<AsyncResult, Exception>(MessageReceivePump.PumpAsyncResult.Finally);
				MessageReceivePump.PumpAsyncResult.onDispatchCompleted = new AsyncCallback(MessageReceivePump.PumpAsyncResult.OnDispatchCompleted);
			}

			public PumpAsyncResult(MessageReceivePump owner, BrokeredMessage firstMessage, AsyncCallback callback, object state) : base(TimeSpan.MaxValue, callback, state)
			{
				this.owner = owner;
				this.firstMessage = firstMessage;
				this.trackingContext = TrackingContext.GetInstance(Guid.NewGuid(), owner.receiver.Path);
				MessageReceivePump.PumpAsyncResult pumpAsyncResult = this;
				pumpAsyncResult.OnCompleting = (Action<AsyncResult, Exception>)Delegate.Combine(pumpAsyncResult.OnCompleting, MessageReceivePump.PumpAsyncResult.CompletingAction);
			}

			private static void Finally(AsyncResult asyncResult, Exception exception)
			{
				if (exception != null)
				{
					MessageReceivePump.PumpAsyncResult pumpAsyncResult = (MessageReceivePump.PumpAsyncResult)asyncResult;
					MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageReceivePumpUnexpectedException(pumpAsyncResult.trackingContext.Activity, pumpAsyncResult.trackingContext.TrackingId, pumpAsyncResult.trackingContext.SystemTracker, exception.ToString()));
				}
			}

			protected override IEnumerator<IteratorAsyncResult<MessageReceivePump.PumpAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				TimeSpan timeSpan;
				while (this.owner.receiver.IsOpened)
				{
					if (!this.owner.semaphore.TryEnter())
					{
						MessageReceivePump.PumpAsyncResult pumpAsyncResult = this;
						IteratorAsyncResult<MessageReceivePump.PumpAsyncResult>.BeginCall beginCall = (MessageReceivePump.PumpAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.owner.semaphore.BeginEnter(c, s);
						yield return pumpAsyncResult.CallAsync(beginCall, (MessageReceivePump.PumpAsyncResult thisPtr, IAsyncResult r) => thisPtr.owner.semaphore.EndEnter(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
					}
					this.message = null;
					if (this.firstMessage != null)
					{
						this.message = this.firstMessage;
						this.firstMessage = null;
					}
					else
					{
						MessageReceivePump.PumpAsyncResult pumpAsyncResult1 = this;
						IteratorAsyncResult<MessageReceivePump.PumpAsyncResult>.BeginCall beginCall1 = (MessageReceivePump.PumpAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.owner.receiver.BeginReceive(thisPtr.owner.OnMessageOptions.ReceiveTimeOut, c, s);
						yield return pumpAsyncResult1.CallAsync(beginCall1, (MessageReceivePump.PumpAsyncResult thisPtr, IAsyncResult r) => thisPtr.message = thisPtr.owner.receiver.EndReceive(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
					}
					if (base.LastAsyncStepException != null)
					{
						MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageReceivePumpReceiveException(this.trackingContext.Activity, this.trackingContext.TrackingId, this.trackingContext.SystemTracker, base.LastAsyncStepException.ToString()));
						this.owner.RaiseExceptionReceivedEvent(base.LastAsyncStepException, "Receive");
						if (MessageReceivePump.ShouldBackoff(base.LastAsyncStepException, out timeSpan))
						{
							MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageReceivePumpBackoff(this.trackingContext.Activity, this.trackingContext.TrackingId, this.trackingContext.SystemTracker, (int)MessageReceivePump.ServerBusyExceptionBackoffAmount.TotalMilliseconds, base.LastAsyncStepException.ToString()));
							yield return base.CallAsyncSleep(timeSpan);
						}
					}
					if (this.message != null)
					{
						try
						{
							(new MessageReceivePump.DispatchAsyncResult(this.owner, this.trackingContext, this.message, MessageReceivePump.PumpAsyncResult.onDispatchCompleted, this.owner)).Start();
						}
						catch (Exception exception)
						{
							if (Fx.IsFatal(exception))
							{
								throw;
							}
							this.owner.semaphore.Exit();
						}
					}
					else
					{
						this.owner.semaphore.Exit();
					}
				}
				MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageReceivePumpStopped(this.trackingContext.Activity, this.trackingContext.TrackingId, this.trackingContext.SystemTracker));
			}

			private static void OnDispatchCompleted(IAsyncResult asyncResult)
			{
				MessageReceivePump asyncState = (MessageReceivePump)asyncResult.AsyncState;
				try
				{
					try
					{
						AsyncResult<MessageReceivePump.DispatchAsyncResult>.End(asyncResult);
					}
					catch (Exception exception)
					{
						if (Fx.IsFatal(exception))
						{
							throw;
						}
					}
				}
				finally
				{
					asyncState.semaphore.Exit();
				}
			}
		}

		private sealed class RenewLockLoopAsyncResult : IteratorAsyncResult<MessageReceivePump.RenewLockLoopAsyncResult>
		{
			private readonly MessageReceivePump pump;

			private readonly TrackingContext trackingContext;

			private readonly BrokeredMessage message;

			private readonly CancellationToken cancellationToken;

			public RenewLockLoopAsyncResult(MessageReceivePump pump, TrackingContext trackingContext, BrokeredMessage message, CancellationToken cancellationToken, AsyncCallback callback, object state) : base(TimeSpan.MaxValue, callback, state)
			{
				this.pump = pump;
				this.trackingContext = trackingContext;
				this.message = message;
				this.cancellationToken = cancellationToken;
			}

			private TimeSpan CalculateRenewAfterDuration()
			{
				TimeSpan lockedUntilUtc = this.message.LockedUntilUtc - DateTime.UtcNow;
				if (lockedUntilUtc < TimeSpan.Zero)
				{
					TimeSpan minimumLockDuration = Constants.MinimumLockDuration;
					lockedUntilUtc = TimeSpan.FromTicks(minimumLockDuration.Ticks / (long)2);
				}
				long ticks = lockedUntilUtc.Ticks / (long)2;
				TimeSpan maximumRenewBufferDuration = Constants.MaximumRenewBufferDuration;
				TimeSpan timeSpan = TimeSpan.FromTicks(Math.Min(ticks, maximumRenewBufferDuration.Ticks));
				return lockedUntilUtc - timeSpan;
			}

			protected override IEnumerator<IteratorAsyncResult<MessageReceivePump.RenewLockLoopAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				while (!this.pump.receiver.IsClosed && !this.cancellationToken.IsCancellationRequested)
				{
					yield return base.CallAsyncSleep(this.CalculateRenewAfterDuration(), this.cancellationToken);
					if (this.cancellationToken.IsCancellationRequested)
					{
						break;
					}
					MessageReceivePump.RenewLockLoopAsyncResult renewLockLoopAsyncResult = this;
					IteratorAsyncResult<MessageReceivePump.RenewLockLoopAsyncResult>.BeginCall beginCall = (MessageReceivePump.RenewLockLoopAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.message.BeginRenewLock(c, s);
					yield return renewLockLoopAsyncResult.CallAsync(beginCall, (MessageReceivePump.RenewLockLoopAsyncResult thisPtr, IAsyncResult r) => thisPtr.message.EndRenewLock(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
					if (base.LastAsyncStepException == null)
					{
						continue;
					}
					Exception lastAsyncStepException = base.LastAsyncStepException;
					if (!(lastAsyncStepException is NotSupportedException))
					{
						if (lastAsyncStepException is MessageLockLostException || lastAsyncStepException is ObjectDisposedException)
						{
							break;
						}
						if (!(lastAsyncStepException is InvalidOperationException))
						{
							if (lastAsyncStepException is OperationCanceledException)
							{
								break;
							}
							this.pump.RaiseExceptionReceivedEvent(lastAsyncStepException, "RenewLock");
							MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageReceivePumpRenewLockFailed(this.trackingContext.Activity, this.trackingContext.SystemTracker, this.trackingContext.TrackingId, this.message.MessageId, lastAsyncStepException.ToStringSlim()));
						}
						else
						{
							MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageReceivePumpRenewLockInvalidOperation(this.trackingContext.Activity, this.trackingContext.SystemTracker, this.trackingContext.TrackingId, this.message.MessageId, lastAsyncStepException.ToStringSlim()));
							break;
						}
					}
					else
					{
						MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageReceivePumpRenewLockNotSupported(this.trackingContext.Activity, this.trackingContext.SystemTracker, this.trackingContext.TrackingId, this.message.MessageId, lastAsyncStepException.ToStringSlim()));
						this.pump.renewSupported = false;
						break;
					}
				}
			}
		}

		private sealed class StartAsyncResult : IteratorAsyncResult<MessageReceivePump.StartAsyncResult>
		{
			private readonly AsyncCallback onPumpCompleted;

			private readonly MessageReceivePump owner;

			private int currentRetryCount;

			public BrokeredMessage InitialMessage
			{
				get;
				private set;
			}

			public StartAsyncResult(MessageReceivePump owner, AsyncCallback callback, object state) : base(TimeSpan.MaxValue, callback, state)
			{
				this.owner = owner;
			}

			protected override IEnumerator<IteratorAsyncResult<MessageReceivePump.StartAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				TimeSpan timeSpan;
				while (this.currentRetryCount <= 3)
				{
					MessageReceivePump.StartAsyncResult startAsyncResult = this;
					startAsyncResult.currentRetryCount = startAsyncResult.currentRetryCount + 1;
					MessageReceivePump.StartAsyncResult startAsyncResult1 = this;
					IteratorAsyncResult<MessageReceivePump.StartAsyncResult>.BeginCall beginCall = (MessageReceivePump.StartAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.owner.receiver.BeginReceive(TimeSpan.Zero, c, s);
					yield return startAsyncResult1.CallAsync(beginCall, (MessageReceivePump.StartAsyncResult thisPtr, IAsyncResult r) => thisPtr.InitialMessage = thisPtr.owner.receiver.EndReceive(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
					if (base.LastAsyncStepException == null)
					{
						break;
					}
					if (this.ShouldRetry(base.LastAsyncStepException))
					{
						if (!MessageReceivePump.ShouldBackoff(base.LastAsyncStepException, out timeSpan))
						{
							continue;
						}
						yield return base.CallAsyncSleep(timeSpan);
					}
					else
					{
						base.Complete(base.LastAsyncStepException);
						goto Label0;
					}
				}
				try
				{
					(new MessageReceivePump.PumpAsyncResult(this.owner, this.InitialMessage, this.onPumpCompleted, this)).Start();
				}
				catch (Exception exception)
				{
					Environment.FailFast(exception.ToString());
				}
			Label0:
				yield break;
			}

			private static void OnPumpCompleted(IAsyncResult ar)
			{
				try
				{
					AsyncResult<MessageReceivePump.PumpAsyncResult>.End(ar);
				}
				catch (Exception exception)
				{
					Environment.FailFast(exception.ToString());
				}
			}

			private bool ShouldRetry(Exception exception)
			{
				if (this.currentRetryCount == 3)
				{
					return false;
				}
				MessagingException messagingException = exception as MessagingException;
				if (messagingException == null)
				{
					return false;
				}
				return messagingException.IsTransient;
			}
		}

		private sealed class SyncCallbackAsyncResult : AsyncResult<MessageReceivePump.SyncCallbackAsyncResult>
		{
			private readonly static Action<object> executeSyncCallback;

			private readonly MessageReceivePump owner;

			private readonly BrokeredMessage message;

			static SyncCallbackAsyncResult()
			{
				MessageReceivePump.SyncCallbackAsyncResult.executeSyncCallback = new Action<object>(MessageReceivePump.SyncCallbackAsyncResult.ExecuteSyncCallback);
			}

			public SyncCallbackAsyncResult(MessageReceivePump owner, BrokeredMessage message, AsyncCallback callback, object state) : base(callback, state)
			{
				this.owner = owner;
				this.message = message;
				IOThreadScheduler.ScheduleCallbackNoFlow(MessageReceivePump.SyncCallbackAsyncResult.executeSyncCallback, this);
			}

			private static void ExecuteSyncCallback(object state)
			{
				MessageReceivePump.SyncCallbackAsyncResult syncCallbackAsyncResult = (MessageReceivePump.SyncCallbackAsyncResult)state;
				Exception exception = null;
				try
				{
					syncCallbackAsyncResult.owner.syncCallback(syncCallbackAsyncResult.message);
				}
				catch (Exception exception2)
				{
					Exception exception1 = exception2;
					if (Fx.IsFatal(exception1))
					{
						throw;
					}
					exception = exception1;
				}
				syncCallbackAsyncResult.Complete(false, exception);
			}
		}
	}
}