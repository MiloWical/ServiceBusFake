using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Common.Parallel;
using Microsoft.ServiceBus.PerformanceCounters;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Transactions;

namespace Microsoft.ServiceBus.Messaging
{
	public abstract class MessageSession : MessageReceiver
	{
		private MessageReceiver innerReceiver;

		internal MessageReceiver InnerMessageReceiver
		{
			get
			{
				return this.innerReceiver;
			}
			set
			{
				this.innerReceiver = value;
				base.InstanceTrackingContext = value.InstanceTrackingContext;
			}
		}

		public override long LastPeekedSequenceNumber
		{
			get
			{
				if (this.innerReceiver != null)
				{
					return this.innerReceiver.LastPeekedSequenceNumber;
				}
				return Constants.DefaultLastPeekedSequenceNumber - (long)1;
			}
			internal set
			{
				if (this.innerReceiver == null)
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("innerReceiver");
				}
				this.innerReceiver.LastPeekedSequenceNumber = value;
			}
		}

		public DateTime LockedUntilUtc
		{
			get;
			protected set;
		}

		public override string Path
		{
			get
			{
				return this.innerReceiver.Path;
			}
		}

		public override int PrefetchCount
		{
			get
			{
				return this.innerReceiver.PrefetchCount;
			}
			set
			{
				this.innerReceiver.PrefetchCount = value;
			}
		}

		public virtual string SessionId
		{
			get;
			protected set;
		}

		protected internal override bool SupportsGetRuntimeEntityDescription
		{
			get
			{
				return this.innerReceiver.SupportsGetRuntimeEntityDescription;
			}
		}

