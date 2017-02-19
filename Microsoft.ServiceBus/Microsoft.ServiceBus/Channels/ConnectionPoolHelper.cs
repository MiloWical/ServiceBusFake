using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.Diagnostics;
using System.Reflection;
using System.ServiceModel;

namespace Microsoft.ServiceBus.Channels
{
	internal abstract class ConnectionPoolHelper
	{
		private IConnectionInitiator connectionInitiator;

		private ConnectionPool connectionPool;

		private Uri via;

		private bool closed;

		private string connectionKey;

		private bool isConnectionFromPool;

		private IConnection rawConnection;

		private IConnection upgradedConnection;

		private object ThisLock
		{
			get
			{
				return this;
			}
		}

		public ConnectionPoolHelper(ConnectionPool connectionPool, IConnectionInitiator connectionInitiator, Uri via)
		{
			this.connectionInitiator = connectionInitiator;
			this.connectionPool = connectionPool;
			this.via = via;
		}

		public void Abort()
		{
			this.ReleaseConnection(true, TimeSpan.Zero);
		}

		protected abstract IConnection AcceptPooledConnection(IConnection connection, ref TimeoutHelper timeoutHelper);

		protected abstract IAsyncResult BeginAcceptPooledConnection(IConnection connection, ref TimeoutHelper timeoutHelper, AsyncCallback callback, object state);

		public IAsyncResult BeginEstablishConnection(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new ConnectionPoolHelper.EstablishConnectionAsyncResult(this, timeout, callback, state);
		}

		public void Close(TimeSpan timeout)
		{
			this.ReleaseConnection(false, timeout);
		}

		protected abstract TimeoutException CreateNewConnectionTimeoutException(TimeSpan timeout, TimeoutException innerException);

		protected abstract IConnection EndAcceptPooledConnection(IAsyncResult result);

		public static IConnection EndEstablishConnection(IAsyncResult result)
		{
			return ConnectionPoolHelper.EstablishConnectionAsyncResult.End(result);
		}

		public IConnection EstablishConnection(TimeSpan timeout)
		{
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			IConnection connection = null;
			IConnection connection1 = null;
			bool flag = true;
			while (flag)
			{
				connection = this.TakeConnection(timeoutHelper.RemainingTime());
				if (connection != null)
				{
					bool flag1 = false;
					try
					{
						try
						{
							connection1 = this.AcceptPooledConnection(connection, ref timeoutHelper);
							flag1 = true;
							break;
						}
						catch (CommunicationException communicationException1)
						{
							CommunicationException communicationException = communicationException1;
							Fx.Exception.TraceHandled(communicationException, string.Concat(this.GetType(), ".EstablishConnection"), null);
						}
						catch (TimeoutException timeoutException1)
						{
							TimeoutException timeoutException = timeoutException1;
							Fx.Exception.TraceHandled(timeoutException, string.Concat(this.GetType(), ".EstablishConnection"), null);
						}
					}
					finally
					{
						if (!flag1)
						{
							if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceInformation)
							{
								DiagnosticTrace diagnosticTrace = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DiagnosticTrace;
								string traceCodeFailedAcceptFromPool = Resources.TraceCodeFailedAcceptFromPool;
								object[] objArray = new object[] { timeoutHelper.RemainingTime() };
								diagnosticTrace.TraceEvent(TraceEventType.Information, TraceCode.FailedAcceptFromPool, Microsoft.ServiceBus.SR.GetString(traceCodeFailedAcceptFromPool, objArray));
							}
							this.connectionPool.ReturnConnection(this.connectionKey, connection, false, TimeSpan.Zero);
						}
					}
				}
				else
				{
					flag = false;
				}
			}
			if (!flag)
			{
				bool flag2 = false;
				TimeSpan timeSpan = timeoutHelper.RemainingTime();
				try
				{
					try
					{
						connection = this.connectionInitiator.Connect(this.via, timeSpan);
					}
					catch (TimeoutException timeoutException3)
					{
						TimeoutException timeoutException2 = timeoutException3;
						throw Fx.Exception.AsInformation(this.CreateNewConnectionTimeoutException(timeSpan, timeoutException2), null);
					}
					this.connectionInitiator = null;
					connection1 = this.AcceptPooledConnection(connection, ref timeoutHelper);
					flag2 = true;
				}
				finally
				{
					if (!flag2)
					{
						this.connectionKey = null;
						if (connection != null)
						{
							connection.Abort();
						}
					}
				}
			}
			this.SnapshotConnection(connection1, connection, flag);
			return connection1;
		}

		private void ReleaseConnection(bool abort, TimeSpan timeout)
		{
			string str;
			IConnection connection;
			IConnection connection1;
			lock (this.ThisLock)
			{
				this.closed = true;
				str = this.connectionKey;
				connection = this.upgradedConnection;
				connection1 = this.rawConnection;
				this.upgradedConnection = null;
				this.rawConnection = null;
			}
			if (connection == null)
			{
				return;
			}
			try
			{
				if (this.isConnectionFromPool)
				{
					this.connectionPool.ReturnConnection(str, connection1, !abort, timeout);
				}
				else if (!abort)
				{
					this.connectionPool.AddConnection(str, connection1, timeout);
				}
				else
				{
					connection.Abort();
				}
			}
			catch (CommunicationException communicationException1)
			{
				CommunicationException communicationException = communicationException1;
				Fx.Exception.TraceHandled(communicationException, string.Concat(this.GetType().Name, ".ReleaseConnection"), null);
				connection.Abort();
			}
		}

