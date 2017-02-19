using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;

namespace Microsoft.ServiceBus.Channels
{
	internal abstract class RequestChannel : ChannelBase, IRequestChannel, IChannel, ICommunicationObject
	{
		private bool manualAddressing;

		private List<Microsoft.ServiceBus.Channels.IRequestBase> outstandingRequests = new List<Microsoft.ServiceBus.Channels.IRequestBase>();

		private EndpointAddress to;

		private Uri via;

		private ManualResetEvent closedEvent;

		private bool closed;

		protected bool ManualAddressing
		{
			get
			{
				return this.manualAddressing;
			}
		}

		public EndpointAddress RemoteAddress
		{
			get
			{
				return this.to;
			}
		}

		public Uri Via
		{
			get
			{
				return this.via;
			}
		}

		protected RequestChannel(ChannelManagerBase channelFactory, EndpointAddress to, Uri via, bool manualAddressing) : base(channelFactory)
		{
			if (!manualAddressing && to == null)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("to");
			}
			this.manualAddressing = manualAddressing;
			this.to = to;
			this.via = via;
		}

		protected void AbortPendingRequests()
		{
			Microsoft.ServiceBus.Channels.IRequestBase[] requestBaseArray = this.CopyPendingRequests(false);
			if (requestBaseArray != null)
			{
				Microsoft.ServiceBus.Channels.IRequestBase[] requestBaseArray1 = requestBaseArray;
				for (int i = 0; i < (int)requestBaseArray1.Length; i++)
				{
					requestBaseArray1[i].Abort(this);
				}
			}
		}

		protected virtual void AddHeadersTo(Message message)
		{
			if (!this.manualAddressing && this.to != null)
			{
				this.to.ApplyTo(message);
			}
		}

		public IAsyncResult BeginRequest(Message message, AsyncCallback callback, object state)
		{
			return this.BeginRequest(message, base.DefaultSendTimeout, callback, state);
		}

