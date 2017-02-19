using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.Diagnostics;
using System.Globalization;
using System.ServiceModel;

namespace Microsoft.ServiceBus.Channels
{
	internal class ConnectionAcceptor
	{
		private int maxAccepts;

		private int maxPendingConnections;

		private int connections;

		private int pendingAccepts;

		private IConnectionListener listener;

		private AsyncCallback acceptCompletedCallback;

		private Action<object> scheduleAcceptCallback;

		private Action onConnectionDequeued;

		private bool isDisposed;

		private ConnectionAvailableCallback callback;

		private ErrorCallback errorCallback;

		public int ConnectionCount
		{
			get
			{
				return this.connections;
			}
		}

		private bool IsAcceptNecessary
		{
			get
			{
				if (this.pendingAccepts >= this.maxAccepts || this.connections + this.pendingAccepts >= this.maxPendingConnections)
				{
					return false;
				}
				return !this.isDisposed;
			}
		}

		private object ThisLock
		{
			get
			{
				return this;
			}
		}

		public ConnectionAcceptor(IConnectionListener listener, int maxAccepts, int maxPendingConnections, ConnectionAvailableCallback callback, ErrorCallback errorCallback)
		{
			if (maxAccepts <= 0)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("maxAccepts", (object)maxAccepts, Microsoft.ServiceBus.SR.GetString(Resources.ValueMustBePositive, new object[0])));
			}
			Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DebugAssert(maxPendingConnections > 0, "maxPendingConnections must be positive");
			this.listener = listener;
			this.maxAccepts = maxAccepts;
			this.maxPendingConnections = maxPendingConnections;
			this.callback = callback;
			this.errorCallback = errorCallback;
			this.onConnectionDequeued = new Action(this.OnConnectionDequeued);
			this.acceptCompletedCallback = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(this.AcceptCompletedCallback));
			this.scheduleAcceptCallback = new Action<object>(this.ScheduleAcceptCallback);
		}

		public void Abort()
		{
			lock (this.ThisLock)
			{
				if (!this.isDisposed)
				{
					this.isDisposed = true;
					this.listener.Abort();
				}
			}
		}

		private void AcceptCompletedCallback(IAsyncResult result)
		{
			if (result.CompletedSynchronously)
			{
				return;
			}
			this.HandleCompletedAccept(result);
		}

		private void AcceptIfNecessary(bool startAccepting)
		{
			if (this.IsAcceptNecessary)
			{
				lock (this.ThisLock)
				{
					while (this.IsAcceptNecessary)
					{
						IAsyncResult asyncResult = null;
						Exception exception = null;
						try
						{
							asyncResult = this.listener.BeginAccept(this.acceptCompletedCallback, null);
						}
						catch (CommunicationException communicationException1)
						{
							CommunicationException communicationException = communicationException1;
							if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceInformation)
							{
								Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.TraceHandledException(communicationException, TraceEventType.Information);
							}
						}
						catch (Exception exception2)
						{
							Exception exception1 = exception2;
							if (Fx.IsFatal(exception1))
							{
								throw;
							}
							if (startAccepting)
							{
								throw;
							}
							if (this.errorCallback == null && !ExceptionHandler.HandleTransportExceptionHelper(exception1))
							{
								throw;
							}
							exception = exception1;
						}
						if (exception != null && this.errorCallback != null)
						{
							this.errorCallback(exception);
						}
						if (asyncResult == null)
						{
							continue;
						}
						if (asyncResult.CompletedSynchronously)
						{
							IOThreadScheduler.ScheduleCallbackNoFlow(this.scheduleAcceptCallback, asyncResult);
						}
						ConnectionAcceptor connectionAcceptor = this;
						connectionAcceptor.pendingAccepts = connectionAcceptor.pendingAccepts + 1;
					}
				}
			}
		}

		public void Close(TimeSpan timeout)
		{
			lock (this.ThisLock)
			{
				if (!this.isDisposed)
				{
					this.isDisposed = true;
					this.listener.Close(timeout);
				}
			}
		}

		private void HandleCompletedAccept(IAsyncResult result)
		{
			IConnection connection = null;
			lock (this.ThisLock)
			{
				bool flag = false;
				Exception exception = null;
				try
				{
					try
					{
						if (!this.isDisposed)
						{
							connection = this.listener.EndAccept(result);
							if (connection != null)
							{
								if (this.connections + 1 >= this.maxPendingConnections && Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceWarning)
								{
									TraceUtility.TraceEvent(TraceEventType.Warning, TraceCode.MaxPendingConnectionsReached, new StringTraceRecord("MaxPendingConnections", this.maxPendingConnections.ToString(CultureInfo.InvariantCulture)), this, null);
								}
								ConnectionAcceptor connectionAcceptor = this;
								connectionAcceptor.connections = connectionAcceptor.connections + 1;
							}
						}
						flag = true;
					}
					catch (CommunicationException communicationException1)
					{
						CommunicationException communicationException = communicationException1;
						if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceInformation)
						{
							Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.TraceHandledException(communicationException, TraceEventType.Information);
						}
					}
					catch (Exception exception2)
					{
						Exception exception1 = exception2;
						if (Fx.IsFatal(exception1))
						{
							throw;
						}
						if (this.errorCallback == null && !ExceptionHandler.HandleTransportExceptionHelper(exception1))
						{
							throw;
						}
						exception = exception1;
					}
				}
				finally
				{
					if (!flag)
					{
						connection = null;
					}
					ConnectionAcceptor connectionAcceptor1 = this;
					connectionAcceptor1.pendingAccepts = connectionAcceptor1.pendingAccepts - 1;
				}
				if (exception != null && this.errorCallback != null)
				{
					this.errorCallback(exception);
				}
			}
			this.AcceptIfNecessary(false);
			if (connection != null)
			{
				this.callback(connection, this.onConnectionDequeued);
			}
		}

		private void OnConnectionDequeued()
		{
			lock (this.ThisLock)
			{
				ConnectionAcceptor connectionAcceptor = this;
				connectionAcceptor.connections = connectionAcceptor.connections - 1;
			}
			this.AcceptIfNecessary(false);
		}

		public void Open(TimeSpan timeout)
		{
			this.listener.Open(timeout);
			this.AcceptIfNecessary(true);
		}

		private void ScheduleAcceptCallback(object state)
		{
			this.HandleCompletedAccept((IAsyncResult)state);
		}
	}
}