		private void SnapshotConnection(IConnection upgradedConnection, IConnection rawConnection, bool isConnectionFromPool)
		{
			lock (this.ThisLock)
			{
				if (this.closed)
				{
					upgradedConnection.Abort();
					if (isConnectionFromPool)
					{
						this.connectionPool.ReturnConnection(this.connectionKey, rawConnection, false, TimeSpan.Zero);
					}
					ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
					string operationAbortedDuringConnectionEstablishment = Resources.OperationAbortedDuringConnectionEstablishment;
					object[] objArray = new object[] { this.via };
					throw exceptionUtility.ThrowHelperError(new CommunicationObjectAbortedException(Microsoft.ServiceBus.SR.GetString(operationAbortedDuringConnectionEstablishment, objArray)));
				}
				this.upgradedConnection = upgradedConnection;
				this.rawConnection = rawConnection;
				this.isConnectionFromPool = isConnectionFromPool;
			}
		}

		private IConnection TakeConnection(TimeSpan timeout)
		{
			return this.connectionPool.TakeConnection(null, this.via, timeout, out this.connectionKey);
		}

		private class EstablishConnectionAsyncResult : AsyncResult
		{
			private ConnectionPoolHelper parent;

			private TimeoutHelper timeoutHelper;

			private IConnection currentConnection;

			private IConnection rawConnection;

			private bool newConnection;

			private bool cleanupConnection;

			private TimeSpan connectTimeout;

			private static AsyncCallback onConnect;

			private static AsyncCallback onProcessConnection;

			static EstablishConnectionAsyncResult()
			{
				ConnectionPoolHelper.EstablishConnectionAsyncResult.onProcessConnection = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(ConnectionPoolHelper.EstablishConnectionAsyncResult.OnProcessConnection));
			}

			public EstablishConnectionAsyncResult(ConnectionPoolHelper parent, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				this.parent = parent;
				this.timeoutHelper = new TimeoutHelper(timeout);
				bool flag = false;
				bool flag1 = false;
				try
				{
					flag1 = this.Begin();
					flag = true;
				}
				finally
				{
					if (!flag)
					{
						this.Cleanup();
					}
				}
				if (flag1)
				{
					this.Cleanup();
					base.Complete(true);
				}
			}

			private bool Begin()
			{
				bool flag;
				IConnection connection = this.parent.TakeConnection(this.timeoutHelper.RemainingTime());
				this.TrackConnection(connection);
				if (this.OpenUsingConnectionPool(out flag))
				{
					return true;
				}
				if (flag)
				{
					return false;
				}
				return this.OpenUsingNewConnection();
			}

