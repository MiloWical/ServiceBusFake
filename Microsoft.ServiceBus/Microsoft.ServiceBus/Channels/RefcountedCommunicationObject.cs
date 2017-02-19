using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.Diagnostics;
using System.ServiceModel;
using System.Threading;

namespace Microsoft.ServiceBus.Channels
{
	internal abstract class RefcountedCommunicationObject : ICommunicationObject
	{
		private bool aborted;

		private bool closeCalled;

		private object mutex;

		private bool onClosingCalled;

		private bool onClosedCalled;

		private bool onOpeningCalled;

		private bool onOpenedCalled;

		private bool raisedClosed;

		private bool raisedClosing;

		private bool raisedFaulted;

		private bool traceOpenAndClose;

		private object eventSender;

		private int refCount;

		private ThreadNeutralSemaphore semaphore;

		private CommunicationState state;

		internal virtual string CloseActivityName
		{
			get
			{
				string activityClose = Resources.ActivityClose;
				object[] fullName = new object[] { this.GetType().FullName };
				return Microsoft.ServiceBus.SR.GetString(activityClose, fullName);
			}
		}

		protected abstract TimeSpan DefaultCloseTimeout
		{
			get;
		}

		protected abstract TimeSpan DefaultOpenTimeout
		{
			get;
		}

		internal virtual string OpenActivityName
		{
			get
			{
				string activityOpen = Resources.ActivityOpen;
				object[] fullName = new object[] { this.GetType().FullName };
				return Microsoft.ServiceBus.SR.GetString(activityOpen, fullName);
			}
		}

		internal virtual ActivityType OpenActivityType
		{
			get
			{
				return ActivityType.Open;
			}
		}

		public CommunicationState State
		{
			get
			{
				return this.state;
			}
		}

		protected object ThisLock
		{
			get
			{
				return this.mutex;
			}
		}

		internal bool TraceOpenAndClose
		{
			get
			{
				return this.traceOpenAndClose;
			}
			set
			{
				this.traceOpenAndClose = (!value ? false : Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldUseActivity);
			}
		}

		protected RefcountedCommunicationObject() : this(new object())
		{
		}

		protected RefcountedCommunicationObject(object mutex)
		{
			this.mutex = mutex;
			this.eventSender = this;
			this.state = CommunicationState.Created;
			this.semaphore = new ThreadNeutralSemaphore(1);
		}

		public void Abort()
		{
			lock (this.ThisLock)
			{
				if (!this.closeCalled)
				{
					if (this.refCount == 0)
					{
						throw new InvalidOperationException(SRClient.InvalidRefcountedCommunicationObject);
					}
					RefcountedCommunicationObject refcountedCommunicationObject = this;
					int num = refcountedCommunicationObject.refCount - 1;
					int num1 = num;
					refcountedCommunicationObject.refCount = num;
					if (num1 > 0)
					{
						return;
					}
				}
				if (this.aborted || this.state == CommunicationState.Closed)
				{
					return;
				}
				else
				{
					this.aborted = true;
					this.state = CommunicationState.Closing;
				}
			}
			if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceInformation)
			{
				DiagnosticTrace diagnosticTrace = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DiagnosticTrace;
				string traceCodeCommunicationObjectAborted = Resources.TraceCodeCommunicationObjectAborted;
				object[] objArray = new object[] { DiagnosticTrace.CreateSourceString(this) };
				diagnosticTrace.TraceEvent(TraceEventType.Information, TraceCode.CommunicationObjectAborted, Microsoft.ServiceBus.SR.GetString(traceCodeCommunicationObjectAborted, objArray), null, null, this);
			}
			bool flag = true;
			try
			{
				this.OnClosing();
				if (!this.onClosingCalled)
				{
					throw TraceUtility.ThrowHelperError(this.CreateBaseClassMethodNotCalledException("OnClosing"), Guid.Empty, this);
				}
				this.OnAbort();
				this.OnClosed();
				if (!this.onClosedCalled)
				{
					throw TraceUtility.ThrowHelperError(this.CreateBaseClassMethodNotCalledException("OnClosed"), Guid.Empty, this);
				}
				flag = false;
			}
			finally
			{
				if (flag && Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceWarning)
				{
					DiagnosticTrace diagnosticTrace1 = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DiagnosticTrace;
					string traceCodeCommunicationObjectAbortFailed = Resources.TraceCodeCommunicationObjectAbortFailed;
					object[] str = new object[] { this.GetCommunicationObjectType().ToString() };
					diagnosticTrace1.TraceEvent(TraceEventType.Warning, TraceCode.CommunicationObjectAbortFailed, Microsoft.ServiceBus.SR.GetString(traceCodeCommunicationObjectAbortFailed, str), null, null, this);
				}
			}
		}

