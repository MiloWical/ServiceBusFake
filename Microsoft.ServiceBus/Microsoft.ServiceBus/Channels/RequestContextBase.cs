using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Channels
{
	internal abstract class RequestContextBase : RequestContext
	{
		private TimeSpan defaultSendTimeout;

		private TimeSpan defaultCloseTimeout;

		private CommunicationState state = CommunicationState.Opened;

		private Message requestMessage;

		private Exception requestMessageException;

		private bool replySent;

		private bool replyInitiated;

		private bool aborted;

		private object thisLock = new object();

		public bool Aborted
		{
			get
			{
				return this.aborted;
			}
		}

		public TimeSpan DefaultCloseTimeout
		{
			get
			{
				return this.defaultCloseTimeout;
			}
		}

		public TimeSpan DefaultSendTimeout
		{
			get
			{
				return this.defaultSendTimeout;
			}
		}

		protected bool ReplyInitiated
		{
			get
			{
				return this.replyInitiated;
			}
		}

		public override Message RequestMessage
		{
			get
			{
				if (this.requestMessageException != null)
				{
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(this.requestMessageException);
				}
				return this.requestMessage;
			}
		}

		protected object ThisLock
		{
			get
			{
				return this.thisLock;
			}
		}

		protected RequestContextBase(Message requestMessage, TimeSpan defaultCloseTimeout, TimeSpan defaultSendTimeout)
		{
			this.defaultSendTimeout = defaultSendTimeout;
			this.defaultCloseTimeout = defaultCloseTimeout;
			this.requestMessage = requestMessage;
		}

		public override void Abort()
		{
			lock (this.ThisLock)
			{
				if (this.state != CommunicationState.Closed)
				{
					this.state = CommunicationState.Closing;
					this.aborted = true;
				}
				else
				{
					return;
				}
			}
			if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceWarning)
			{
				TraceUtility.TraceEvent(TraceEventType.Warning, TraceCode.RequestContextAbort, this);
			}
			try
			{
				this.OnAbort();
			}
			finally
			{
				this.state = CommunicationState.Closed;
			}
		}

		public override IAsyncResult BeginReply(Message message, AsyncCallback callback, object state)
		{
			return this.BeginReply(message, this.defaultSendTimeout, callback, state);
		}

		public override IAsyncResult BeginReply(Message message, TimeSpan timeout, AsyncCallback callback, object state)
		{
			lock (this.thisLock)
			{
				this.ThrowIfInvalidReply();
				this.replyInitiated = true;
			}
			return this.OnBeginReply(message, timeout, callback, state);
		}

		public override void Close()
		{
			this.Close(this.defaultCloseTimeout);
		}

		public override void Close(TimeSpan timeout)
		{
			if (timeout < TimeSpan.Zero)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("timeout", (object)timeout, Microsoft.ServiceBus.SR.GetString(Resources.ValueMustBeNonNegative, new object[0])));
			}
			bool flag = false;
			lock (this.ThisLock)
			{
				if (this.state == CommunicationState.Opened)
				{
					this.state = CommunicationState.Closing;
					if (!this.replyInitiated)
					{
						this.replyInitiated = true;
						flag = true;
					}
				}
				else
				{
					return;
				}
			}
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			bool flag1 = true;
			try
			{
				if (flag)
				{
					this.OnReply(null, timeoutHelper.RemainingTime());
				}
				this.OnClose(timeoutHelper.RemainingTime());
				this.state = CommunicationState.Closed;
				flag1 = false;
			}
			finally
			{
				if (flag1)
				{
					this.Abort();
				}
			}
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (!disposing)
			{
				return;
			}
			if (this.replySent)
			{
				this.Close();
				return;
			}
			this.Abort();
		}

		public override void EndReply(IAsyncResult result)
		{
			this.OnEndReply(result);
			this.replySent = true;
		}

		protected abstract void OnAbort();

		protected abstract IAsyncResult OnBeginReply(Message message, TimeSpan timeout, AsyncCallback callback, object state);

		protected abstract void OnClose(TimeSpan timeout);

		protected abstract void OnEndReply(IAsyncResult result);

		protected abstract void OnReply(Message message, TimeSpan timeout);

		public override void Reply(Message message)
		{
			this.Reply(message, this.defaultSendTimeout);
		}

		public override void Reply(Message message, TimeSpan timeout)
		{
			lock (this.thisLock)
			{
				this.ThrowIfInvalidReply();
				this.replyInitiated = true;
			}
			this.OnReply(message, timeout);
			this.replySent = true;
		}

		protected void SetRequestMessage(Message requestMessage)
		{
			Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DebugAssert(this.requestMessageException == null, "Cannot have both a requestMessage and a requestException.");
			this.requestMessage = requestMessage;
		}

		protected void SetRequestMessage(Exception requestMessageException)
		{
			Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DebugAssert(this.requestMessage == null, "Cannot have both a requestMessage and a requestException.");
			this.requestMessageException = requestMessageException;
		}

		protected void ThrowIfInvalidReply()
		{
			if (this.state == CommunicationState.Closed || this.state == CommunicationState.Closing)
			{
				if (!this.aborted)
				{
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ObjectDisposedException(base.GetType().FullName));
				}
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new CommunicationObjectAbortedException(Microsoft.ServiceBus.SR.GetString(Resources.RequestContextAborted, new object[0])));
			}
			if (this.replyInitiated)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(Resources.ReplyAlreadySent, new object[0])));
			}
		}
	}
}