		internal MessageSession(ReceiveMode receiveMode, string sessionId, DateTime lockedUntilUtc, MessageReceiver innerReceiver) : base(innerReceiver.MessagingFactory, innerReceiver.RetryPolicy, receiveMode, null)
		{
			if (innerReceiver == null)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("innerReceiver");
			}
			this.SessionId = sessionId;
			this.LockedUntilUtc = lockedUntilUtc;
			this.InnerMessageReceiver = innerReceiver;
		}

		internal MessageSession(ReceiveMode receiveMode, string sessionId, DateTime lockedUntilUtc, Microsoft.ServiceBus.Messaging.MessagingFactory messagingFactory, Microsoft.ServiceBus.RetryPolicy retryPolicy) : base(messagingFactory, retryPolicy, receiveMode, null)
		{
			this.SessionId = sessionId;
			this.LockedUntilUtc = lockedUntilUtc;
		}

		private void AbortInnerReceiverAndFault()
		{
			if (this.InnerMessageReceiver != null)
			{
				this.InnerMessageReceiver.Abort();
			}
			base.Fault();
		}

		public IAsyncResult BeginGetState(AsyncCallback callback, object state)
		{
			IAsyncResult asyncResult;
			base.ThrowIfDisposed();
			try
			{
				TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), this.Path);
				MessageSession.TraceGetState(EventTraceActivity.CreateFromThread(), instance, this.SessionId);
				MessageSession.RetrySessionAsyncResult retrySessionAsyncResult = new MessageSession.RetrySessionAsyncResult(this, instance, MessageSession.SessionOperation.GetState, null, this.OperationTimeout, callback, state);
				retrySessionAsyncResult.Start();
				asyncResult = retrySessionAsyncResult;
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(exception, null);
				}
				throw;
			}
			return asyncResult;
		}

		public IAsyncResult BeginRenewLock(AsyncCallback callback, object state)
		{
			IAsyncResult asyncResult;
			base.ThrowIfDisposed();
			try
			{
				TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), this.Path);
				MessageSession.TraceRenewLock(EventTraceActivity.CreateFromThread(), instance);
				MessageSession.RetrySessionAsyncResult retrySessionAsyncResult = new MessageSession.RetrySessionAsyncResult(this, instance, MessageSession.SessionOperation.RenewSessionLock, null, this.OperationTimeout, callback, state);
				retrySessionAsyncResult.Start();
				asyncResult = retrySessionAsyncResult;
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(exception, null);
				}
				throw;
			}
			return asyncResult;
		}

		public IAsyncResult BeginSetState(Stream stream, AsyncCallback callback, object state)
		{
			IAsyncResult asyncResult;
			base.ThrowIfDisposed();
			try
			{
				TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), this.Path);
				MessageSession.TraceSetState(EventTraceActivity.CreateFromThread(), instance, this.SessionId);
				MessageSession.RetrySessionAsyncResult retrySessionAsyncResult = new MessageSession.RetrySessionAsyncResult(this, instance, MessageSession.SessionOperation.SetState, stream, this.OperationTimeout, callback, state);
				retrySessionAsyncResult.Start();
				asyncResult = retrySessionAsyncResult;
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(exception, null);
				}
				throw;
			}
			return asyncResult;
		}

		public Stream EndGetState(IAsyncResult result)
		{
			Stream stateEnd;
			base.ThrowIfDisposed();
			try
			{
				stateEnd = MessageSession.RetrySessionAsyncResult.GetStateEnd(result);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(exception, null);
				}
				throw;
			}
			return stateEnd;
		}

		public void EndRenewLock(IAsyncResult result)
		{
			base.ThrowIfDisposed();
			try
			{
				this.LockedUntilUtc = MessageSession.RetrySessionAsyncResult.RenewLockEnd(result);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(exception, null);
				}
				throw;
			}
		}

		public void EndSetState(IAsyncResult result)
		{
			base.ThrowIfDisposed();
			try
			{
				MessageSession.RetrySessionAsyncResult.SetStateEnd(result);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(exception, null);
				}
				throw;
			}
		}

		public Stream GetState()
		{
			Stream state;
			base.ThrowIfDisposed();
			try
			{
				state = this.GetState(null);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(exception, null);
				}
				throw;
			}
			return state;
		}

		private Stream GetState(TrackingContext trackingContext)
		{
			if (trackingContext == null)
			{
				trackingContext = TrackingContext.GetInstance(Guid.NewGuid(), this.Path);
			}
			MessageSession.TraceGetState(EventTraceActivity.CreateFromThread(), trackingContext, this.SessionId);
			return this.OnGetState(trackingContext, this.OperationTimeout);
		}

		public Task<Stream> GetStateAsync()
		{
			return TaskHelpers.CreateTask<Stream>(new Func<AsyncCallback, object, IAsyncResult>(this.BeginGetState), new Func<IAsyncResult, Stream>(this.EndGetState));
		}

		private void InnerReceiverFaulted(object sender, EventArgs args)
		{
			base.Fault(this.innerReceiver.GetPendingException());
		}

		protected override void OnAbandon(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, IDictionary<string, object> propertiesToModify, TimeSpan timeout)
		{
			base.ThrowIfDisposed();
			try
			{
				this.innerReceiver.Abandon(trackingContext, lockTokens, propertiesToModify, timeout);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(this.ProcessException(exception), null);
				}
				throw;
			}
		}

		protected override void OnAbort()
		{
			if (this.innerReceiver != null)
			{
				this.innerReceiver.Abort();
			}
		}

		protected override IAsyncResult OnBeginAbandon(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, IDictionary<string, object> propertiesToModify, bool fromSync, TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult asyncResult;
			base.ThrowIfDisposed();
			try
			{
				asyncResult = this.innerReceiver.BeginAbandon(trackingContext, lockTokens, propertiesToModify, timeout, callback, state);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(this.ProcessException(exception), null);
				}
				throw;
			}
			return asyncResult;
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (this.innerReceiver == null)
			{
				return new CompletedAsyncResult(callback, state);
			}
			return this.innerReceiver.BeginClose(callback, state);
		}

		protected override IAsyncResult OnBeginComplete(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, bool fromSync, TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult asyncResult;
			base.ThrowIfDisposed();
			try
			{
				asyncResult = this.innerReceiver.BeginComplete(trackingContext, lockTokens, timeout, callback, state);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(this.ProcessException(exception), null);
				}
				throw;
			}
			return asyncResult;
		}

		protected override IAsyncResult OnBeginComplete(TrackingContext trackingContext, IEnumerable<ArraySegment<byte>> deliveryTags, bool fromSync, TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw Fx.Exception.AsError(new NotSupportedException(SRClient.EventHubUnsupportedOperation("Sessionful MessageComplete")), null);
		}

		protected override IAsyncResult OnBeginDeadLetter(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, IDictionary<string, object> propertiesToModify, string deadLetterReason, string deadLetterErrorDescription, bool fromSync, TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult asyncResult;
			base.ThrowIfDisposed();
			try
			{
				asyncResult = this.innerReceiver.BeginDeadLetter(trackingContext, lockTokens, propertiesToModify, deadLetterReason, deadLetterErrorDescription, timeout, callback, state);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(this.ProcessException(exception), null);
				}
				throw;
			}
			return asyncResult;
		}

		protected override IAsyncResult OnBeginDefer(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, IDictionary<string, object> propertiesToModify, bool fromSync, TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult asyncResult;
			base.ThrowIfDisposed();
			try
			{
				asyncResult = this.innerReceiver.BeginDefer(trackingContext, lockTokens, propertiesToModify, timeout, callback, state);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(this.ProcessException(exception), null);
				}
				throw;
			}
			return asyncResult;
		}

		internal override IAsyncResult OnBeginGetRuntimeEntityDescriptionAsyncResult(TrackingContext trackingContext, TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult asyncResult;
			base.ThrowIfDisposed();
			try
			{
				asyncResult = this.innerReceiver.BeginGetRuntimeEntityDescriptionAsyncResult(trackingContext, timeout, callback, state);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(this.ProcessException(exception), null);
				}
				throw;
			}
			return asyncResult;
		}

		protected abstract IAsyncResult OnBeginGetState(TrackingContext trackingContext, TimeSpan timeout, AsyncCallback callback, object state);

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (this.innerReceiver != null)
			{
				this.innerReceiver.SafeAddFaulted(new EventHandler(this.InnerReceiverFaulted));
			}
			base.ThrowIfFaulted();
			return new CompletedAsyncResult(callback, state);
		}

		protected override IAsyncResult OnBeginPeek(TrackingContext trackingContext, long fromSequenceNumber, int messageCount, TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult asyncResult;
			base.ThrowIfDisposed();
			try
			{
				asyncResult = this.innerReceiver.BeginPeekBatch(trackingContext, fromSequenceNumber, messageCount, timeout, callback, state);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(this.ProcessException(exception), null);
				}
				throw;
			}
			return asyncResult;
		}

		protected abstract IAsyncResult OnBeginRenewLock(TrackingContext trackingContext, TimeSpan timeout, AsyncCallback callback, object state);

		protected override IAsyncResult OnBeginRenewMessageLocks(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, bool fromSync, TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult asyncResult;
			base.ThrowIfDisposed();
			try
			{
				asyncResult = this.innerReceiver.BeginRenewMessageLocks(trackingContext, lockTokens, timeout, callback, state);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(this.ProcessException(exception), null);
				}
				throw;
			}
			return asyncResult;
		}

		protected abstract IAsyncResult OnBeginSetState(TrackingContext trackingContext, Stream stream, TimeSpan timeout, AsyncCallback callback, object state);

		protected override IAsyncResult OnBeginTryReceive(TrackingContext trackingContext, int messageCount, TimeSpan serverWaitTime, AsyncCallback callback, object state)
		{
			IAsyncResult asyncResult;
			base.ThrowIfDisposed();
			try
			{
				asyncResult = this.innerReceiver.BeginTryReceive(trackingContext, messageCount, serverWaitTime, callback, state);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(this.ProcessException(exception), null);
				}
				throw;
			}
			return asyncResult;
		}

		protected override IAsyncResult OnBeginTryReceive(TrackingContext trackingContext, IEnumerable<long> receipts, TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult asyncResult;
			base.ThrowIfDisposed();
			try
			{
				asyncResult = this.innerReceiver.BeginTryReceive(trackingContext, receipts, timeout, callback, state);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(this.ProcessException(exception), null);
				}
				throw;
			}
			return asyncResult;
		}

		protected override IAsyncResult OnBeginTryReceive2(TrackingContext trackingContext, int messageCount, TimeSpan serverWaitTime, AsyncCallback callback, object state)
		{
			IAsyncResult asyncResult;
			base.ThrowIfDisposed();
			try
			{
				asyncResult = this.innerReceiver.BeginTryReceive2(trackingContext, messageCount, serverWaitTime, callback, state);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(this.ProcessException(exception), null);
				}
				throw;
			}
			return asyncResult;
		}

		protected override void OnComplete(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, TimeSpan timeout)
		{
			base.ThrowIfDisposed();
			try
			{
				this.innerReceiver.Complete(trackingContext, lockTokens, timeout);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(this.ProcessException(exception), null);
				}
				throw;
			}
		}

		protected override void OnDeadLetter(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, IDictionary<string, object> propertiesToModify, string deadLetterReason, string deadLetterErrorDescription, TimeSpan timeout)
		{
			base.ThrowIfDisposed();
			try
			{
				this.innerReceiver.DeadLetter(trackingContext, lockTokens, propertiesToModify, deadLetterReason, deadLetterErrorDescription, timeout);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(this.ProcessException(exception), null);
				}
				throw;
			}
		}

		protected override void OnDefer(TrackingContext trackingContext, IEnumerable<Guid> lockTokens, IDictionary<string, object> propertiesToModify, TimeSpan timeout)
		{
			base.ThrowIfDisposed();
			try
			{
				this.innerReceiver.Defer(trackingContext, lockTokens, propertiesToModify, timeout);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(this.ProcessException(exception), null);
				}
				throw;
			}
		}

		protected override void OnEndAbandon(IAsyncResult result)
		{
			base.ThrowIfDisposed();
			try
			{
				this.innerReceiver.EndAbandon(result);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(this.ProcessException(exception), null);
				}
				throw;
			}
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			if (result is CompletedAsyncResult)
			{
				CompletedAsyncResult.End(result);
				return;
			}
			this.innerReceiver.EndClose(result);
		}

		protected override void OnEndComplete(IAsyncResult result)
		{
			base.ThrowIfDisposed();
			try
			{
				this.innerReceiver.EndComplete(result);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(this.ProcessException(exception), null);
				}
				throw;
			}
		}

		protected override void OnEndDeadLetter(IAsyncResult result)
		{
			base.ThrowIfDisposed();
			try
			{
				this.innerReceiver.EndDeadLetter(result);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(this.ProcessException(exception), null);
				}
				throw;
			}
		}

		protected override void OnEndDefer(IAsyncResult result)
		{
			base.ThrowIfDisposed();
			try
			{
				this.innerReceiver.EndDefer(result);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(this.ProcessException(exception), null);
				}
				throw;
			}
		}

		internal override Microsoft.ServiceBus.Messaging.RuntimeEntityDescription OnEndGetRuntimeEntityDescriptionAsyncResult(IAsyncResult result)
		{
			Microsoft.ServiceBus.Messaging.RuntimeEntityDescription runtimeEntityDescription;
			base.ThrowIfDisposed();
			try
			{
				runtimeEntityDescription = this.innerReceiver.EndGetRuntimeEntityDescriptionAsyncResult(result);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(this.ProcessException(exception), null);
				}
				throw;
			}
			return runtimeEntityDescription;
		}

		protected abstract Stream OnEndGetState(IAsyncResult result);

		protected override void OnEndOpen(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
		}

		protected override IEnumerable<BrokeredMessage> OnEndPeek(IAsyncResult result)
		{
			IEnumerable<BrokeredMessage> brokeredMessages;
			base.ThrowIfDisposed();
			try
			{
				brokeredMessages = this.innerReceiver.EndPeekBatch(result);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(this.ProcessException(exception), null);
				}
				throw;
			}
			return brokeredMessages;
		}

		protected abstract DateTime OnEndRenewLock(IAsyncResult result);

		protected override IEnumerable<DateTime> OnEndRenewMessageLocks(IAsyncResult result)
		{
			IEnumerable<DateTime> dateTimes;
			base.ThrowIfDisposed();
			try
			{
				dateTimes = this.innerReceiver.EndRenewMessageLocks(result);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(this.ProcessException(exception), null);
				}
				throw;
			}
			return dateTimes;
		}

		protected abstract void OnEndSetState(IAsyncResult result);

		protected override bool OnEndTryReceive(IAsyncResult result, out IEnumerable<BrokeredMessage> messages)
		{
			bool flag;
			base.ThrowIfDisposed();
			try
			{
				flag = this.innerReceiver.EndTryReceive(result, out messages);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(this.ProcessException(exception), null);
				}
				throw;
			}
			return flag;
		}

		protected override bool OnEndTryReceive2(IAsyncResult result, out IEnumerable<BrokeredMessage> messages)
		{
			bool flag;
			base.ThrowIfDisposed();
			try
			{
				flag = this.innerReceiver.EndTryReceive2(result, out messages);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(this.ProcessException(exception), null);
				}
				throw;
			}
			return flag;
		}

		protected virtual Stream OnGetState(TrackingContext trackingContext, TimeSpan timeout)
		{
			MessageSession.RetrySessionAsyncResult retrySessionAsyncResult = new MessageSession.RetrySessionAsyncResult(this, trackingContext, MessageSession.SessionOperation.GetState, null, this.OperationTimeout, null, null);
			retrySessionAsyncResult.RunSynchronously();
			return retrySessionAsyncResult.SessionState;
		}

		protected virtual DateTime OnRenewLock(TrackingContext trackingContext, TimeSpan timeout)
		{
			MessageSession.RetrySessionAsyncResult retrySessionAsyncResult = new MessageSession.RetrySessionAsyncResult(this, trackingContext, MessageSession.SessionOperation.RenewSessionLock, null, this.OperationTimeout, null, null);
			retrySessionAsyncResult.RunSynchronously();
			return retrySessionAsyncResult.LockedUntilUtcTime;
		}

		protected virtual void OnSetState(TrackingContext trackingContext, Stream stream, TimeSpan timeout)
		{
			MessageSession.RetrySessionAsyncResult retrySessionAsyncResult = new MessageSession.RetrySessionAsyncResult(this, trackingContext, MessageSession.SessionOperation.SetState, stream, timeout, null, null);
			retrySessionAsyncResult.RunSynchronously();
		}

		protected override bool OnTryReceive(TrackingContext trackingContext, IEnumerable<long> receipts, TimeSpan timeout, out IEnumerable<BrokeredMessage> messages)
		{
			bool flag;
			base.ThrowIfDisposed();
			try
			{
				flag = this.innerReceiver.TryReceive(trackingContext, receipts, timeout, out messages);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(this.ProcessException(exception), null);
				}
				throw;
			}
			return flag;
		}

		protected override bool OnTryReceive(TrackingContext trackingContext, int messageCount, TimeSpan serverWaitTime, out IEnumerable<BrokeredMessage> messages)
		{
			bool flag;
			base.ThrowIfDisposed();
			try
			{
				flag = this.innerReceiver.TryReceive(trackingContext, messageCount, serverWaitTime, out messages);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(this.ProcessException(exception), null);
				}
				throw;
			}
			return flag;
		}

		private Exception ProcessException(Exception exception)
		{
			if (base.IsClosedOrClosing)
			{
				return new OperationCanceledException(SRClient.EntityClosedOrAborted, exception);
			}
			if (!(exception is CommunicationException))
			{
				if (exception is SessionLockLostException)
				{
					this.AbortInnerReceiverAndFault();
				}
				return exception;
			}
			Exception exception1 = MessagingExceptionHelper.Unwrap((CommunicationException)exception, base.IsClosedOrClosing);
			if (exception1 is SessionLockLostException)
			{
				this.AbortInnerReceiverAndFault();
			}
			return exception1;
		}

		public void RenewLock()
		{
			base.ThrowIfDisposed();
			try
			{
				TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), this.Path);
				MessageSession.TraceRenewLock(EventTraceActivity.CreateFromThread(), instance);
				this.LockedUntilUtc = this.OnRenewLock(instance, this.OperationTimeout);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(exception, null);
				}
				throw;
			}
		}

		public Task RenewLockAsync()
		{
			return TaskHelpers.CreateTask(new Func<AsyncCallback, object, IAsyncResult>(this.BeginRenewLock), new Action<IAsyncResult>(this.EndRenewLock));
		}

		public void SetState(Stream stream)
		{
			base.ThrowIfDisposed();
			try
			{
				this.SetState(null, stream);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(exception, null);
				}
				throw;
			}
		}

		private void SetState(TrackingContext trackingContext, Stream stream)
		{
			if (trackingContext == null)
			{
				trackingContext = TrackingContext.GetInstance(Guid.NewGuid(), this.Path);
			}
			MessageSession.TraceSetState(EventTraceActivity.CreateFromThread(), trackingContext, this.SessionId);
			this.OnSetState(trackingContext, stream, this.OperationTimeout);
		}

		public Task SetStateAsync(Stream stream)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginSetState(stream, c, s), new Action<IAsyncResult>(this.EndSetState));
		}

		private static void TraceGetState(EventTraceActivity fromActivity, TrackingContext trackingContext, string sessionId)
		{
			if (trackingContext != null && fromActivity != null && fromActivity != EventTraceActivity.Empty)
			{
				MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.GetStateTransfer(fromActivity, trackingContext.Activity, sessionId));
			}
		}

		private static void TraceRenewLock(EventTraceActivity fromActivity, TrackingContext trackingContext)
		{
			if (trackingContext != null && fromActivity != null && fromActivity != EventTraceActivity.Empty)
			{
				MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.RenewSessionLockTransfer(fromActivity, trackingContext.Activity));
			}
		}

		private static void TraceSetState(EventTraceActivity fromActivity, TrackingContext trackingContext, string sessionId)
		{
			if (trackingContext != null && fromActivity != null && fromActivity != EventTraceActivity.Empty)
			{
				MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.SetStateTransfer(fromActivity, trackingContext.Activity, sessionId));
			}
		}

		private sealed class RetrySessionAsyncResult : RetryAsyncResult<MessageSession.RetrySessionAsyncResult>
		{
			private readonly MessageSession session;

			private readonly TrackingContext trackingContext;

			private readonly MessageSession.SessionOperation operation;

			private readonly Stream stream;

			public DateTime LockedUntilUtcTime
			{
				get;
				private set;
			}

			public Stream SessionState
			{
				get;
				private set;
			}

			public RetrySessionAsyncResult(MessageSession session, TrackingContext trackingContext, MessageSession.SessionOperation operation, Stream stream, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				if (trackingContext == null)
				{
					throw Fx.Exception.ArgumentNull("trackingContext");
				}
				if (session == null)
				{
					throw Fx.Exception.ArgumentNull("session");
				}
				this.session = session;
				this.operation = operation;
				this.stream = stream;
				this.trackingContext = trackingContext;
			}

			protected override IEnumerator<IteratorAsyncResult<MessageSession.RetrySessionAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				TimeSpan timeSpan;
				bool flag;
				bool flag1;
				int num = 0;
				timeSpan = (this.session.RetryPolicy.IsServerBusy ? Microsoft.ServiceBus.RetryPolicy.ServerBusyBaseSleepTime : TimeSpan.Zero);
				TimeSpan zero = timeSpan;
				if (!this.session.RetryPolicy.IsServerBusy || !(Microsoft.ServiceBus.RetryPolicy.ServerBusyBaseSleepTime >= base.OriginalTimeout))
				{
					do
					{
						flag1 = false;
						if (zero != TimeSpan.Zero)
						{
							yield return base.CallAsyncSleep(zero);
						}
						switch (this.operation)
						{
							case MessageSession.SessionOperation.GetState:
							{
								MessageSession.RetrySessionAsyncResult retrySessionAsyncResult = this;
								Transaction ambientTransaction = base.AmbientTransaction;
								IteratorAsyncResult<MessageSession.RetrySessionAsyncResult>.BeginCall beginCall = (MessageSession.RetrySessionAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.session.OnBeginGetState(thisPtr.trackingContext, t, c, s);
								yield return retrySessionAsyncResult.CallTransactionalAsync(ambientTransaction, beginCall, (MessageSession.RetrySessionAsyncResult thisPtr, IAsyncResult r) => thisPtr.SessionState = thisPtr.session.OnEndGetState(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
								break;
							}
							case MessageSession.SessionOperation.SetState:
							{
								if (this.stream != null && this.stream.CanSeek && this.stream.Position != (long)0)
								{
									this.stream.Position = (long)0;
								}
								MessageSession.RetrySessionAsyncResult retrySessionAsyncResult1 = this;
								Transaction transaction = base.AmbientTransaction;
								IteratorAsyncResult<MessageSession.RetrySessionAsyncResult>.BeginCall beginCall1 = (MessageSession.RetrySessionAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.session.OnBeginSetState(thisPtr.trackingContext, thisPtr.stream, t, c, s);
								yield return retrySessionAsyncResult1.CallTransactionalAsync(transaction, beginCall1, (MessageSession.RetrySessionAsyncResult thisPtr, IAsyncResult r) => thisPtr.session.OnEndSetState(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
								break;
							}
							case MessageSession.SessionOperation.RenewSessionLock:
							{
								MessageSession.RetrySessionAsyncResult retrySessionAsyncResult2 = this;
								IteratorAsyncResult<MessageSession.RetrySessionAsyncResult>.BeginCall beginCall2 = (MessageSession.RetrySessionAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.session.OnBeginRenewLock(thisPtr.trackingContext, t, c, s);
								yield return retrySessionAsyncResult2.CallAsync(beginCall2, (MessageSession.RetrySessionAsyncResult thisPtr, IAsyncResult r) => thisPtr.LockedUntilUtcTime = thisPtr.session.OnEndRenewLock(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
								break;
							}
						}
						if (base.LastAsyncStepException == null)
						{
							this.session.RetryPolicy.ResetServerBusy();
						}
						else
						{
							base.LastAsyncStepException = this.session.ProcessException(base.LastAsyncStepException);
							MessagingPerformanceCounters.IncrementExceptionPerSec(this.session.MessagingFactory.Address, 1, base.LastAsyncStepException);
							flag = (base.TransactionExists ? false : this.session.RetryPolicy.ShouldRetry(base.RemainingTime(), num, base.LastAsyncStepException, out zero));
							flag1 = flag;
							if (this.operation == MessageSession.SessionOperation.SetState && this.stream != null && !this.stream.CanSeek)
							{
								flag1 = false;
								zero = TimeSpan.Zero;
								MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRetryPolicyStreamNotSeekable(this.trackingContext.Activity, this.trackingContext.TrackingId, this.session.RetryPolicy.GetType().Name, this.operation.ToString()));
							}
							if (!flag1)
							{
								continue;
							}
							MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRetryPolicyIteration(this.trackingContext.Activity, this.trackingContext.TrackingId, this.session.RetryPolicy.GetType().Name, this.operation.ToString(), num, zero.ToString(), this.LastAsyncStepException.GetType().FullName, this.LastAsyncStepException.Message));
							num++;
						}
					}
					while (flag1);
					base.Complete(base.LastAsyncStepException);
				}
				else
				{
					string str = this.session.RetryPolicy.ServerBusyExceptionMessage;
					yield return base.CallAsyncSleep(base.RemainingTime());
					base.Complete(new ServerBusyException(str, this.trackingContext));
				}
			}

			public static Stream GetStateEnd(IAsyncResult r)
			{
				return AsyncResult<MessageSession.RetrySessionAsyncResult>.End(r).SessionState;
			}

			public static DateTime RenewLockEnd(IAsyncResult r)
			{
				return AsyncResult<MessageSession.RetrySessionAsyncResult>.End(r).LockedUntilUtcTime;
			}

			public static void SetStateEnd(IAsyncResult r)
			{
				AsyncResult<MessageSession.RetrySessionAsyncResult>.End(r);
			}
		}

		private enum SessionOperation
		{
			GetState,
			SetState,
			RenewSessionLock
		}
	}
}