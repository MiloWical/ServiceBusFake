using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Common.Parallel;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus.Messaging
{
	public abstract class ClientEntity : IMessageClientEntity, ICloseable
	{
		private readonly object thisLock;

		private readonly Lazy<Queue<Exception>> exceptionQueue;

		private readonly string traceId;

		private bool isClosing;

		private bool raisedClosed;

		private bool raisedOpened;

		private bool raisedFaulted;

		private Microsoft.ServiceBus.RetryPolicy retryPolicy;

		private Microsoft.ServiceBus.Messaging.RuntimeEntityDescription runtimeEntityDescription;

		internal MessageClientEntityManager ClientEntityManager
		{
			get;
			set;
		}

		public bool IsClosed
		{
			get
			{
				bool flag;
				lock (this.ThisLock)
				{
					flag = this.raisedClosed;
				}
				return flag;
			}
		}

		internal bool IsClosedOrClosing
		{
			get
			{
				bool flag;
				lock (this.ThisLock)
				{
					flag = (this.raisedClosed ? true : this.isClosing);
				}
				return flag;
			}
		}

		internal bool IsFaulted
		{
			get
			{
				bool flag;
				lock (this.ThisLock)
				{
					flag = this.raisedFaulted;
				}
				return flag;
			}
		}

		internal bool IsOpened
		{
			get
			{
				bool flag;
				lock (this.ThisLock)
				{
					flag = (!this.raisedOpened || this.raisedClosed ? false : !this.raisedFaulted);
				}
				return flag;
			}
		}

		bool Microsoft.ServiceBus.Messaging.ICloseable.IsClosedOrClosing
		{
			get
			{
				return this.IsClosedOrClosing;
			}
		}

		internal abstract TimeSpan OperationTimeout
		{
			get;
		}

		public Microsoft.ServiceBus.RetryPolicy RetryPolicy
		{
			get
			{
				return this.retryPolicy;
			}
			set
			{
				if (value == null)
				{
					throw FxTrace.Exception.ArgumentNull("RetryPolicy");
				}
				this.retryPolicy = value;
				if (this.ClientEntityManager != null)
				{
					this.ClientEntityManager.UpdateRetryPolicy(this.retryPolicy);
				}
			}
		}

		internal Microsoft.ServiceBus.Messaging.RuntimeEntityDescription RuntimeEntityDescription
		{
			get
			{
				return this.runtimeEntityDescription;
			}
			set
			{
				this.runtimeEntityDescription = value;
				this.OnRuntimeDescriptionChanged(this.runtimeEntityDescription);
			}
		}

		internal bool ShouldLinkRetryPolicy
		{
			get;
			set;
		}

		protected object ThisLock
		{
			get
			{
				return this.thisLock;
			}
		}

		internal ClientEntity()
		{
			this.thisLock = new object();
			this.exceptionQueue = new Lazy<Queue<Exception>>();
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] name = new object[] { this.GetType().Name, this.GetHashCode() };
			this.traceId = string.Format(invariantCulture, "{0}/{1}", name);
			this.retryPolicy = Microsoft.ServiceBus.RetryPolicy.Default;
		}

		public virtual void Abort()
		{
			this.Abort(false);
		}

		private void Abort(bool fromClose)
		{
			lock (this.ThisLock)
			{
				if (!fromClose)
				{
					if (this.raisedClosed || this.isClosing)
					{
						return;
					}
				}
				else if (this.raisedClosed)
				{
					return;
				}
				if (!this.isClosing)
				{
					this.isClosing = true;
				}
			}
			MessagingClientEtwProvider.TraceClient(() => {
			});
			this.OnAbort();
			this.OnClosed();
		}

		internal IAsyncResult BeginClose(AsyncCallback callback, object state)
		{
			return this.BeginClose(this.OperationTimeout, callback, state);
		}

		internal virtual IAsyncResult BeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult alreadyClosedAsyncResult;
			lock (this.ThisLock)
			{
				if (!this.isClosing)
				{
					this.isClosing = true;
				}
				else
				{
					alreadyClosedAsyncResult = new ClientEntity.AlreadyClosedAsyncResult(callback, state);
					return alreadyClosedAsyncResult;
				}
			}
			try
			{
				MessagingClientEtwProvider.TraceClient(() => {
				});
				alreadyClosedAsyncResult = this.OnBeginClose(timeout, callback, state);
			}
			catch
			{
				this.Abort(true);
				throw;
			}
			return alreadyClosedAsyncResult;
		}

		internal IAsyncResult BeginOpen(AsyncCallback callback, object state)
		{
			return this.BeginOpen(this.OperationTimeout, callback, state);
		}

		internal IAsyncResult BeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			this.ThrowIfDisposed();
			return this.OnBeginOpen(timeout, callback, state);
		}

		public void Close()
		{
			this.Close(this.OperationTimeout);
		}

		internal virtual void Close(TimeSpan timeout)
		{
			lock (this.ThisLock)
			{
				if (!this.isClosing)
				{
					this.isClosing = true;
				}
				else
				{
					return;
				}
			}
			try
			{
				MessagingClientEtwProvider.TraceClient(() => {
				});
				this.OnClose(timeout);
				this.OnClosed();
			}
			catch
			{
				this.Abort(true);
				throw;
			}
		}

		public Task CloseAsync()
		{
			ClientEntity clientEntity = this;
			return TaskHelpers.CreateTask(new Func<AsyncCallback, object, IAsyncResult>(this.BeginClose), new Action<IAsyncResult>(clientEntity.EndClose));
		}

		internal virtual void EndClose(IAsyncResult result)
		{
			try
			{
				if (!(result is ClientEntity.AlreadyClosedAsyncResult))
				{
					this.OnEndClose(result);
					this.OnClosed();
				}
				else
				{
					ClientEntity.AlreadyClosedAsyncResult.End(result);
				}
			}
			catch
			{
				this.Abort(true);
				throw;
			}
		}

		internal virtual void EndOpen(IAsyncResult result)
		{
			this.OnEndOpen(result);
			this.OnOpened();
		}

		internal void Fault(Exception exception)
		{
			if (this.IsFaulted)
			{
				return;
			}
			if (exception != null)
			{
				this.exceptionQueue.Value.Enqueue(exception);
			}
			this.Fault();
		}

		protected void Fault()
		{
			lock (this.ThisLock)
			{
				if (this.raisedClosed || this.raisedFaulted)
				{
					return;
				}
			}
			this.OnFaulted();
		}

		internal Exception GetPendingException()
		{
			if (!this.exceptionQueue.IsValueCreated || this.exceptionQueue.Value.Count <= 0)
			{
				return null;
			}
			return this.exceptionQueue.Value.Dequeue();
		}

		protected abstract void OnAbort();

		protected abstract IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state);

		protected abstract IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state);

		protected virtual void OnClose(TimeSpan timeout)
		{
			this.OnEndClose(this.OnBeginClose(timeout, null, null));
		}

		protected virtual void OnClosed()
		{
			lock (this.ThisLock)
			{
				if (!this.raisedClosed)
				{
					this.raisedClosed = true;
					this.isClosing = false;
				}
				else
				{
					return;
				}
			}
			MessagingClientEtwProvider.TraceClient(() => {
			});
			EventHandler eventHandler = this.Closed;
			if (eventHandler != null)
			{
				eventHandler(this, EventArgs.Empty);
			}
		}

		protected abstract void OnEndClose(IAsyncResult result);

		protected abstract void OnEndOpen(IAsyncResult result);

		protected virtual void OnFaulted()
		{
			lock (this.ThisLock)
			{
				if (!this.raisedFaulted)
				{
					this.raisedFaulted = true;
				}
				else
				{
					return;
				}
			}
			MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteLogOperationWarning("Entity Faulted", this.traceId));
			EventHandler eventHandler = this.Faulted;
			if (eventHandler != null)
			{
				eventHandler(this, EventArgs.Empty);
			}
		}

		protected virtual void OnOpen(TimeSpan timeout)
		{
			this.OnEndOpen(this.OnBeginOpen(timeout, null, null));
		}

		protected virtual void OnOpened()
		{
			lock (this.ThisLock)
			{
				if (!this.raisedOpened)
				{
					this.raisedOpened = true;
				}
				else
				{
					return;
				}
			}
			MessagingClientEtwProvider.TraceClient(() => {
			});
			EventHandler eventHandler = this.Opened;
			if (eventHandler != null)
			{
				eventHandler(this, EventArgs.Empty);
			}
		}

		internal virtual void OnRuntimeDescriptionChanged(Microsoft.ServiceBus.Messaging.RuntimeEntityDescription newValue)
		{
		}

		internal void Open()
		{
			this.Open(this.OperationTimeout);
		}

		internal virtual void Open(TimeSpan timeout)
		{
			this.ThrowIfDisposed();
			this.OnOpen(timeout);
			this.OnOpened();
		}

		protected void ThrowIfClosed()
		{
			this.ThrowPending();
			if (this.IsClosed)
			{
				throw FxTrace.Exception.AsError(new OperationCanceledException(SRClient.MessageEntityDisposed), null);
			}
		}

		protected void ThrowIfDisposed()
		{
			this.ThrowIfClosed();
			this.ThrowIfFaulted();
		}

		protected void ThrowIfDisposedOrImmutable()
		{
			this.ThrowIfClosed();
			this.ThrowIfFaulted();
		}

		protected void ThrowIfDisposedOrNotOpen()
		{
			this.ThrowIfDisposed();
			if (!this.IsOpened)
			{
				throw FxTrace.Exception.AsError(new OperationCanceledException(SRClient.MessageEntityDisposed), null);
			}
		}

		protected void ThrowIfFaulted()
		{
			this.ThrowPending();
			if (this.IsFaulted)
			{
				throw FxTrace.Exception.AsError(new OperationCanceledException(SRClient.MessageEntityDisposed), null);
			}
		}

		internal void ThrowPending()
		{
			if (this.exceptionQueue.IsValueCreated && this.exceptionQueue.Value.Count > 0)
			{
				Exception exception = this.exceptionQueue.Value.Dequeue();
				if (exception != null)
				{
					throw FxTrace.Exception.AsError(exception, null);
				}
			}
		}

		internal event EventHandler Closed;

		internal event EventHandler Faulted;

		internal event EventHandler Opened;

		private class AlreadyClosedAsyncResult : AsyncResult
		{
			public AlreadyClosedAsyncResult(AsyncCallback callback, object state) : base(callback, state)
			{
				base.Complete(true);
			}

			public static new void End(IAsyncResult result)
			{
				AsyncResult.End<ClientEntity.AlreadyClosedAsyncResult>(result);
			}
		}
	}
}