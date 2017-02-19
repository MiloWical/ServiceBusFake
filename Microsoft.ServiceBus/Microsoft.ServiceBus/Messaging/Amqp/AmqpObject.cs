using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal abstract class AmqpObject
	{
		private readonly static AsyncCallback onSafeCloseComplete;

		private static int nextId;

		private readonly SequenceNumber identifier;

		private readonly string name;

		private readonly object thisLock = new object();

		private AmqpObject.OpenAsyncResult pendingOpen;

		private AmqpObject.CloseAsyncResult pendingClose;

		private bool openCalled;

		private bool closeCalled;

		private bool abortCalled;

		private IList<AmqpSymbol> mutualCapabilities;

		public TimeSpan DefaultCloseTimeout
		{
			get;
			protected set;
		}

		public TimeSpan DefaultOpenTimeout
		{
			get;
			protected set;
		}

		public SequenceNumber Identifier
		{
			get
			{
				return this.identifier;
			}
		}

		public IList<AmqpSymbol> MutualCapabilities
		{
			get
			{
				object amqpSymbols = this.mutualCapabilities;
				if (amqpSymbols == null)
				{
					amqpSymbols = new List<AmqpSymbol>();
				}
				return amqpSymbols;
			}
		}

		public AmqpObjectState State
		{
			get;
			protected set;
		}

		public Exception TerminalException
		{
			get;
			protected set;
		}

		protected object ThisLock
		{
			get
			{
				return this.thisLock;
			}
		}

		static AmqpObject()
		{
			AmqpObject.onSafeCloseComplete = new AsyncCallback(AmqpObject.OnSafeCloseComplete);
			AmqpObject.nextId = -1;
		}

		private AmqpObject()
		{
		}

		protected AmqpObject(string type)
		{
			this.identifier = SequenceNumber.Increment(ref AmqpObject.nextId);
			this.name = string.Concat(type, this.identifier);
			this.DefaultOpenTimeout = TimeSpan.FromSeconds(60);
			this.DefaultCloseTimeout = TimeSpan.FromSeconds(60);
		}

		public void Abort()
		{
			lock (this.ThisLock)
			{
				if (this.abortCalled || this.State == AmqpObjectState.End)
				{
					return;
				}
				else
				{
					this.State = AmqpObjectState.End;
					this.abortCalled = true;
				}
			}
			try
			{
				try
				{
					this.AbortInternal();
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					MessagingClientEtwProvider.TraceClient<Exception>((Exception ex) => MessagingClientEtwProvider.Provider.ThrowingExceptionError(EventTraceActivity.Empty, ex.ToStringSlim()), exception);
					throw;
				}
			}
			finally
			{
				this.NotifyClosed();
			}
		}

		protected abstract void AbortInternal();

		public IAsyncResult BeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			bool flag = false;
			lock (this.ThisLock)
			{
				flag = (this.closeCalled || this.State == AmqpObjectState.End ? true : this.State == AmqpObjectState.CloseSent);
				this.closeCalled = true;
			}
			if (flag)
			{
				return new CompletedAsyncResult(callback, state);
			}
			MessagingClientEtwProvider.TraceClient<string>((string source) => {
			}, this.name);
			return new AmqpObject.CloseAsyncResult(this, timeout, callback, state);
		}

		public IAsyncResult BeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			lock (this.thisLock)
			{
				if (this.openCalled)
				{
					throw Fx.Exception.AsWarning(new InvalidOperationException(SRAmqp.AmqpInvalidReOpenOperation(this, this.State)), null);
				}
				this.openCalled = true;
			}
			MessagingClientEtwProvider.TraceClient<string>((string source) => {
			}, this.name);
			return new AmqpObject.OpenAsyncResult(this, timeout, callback, state);
		}

		public void Close()
		{
			this.Close(this.DefaultCloseTimeout);
		}

		public void Close(TimeSpan timeout)
		{
			bool flag = false;
			lock (this.ThisLock)
			{
				if ((this.closeCalled || this.State == AmqpObjectState.End ? false : this.State != AmqpObjectState.CloseSent))
				{
					this.closeCalled = true;
					if (this.State == AmqpObjectState.CloseReceived)
					{
						flag = true;
					}
				}
				else
				{
					return;
				}
			}
			if (flag)
			{
				this.CloseInternal();
				this.NotifyClosed();
				return;
			}
			this.OnClose(timeout);
		}

		internal Task CloseAsync(TimeSpan timeout)
		{
			return Task.Factory.FromAsync<TimeSpan>(new Func<TimeSpan, AsyncCallback, object, IAsyncResult>(this.BeginClose), new Action<IAsyncResult>(this.EndClose), timeout, null);
		}

		protected abstract bool CloseInternal();

		protected void CompleteClose(bool syncComplete, Exception exception)
		{
			AmqpObject.CloseAsyncResult closeAsyncResult = Interlocked.Exchange<AmqpObject.CloseAsyncResult>(ref this.pendingClose, null);
			if (closeAsyncResult != null)
			{
				closeAsyncResult.Signal(syncComplete, exception);
			}
		}

		protected void CompleteOpen(bool syncComplete, Exception exception)
		{
			AmqpObject.OpenAsyncResult openAsyncResult = Interlocked.Exchange<AmqpObject.OpenAsyncResult>(ref this.pendingOpen, null);
			if (openAsyncResult != null)
			{
				openAsyncResult.Signal(syncComplete, exception);
			}
		}

		public void EndClose(IAsyncResult result)
		{
			if (result is CompletedAsyncResult)
			{
				CompletedAsyncResult.End(result);
				return;
			}
			MessagingClientEtwProvider.TraceClient<string>((string source) => {
			}, this.name);
			AmqpObject.CloseAsyncResult.End(result);
		}

		public void EndOpen(IAsyncResult result)
		{
			AmqpObject.OpenAsyncResult.End(result);
			MessagingClientEtwProvider.TraceClient<string>((string source) => {
			}, this.name);
		}

		protected void FindMutualCapabilites(Multiple<AmqpSymbol> desired, Multiple<AmqpSymbol> offered)
		{
			this.mutualCapabilities = Multiple<AmqpSymbol>.Intersect(desired, offered);
		}

		internal bool IsClosing()
		{
			AmqpObjectState state = this.State;
			if (state == AmqpObjectState.CloseSent || state == AmqpObjectState.CloseReceived || state == AmqpObjectState.ClosePipe || state == AmqpObjectState.End)
			{
				return true;
			}
			return state == AmqpObjectState.Faulted;
		}

		private void NotifyClosed()
		{
			if (this.pendingOpen != null)
			{
				object terminalException = this.TerminalException;
				if (terminalException == null)
				{
					terminalException = new OperationCanceledException(SRAmqp.AmqpObjectAborted(this.name));
				}
				this.CompleteOpen(false, (Exception)terminalException);
			}
			if (this.pendingClose != null)
			{
				Exception operationCanceledException = this.TerminalException;
				if (operationCanceledException == null)
				{
					operationCanceledException = new OperationCanceledException(SRAmqp.AmqpObjectAborted(this.name));
				}
				this.CompleteClose(false, operationCanceledException);
			}
			EventHandler eventHandler = Interlocked.Exchange<EventHandler>(ref this.Closed, null);
			if (eventHandler != null)
			{
				eventHandler(this, EventArgs.Empty);
			}
		}

		private void NotifyOpened()
		{
			EventHandler eventHandler = Interlocked.Exchange<EventHandler>(ref this.Opened, null);
			if (eventHandler != null)
			{
				eventHandler(this, EventArgs.Empty);
			}
		}

		protected void NotifyOpening(Performative command)
		{
			EventHandler<OpenEventArgs> eventHandler = this.Opening;
			if (eventHandler != null)
			{
				eventHandler(this, new OpenEventArgs(command));
			}
		}

		protected virtual void OnClose(TimeSpan timeout)
		{
			AmqpObject.CloseAsyncResult.End(new AmqpObject.CloseAsyncResult(this, timeout, null, null));
		}

		protected virtual void OnOpen(TimeSpan timeout)
		{
			AmqpObject.OpenAsyncResult.End(new AmqpObject.OpenAsyncResult(this, timeout, null, null));
		}

		protected void OnReceiveCloseCommand(string command, Error error)
		{
			Exception amqpException;
			if (error == null)
			{
				amqpException = null;
			}
			else
			{
				amqpException = new AmqpException(error);
			}
			this.TerminalException = amqpException;
			try
			{
				if (this.TransitState(command, StateTransition.ReceiveClose).To != AmqpObjectState.End)
				{
					if (this.TerminalException != null)
					{
						this.CompleteOpen(false, this.TerminalException);
					}
					this.Close();
				}
				else
				{
					this.CompleteClose(false, this.TerminalException);
				}
			}
			catch (AmqpException amqpException1)
			{
				this.Abort();
			}
		}

		private static void OnSafeCloseComplete(IAsyncResult result)
		{
			AmqpObject asyncState = (AmqpObject)result.AsyncState;
			try
			{
				asyncState.EndClose(result);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				Fx.Exception.AsError(exception, null);
				asyncState.Abort();
			}
		}

		public void Open()
		{
			this.Open(this.DefaultOpenTimeout);
		}

		public void Open(TimeSpan timeout)
		{
			bool flag = false;
			lock (this.thisLock)
			{
				if (this.openCalled)
				{
					throw Fx.Exception.AsWarning(new InvalidOperationException(SRAmqp.AmqpInvalidReOpenOperation(this, this.State)), null);
				}
				this.openCalled = true;
				if (this.State == AmqpObjectState.OpenReceived)
				{
					flag = true;
				}
			}
			if (!flag)
			{
				this.OnOpen(timeout);
				return;
			}
			this.OpenInternal();
			this.NotifyOpened();
		}

		internal Task OpenAsync(TimeSpan timeout)
		{
			return Task.Factory.FromAsync<TimeSpan>(new Func<TimeSpan, AsyncCallback, object, IAsyncResult>(this.BeginOpen), new Action<IAsyncResult>(this.EndOpen), timeout, null);
		}

		protected abstract bool OpenInternal();

		internal void SafeAddClosed(EventHandler handler)
		{
			AmqpObjectState state;
			lock (this.ThisLock)
			{
				this.Closed += handler;
				state = this.State;
			}
			if (state == AmqpObjectState.ClosePipe || state == AmqpObjectState.CloseReceived || state == AmqpObjectState.CloseSent || state == AmqpObjectState.Faulted || state == AmqpObjectState.End)
			{
				handler(this, EventArgs.Empty);
			}
		}

		public void SafeClose()
		{
			this.SafeClose(null);
		}

		public void SafeClose(Exception exception)
		{
			this.TerminalException = exception;
			lock (this.thisLock)
			{
				if (this.State != AmqpObjectState.Opened && this.State != AmqpObjectState.OpenReceived && !this.IsClosing())
				{
					this.State = AmqpObjectState.Faulted;
				}
			}
			try
			{
				this.BeginClose(TimeSpan.FromSeconds(15), AmqpObject.onSafeCloseComplete, this);
			}
			catch (Exception exception1)
			{
				if (Fx.IsFatal(exception1))
				{
					throw;
				}
				this.Abort();
			}
		}

		public override string ToString()
		{
			return this.name;
		}

		protected StateTransition TransitState(string operation, StateTransition[] states)
		{
			StateTransition stateTransition = null;
			lock (this.ThisLock)
			{
				StateTransition[] stateTransitionArray = states;
				int num = 0;
				while (num < (int)stateTransitionArray.Length)
				{
					StateTransition stateTransition1 = stateTransitionArray[num];
					if (stateTransition1.From != this.State)
					{
						num++;
					}
					else
					{
						this.State = stateTransition1.To;
						stateTransition = stateTransition1;
						break;
					}
				}
			}
			if (stateTransition == null)
			{
				throw new AmqpException(AmqpError.IllegalState, SRAmqp.AmqpIllegalOperationState(operation, this.State));
			}
			MessagingClientEtwProvider.TraceClient<AmqpObject, string, StateTransition>((AmqpObject source, string op, StateTransition st) => {
			}, this, operation, stateTransition);
			return stateTransition;
		}

		public event EventHandler Closed;

		public event EventHandler Opened;

		public event EventHandler<OpenEventArgs> Opening;

		private abstract class AmqpObjectAsyncResult : TimeoutAsyncResult<AmqpObject>
		{
			private readonly AmqpObject amqpObject;

			protected override AmqpObject Target
			{
				get
				{
					return this.amqpObject;
				}
			}

			protected AmqpObjectAsyncResult(AmqpObject amqpObject, TimeSpan timeout, AsyncCallback callback, object asyncState) : base(timeout, callback, asyncState)
			{
				this.amqpObject = amqpObject;
			}

			protected abstract bool OnStart();

			public void Signal(bool syncComplete, Exception exception)
			{
				this.UpdateState(exception);
				base.CompleteSelf(syncComplete, exception);
			}

			protected void Start()
			{
				bool flag = false;
				Exception exception = null;
				try
				{
					flag = this.OnStart();
				}
				catch (Exception exception2)
				{
					Exception exception1 = exception2;
					if (Fx.IsFatal(exception1))
					{
						throw;
					}
					flag = true;
					exception = exception1;
				}
				if (flag)
				{
					this.amqpObject.pendingOpen = null;
					this.amqpObject.pendingClose = null;
					this.Signal(true, exception);
				}
			}

			protected abstract void UpdateState(Exception exception);
		}

		private sealed class CloseAsyncResult : AmqpObject.AmqpObjectAsyncResult
		{
			public CloseAsyncResult(AmqpObject amqpObject, TimeSpan timeout, AsyncCallback callback, object asyncState) : base(amqpObject, timeout, callback, asyncState)
			{
				amqpObject.pendingClose = this;
				base.Start();
			}

			public static new void End(IAsyncResult result)
			{
				AsyncResult.End<AmqpObject.CloseAsyncResult>(result);
			}

			protected override bool OnStart()
			{
				return this.Target.CloseInternal();
			}

			protected override void UpdateState(Exception exception)
			{
				this.Target.State = AmqpObjectState.End;
				this.Target.NotifyClosed();
			}
		}

		private sealed class OpenAsyncResult : AmqpObject.AmqpObjectAsyncResult
		{
			public OpenAsyncResult(AmqpObject amqpObject, TimeSpan timeout, AsyncCallback callback, object asyncState) : base(amqpObject, timeout, callback, asyncState)
			{
				amqpObject.pendingOpen = this;
				base.Start();
			}

			public static new void End(IAsyncResult result)
			{
				AsyncResult.End<AmqpObject.OpenAsyncResult>(result).Target.NotifyOpened();
			}

			protected override bool OnStart()
			{
				return this.Target.OpenInternal();
			}

			protected override void UpdateState(Exception exception)
			{
				if (exception == null)
				{
					this.Target.State = AmqpObjectState.Opened;
				}
			}
		}
	}
}