		public bool AddRef()
		{
			bool flag;
			lock (this.ThisLock)
			{
				if (this.state == CommunicationState.Closing || this.state == CommunicationState.Closed)
				{
					flag = false;
				}
				else
				{
					RefcountedCommunicationObject refcountedCommunicationObject = this;
					refcountedCommunicationObject.refCount = refcountedCommunicationObject.refCount + 1;
					flag = true;
				}
			}
			return flag;
		}

		public IAsyncResult BeginClose(AsyncCallback callback, object state)
		{
			return this.BeginClose(this.DefaultCloseTimeout, callback, state);
		}

		public IAsyncResult BeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			CommunicationState communicationState;
			IAsyncResult skippingOperationAsyncResult;
			ServiceModelActivity serviceModelActivity;
			if (timeout < TimeSpan.Zero)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("timeout", Microsoft.ServiceBus.SR.GetString(Resources.SFxTimeoutOutOfRange0, new object[0])));
			}
			if (!Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldUseActivity || !this.TraceOpenAndClose)
			{
				serviceModelActivity = null;
			}
			else
			{
				serviceModelActivity = this.CreateCloseActivity();
			}
			using (serviceModelActivity)
			{
				lock (this.ThisLock)
				{
					if (this.refCount == 0)
					{
						throw new InvalidOperationException(SRClient.InvalidRefcountedCommunicationObject);
					}
					RefcountedCommunicationObject refcountedCommunicationObject = this;
					int num = refcountedCommunicationObject.refCount - 1;
					int num1 = num;
					refcountedCommunicationObject.refCount = num;
					if (num1 <= 0)
					{
						communicationState = this.state;
						if (communicationState != CommunicationState.Closed)
						{
							this.state = CommunicationState.Closing;
						}
						this.closeCalled = true;
					}
					else
					{
						skippingOperationAsyncResult = new RefcountedCommunicationObject.SkippingOperationAsyncResult(callback, state);
						return skippingOperationAsyncResult;
					}
				}
				switch (communicationState)
				{
					case CommunicationState.Created:
					case CommunicationState.Opening:
					case CommunicationState.Faulted:
					{
						this.Abort();
						if (communicationState == CommunicationState.Faulted)
						{
							throw TraceUtility.ThrowHelperError(this.CreateFaultedException(), Guid.Empty, this);
						}
						skippingOperationAsyncResult = new RefcountedCommunicationObject.AlreadyClosedAsyncResult(callback, state);
						return skippingOperationAsyncResult;
					}
					case CommunicationState.Opened:
					{
						bool flag = true;
						try
						{
							this.OnClosing();
							if (!this.onClosingCalled)
							{
								throw TraceUtility.ThrowHelperError(this.CreateBaseClassMethodNotCalledException("OnClosing"), Guid.Empty, this);
							}
							IAsyncResult closeAsyncResult = new RefcountedCommunicationObject.CloseAsyncResult(this, timeout, callback, state);
							flag = false;
							skippingOperationAsyncResult = closeAsyncResult;
							return skippingOperationAsyncResult;
						}
						finally
						{
							if (flag)
							{
								if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceWarning)
								{
									DiagnosticTrace diagnosticTrace = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DiagnosticTrace;
									string traceCodeCommunicationObjectCloseFailed = Resources.TraceCodeCommunicationObjectCloseFailed;
									object[] str = new object[] { this.GetCommunicationObjectType().ToString() };
									diagnosticTrace.TraceEvent(TraceEventType.Warning, TraceCode.CommunicationObjectCloseFailed, Microsoft.ServiceBus.SR.GetString(traceCodeCommunicationObjectCloseFailed, str), null, null, this);
								}
								this.Abort();
							}
						}
						break;
					}
					case CommunicationState.Closing:
					case CommunicationState.Closed:
					{
						skippingOperationAsyncResult = new RefcountedCommunicationObject.AlreadyClosedAsyncResult(callback, state);
						return skippingOperationAsyncResult;
					}
				}
				throw Fx.AssertAndThrow("CommunicationObject.BeginClose: Unknown CommunicationState");
			}
			return skippingOperationAsyncResult;
		}

		public IAsyncResult BeginOpen(AsyncCallback callback, object state)
		{
			return this.BeginOpen(this.DefaultOpenTimeout, callback, state);
		}

		public IAsyncResult BeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			CommunicationState communicationState;
			IAsyncResult skippingOperationAsyncResult;
			if (timeout < TimeSpan.Zero)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("timeout", Microsoft.ServiceBus.SR.GetString(Resources.SFxTimeoutOutOfRange0, new object[0])));
			}
			lock (this.ThisLock)
			{
				this.ThrowIfDisposed();
				if (this.state != CommunicationState.Opened)
				{
					communicationState = this.state;
					if (this.state == CommunicationState.Created)
					{
						this.state = CommunicationState.Opening;
						if (!this.semaphore.TryEnter())
						{
							throw new Exception(SRClient.InvalidStateMachineRefcountedCommunicationObject);
						}
					}
				}
				else
				{
					skippingOperationAsyncResult = new RefcountedCommunicationObject.SkippingOperationAsyncResult(callback, state);
					return skippingOperationAsyncResult;
				}
			}
			if (communicationState == CommunicationState.Opening)
			{
				return new RefcountedCommunicationObject.AlreadyOpeningAsyncResult(this.semaphore, timeout, callback, state);
			}
			bool flag = true;
			try
			{
				this.OnOpening();
				if (!this.onOpeningCalled)
				{
					throw TraceUtility.ThrowHelperError(this.CreateBaseClassMethodNotCalledException("OnOpening"), Guid.Empty, this);
				}
				IAsyncResult openAsyncResult = new RefcountedCommunicationObject.OpenAsyncResult(this, timeout, callback, state);
				flag = false;
				skippingOperationAsyncResult = openAsyncResult;
			}
			finally
			{
				if (flag)
				{
					if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceWarning)
					{
						DiagnosticTrace diagnosticTrace = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DiagnosticTrace;
						string traceCodeCommunicationObjectOpenFailed = Resources.TraceCodeCommunicationObjectOpenFailed;
						object[] str = new object[] { this.GetCommunicationObjectType().ToString() };
						diagnosticTrace.TraceEvent(TraceEventType.Warning, TraceCode.CommunicationObjectOpenFailed, Microsoft.ServiceBus.SR.GetString(traceCodeCommunicationObjectOpenFailed, str), null, null, this);
					}
					this.Fault();
				}
			}
			return skippingOperationAsyncResult;
		}

		public void Close()
		{
			this.Close(this.DefaultCloseTimeout);
		}

		public void Close(TimeSpan timeout)
		{
			CommunicationState communicationState;
			ServiceModelActivity serviceModelActivity;
			if (timeout < TimeSpan.Zero)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("timeout", Microsoft.ServiceBus.SR.GetString(Resources.SFxTimeoutOutOfRange0, new object[0])));
			}
			if (!Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldUseActivity || !this.TraceOpenAndClose)
			{
				serviceModelActivity = null;
			}
			else
			{
				serviceModelActivity = this.CreateCloseActivity();
			}
			using (serviceModelActivity)
			{
				lock (this.ThisLock)
				{
					if (this.refCount == 0)
					{
						throw new InvalidOperationException(SRClient.InvalidRefcountedCommunicationObject);
					}
					RefcountedCommunicationObject refcountedCommunicationObject = this;
					int num = refcountedCommunicationObject.refCount - 1;
					int num1 = num;
					refcountedCommunicationObject.refCount = num;
					if (num1 <= 0)
					{
						communicationState = this.state;
						if (communicationState != CommunicationState.Closed)
						{
							this.state = CommunicationState.Closing;
						}
						this.closeCalled = true;
					}
					else
					{
						return;
					}
				}
				switch (communicationState)
				{
					case CommunicationState.Created:
					case CommunicationState.Opening:
					case CommunicationState.Faulted:
					{
						this.Abort();
						if (communicationState != CommunicationState.Faulted)
						{
							return;
						}
						throw TraceUtility.ThrowHelperError(this.CreateFaultedException(), Guid.Empty, this);
					}
					case CommunicationState.Opened:
					{
						bool flag = true;
						try
						{
							this.OnClosing();
							if (!this.onClosingCalled)
							{
								throw TraceUtility.ThrowHelperError(this.CreateBaseClassMethodNotCalledException("OnClosing"), Guid.Empty, this);
							}
							this.OnClose(timeout);
							this.OnClosed();
							if (!this.onClosedCalled)
							{
								throw TraceUtility.ThrowHelperError(this.CreateBaseClassMethodNotCalledException("OnClosed"), Guid.Empty, this);
							}
							flag = false;
							return;
						}
						finally
						{
							if (flag)
							{
								if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceWarning)
								{
									DiagnosticTrace diagnosticTrace = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DiagnosticTrace;
									string traceCodeCommunicationObjectCloseFailed = Resources.TraceCodeCommunicationObjectCloseFailed;
									object[] str = new object[] { this.GetCommunicationObjectType().ToString() };
									diagnosticTrace.TraceEvent(TraceEventType.Warning, TraceCode.CommunicationObjectCloseFailed, Microsoft.ServiceBus.SR.GetString(traceCodeCommunicationObjectCloseFailed, str), null, null, this);
								}
								this.Abort();
							}
						}
						break;
					}
					case CommunicationState.Closing:
					case CommunicationState.Closed:
					{
						return;
					}
				}
				throw Fx.AssertAndThrow("CommunicationObject.BeginClose: Unknown CommunicationState");
			}
		}

		internal Exception CreateAbortedException()
		{
			string communicationObjectAbortedStack2 = Resources.CommunicationObjectAbortedStack2;
			object[] str = new object[] { this.GetCommunicationObjectType().ToString(), string.Empty };
			return new CommunicationObjectAbortedException(Microsoft.ServiceBus.SR.GetString(communicationObjectAbortedStack2, str));
		}

		private Exception CreateBaseClassMethodNotCalledException(string method)
		{
			string communicationObjectBaseClassMethodNotCalled = Resources.CommunicationObjectBaseClassMethodNotCalled;
			object[] str = new object[] { this.GetCommunicationObjectType().ToString(), method };
			return new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(communicationObjectBaseClassMethodNotCalled, str));
		}

		private ServiceModelActivity CreateCloseActivity()
		{
			ServiceModelActivity serviceModelActivity = null;
			serviceModelActivity = ServiceModelActivity.CreateBoundedActivity();
			if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldUseActivity)
			{
				ServiceModelActivity.Start(serviceModelActivity, this.CloseActivityName, ActivityType.Close);
			}
			return serviceModelActivity;
		}

		internal Exception CreateClosedException()
		{
			if (!this.closeCalled)
			{
				return this.CreateAbortedException();
			}
			return new ObjectDisposedException(this.GetCommunicationObjectType().ToString());
		}

		private Exception CreateFaultedException()
		{
			string communicationObjectFaultedStack2 = Resources.CommunicationObjectFaultedStack2;
			object[] str = new object[] { this.GetCommunicationObjectType().ToString(), string.Empty };
			return new CommunicationObjectFaultedException(Microsoft.ServiceBus.SR.GetString(communicationObjectFaultedStack2, str));
		}

		public void EndClose(IAsyncResult result)
		{
			if (result is RefcountedCommunicationObject.AlreadyClosedAsyncResult)
			{
				CompletedAsyncResult.End(result);
				return;
			}
			if (result is RefcountedCommunicationObject.SkippingOperationAsyncResult)
			{
				CompletedAsyncResult.End(result);
				return;
			}
			RefcountedCommunicationObject.CloseAsyncResult.End(result);
		}

		public void EndOpen(IAsyncResult result)
		{
			if (result is RefcountedCommunicationObject.AlreadyOpeningAsyncResult)
			{
				RefcountedCommunicationObject.AlreadyOpeningAsyncResult.End(result);
				return;
			}
			if (result is RefcountedCommunicationObject.SkippingOperationAsyncResult)
			{
				CompletedAsyncResult.End(result);
				return;
			}
			RefcountedCommunicationObject.OpenAsyncResult.End(result);
		}

		protected void Fault()
		{
			lock (this.ThisLock)
			{
				if (this.state == CommunicationState.Closed || this.state == CommunicationState.Closing)
				{
					return;
				}
				else if (this.state != CommunicationState.Faulted)
				{
					this.state = CommunicationState.Faulted;
				}
				else
				{
					return;
				}
			}
			this.OnFaulted();
		}

		protected virtual Type GetCommunicationObjectType()
		{
			return this.GetType();
		}

		protected abstract void OnAbort();

		protected abstract IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state);

		protected abstract IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state);

		protected abstract void OnClose(TimeSpan timeout);

		protected virtual void OnClosed()
		{
			this.onClosedCalled = true;
			lock (this.ThisLock)
			{
				if (!this.raisedClosed)
				{
					this.raisedClosed = true;
					this.state = CommunicationState.Closed;
				}
				else
				{
					return;
				}
			}
			if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceVerbose)
			{
				DiagnosticTrace diagnosticTrace = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DiagnosticTrace;
				string traceCodeCommunicationObjectClosed = Resources.TraceCodeCommunicationObjectClosed;
				object[] objArray = new object[] { DiagnosticTrace.CreateSourceString(this) };
				diagnosticTrace.TraceEvent(TraceEventType.Verbose, TraceCode.CommunicationObjectClosed, Microsoft.ServiceBus.SR.GetString(traceCodeCommunicationObjectClosed, objArray), null, null, this);
			}
			EventHandler eventHandler = this.Closed;
			if (eventHandler != null)
			{
				try
				{
					eventHandler(this.eventSender, EventArgs.Empty);
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (!Fx.IsFatal(exception))
					{
						throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperCallback(exception);
					}
					throw;
				}
			}
		}

		protected virtual void OnClosing()
		{
			this.onClosingCalled = true;
			lock (this.ThisLock)
			{
				if (!this.raisedClosing)
				{
					this.raisedClosing = true;
				}
				else
				{
					return;
				}
			}
			if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceVerbose)
			{
				DiagnosticTrace diagnosticTrace = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DiagnosticTrace;
				string traceCodeCommunicationObjectClosing = Resources.TraceCodeCommunicationObjectClosing;
				object[] objArray = new object[] { DiagnosticTrace.CreateSourceString(this) };
				diagnosticTrace.TraceEvent(TraceEventType.Verbose, TraceCode.CommunicationObjectClosing, Microsoft.ServiceBus.SR.GetString(traceCodeCommunicationObjectClosing, objArray), null, null, this);
			}
			EventHandler eventHandler = this.Closing;
			if (eventHandler != null)
			{
				try
				{
					eventHandler(this.eventSender, EventArgs.Empty);
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (!Fx.IsFatal(exception))
					{
						throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperCallback(exception);
					}
					throw;
				}
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
			if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceWarning)
			{
				DiagnosticTrace diagnosticTrace = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DiagnosticTrace;
				string traceCodeCommunicationObjectFaulted = Resources.TraceCodeCommunicationObjectFaulted;
				object[] str = new object[] { this.GetCommunicationObjectType().ToString() };
				diagnosticTrace.TraceEvent(TraceEventType.Warning, TraceCode.CommunicationObjectFaulted, Microsoft.ServiceBus.SR.GetString(traceCodeCommunicationObjectFaulted, str), null, null, this);
			}
			EventHandler eventHandler = this.Faulted;
			if (eventHandler != null)
			{
				try
				{
					eventHandler(this.eventSender, EventArgs.Empty);
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (!Fx.IsFatal(exception))
					{
						throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperCallback(exception);
					}
					throw;
				}
			}
		}

		protected abstract void OnOpen(TimeSpan timeout);

		protected virtual void OnOpened()
		{
			this.onOpenedCalled = true;
			lock (this.ThisLock)
			{
				if (this.aborted || this.state != CommunicationState.Opening)
				{
					return;
				}
				else
				{
					this.state = CommunicationState.Opened;
				}
			}
			if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceVerbose)
			{
				DiagnosticTrace diagnosticTrace = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DiagnosticTrace;
				string traceCodeCommunicationObjectOpened = Resources.TraceCodeCommunicationObjectOpened;
				object[] objArray = new object[] { DiagnosticTrace.CreateSourceString(this) };
				diagnosticTrace.TraceEvent(TraceEventType.Verbose, TraceCode.CommunicationObjectOpened, Microsoft.ServiceBus.SR.GetString(traceCodeCommunicationObjectOpened, objArray), null, null, this);
			}
			EventHandler eventHandler = this.Opened;
			if (eventHandler != null)
			{
				try
				{
					eventHandler(this.eventSender, EventArgs.Empty);
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (!Fx.IsFatal(exception))
					{
						throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperCallback(exception);
					}
					throw;
				}
			}
		}

		protected virtual void OnOpening()
		{
			this.onOpeningCalled = true;
			if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceVerbose)
			{
				DiagnosticTrace diagnosticTrace = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DiagnosticTrace;
				string traceCodeCommunicationObjectOpening = Resources.TraceCodeCommunicationObjectOpening;
				object[] objArray = new object[] { DiagnosticTrace.CreateSourceString(this) };
				diagnosticTrace.TraceEvent(TraceEventType.Verbose, TraceCode.CommunicationObjectOpening, Microsoft.ServiceBus.SR.GetString(traceCodeCommunicationObjectOpening, objArray), null, null, this);
			}
			EventHandler eventHandler = this.Opening;
			if (eventHandler != null)
			{
				try
				{
					eventHandler(this.eventSender, EventArgs.Empty);
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (!Fx.IsFatal(exception))
					{
						throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperCallback(exception);
					}
					throw;
				}
			}
		}

		public void Open()
		{
			this.Open(this.DefaultOpenTimeout);
		}

		public void Open(TimeSpan timeout)
		{
			CommunicationState communicationState;
			ServiceModelActivity serviceModelActivity;
			if (timeout < TimeSpan.Zero)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("timeout", Microsoft.ServiceBus.SR.GetString(Resources.SFxTimeoutOutOfRange0, new object[0])));
			}
			if (!Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldUseActivity || !this.TraceOpenAndClose)
			{
				serviceModelActivity = null;
			}
			else
			{
				serviceModelActivity = ServiceModelActivity.CreateBoundedActivity();
			}
			using (ServiceModelActivity serviceModelActivity1 = serviceModelActivity)
			{
				lock (this.ThisLock)
				{
					this.ThrowIfDisposed();
					if (this.state != CommunicationState.Opened)
					{
						communicationState = this.state;
						if (this.state == CommunicationState.Created)
						{
							this.state = CommunicationState.Opening;
							if (!this.semaphore.TryEnter())
							{
								throw new Exception(SRClient.InvalidStateMachineRefcountedCommunicationObject);
							}
						}
					}
					else
					{
						return;
					}
				}
				if (communicationState != CommunicationState.Opening)
				{
					if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldUseActivity)
					{
						ServiceModelActivity.Start(serviceModelActivity1, this.OpenActivityName, this.OpenActivityType);
					}
					bool flag = true;
					try
					{
						this.OnOpening();
						if (!this.onOpeningCalled)
						{
							throw TraceUtility.ThrowHelperError(this.CreateBaseClassMethodNotCalledException("OnOpening"), Guid.Empty, this);
						}
						this.OnOpen(timeout);
						this.OnOpened();
						if (!this.onOpenedCalled)
						{
							throw TraceUtility.ThrowHelperError(this.CreateBaseClassMethodNotCalledException("OnOpened"), Guid.Empty, this);
						}
						flag = false;
					}
					finally
					{
						if (flag)
						{
							if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceWarning)
							{
								DiagnosticTrace diagnosticTrace = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DiagnosticTrace;
								string traceCodeCommunicationObjectOpenFailed = Resources.TraceCodeCommunicationObjectOpenFailed;
								object[] str = new object[] { this.GetCommunicationObjectType().ToString() };
								diagnosticTrace.TraceEvent(TraceEventType.Warning, TraceCode.CommunicationObjectOpenFailed, Microsoft.ServiceBus.SR.GetString(traceCodeCommunicationObjectOpenFailed, str), null, null, this);
							}
							this.Fault();
						}
						this.semaphore.Exit();
					}
				}
				else
				{
					this.semaphore.Enter(timeout);
					this.semaphore.Exit();
				}
			}
		}

		protected internal void ThrowIfDisposed()
		{
			switch (this.state)
			{
				case CommunicationState.Created:
				case CommunicationState.Opening:
				case CommunicationState.Opened:
				{
					return;
				}
				case CommunicationState.Closing:
				{
					throw TraceUtility.ThrowHelperError(this.CreateClosedException(), Guid.Empty, this);
				}
				case CommunicationState.Closed:
				{
					throw TraceUtility.ThrowHelperError(this.CreateClosedException(), Guid.Empty, this);
				}
				case CommunicationState.Faulted:
				{
					throw TraceUtility.ThrowHelperError(this.CreateFaultedException(), Guid.Empty, this);
				}
			}
			throw Fx.AssertAndThrow("ThrowIfDisposed: Unknown CommunicationObject.state");
		}

		public event EventHandler Closed;

		public event EventHandler Closing;

		public event EventHandler Faulted;

		public event EventHandler Opened;

		public event EventHandler Opening;

		private class AlreadyClosedAsyncResult : CompletedAsyncResult
		{
			public AlreadyClosedAsyncResult(AsyncCallback callback, object state) : base(callback, state)
			{
			}
		}

		private class AlreadyOpeningAsyncResult : AsyncResult
		{
			private ThreadNeutralSemaphore semaphore;

			public AlreadyOpeningAsyncResult(ThreadNeutralSemaphore semaphore, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				this.semaphore = semaphore;
				this.semaphore.Enter(new Action<object>(this.Callback), this);
			}

			private void Callback(object state)
			{
				((RefcountedCommunicationObject.AlreadyOpeningAsyncResult)state).Complete(false);
				this.semaphore.Exit();
			}

			public static new void End(IAsyncResult result)
			{
				AsyncResult.End<RefcountedCommunicationObject.AlreadyOpeningAsyncResult>(result);
			}
		}

		private class CloseAsyncResult : AsyncResult
		{
			private RefcountedCommunicationObject communicationObject;

			private static AsyncCallback onCloseComplete;

			static CloseAsyncResult()
			{
				RefcountedCommunicationObject.CloseAsyncResult.onCloseComplete = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(RefcountedCommunicationObject.CloseAsyncResult.OnCloseComplete));
			}

			public CloseAsyncResult(RefcountedCommunicationObject communicationObject, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				this.communicationObject = communicationObject;
				IAsyncResult asyncResult = this.communicationObject.OnBeginClose(timeout, RefcountedCommunicationObject.CloseAsyncResult.onCloseComplete, this);
				if (asyncResult.CompletedSynchronously)
				{
					this.HandleCloseComplete(asyncResult);
					base.Complete(true);
				}
			}

			public static new void End(IAsyncResult result)
			{
				AsyncResult.End<RefcountedCommunicationObject.CloseAsyncResult>(result);
			}

			private void HandleCloseComplete(IAsyncResult result)
			{
				this.communicationObject.OnEndClose(result);
				this.communicationObject.OnClosed();
				if (!this.communicationObject.onClosedCalled)
				{
					throw TraceUtility.ThrowHelperError(this.communicationObject.CreateBaseClassMethodNotCalledException("OnClosed"), Guid.Empty, this.communicationObject);
				}
			}

			private static void OnCloseComplete(IAsyncResult result)
			{
				if (result.CompletedSynchronously)
				{
					return;
				}
				RefcountedCommunicationObject.CloseAsyncResult asyncState = (RefcountedCommunicationObject.CloseAsyncResult)result.AsyncState;
				using (Activity activity = ServiceModelActivity.BoundOperation(null))
				{
					Exception exception = null;
					try
					{
						asyncState.HandleCloseComplete(result);
					}
					catch (Exception exception2)
					{
						Exception exception1 = exception2;
						if (Fx.IsFatal(exception1))
						{
							throw;
						}
						exception = exception1;
						if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceWarning)
						{
							DiagnosticTrace diagnosticTrace = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DiagnosticTrace;
							string traceCodeCommunicationObjectCloseFailed = Resources.TraceCodeCommunicationObjectCloseFailed;
							object[] str = new object[] { asyncState.communicationObject.GetCommunicationObjectType().ToString() };
							diagnosticTrace.TraceEvent(TraceEventType.Warning, TraceCode.CommunicationObjectCloseFailed, Microsoft.ServiceBus.SR.GetString(traceCodeCommunicationObjectCloseFailed, str), null, null, asyncState);
						}
						asyncState.communicationObject.Abort();
					}
					asyncState.Complete(false, exception);
				}
			}
		}

		private class OpenAsyncResult : AsyncResult
		{
			private RefcountedCommunicationObject communicationObject;

			private static AsyncCallback onOpenComplete;

			static OpenAsyncResult()
			{
				RefcountedCommunicationObject.OpenAsyncResult.onOpenComplete = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(RefcountedCommunicationObject.OpenAsyncResult.OnOpenComplete));
			}

			public OpenAsyncResult(RefcountedCommunicationObject communicationObject, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				this.communicationObject = communicationObject;
				IAsyncResult asyncResult = this.communicationObject.OnBeginOpen(timeout, RefcountedCommunicationObject.OpenAsyncResult.onOpenComplete, this);
				if (asyncResult.CompletedSynchronously)
				{
					this.HandleOpenComplete(asyncResult);
					base.Complete(true);
				}
			}

			public static new void End(IAsyncResult result)
			{
				AsyncResult.End<RefcountedCommunicationObject.OpenAsyncResult>(result);
			}

			private void HandleOpenComplete(IAsyncResult result)
			{
				try
				{
					this.communicationObject.OnEndOpen(result);
					this.communicationObject.OnOpened();
					if (!this.communicationObject.onOpenedCalled)
					{
						throw TraceUtility.ThrowHelperError(this.communicationObject.CreateBaseClassMethodNotCalledException("OnOpened"), Guid.Empty, this.communicationObject);
					}
				}
				finally
				{
					this.communicationObject.semaphore.Exit();
				}
			}

			private static void OnOpenComplete(IAsyncResult result)
			{
				if (result.CompletedSynchronously)
				{
					return;
				}
				Exception exception = null;
				RefcountedCommunicationObject.OpenAsyncResult asyncState = (RefcountedCommunicationObject.OpenAsyncResult)result.AsyncState;
				try
				{
					asyncState.HandleOpenComplete(result);
				}
				catch (Exception exception2)
				{
					Exception exception1 = exception2;
					if (Fx.IsFatal(exception1))
					{
						throw;
					}
					exception = exception1;
					if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceWarning)
					{
						DiagnosticTrace diagnosticTrace = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DiagnosticTrace;
						string traceCodeCommunicationObjectOpenFailed = Resources.TraceCodeCommunicationObjectOpenFailed;
						object[] str = new object[] { asyncState.communicationObject.GetCommunicationObjectType().ToString() };
						diagnosticTrace.TraceEvent(TraceEventType.Warning, TraceCode.CommunicationObjectOpenFailed, Microsoft.ServiceBus.SR.GetString(traceCodeCommunicationObjectOpenFailed, str), null, null, asyncState);
					}
					asyncState.communicationObject.Fault();
				}
				asyncState.Complete(false, exception);
			}
		}

		private class SkippingOperationAsyncResult : CompletedAsyncResult
		{
			public SkippingOperationAsyncResult(AsyncCallback callback, object state) : base(callback, state)
			{
			}
		}
	}
}