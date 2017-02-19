using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus.Messaging
{
	internal sealed class MessageSessionPump : CommunicationObject
	{
		private readonly static TimeSpan SleepAmount;

		private readonly static TimeSpan NonTransientExceptionSleepAmount;

		private readonly static TimeSpan ServerWaitTimeForFirstAcceptMessageSessionCall;

		private readonly static AsyncCallback StaticOnPumpSessionCompleted;

		private readonly static AsyncCallback StaticOnPumpMessageCompleted;

		private readonly IMessageSessionEntity entity;

		private readonly PendingOperationTracker operationTracker;

		private readonly IMessageSessionAsyncHandlerFactory handlerFactory;

		private readonly SessionHandlerOptions options;

		private readonly AsyncSemaphore acceptSemaphore;

		private readonly AsyncSemaphore instanceSemaphore;

		private readonly CancellationTokenSource pumpCancellationTokenSource;

		private readonly TrackingContext trackingContext;

		private bool renewSupported;

		protected override TimeSpan DefaultCloseTimeout
		{
			get
			{
				return TimeSpan.FromMinutes(1);
			}
		}

		protected override TimeSpan DefaultOpenTimeout
		{
			get
			{
				return TimeSpan.FromMinutes(1);
			}
		}

		public IMessageSessionEntity Entity
		{
			get
			{
				return this.entity;
			}
		}

		static MessageSessionPump()
		{
			MessageSessionPump.SleepAmount = TimeSpan.FromSeconds(1);
			MessageSessionPump.NonTransientExceptionSleepAmount = TimeSpan.FromSeconds(60);
			MessageSessionPump.ServerWaitTimeForFirstAcceptMessageSessionCall = TimeSpan.FromSeconds(5);
			MessageSessionPump.StaticOnPumpSessionCompleted = new AsyncCallback(MessageSessionPump.OnPumpSessionCompleted);
			MessageSessionPump.StaticOnPumpMessageCompleted = new AsyncCallback(MessageSessionPump.OnPumpMessageCompleted);
		}

		public MessageSessionPump(string entityPath, IMessageSessionEntity entity, IMessageSessionAsyncHandlerFactory handlerFactory, SessionHandlerOptions options)
		{
			this.trackingContext = TrackingContext.GetInstance(Guid.NewGuid(), entityPath);
			this.entity = entity;
			this.handlerFactory = handlerFactory;
			this.options = options ?? new SessionHandlerOptions();
			this.operationTracker = new PendingOperationTracker();
			this.acceptSemaphore = new AsyncSemaphore(this.options.MaxPendingAcceptSessionCalls);
			this.instanceSemaphore = new AsyncSemaphore(this.options.MaxConcurrentSessions);
			this.pumpCancellationTokenSource = new CancellationTokenSource();
			this.renewSupported = true;
			if (this.entity.PrefetchCount == 0)
			{
				this.entity.PrefetchCount = Constants.DefaultClientPumpPrefetchCount;
			}
		}

		private IAsyncResult BeginPumpMessage(MessageSession session, CancellationToken token, AsyncCallback callback, object state)
		{
			IAsyncResult asyncResult;
			this.operationTracker.IncrementOperationCount();
			try
			{
				MessageSessionPump.PumpMessageAsyncResult pumpMessageAsyncResult = new MessageSessionPump.PumpMessageAsyncResult(this, this.trackingContext, session, token, callback, state);
				asyncResult = pumpMessageAsyncResult.Start();
			}
			catch (Exception exception)
			{
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				this.operationTracker.DecrementOperationCount();
				throw;
			}
			return asyncResult;
		}

		private IAsyncResult BeginPumpSession(MessageSession initialSession, CancellationToken cancellationToken, AsyncCallback callback, object state)
		{
			IAsyncResult asyncResult;
			this.operationTracker.IncrementOperationCount();
			try
			{
				MessageSessionPump.PumpSessionAsyncResult pumpSessionAsyncResult = new MessageSessionPump.PumpSessionAsyncResult(this.trackingContext, this, initialSession, cancellationToken, callback, state);
				asyncResult = pumpSessionAsyncResult.Start();
			}
			catch (Exception exception)
			{
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				this.operationTracker.DecrementOperationCount();
				throw;
			}
			return asyncResult;
		}

		private void EndPumpMessage(IAsyncResult result)
		{
			try
			{
				AsyncResult<MessageSessionPump.PumpMessageAsyncResult>.End(result);
			}
			finally
			{
				this.operationTracker.DecrementOperationCount();
			}
		}

		private void EndPumpSession(IAsyncResult result)
		{
			try
			{
				AsyncResult<MessageSessionPump.PumpSessionAsyncResult>.End(result);
			}
			finally
			{
				this.operationTracker.DecrementOperationCount();
			}
		}

		private static bool IndicatesSessionLost(Exception exception)
		{
			if (exception is OperationCanceledException)
			{
				return true;
			}
			return exception is SessionLockLostException;
		}

		private static bool IsNonTransientException(Exception e)
		{
			MessagingException messagingException = e as MessagingException;
			if (messagingException != null)
			{
				return !messagingException.IsTransient;
			}
			return !(e is TimeoutException);
		}

		private bool IsOpenningOrOpened()
		{
			CommunicationState state = base.State;
			if (state == CommunicationState.Opening)
			{
				return true;
			}
			return state == CommunicationState.Opened;
		}

		protected override void OnAbort()
		{
			MessageSessionPump.CloseOrAbortAsyncResult closeOrAbortAsyncResult = new MessageSessionPump.CloseOrAbortAsyncResult(this, true, TimeSpan.MaxValue, null, null);
			closeOrAbortAsyncResult.RunSynchronously();
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return (new MessageSessionPump.CloseOrAbortAsyncResult(this, false, timeout, callback, state)).Start();
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return (new MessageSessionPump.OpenAsyncResult(this, timeout, callback, state)).Start();
		}

		protected override void OnClose(TimeSpan timeout)
		{
			MessageSessionPump.CloseOrAbortAsyncResult closeOrAbortAsyncResult = new MessageSessionPump.CloseOrAbortAsyncResult(this, false, timeout, null, null);
			closeOrAbortAsyncResult.RunSynchronously();
		}

		protected override void OnClosed()
		{
			this.pumpCancellationTokenSource.Dispose();
			base.OnClosed();
		}

		protected override void OnEndClose(IAsyncResult ar)
		{
			AsyncResult<MessageSessionPump.CloseOrAbortAsyncResult>.End(ar);
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			AsyncResult<MessageSessionPump.OpenAsyncResult>.End(result);
		}

		protected override void OnOpen(TimeSpan timeout)
		{
			(new MessageSessionPump.OpenAsyncResult(this, timeout, null, null)).RunSynchronously();
		}

		private static void OnPumpMessageCompleted(IAsyncResult ar)
		{
			MessageSessionPump asyncState = (MessageSessionPump)ar.AsyncState;
			try
			{
				asyncState.EndPumpMessage(ar);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageSessionPumpUnexpectedException(asyncState.trackingContext.Activity, asyncState.trackingContext.TrackingId, asyncState.trackingContext.SystemTracker, string.Empty, exception.ToString()));
				throw;
			}
		}

		private static void OnPumpSessionCompleted(IAsyncResult ar)
		{
			MessageSessionPump asyncState = (MessageSessionPump)ar.AsyncState;
			try
			{
				asyncState.EndPumpSession(ar);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageSessionPumpUnexpectedException(asyncState.trackingContext.Activity, asyncState.trackingContext.TrackingId, asyncState.trackingContext.SystemTracker, string.Empty, exception.ToString()));
				throw;
			}
		}

		private void RaiseExceptionRecieved(Exception e, string action)
		{
			ExceptionReceivedEventArgs exceptionReceivedEventArg = new ExceptionReceivedEventArgs(e, action);
			this.options.RaiseExceptionReceived(this.entity, exceptionReceivedEventArg);
		}

		private void ScheduleMessagePump(MessageSession session)
		{
			try
			{
				this.BeginPumpMessage(session, this.pumpCancellationTokenSource.Token, MessageSessionPump.StaticOnPumpMessageCompleted, this);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageSessionPumpUnexpectedException(this.trackingContext.Activity, this.trackingContext.TrackingId, this.trackingContext.SystemTracker, session.SessionId, exception.ToString()));
				throw;
			}
		}

		private void ScheduleSesssionPump(MessageSession prefetchedSession)
		{
			try
			{
				this.BeginPumpSession(prefetchedSession, this.pumpCancellationTokenSource.Token, MessageSessionPump.StaticOnPumpSessionCompleted, this);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageSessionPumpUnexpectedException(this.trackingContext.Activity, this.trackingContext.TrackingId, this.trackingContext.SystemTracker, string.Empty, exception.ToString()));
				throw;
			}
		}

		private sealed class CloseOrAbortAsyncResult : IteratorAsyncResult<MessageSessionPump.CloseOrAbortAsyncResult>
		{
			private readonly MessageSessionPump owner;

			private readonly bool shouldAbort;

			public CloseOrAbortAsyncResult(MessageSessionPump owner, bool shouldAbort, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.owner = owner;
				this.shouldAbort = shouldAbort;
			}

			protected override IEnumerator<IteratorAsyncResult<MessageSessionPump.CloseOrAbortAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				this.owner.pumpCancellationTokenSource.Cancel();
				if (!this.shouldAbort && this.owner.options.WaitForPendingOperationsOnClose)
				{
					MessageSessionPump.CloseOrAbortAsyncResult closeOrAbortAsyncResult = this;
					IteratorAsyncResult<MessageSessionPump.CloseOrAbortAsyncResult>.BeginCall beginCall = (MessageSessionPump.CloseOrAbortAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.owner.operationTracker.BeginWaitPendingOperations(t, c, s);
					yield return closeOrAbortAsyncResult.CallAsync(beginCall, (MessageSessionPump.CloseOrAbortAsyncResult thisPtr, IAsyncResult r) => thisPtr.owner.operationTracker.EndWaitPendingOperations(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				}
			}
		}

		private sealed class OpenAsyncResult : IteratorAsyncResult<MessageSessionPump.OpenAsyncResult>
		{
			private readonly MessageSessionPump owner;

			private MessageSession initialSession;

			public OpenAsyncResult(MessageSessionPump owner, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.owner = owner;
			}

			protected override IEnumerator<IteratorAsyncResult<MessageSessionPump.OpenAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				while (true)
				{
					if (base.RemainingTime() > TimeSpan.Zero)
					{
						MessageSessionPump.OpenAsyncResult openAsyncResult = this;
						IteratorAsyncResult<MessageSessionPump.OpenAsyncResult>.BeginCall beginCall = (MessageSessionPump.OpenAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.owner.entity.BeginAcceptMessageSession(MessageSessionPump.ServerWaitTimeForFirstAcceptMessageSessionCall, c, s);
						yield return openAsyncResult.CallAsync(beginCall, (MessageSessionPump.OpenAsyncResult thisPtr, IAsyncResult r) => thisPtr.initialSession = thisPtr.owner.entity.EndAcceptMessageSession(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
						if (base.LastAsyncStepException == null || base.LastAsyncStepException is TimeoutException)
						{
							for (int i = 0; i < this.owner.options.MaxPendingAcceptSessionCalls; i++)
							{
								if (this.initialSession == null || i != 0)
								{
									this.owner.ScheduleSesssionPump(null);
								}
								else
								{
									if (!this.owner.instanceSemaphore.TryEnter())
									{
										Fx.AssertAndFailFastService("Bug: MessageSessionSemaphore failed to enter the instanceSemaphore for the first call.");
										MessageSessionPump.OpenAsyncResult openAsyncResult1 = this;
										IteratorAsyncResult<MessageSessionPump.OpenAsyncResult>.BeginCall beginCall1 = (MessageSessionPump.OpenAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.owner.instanceSemaphore.BeginEnter(c, s);
										yield return openAsyncResult1.CallAsync(beginCall1, (MessageSessionPump.OpenAsyncResult thisPtr, IAsyncResult r) => thisPtr.owner.instanceSemaphore.EndEnter(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
									}
									this.owner.ScheduleSesssionPump(this.initialSession);
								}
							}
						}
						else if (!MessageSessionPump.IsNonTransientException(base.LastAsyncStepException))
						{
							long ticks = base.RemainingTime().Ticks;
							TimeSpan sleepAmount = MessageSessionPump.SleepAmount;
							TimeSpan timeSpan = TimeSpan.FromTicks(Math.Min(ticks, sleepAmount.Ticks));
							if (timeSpan > TimeSpan.Zero)
							{
								yield return base.CallAsyncSleep(timeSpan);
							}
						}
						else
						{
							base.Complete(base.LastAsyncStepException);
							break;
						}
					}
					else
					{
						base.Complete(new TimeoutException(SRCore.TimeoutOnOperation(base.OriginalTimeout)));
						break;
					}
				}
			}
		}

		private sealed class PumpMessageAsyncResult : IteratorAsyncResult<MessageSessionPump.PumpMessageAsyncResult>
		{
			private readonly static AsyncCallback StaticOnRenewSessionLockCompleted;

			private readonly static Action<object> StaticOnUserCallTimedOut;

			private readonly static Action<object> StaticOnPumpCancelled;

			private readonly static TimeSpan MessageCompletionOperationTimeout;

			private readonly MessageSessionPump owner;

			private readonly TrackingContext trackingContext;

			private readonly MessageSession session;

			private readonly CancellationTokenSource renewCancellationTokenSource;

			private readonly IOThreadTimer userCallTimer;

			private readonly List<Guid> completionList;

			private BrokeredMessage message;

			private IMessageSessionAsyncHandler sessionHandler;

			private CancellationToken cancellationToken;

			private CancellationTokenRegistration cancellationTokenRegistration;

			private bool exceptionHandled;

			private volatile bool completionInProgress;

			static PumpMessageAsyncResult()
			{
				MessageSessionPump.PumpMessageAsyncResult.StaticOnRenewSessionLockCompleted = new AsyncCallback(MessageSessionPump.PumpMessageAsyncResult.OnRenewSessionLockCompleted);
				MessageSessionPump.PumpMessageAsyncResult.StaticOnUserCallTimedOut = new Action<object>(MessageSessionPump.PumpMessageAsyncResult.OnUserCallTimedOut);
				MessageSessionPump.PumpMessageAsyncResult.StaticOnPumpCancelled = new Action<object>(MessageSessionPump.PumpMessageAsyncResult.OnPumpCancelled);
				MessageSessionPump.PumpMessageAsyncResult.MessageCompletionOperationTimeout = Constants.DefaultOperationTimeout;
			}

			public PumpMessageAsyncResult(MessageSessionPump owner, TrackingContext trackingContext, MessageSession session, CancellationToken cancellationToken, AsyncCallback callback, object state) : base(TimeSpan.MaxValue, callback, state)
			{
				this.owner = owner;
				this.trackingContext = trackingContext;
				this.session = session;
				this.renewCancellationTokenSource = new CancellationTokenSource();
				this.cancellationToken = cancellationToken;
				this.userCallTimer = new IOThreadTimer(MessageSessionPump.PumpMessageAsyncResult.StaticOnUserCallTimedOut, this, true);
				this.completionList = new List<Guid>();
			}

			protected override IEnumerator<IteratorAsyncResult<MessageSessionPump.PumpMessageAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				try
				{
					try
					{
						this.cancellationTokenRegistration = this.cancellationToken.Register(MessageSessionPump.PumpMessageAsyncResult.StaticOnPumpCancelled, this);
					}
					catch (ObjectDisposedException objectDisposedException)
					{
						this.renewCancellationTokenSource.Cancel();
						goto Label0;
					}
					if (this.owner.renewSupported)
					{
						this.ScheduleRenew();
					}
					MessageSessionPump.PumpMessageAsyncResult pumpMessageAsyncResult = this;
					IteratorAsyncResult<MessageSessionPump.PumpMessageAsyncResult>.BeginCall beginCall = (MessageSessionPump.PumpMessageAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.session.BeginReceive(TimeSpan.Zero, c, s);
					yield return pumpMessageAsyncResult.CallAsync(beginCall, (MessageSessionPump.PumpMessageAsyncResult thisPtr, IAsyncResult r) => thisPtr.message = thisPtr.session.EndReceive(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
					if (base.LastAsyncStepException != null)
					{
						Exception lastAsyncStepException = base.LastAsyncStepException;
						MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageSessionPumpFirstReceiveFailed(this.trackingContext.Activity, this.trackingContext.TrackingId, this.trackingContext.SystemTracker, this.session.SessionId, lastAsyncStepException.ToString()));
						if (!this.owner.IsOpenningOrOpened())
						{
							goto Label1;
						}
						this.owner.RaiseExceptionRecieved(lastAsyncStepException, "Receive");
					}
					if (this.message != null)
					{
						MessageSessionPump.PumpMessageAsyncResult pumpMessageAsyncResult1 = this;
						pumpMessageAsyncResult1.RunUserCode((MessageSessionPump.PumpMessageAsyncResult thisPtr) => thisPtr.sessionHandler = thisPtr.owner.handlerFactory.CreateInstance(thisPtr.session, thisPtr.message), "FactoryCreateInstance");
						if (this.sessionHandler != null)
						{
							do
							{
								this.userCallTimer.Set(this.owner.options.AutoRenewTimeout);
								try
								{
									MessageSessionPump.PumpMessageAsyncResult pumpMessageAsyncResult2 = this;
									yield return pumpMessageAsyncResult2.CallTask((MessageSessionPump.PumpMessageAsyncResult thisPtr, TimeSpan t) => thisPtr.sessionHandler.OnMessageAsync(thisPtr.session, thisPtr.message), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
								}
								finally
								{
									this.userCallTimer.Cancel();
								}
								if (base.LastAsyncStepException != null)
								{
									this.ReportUserException(base.LastAsyncStepException, "OnMessageAsync");
									if (!this.session.IsClosedOrClosing)
									{
										MessageSessionPump.PumpMessageAsyncResult pumpMessageAsyncResult3 = this;
										IteratorAsyncResult<MessageSessionPump.PumpMessageAsyncResult>.BeginCall beginCall1 = (MessageSessionPump.PumpMessageAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.message.BeginAbandon(c, s);
										yield return pumpMessageAsyncResult3.CallAsync(beginCall1, (MessageSessionPump.PumpMessageAsyncResult thisPtr, IAsyncResult r) => thisPtr.message.EndAbandon(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
										if (base.LastAsyncStepException != null)
										{
											Exception exception = base.LastAsyncStepException;
											MessageSessionPump.PumpMessageAsyncResult pumpMessageAsyncResult4 = this;
											IteratorAsyncResult<MessageSessionPump.PumpMessageAsyncResult>.BeginCall beginCall2 = (MessageSessionPump.PumpMessageAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => (new MessageSessionPump.PumpMessageAsyncResult.HandleExceptionAsyncResult(thisPtr, "Abandon", exception, true, t, c, s)).Start();
											yield return pumpMessageAsyncResult4.CallAsync(beginCall2, (MessageSessionPump.PumpMessageAsyncResult thisPtr, IAsyncResult r) => thisPtr.exceptionHandled = AsyncResult<MessageSessionPump.PumpMessageAsyncResult.HandleExceptionAsyncResult>.End(r).Handled, IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
											if (!this.exceptionHandled)
											{
												goto Label0;
											}
										}
									}
								}
								else if (this.owner.options.AutoComplete && !this.session.IsClosedOrClosing)
								{
									MessageSessionPump.PumpMessageAsyncResult.ScheduleMessageCompletion(this, this.message);
								}
								if (!this.owner.IsOpenningOrOpened() || this.owner.entity.IsClosed || this.session.IsClosedOrClosing)
								{
									break;
								}
								Timestamp now = Timestamp.Now;
								if (this.message != null)
								{
									this.message.Dispose();
									this.message = null;
								}
								do
								{
									MessageSessionPump.PumpMessageAsyncResult pumpMessageAsyncResult5 = this;
									IteratorAsyncResult<MessageSessionPump.PumpMessageAsyncResult>.BeginCall beginCall3 = (MessageSessionPump.PumpMessageAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.session.BeginReceive(thisPtr.owner.options.MessageWaitTimeout, c, s);
									yield return pumpMessageAsyncResult5.CallAsync(beginCall3, (MessageSessionPump.PumpMessageAsyncResult thisPtr, IAsyncResult r) => thisPtr.message = thisPtr.session.EndReceive(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
									if (base.LastAsyncStepException != null)
									{
										Exception lastAsyncStepException1 = base.LastAsyncStepException;
										MessageSessionPump.PumpMessageAsyncResult pumpMessageAsyncResult6 = this;
										IteratorAsyncResult<MessageSessionPump.PumpMessageAsyncResult>.BeginCall beginCall4 = (MessageSessionPump.PumpMessageAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => (new MessageSessionPump.PumpMessageAsyncResult.HandleExceptionAsyncResult(thisPtr, "Receive", lastAsyncStepException1, t, c, s)).Start();
										yield return pumpMessageAsyncResult6.CallAsync(beginCall4, (MessageSessionPump.PumpMessageAsyncResult thisPtr, IAsyncResult r) => thisPtr.exceptionHandled = AsyncResult<MessageSessionPump.PumpMessageAsyncResult.HandleExceptionAsyncResult>.End(r).Handled, IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
										if (this.exceptionHandled)
										{
											TimeSpan messageWaitTimeout = this.owner.options.MessageWaitTimeout - now.Elapsed;
											long ticks = messageWaitTimeout.Ticks;
											TimeSpan sleepAmount = MessageSessionPump.SleepAmount;
											TimeSpan timeSpan = TimeSpan.FromTicks(Math.Min(ticks, sleepAmount.Ticks));
											if (timeSpan <= TimeSpan.Zero)
											{
												continue;
											}
											yield return base.CallAsyncSleep(timeSpan, this.cancellationToken);
										}
										else
										{
											goto Label0;
										}
									}
									else if (this.message != null)
									{
										break;
									}
								}
								while (now.Elapsed < this.owner.options.MessageWaitTimeout);
								if (this.message != null)
								{
									continue;
								}
								this.userCallTimer.Set(this.owner.options.AutoRenewTimeout);
								try
								{
									MessageSessionPump.PumpMessageAsyncResult pumpMessageAsyncResult7 = this;
									yield return pumpMessageAsyncResult7.CallTask((MessageSessionPump.PumpMessageAsyncResult thisPtr, TimeSpan t) => thisPtr.sessionHandler.OnCloseSessionAsync(thisPtr.session), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
								}
								finally
								{
									this.userCallTimer.Cancel();
								}
								if (base.LastAsyncStepException == null)
								{
									break;
								}
								this.ReportUserException(base.LastAsyncStepException, "OnCloseSessionAsync");
								break;
							}
							while (this.owner.IsOpenningOrOpened() && !this.session.IsClosed && !this.cancellationToken.IsCancellationRequested);
						}
						else
						{
							yield return base.CallAsyncSleep(MessageSessionPump.SleepAmount);
						}
					}
					else
					{
						MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageSessionPumpFirstReceiveReturnedNoMessage(this.trackingContext.Activity, this.trackingContext.TrackingId, this.trackingContext.SystemTracker, this.session.SessionId));
					}
				Label1:
					if (this.session.IsClosed)
					{
						goto Label0;
					}
					MessageSessionPump.PumpMessageAsyncResult pumpMessageAsyncResult8 = this;
					IteratorAsyncResult<MessageSessionPump.PumpMessageAsyncResult>.BeginCall beginCall5 = (MessageSessionPump.PumpMessageAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.session.BeginClose(c, s);
					yield return pumpMessageAsyncResult8.CallAsync(beginCall5, (MessageSessionPump.PumpMessageAsyncResult thisPtr, IAsyncResult r) => thisPtr.session.EndClose(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
					if (base.LastAsyncStepException == null)
					{
						goto Label0;
					}
					Exception exception1 = base.LastAsyncStepException;
					MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageSessionPumpSessionCloseFailed(this.trackingContext.Activity, this.trackingContext.TrackingId, this.trackingContext.SystemTracker, this.session.SessionId, exception1.ToString()));
					this.owner.RaiseExceptionRecieved(exception1, "SessionClose");
				}
				finally
				{
					try
					{
						if (this.message != null)
						{
							this.message.Dispose();
							this.message = null;
						}
						if (!this.session.IsClosed)
						{
							this.session.Abort();
						}
						this.RunUserCode((MessageSessionPump.PumpMessageAsyncResult thisPtr) => thisPtr.owner.handlerFactory.DisposeInstance(thisPtr.sessionHandler), "FactoryDisposeInstance");
					}
					finally
					{
						this.owner.instanceSemaphore.Exit();
						this.cancellationTokenRegistration.Dispose();
						this.renewCancellationTokenSource.Cancel();
						this.renewCancellationTokenSource.Dispose();
					}
				}
			Label0:
				yield break;
			}

			private static void OnCompleteMessageCompletion(IAsyncResult result)
			{
				MessageSessionPump.PumpMessageAsyncResult.BatchCompleteAsyncResult batchCompleteAsyncResult = (MessageSessionPump.PumpMessageAsyncResult.BatchCompleteAsyncResult)result;
				MessageSessionPump.PumpMessageAsyncResult owner = batchCompleteAsyncResult.Owner;
				lock (owner.completionList)
				{
					try
					{
						try
						{
							AsyncResult<MessageSessionPump.PumpMessageAsyncResult.BatchCompleteAsyncResult>.End(batchCompleteAsyncResult);
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
						owner.completionInProgress = false;
					}
				}
				MessageSessionPump.PumpMessageAsyncResult.ScheduleMessageCompletion(owner, null);
			}

			private static void OnPumpCancelled(object state)
			{
				MessageSessionPump.PumpMessageAsyncResult pumpMessageAsyncResult = (MessageSessionPump.PumpMessageAsyncResult)state;
				try
				{
					pumpMessageAsyncResult.renewCancellationTokenSource.Cancel();
				}
				catch (ObjectDisposedException objectDisposedException)
				{
				}
			}

			private static void OnRenewSessionLockCompleted(IAsyncResult result)
			{
				MessageSessionPump.PumpMessageAsyncResult asyncState = (MessageSessionPump.PumpMessageAsyncResult)result.AsyncState;
				try
				{
					AsyncResult<MessageSessionPump.RenewSessionLockAsyncResult>.End(result);
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageSessionPumpRenewEndFailed(asyncState.trackingContext.Activity, asyncState.trackingContext.TrackingId, asyncState.trackingContext.SystemTracker, asyncState.session.SessionId, exception.ToString()));
				}
			}

			private static void OnUserCallTimedOut(object state)
			{
				MessageSessionPump.PumpMessageAsyncResult pumpMessageAsyncResult = (MessageSessionPump.PumpMessageAsyncResult)state;
				MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageSessionPumpUserCallTimedOut(pumpMessageAsyncResult.trackingContext.Activity, pumpMessageAsyncResult.trackingContext.TrackingId, pumpMessageAsyncResult.trackingContext.SystemTracker, pumpMessageAsyncResult.session.SessionId, pumpMessageAsyncResult.owner.options.AutoRenewTimeout.ToString()));
				pumpMessageAsyncResult.renewCancellationTokenSource.Cancel();
			}

			private void ReportUserException(Exception exception, string action)
			{
				MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageSessionPumpUserException(this.trackingContext.Activity, this.trackingContext.TrackingId, this.trackingContext.SystemTracker, this.session.SessionId, exception.ToString()));
				this.owner.RaiseExceptionRecieved(exception, action);
			}

			private void RunUserCode(Action<MessageSessionPump.PumpMessageAsyncResult> action, string actionName)
			{
				try
				{
					try
					{
						this.userCallTimer.Set(this.owner.options.AutoRenewTimeout);
						action(this);
					}
					catch (Exception exception1)
					{
						Exception exception = exception1;
						if (Fx.IsFatal(exception))
						{
							throw;
						}
						this.ReportUserException(exception, actionName);
					}
				}
				finally
				{
					this.userCallTimer.Cancel();
				}
			}

			private static void ScheduleMessageCompletion(MessageSessionPump.PumpMessageAsyncResult result, BrokeredMessage messageToComplete)
			{
				if (result.owner.IsDisposed || result.session.IsClosedOrClosing || result.cancellationToken.IsCancellationRequested || !result.owner.IsOpenningOrOpened())
				{
					return;
				}
				MessageSessionPump.PumpMessageAsyncResult.BatchCompleteAsyncResult batchCompleteAsyncResult = null;
				lock (result.completionList)
				{
					if (messageToComplete != null)
					{
						result.completionList.Add(messageToComplete.LockToken);
					}
					if (!result.completionInProgress && result.completionList.Count > 0)
					{
						result.completionInProgress = true;
						batchCompleteAsyncResult = new MessageSessionPump.PumpMessageAsyncResult.BatchCompleteAsyncResult(result, MessageSessionPump.PumpMessageAsyncResult.MessageCompletionOperationTimeout, new AsyncCallback(MessageSessionPump.PumpMessageAsyncResult.OnCompleteMessageCompletion), null);
					}
				}
				if (batchCompleteAsyncResult != null)
				{
					IOThreadScheduler.ScheduleCallbackNoFlow((object o) => ((MessageSessionPump.PumpMessageAsyncResult.BatchCompleteAsyncResult)o).Start(), batchCompleteAsyncResult);
				}
			}

			private void ScheduleRenew()
			{
				try
				{
					MessageSessionPump.RenewSessionLockAsyncResult renewSessionLockAsyncResult = new MessageSessionPump.RenewSessionLockAsyncResult(this.trackingContext, this.owner, this.session, this.renewCancellationTokenSource.Token, MessageSessionPump.PumpMessageAsyncResult.StaticOnRenewSessionLockCompleted, this);
					renewSessionLockAsyncResult.Start();
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageSessionPumpRenewBeginFailed(this.trackingContext.Activity, this.trackingContext.TrackingId, this.trackingContext.SystemTracker, this.session.SessionId, exception.ToString()));
				}
			}

			private sealed class BatchCompleteAsyncResult : IteratorAsyncResult<MessageSessionPump.PumpMessageAsyncResult.BatchCompleteAsyncResult>
			{
				private readonly static TimeSpan completionBufferWaitTime;

				private readonly MessageSessionPump.PumpMessageAsyncResult owner;

				private readonly List<Guid> lockTokens;

				private bool exceptionHandled;

				public MessageSessionPump.PumpMessageAsyncResult Owner
				{
					get
					{
						return this.owner;
					}
				}

				static BatchCompleteAsyncResult()
				{
					MessageSessionPump.PumpMessageAsyncResult.BatchCompleteAsyncResult.completionBufferWaitTime = TimeSpan.FromMilliseconds(500);
				}

				public BatchCompleteAsyncResult(MessageSessionPump.PumpMessageAsyncResult owner, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
				{
					this.owner = owner;
					this.lockTokens = new List<Guid>();
				}

				protected override IEnumerator<IteratorAsyncResult<MessageSessionPump.PumpMessageAsyncResult.BatchCompleteAsyncResult>.AsyncStep> GetAsyncSteps()
				{
					List<Guid> guids = null;
					yield return base.CallAsyncSleep(MessageSessionPump.PumpMessageAsyncResult.BatchCompleteAsyncResult.completionBufferWaitTime);
					if (!this.owner.cancellationToken.IsCancellationRequested && !this.owner.session.IsClosedOrClosing && !this.owner.owner.IsDisposed && this.owner.owner.IsOpenningOrOpened())
					{
						this.Owner.owner.operationTracker.IncrementOperationCount();
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
							MessageSessionPump.PumpMessageAsyncResult.BatchCompleteAsyncResult batchCompleteAsyncResult = this;
							IteratorAsyncResult<MessageSessionPump.PumpMessageAsyncResult.BatchCompleteAsyncResult>.BeginCall beginCall = (MessageSessionPump.PumpMessageAsyncResult.BatchCompleteAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.owner.session.BeginComplete(thisPtr.lockTokens, c, s);
							yield return batchCompleteAsyncResult.CallAsync(beginCall, (MessageSessionPump.PumpMessageAsyncResult.BatchCompleteAsyncResult thisPtr, IAsyncResult r) => thisPtr.owner.session.EndComplete(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
							try
							{
								if (base.LastAsyncStepException == null)
								{
									goto Label0;
								}
								Exception lastAsyncStepException = base.LastAsyncStepException;
								MessageSessionPump.PumpMessageAsyncResult.BatchCompleteAsyncResult batchCompleteAsyncResult1 = this;
								IteratorAsyncResult<MessageSessionPump.PumpMessageAsyncResult.BatchCompleteAsyncResult>.BeginCall beginCall1 = (MessageSessionPump.PumpMessageAsyncResult.BatchCompleteAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => (new MessageSessionPump.PumpMessageAsyncResult.HandleExceptionAsyncResult(thisPtr.owner, "Complete", lastAsyncStepException, true, t, c, s)).Start();
								yield return batchCompleteAsyncResult1.CallAsync(beginCall1, (MessageSessionPump.PumpMessageAsyncResult.BatchCompleteAsyncResult thisPtr, IAsyncResult r) => thisPtr.exceptionHandled = AsyncResult<MessageSessionPump.PumpMessageAsyncResult.HandleExceptionAsyncResult>.End(r).Handled, IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
								if (this.exceptionHandled)
								{
									goto Label0;
								}
								base.Complete(lastAsyncStepException);
							}
							finally
							{
								this.Owner.owner.operationTracker.DecrementOperationCount();
							}
						}
						else
						{
							this.Owner.owner.operationTracker.DecrementOperationCount();
						}
					}
				Label0:
					yield break;
				}
			}

			private sealed class HandleExceptionAsyncResult : IteratorAsyncResult<MessageSessionPump.PumpMessageAsyncResult.HandleExceptionAsyncResult>
			{
				private readonly static Action<AsyncResult, Exception> StaticFinally;

				private readonly MessageSessionPump.PumpMessageAsyncResult owner;

				private readonly TrackingContext trackingContext;

				private readonly MessageSession session;

				private readonly string action;

				private readonly Exception exception;

				private readonly MessageSessionPump pump;

				private readonly CancellationTokenSource renewCancellationTokenSource;

				private readonly IMessageSessionAsyncHandler sessionHandler;

				private readonly bool closeSession;

				public bool Handled
				{
					get;
					private set;
				}

				static HandleExceptionAsyncResult()
				{
					MessageSessionPump.PumpMessageAsyncResult.HandleExceptionAsyncResult.StaticFinally = new Action<AsyncResult, Exception>(MessageSessionPump.PumpMessageAsyncResult.HandleExceptionAsyncResult.Finally);
				}

				public HandleExceptionAsyncResult(MessageSessionPump.PumpMessageAsyncResult owner, string action, Exception exception, TimeSpan timeout, AsyncCallback callback, object state) : this(owner, action, exception, false, timeout, callback, state)
				{
				}

				public HandleExceptionAsyncResult(MessageSessionPump.PumpMessageAsyncResult owner, string action, Exception exception, bool closeSession, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
				{
					this.action = action;
					this.exception = exception;
					this.owner = owner;
					this.trackingContext = owner.trackingContext;
					this.session = owner.session;
					this.pump = owner.owner;
					this.renewCancellationTokenSource = owner.renewCancellationTokenSource;
					this.sessionHandler = owner.sessionHandler;
					this.closeSession = closeSession;
					MessageSessionPump.PumpMessageAsyncResult.HandleExceptionAsyncResult handleExceptionAsyncResult = this;
					handleExceptionAsyncResult.OnCompleting = (Action<AsyncResult, Exception>)Delegate.Combine(handleExceptionAsyncResult.OnCompleting, MessageSessionPump.PumpMessageAsyncResult.HandleExceptionAsyncResult.StaticFinally);
				}

				private static void Finally(AsyncResult ar, Exception e)
				{
					if (e != null)
					{
						Fx.AssertAndFailFastService(e.ToString());
					}
				}

				protected override IEnumerator<IteratorAsyncResult<MessageSessionPump.PumpMessageAsyncResult.HandleExceptionAsyncResult>.AsyncStep> GetAsyncSteps()
				{
					MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageSessionPumpActionFailed(this.trackingContext.Activity, this.trackingContext.TrackingId, this.trackingContext.SystemTracker, this.session.SessionId, this.action, this.exception.ToString()));
					if (this.closeSession || !this.pump.IsOpenningOrOpened())
					{
						if (!this.session.IsClosed)
						{
							MessageSessionPump.PumpMessageAsyncResult.HandleExceptionAsyncResult handleExceptionAsyncResult = this;
							IteratorAsyncResult<MessageSessionPump.PumpMessageAsyncResult.HandleExceptionAsyncResult>.BeginCall beginCall = (MessageSessionPump.PumpMessageAsyncResult.HandleExceptionAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.session.BeginClose(c, s);
							yield return handleExceptionAsyncResult.CallAsync(beginCall, (MessageSessionPump.PumpMessageAsyncResult.HandleExceptionAsyncResult thisPtr, IAsyncResult r) => thisPtr.session.EndClose(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
							if (base.LastAsyncStepException != null)
							{
								Exception lastAsyncStepException = base.LastAsyncStepException;
								MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageSessionPumpSessionCloseFailed(this.trackingContext.Activity, this.trackingContext.TrackingId, this.trackingContext.SystemTracker, this.session.SessionId, lastAsyncStepException.ToString()));
								this.pump.RaiseExceptionRecieved(lastAsyncStepException, "CloseSession");
							}
						}
						if (this.pump.IsOpenningOrOpened())
						{
							goto Label1;
						}
						this.Handled = false;
						goto Label0;
					}
				Label1:
					this.pump.RaiseExceptionRecieved(this.exception, this.action);
					if (this.closeSession || MessageSessionPump.IndicatesSessionLost(this.exception) || MessageSessionPump.IsNonTransientException(this.exception))
					{
						try
						{
							if (!this.renewCancellationTokenSource.IsCancellationRequested)
							{
								this.renewCancellationTokenSource.Cancel();
							}
						}
						catch (ObjectDisposedException objectDisposedException)
						{
						}
						MessageSessionPump.PumpMessageAsyncResult.HandleExceptionAsyncResult handleExceptionAsyncResult1 = this;
						yield return handleExceptionAsyncResult1.CallTask((MessageSessionPump.PumpMessageAsyncResult.HandleExceptionAsyncResult thisPtr, TimeSpan t) => thisPtr.sessionHandler.OnSessionLostAsync(this.exception), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
						if (base.LastAsyncStepException != null)
						{
							this.owner.ReportUserException(base.LastAsyncStepException, "OnSessionLostAsync");
						}
						this.Handled = false;
					}
					else
					{
						this.Handled = true;
					}
				Label0:
					yield break;
				}
			}
		}

		private sealed class PumpSessionAsyncResult : IteratorAsyncResult<MessageSessionPump.PumpSessionAsyncResult>
		{
			private readonly MessageSessionPump owner;

			private readonly TrackingContext trackingContext;

			private MessageSession session;

			private CancellationToken cancellationToken;

			public PumpSessionAsyncResult(TrackingContext trackingContext, MessageSessionPump owner, MessageSession initialSession, CancellationToken cancellationToken, AsyncCallback callback, object state) : base(TimeSpan.MaxValue, callback, state)
			{
				this.trackingContext = trackingContext;
				this.owner = owner;
				this.session = initialSession;
				this.cancellationToken = cancellationToken;
			}

			protected override IEnumerator<IteratorAsyncResult<MessageSessionPump.PumpSessionAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				do
				{
					bool flag = false;
					try
					{
						if (this.session == null)
						{
							if (!this.owner.instanceSemaphore.TryEnter())
							{
								MessageSessionPump.PumpSessionAsyncResult pumpSessionAsyncResult = this;
								IteratorAsyncResult<MessageSessionPump.PumpSessionAsyncResult>.BeginCall beginCall = (MessageSessionPump.PumpSessionAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.owner.instanceSemaphore.BeginEnter(c, s);
								yield return pumpSessionAsyncResult.CallAsync(beginCall, (MessageSessionPump.PumpSessionAsyncResult thisPtr, IAsyncResult r) => thisPtr.owner.instanceSemaphore.EndEnter(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
							}
							flag = true;
							try
							{
								if (!this.owner.acceptSemaphore.TryEnter())
								{
									MessageSessionPump.PumpSessionAsyncResult pumpSessionAsyncResult1 = this;
									IteratorAsyncResult<MessageSessionPump.PumpSessionAsyncResult>.BeginCall beginCall1 = (MessageSessionPump.PumpSessionAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.owner.acceptSemaphore.BeginEnter(c, s);
									yield return pumpSessionAsyncResult1.CallAsync(beginCall1, (MessageSessionPump.PumpSessionAsyncResult thisPtr, IAsyncResult r) => thisPtr.owner.acceptSemaphore.EndEnter(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
								}
								this.session = null;
								MessageSessionPump.PumpSessionAsyncResult pumpSessionAsyncResult2 = this;
								IteratorAsyncResult<MessageSessionPump.PumpSessionAsyncResult>.BeginCall beginCall2 = (MessageSessionPump.PumpSessionAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.owner.Entity.BeginAcceptMessageSession(c, s);
								yield return pumpSessionAsyncResult2.CallAsync(beginCall2, (MessageSessionPump.PumpSessionAsyncResult thisPtr, IAsyncResult r) => thisPtr.session = thisPtr.owner.Entity.EndAcceptMessageSession(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
								if (base.LastAsyncStepException == null)
								{
									goto Label2;
								}
								Exception lastAsyncStepException = base.LastAsyncStepException;
								MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageSessionPumpAcceptSessionFailed(this.trackingContext.Activity, this.trackingContext.TrackingId, this.trackingContext.SystemTracker, lastAsyncStepException.ToStringSlim()));
								if (MessageSessionPump.PumpSessionAsyncResult.IndicatesEntityClosed(lastAsyncStepException))
								{
									goto Label0;
								}
								if (MessageSessionPump.IsNonTransientException(lastAsyncStepException))
								{
									yield return base.CallAsyncSleep(MessageSessionPump.NonTransientExceptionSleepAmount, this.cancellationToken);
								}
								if (lastAsyncStepException is TimeoutException)
								{
									goto Label1;
								}
								this.owner.RaiseExceptionRecieved(lastAsyncStepException, "AcceptMessageSession");
								goto Label1;
							}
							finally
							{
								this.owner.acceptSemaphore.Exit();
							}
						}
					Label2:
						this.owner.ScheduleMessagePump(this.session);
						flag = false;
						continue;
					}
					finally
					{
						this.session = null;
						if (flag)
						{
							this.owner.instanceSemaphore.Exit();
						}
					}
				Label1:
				}
				while (this.owner.IsOpenningOrOpened() && !this.owner.entity.IsClosed && !this.cancellationToken.IsCancellationRequested);
			Label3:
				yield break;
			Label0:
				goto Label3;
			}

			private static bool IndicatesEntityClosed(Exception e)
			{
				return e is OperationCanceledException;
			}
		}

		private sealed class RenewSessionLockAsyncResult : IteratorAsyncResult<MessageSessionPump.RenewSessionLockAsyncResult>
		{
			private readonly TrackingContext trackingContext;

			private readonly MessageSessionPump pump;

			private readonly MessageSession session;

			private CancellationToken cancellationToken;

			public RenewSessionLockAsyncResult(TrackingContext trackingContext, MessageSessionPump pump, MessageSession session, CancellationToken cancellationToken, AsyncCallback callback, object state) : base(TimeSpan.MaxValue, callback, state)
			{
				this.trackingContext = trackingContext;
				this.pump = pump;
				this.session = session;
				this.cancellationToken = cancellationToken;
			}

			private TimeSpan CalculateRenewAfterDuration()
			{
				TimeSpan lockedUntilUtc = this.session.LockedUntilUtc - DateTime.UtcNow;
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

			protected override IEnumerator<IteratorAsyncResult<MessageSessionPump.RenewSessionLockAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				while (this.pump.IsOpenningOrOpened() && !this.cancellationToken.IsCancellationRequested)
				{
					yield return base.CallAsyncSleep(this.CalculateRenewAfterDuration(), this.cancellationToken);
					if (this.cancellationToken.IsCancellationRequested)
					{
						break;
					}
					MessageSessionPump.RenewSessionLockAsyncResult renewSessionLockAsyncResult = this;
					IteratorAsyncResult<MessageSessionPump.RenewSessionLockAsyncResult>.BeginCall beginCall = (MessageSessionPump.RenewSessionLockAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.session.BeginRenewLock(c, s);
					yield return renewSessionLockAsyncResult.CallAsync(beginCall, (MessageSessionPump.RenewSessionLockAsyncResult thisPtr, IAsyncResult r) => thisPtr.session.EndRenewLock(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
					if (base.LastAsyncStepException == null)
					{
						continue;
					}
					Exception lastAsyncStepException = base.LastAsyncStepException;
					if (lastAsyncStepException is NotSupportedException)
					{
						this.pump.renewSupported = false;
						MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageSessionPumpRenewNotSupported(this.trackingContext.Activity, this.trackingContext.TrackingId, this.trackingContext.SystemTracker, this.session.SessionId, lastAsyncStepException.ToString()));
						break;
					}
					else if (!MessageSessionPump.IndicatesSessionLost(lastAsyncStepException))
					{
						if (!this.session.IsClosedOrClosing)
						{
							this.pump.RaiseExceptionRecieved(lastAsyncStepException, "RenewLock");
						}
						MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageSessionPumpRenewFailed(this.trackingContext.Activity, this.trackingContext.TrackingId, this.trackingContext.SystemTracker, this.session.SessionId, lastAsyncStepException.ToString()));
					}
					else
					{
						MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.MessageSessionPumpRenewDetectedSessionLost(this.trackingContext.Activity, this.trackingContext.TrackingId, this.trackingContext.SystemTracker, this.session.SessionId, lastAsyncStepException.ToString()));
						break;
					}
				}
			}
		}
	}
}