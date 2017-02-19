using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Messaging.Channels
{
	internal sealed class ServiceBusInputSessionChannelListener : ServiceBusChannelListener<IInputSessionChannel>
	{
		private readonly static TimeSpan InitialRetrySleepTime;

		private readonly static TimeSpan MaxRetrySleepTime;

		private object retryTimeoutLock = new object();

		private TimeSpan retryTimeSpan;

		static ServiceBusInputSessionChannelListener()
		{
			ServiceBusInputSessionChannelListener.InitialRetrySleepTime = TimeSpan.FromMilliseconds(500);
			ServiceBusInputSessionChannelListener.MaxRetrySleepTime = TimeSpan.FromSeconds(60);
		}

		public ServiceBusInputSessionChannelListener(BindingContext context, NetMessagingTransportBindingElement transport) : base(context, transport)
		{
			this.retryTimeSpan = ServiceBusInputSessionChannelListener.InitialRetrySleepTime;
		}

		private CommunicationException HandleMessagingException(MessagingException messagingException)
		{
			bool flag;
			CommunicationException communicationException = MessagingExceptionHelper.ConvertToCommunicationException(messagingException, out flag);
			if (communicationException is CommunicationObjectFaultedException || communicationException is CommunicationObjectAbortedException)
			{
				communicationException = new CommunicationException(messagingException.Message, messagingException);
			}
			return communicationException;
		}

		public void IncrementRetryTimeSpan(TimeSpan currentRetryTimeSpan)
		{
			if (currentRetryTimeSpan == this.retryTimeSpan && this.retryTimeSpan < ServiceBusInputSessionChannelListener.MaxRetrySleepTime)
			{
				lock (this.retryTimeoutLock)
				{
					if (currentRetryTimeSpan == this.retryTimeSpan && this.retryTimeSpan < ServiceBusInputSessionChannelListener.MaxRetrySleepTime)
					{
						ServiceBusInputSessionChannelListener serviceBusInputSessionChannelListener = this;
						serviceBusInputSessionChannelListener.retryTimeSpan = serviceBusInputSessionChannelListener.retryTimeSpan + this.retryTimeSpan;
						if (this.retryTimeSpan > ServiceBusInputSessionChannelListener.MaxRetrySleepTime)
						{
							this.retryTimeSpan = ServiceBusInputSessionChannelListener.MaxRetrySleepTime;
						}
					}
				}
			}
		}

		protected override IInputSessionChannel OnAcceptChannel(TimeSpan timeout)
		{
			ServiceBusInputSessionChannelListener.AcceptChannelAsyncResult acceptChannelAsyncResult = new ServiceBusInputSessionChannelListener.AcceptChannelAsyncResult(this, this.retryTimeSpan, timeout, null, null);
			return acceptChannelAsyncResult.RunSynchronously().Channel;
		}

		protected override IAsyncResult OnBeginAcceptChannel(TimeSpan timeout, AsyncCallback callback, object state)
		{
			TimeSpan timeSpan = this.retryTimeSpan;
			return (new ServiceBusInputSessionChannelListener.AcceptChannelAsyncResult(this, timeSpan, timeout, callback, state)).Start();
		}

		protected override IAsyncResult OnBeginWaitForChannel(TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new NotSupportedException(), null);
		}

		protected override IInputSessionChannel OnEndAcceptChannel(IAsyncResult result)
		{
			return AsyncResult<ServiceBusInputSessionChannelListener.AcceptChannelAsyncResult>.End(result).Channel;
		}

		protected override bool OnEndWaitForChannel(IAsyncResult result)
		{
			throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new NotSupportedException(), null);
		}

		private CommunicationException OnException(Exception exception)
		{
			if (Fx.IsFatal(exception))
			{
				return null;
			}
			MessagingException messagingException = exception as MessagingException;
			MessagingException messagingException1 = messagingException;
			if (messagingException != null)
			{
				return this.HandleMessagingException(messagingException1);
			}
			if (exception is OperationCanceledException)
			{
				MessagingException innerException = exception.InnerException as MessagingException;
				if (innerException != null)
				{
					return this.HandleMessagingException(innerException);
				}
			}
			return null;
		}

		protected override bool OnWaitForChannel(TimeSpan timeout)
		{
			throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new NotSupportedException(), null);
		}

		public void ResetRetryTimeSpan()
		{
			if (this.retryTimeSpan == ServiceBusInputSessionChannelListener.InitialRetrySleepTime)
			{
				return;
			}
			lock (this.retryTimeoutLock)
			{
				this.retryTimeSpan = ServiceBusInputSessionChannelListener.InitialRetrySleepTime;
			}
		}

		private class AcceptChannelAsyncResult : IteratorAsyncResult<ServiceBusInputSessionChannelListener.AcceptChannelAsyncResult>
		{
			private readonly ServiceBusInputSessionChannelListener owner;

			private readonly TimeSpan acceptMessageSessionTimeout;

			private readonly TimeSpan retryTimeSpan;

			public ServiceBusInputSessionChannel Channel
			{
				get;
				private set;
			}

			public AcceptChannelAsyncResult(ServiceBusInputSessionChannelListener owner, TimeSpan retryTimeSpan, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.owner = owner;
				this.retryTimeSpan = retryTimeSpan;
				this.acceptMessageSessionTimeout = (base.OriginalTimeout == TimeSpan.MaxValue ? this.owner.DefaultReceiveTimeout : timeout);
				this.Channel = null;
			}

			protected override IEnumerator<IteratorAsyncResult<ServiceBusInputSessionChannelListener.AcceptChannelAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				if (!this.owner.DoneReceivingInCurrentState())
				{
					Stopwatch stopwatch = Stopwatch.StartNew();
					ServiceBusInputSessionChannelListener.AcceptChannelAsyncResult acceptChannelAsyncResult = this;
					IteratorAsyncResult<ServiceBusInputSessionChannelListener.AcceptChannelAsyncResult>.BeginCall beginCall = (ServiceBusInputSessionChannelListener.AcceptChannelAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.owner.MessagingFactory.BeginAcceptMessageSession(this.owner.MessagingAddress.EntityName, null, this.owner.ReceiveMode, thisPtr.acceptMessageSessionTimeout, thisPtr.acceptMessageSessionTimeout, c, s);
					yield return acceptChannelAsyncResult.CallAsync(beginCall, (ServiceBusInputSessionChannelListener.AcceptChannelAsyncResult thisPtr, IAsyncResult r) => thisPtr.Channel = new ServiceBusInputSessionChannel(thisPtr.owner.MessagingFactory.EndAcceptMessageSession(r), thisPtr.owner), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
					stopwatch.Stop();
					if (base.LastAsyncStepException != null)
					{
						Exception lastAsyncStepException = this.owner.OnException(base.LastAsyncStepException);
						if (lastAsyncStepException == null)
						{
							lastAsyncStepException = base.LastAsyncStepException;
						}
						Exception exception = lastAsyncStepException;
						long ticks = base.RemainingTime().Ticks;
						long num = this.retryTimeSpan.Ticks;
						TimeSpan elapsed = stopwatch.Elapsed;
						TimeSpan timeSpan = TimeSpan.FromTicks(Math.Min(ticks, num - elapsed.Ticks));
						if (timeSpan > TimeSpan.Zero)
						{
							yield return base.CallAsyncSleep(timeSpan);
						}
						this.owner.IncrementRetryTimeSpan(this.retryTimeSpan);
						base.Complete(exception);
					}
					else
					{
						this.owner.ResetRetryTimeSpan();
						base.Complete(null);
					}
				}
				else
				{
					base.Complete(null);
				}
			}
		}
	}
}