		public IAsyncResult BeginRequest(Message message, TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (message == null)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("message");
			}
			if (timeout < TimeSpan.Zero)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("timeout", (object)timeout, Microsoft.ServiceBus.SR.GetString(Resources.SFxTimeoutOutOfRange0, new object[0])));
			}
			base.ThrowIfDisposedOrNotOpen();
			this.AddHeadersTo(message);
			Microsoft.ServiceBus.Channels.IAsyncRequest asyncRequest = this.CreateAsyncRequest(message, callback, state);
			this.TrackRequest(asyncRequest);
			bool flag = true;
			try
			{
				asyncRequest.BeginSendRequest(message, timeout);
				flag = false;
			}
			finally
			{
				if (flag)
				{
					this.ReleaseRequest(asyncRequest);
				}
			}
			return asyncRequest;
		}

		protected IAsyncResult BeginWaitForPendingRequests(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new Microsoft.ServiceBus.Channels.RequestChannel.WaitForPendingRequestsAsyncResult(timeout, this, this.SetupWaitForPendingRequests(), callback, state);
		}

		private Microsoft.ServiceBus.Channels.IRequestBase[] CopyPendingRequests(bool createEventIfNecessary)
		{
			Microsoft.ServiceBus.Channels.IRequestBase[] requestBaseArray = null;
			lock (this.outstandingRequests)
			{
				if (this.outstandingRequests.Count > 0)
				{
					requestBaseArray = new Microsoft.ServiceBus.Channels.IRequestBase[this.outstandingRequests.Count];
					this.outstandingRequests.CopyTo(requestBaseArray);
					this.outstandingRequests.Clear();
					if (createEventIfNecessary && this.closedEvent == null)
					{
						this.closedEvent = new ManualResetEvent(false);
					}
				}
			}
			return requestBaseArray;
		}

		protected abstract Microsoft.ServiceBus.Channels.IAsyncRequest CreateAsyncRequest(Message message, AsyncCallback callback, object state);

		protected abstract Microsoft.ServiceBus.Channels.IRequest CreateRequest(Message message);

		public Message EndRequest(IAsyncResult result)
		{
			Message message;
			if (result == null)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("result");
			}
			Microsoft.ServiceBus.Channels.IAsyncRequest asyncRequest = result as Microsoft.ServiceBus.Channels.IAsyncRequest;
			if (asyncRequest == null)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperArgument("result", Microsoft.ServiceBus.SR.GetString(Resources.InvalidAsyncResult, new object[0]));
			}
			try
			{
				Message message1 = asyncRequest.End();
				if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceInformation)
				{
					TraceUtility.TraceEvent(TraceEventType.Information, TraceCode.RequestChannelReplyReceived, message1);
				}
				message = message1;
			}
			finally
			{
				this.ReleaseRequest(asyncRequest);
			}
			return message;
		}

		protected static void EndWaitForPendingRequests(IAsyncResult result)
		{
			Microsoft.ServiceBus.Channels.RequestChannel.WaitForPendingRequestsAsyncResult.End(result);
		}

		protected void FaultPendingRequests()
		{
			Microsoft.ServiceBus.Channels.IRequestBase[] requestBaseArray = this.CopyPendingRequests(false);
			if (requestBaseArray != null)
			{
				Microsoft.ServiceBus.Channels.IRequestBase[] requestBaseArray1 = requestBaseArray;
				for (int i = 0; i < (int)requestBaseArray1.Length; i++)
				{
					requestBaseArray1[i].Fault(this);
				}
			}
		}

		private void FinishClose()
		{
			lock (this.outstandingRequests)
			{
				if (!this.closed)
				{
					this.closed = true;
					if (this.closedEvent != null)
					{
						this.closedEvent.Close();
					}
				}
			}
		}

		public override T GetProperty<T>()
		where T : class
		{
			if (typeof(T) == typeof(IRequestChannel))
			{
				return (T)this;
			}
			T property = base.GetProperty<T>();
			if (property != null)
			{
				return property;
			}
			return default(T);
		}

		protected override void OnAbort()
		{
			this.AbortPendingRequests();
		}

		private void ReleaseRequest(Microsoft.ServiceBus.Channels.IRequestBase request)
		{
			lock (this.outstandingRequests)
			{
				this.outstandingRequests.Remove(request);
				if (this.outstandingRequests.Count == 0 && !this.closed && this.closedEvent != null)
				{
					this.closedEvent.Set();
				}
			}
		}

		public Message Request(Message message)
		{
			return this.Request(message, base.DefaultSendTimeout);
		}

		public Message Request(Message message, TimeSpan timeout)
		{
			Message message1;
			Message message2;
			if (message == null)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("message");
			}
			if (timeout < TimeSpan.Zero)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("timeout", (object)timeout, Microsoft.ServiceBus.SR.GetString(Resources.SFxTimeoutOutOfRange0, new object[0])));
			}
			base.ThrowIfDisposedOrNotOpen();
			this.AddHeadersTo(message);
			Microsoft.ServiceBus.Channels.IRequest request = this.CreateRequest(message);
			this.TrackRequest(request);
			try
			{
				Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
				TimeSpan timeSpan = timeoutHelper.RemainingTime();
				try
				{
					request.SendRequest(message, timeSpan);
				}
				catch (TimeoutException timeoutException1)
				{
					TimeoutException timeoutException = timeoutException1;
					string requestChannelSendTimedOut = Resources.RequestChannelSendTimedOut;
					object[] objArray = new object[] { timeSpan };
					throw TraceUtility.ThrowHelperError(new TimeoutException(Microsoft.ServiceBus.SR.GetString(requestChannelSendTimedOut, objArray), timeoutException), message);
				}
				timeSpan = timeoutHelper.RemainingTime();
				try
				{
					message1 = request.WaitForReply(timeSpan);
				}
				catch (TimeoutException timeoutException3)
				{
					TimeoutException timeoutException2 = timeoutException3;
					string requestChannelWaitForReplyTimedOut = Resources.RequestChannelWaitForReplyTimedOut;
					object[] objArray1 = new object[] { timeSpan };
					throw TraceUtility.ThrowHelperError(new TimeoutException(Microsoft.ServiceBus.SR.GetString(requestChannelWaitForReplyTimedOut, objArray1), timeoutException2), message);
				}
				if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceInformation)
				{
					TraceUtility.TraceEvent(TraceEventType.Information, TraceCode.RequestChannelReplyReceived, message1);
				}
				message2 = message1;
			}
			finally
			{
				this.ReleaseRequest(request);
			}
			return message2;
		}

		private Microsoft.ServiceBus.Channels.IRequestBase[] SetupWaitForPendingRequests()
		{
			return this.CopyPendingRequests(true);
		}

		private void TrackRequest(Microsoft.ServiceBus.Channels.IRequestBase request)
		{
			lock (this.outstandingRequests)
			{
				base.ThrowIfDisposedOrNotOpen();
				this.outstandingRequests.Add(request);
			}
		}

		protected void WaitForPendingRequests(TimeSpan timeout)
		{
			Microsoft.ServiceBus.Channels.IRequestBase[] requestBaseArray = this.SetupWaitForPendingRequests();
			if (requestBaseArray != null && !this.closedEvent.WaitOne(timeout, false))
			{
				Microsoft.ServiceBus.Channels.IRequestBase[] requestBaseArray1 = requestBaseArray;
				for (int i = 0; i < (int)requestBaseArray1.Length; i++)
				{
					requestBaseArray1[i].Abort(this);
				}
			}
			this.FinishClose();
		}

		private class WaitForPendingRequestsAsyncResult : AsyncResult
		{
			private static WaitOrTimerCallback completeWaitCallBack;

			private Microsoft.ServiceBus.Channels.IRequestBase[] pendingRequests;

			private Microsoft.ServiceBus.Channels.RequestChannel requestChannel;

			private TimeSpan timeout;

			private RegisteredWaitHandle waitHandle;

			static WaitForPendingRequestsAsyncResult()
			{
				Microsoft.ServiceBus.Channels.RequestChannel.WaitForPendingRequestsAsyncResult.completeWaitCallBack = new WaitOrTimerCallback(Microsoft.ServiceBus.Channels.RequestChannel.WaitForPendingRequestsAsyncResult.OnCompleteWaitCallBack);
			}

			public WaitForPendingRequestsAsyncResult(TimeSpan timeout, Microsoft.ServiceBus.Channels.RequestChannel requestChannel, Microsoft.ServiceBus.Channels.IRequestBase[] pendingRequests, AsyncCallback callback, object state) : base(callback, state)
			{
				this.requestChannel = requestChannel;
				this.pendingRequests = pendingRequests;
				this.timeout = timeout;
				if (this.timeout == TimeSpan.Zero || this.pendingRequests == null)
				{
					this.AbortRequests();
					this.CleanupEvents();
					base.Complete(true);
					return;
				}
				this.waitHandle = ThreadPool.UnsafeRegisterWaitForSingleObject(this.requestChannel.closedEvent, Microsoft.ServiceBus.Channels.RequestChannel.WaitForPendingRequestsAsyncResult.completeWaitCallBack, this, Microsoft.ServiceBus.Common.TimeoutHelper.ToMilliseconds(timeout), true);
			}

			private void AbortRequests()
			{
				if (this.pendingRequests != null)
				{
					Microsoft.ServiceBus.Channels.IRequestBase[] requestBaseArray = this.pendingRequests;
					for (int i = 0; i < (int)requestBaseArray.Length; i++)
					{
						requestBaseArray[i].Abort(this.requestChannel);
					}
				}
			}

			private void CleanupEvents()
			{
				if (this.requestChannel.closedEvent != null)
				{
					if (this.waitHandle != null)
					{
						this.waitHandle.Unregister(this.requestChannel.closedEvent);
					}
					this.requestChannel.FinishClose();
				}
			}

			public static new void End(IAsyncResult result)
			{
				AsyncResult.End<Microsoft.ServiceBus.Channels.RequestChannel.WaitForPendingRequestsAsyncResult>(result);
			}

			private static void OnCompleteWaitCallBack(object state, bool timedOut)
			{
				Microsoft.ServiceBus.Channels.RequestChannel.WaitForPendingRequestsAsyncResult waitForPendingRequestsAsyncResult = (Microsoft.ServiceBus.Channels.RequestChannel.WaitForPendingRequestsAsyncResult)state;
				Exception exception = null;
				try
				{
					if (timedOut)
					{
						waitForPendingRequestsAsyncResult.AbortRequests();
					}
					waitForPendingRequestsAsyncResult.CleanupEvents();
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
				waitForPendingRequestsAsyncResult.Complete(false, exception);
			}
		}
	}
}