			private void Cleanup()
			{
				if (this.cleanupConnection)
				{
					if (this.newConnection)
					{
						if (this.currentConnection != null)
						{
							this.currentConnection.Abort();
							this.currentConnection = null;
						}
					}
					else if (this.rawConnection != null)
					{
						if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceInformation)
						{
							DiagnosticTrace diagnosticTrace = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DiagnosticTrace;
							string traceCodeFailedAcceptFromPool = Resources.TraceCodeFailedAcceptFromPool;
							object[] objArray = new object[] { this.timeoutHelper.RemainingTime() };
							diagnosticTrace.TraceEvent(TraceEventType.Information, TraceCode.FailedAcceptFromPool, Microsoft.ServiceBus.SR.GetString(traceCodeFailedAcceptFromPool, objArray));
						}
						this.parent.connectionPool.ReturnConnection(this.parent.connectionKey, this.rawConnection, false, this.timeoutHelper.RemainingTime());
						this.currentConnection = null;
						this.rawConnection = null;
					}
					this.cleanupConnection = false;
				}
			}

			public static new IConnection End(IAsyncResult result)
			{
				return AsyncResult.End<ConnectionPoolHelper.EstablishConnectionAsyncResult>(result).currentConnection;
			}

			private bool HandleConnect(IAsyncResult connectResult)
			{
				try
				{
					this.TrackConnection(this.parent.connectionInitiator.EndConnect(connectResult));
				}
				catch (TimeoutException timeoutException1)
				{
					TimeoutException timeoutException = timeoutException1;
					throw Fx.Exception.AsWarning(this.parent.CreateNewConnectionTimeoutException(this.connectTimeout, timeoutException), null);
				}
				if (!this.ProcessConnection())
				{
					return false;
				}
				this.SnapshotConnection();
				return true;
			}

			private bool HandleProcessConnection(IAsyncResult result)
			{
				this.currentConnection = this.parent.EndAcceptPooledConnection(result);
				this.cleanupConnection = false;
				return true;
			}

			private static void OnConnect(IAsyncResult result)
			{
				bool flag;
				if (result.CompletedSynchronously)
				{
					return;
				}
				ConnectionPoolHelper.EstablishConnectionAsyncResult asyncState = (ConnectionPoolHelper.EstablishConnectionAsyncResult)result.AsyncState;
				Exception exception = null;
				try
				{
					flag = asyncState.HandleConnect(result);
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
					asyncState.Cleanup();
					asyncState.Complete(false, exception);
				}
			}

			private static void OnProcessConnection(IAsyncResult result)
			{
				bool flag;
				if (result.CompletedSynchronously)
				{
					return;
				}
				ConnectionPoolHelper.EstablishConnectionAsyncResult asyncState = (ConnectionPoolHelper.EstablishConnectionAsyncResult)result.AsyncState;
				Exception exception = null;
				try
				{
					bool flag1 = false;
					try
					{
						flag = asyncState.HandleProcessConnection(result);
						if (flag)
						{
							flag1 = true;
						}
					}
					catch (CommunicationException communicationException1)
					{
						CommunicationException communicationException = communicationException1;
						if (asyncState.newConnection)
						{
							flag = true;
							exception = communicationException;
						}
						else
						{
							Fx.Exception.TraceHandled(communicationException, string.Concat(asyncState.GetType(), ".OnProcessConnection"), asyncState.Activity);
							asyncState.Cleanup();
							flag = asyncState.Begin();
						}
					}
					catch (TimeoutException timeoutException1)
					{
						TimeoutException timeoutException = timeoutException1;
						if (asyncState.newConnection)
						{
							flag = true;
							exception = timeoutException;
						}
						else
						{
							Fx.Exception.TraceHandled(timeoutException, string.Concat(asyncState.GetType(), ".OnProcessConnection"), asyncState.Activity);
							asyncState.Cleanup();
							flag = asyncState.Begin();
						}
					}
					if (flag1)
					{
						asyncState.SnapshotConnection();
					}
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
					asyncState.Cleanup();
					asyncState.Complete(false, exception);
				}
			}

			private bool OpenUsingConnectionPool(out bool openingFromPool)
			{
				openingFromPool = true;
				while (this.currentConnection != null)
				{
					bool flag = false;
					try
					{
						if (!this.ProcessConnection())
						{
							return false;
						}
						else
						{
							flag = true;
						}
					}
					catch (CommunicationException communicationException1)
					{
						CommunicationException communicationException = communicationException1;
						Fx.Exception.TraceHandled(communicationException, string.Concat(base.GetType(), ".OpenUsingConnectionPool"), null);
						this.Cleanup();
					}
					catch (TimeoutException timeoutException1)
					{
						TimeoutException timeoutException = timeoutException1;
						Fx.Exception.TraceHandled(timeoutException, string.Concat(base.GetType(), ".OpenUsingConnectionPool"), null);
						this.Cleanup();
					}
					if (flag)
					{
						this.SnapshotConnection();
						return true;
					}
					IConnection connection = this.parent.TakeConnection(this.timeoutHelper.RemainingTime());
					this.TrackConnection(connection);
				}
				openingFromPool = false;
				return false;
			}

			private bool OpenUsingNewConnection()
			{
				IAsyncResult asyncResult;
				this.newConnection = true;
				try
				{
					this.connectTimeout = this.timeoutHelper.RemainingTime();
					if (ConnectionPoolHelper.EstablishConnectionAsyncResult.onConnect == null)
					{
						ConnectionPoolHelper.EstablishConnectionAsyncResult.onConnect = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(ConnectionPoolHelper.EstablishConnectionAsyncResult.OnConnect));
					}
					asyncResult = this.parent.connectionInitiator.BeginConnect(this.parent.via, this.connectTimeout, ConnectionPoolHelper.EstablishConnectionAsyncResult.onConnect, this);
				}
				catch (TimeoutException timeoutException1)
				{
					TimeoutException timeoutException = timeoutException1;
					throw Fx.Exception.AsWarning(this.parent.CreateNewConnectionTimeoutException(this.connectTimeout, timeoutException), null);
				}
				if (!asyncResult.CompletedSynchronously)
				{
					return false;
				}
				return this.HandleConnect(asyncResult);
			}

			private bool ProcessConnection()
			{
				IAsyncResult asyncResult = this.parent.BeginAcceptPooledConnection(this.rawConnection, ref this.timeoutHelper, ConnectionPoolHelper.EstablishConnectionAsyncResult.onProcessConnection, this);
				if (!asyncResult.CompletedSynchronously)
				{
					return false;
				}
				return this.HandleProcessConnection(asyncResult);
			}

			private void SnapshotConnection()
			{
				this.parent.SnapshotConnection(this.currentConnection, this.rawConnection, !this.newConnection);
			}

			private void TrackConnection(IConnection connection)
			{
				this.cleanupConnection = true;
				this.rawConnection = connection;
				this.currentConnection = connection;
			}
		}
	}
}