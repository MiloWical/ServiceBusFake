using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using System;
using System.Diagnostics;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Channels
{
	internal abstract class InitialServerConnectionReader : IDisposable
	{
		private int maxViaSize;

		private int maxContentTypeSize;

		private Microsoft.ServiceBus.Channels.IConnection connection;

		private Action connectionDequeuedCallback;

		private Microsoft.ServiceBus.Channels.ConnectionClosedCallback closedCallback;

		private bool isClosed;

		public Microsoft.ServiceBus.Channels.IConnection Connection
		{
			get
			{
				return this.connection;
			}
		}

		public Action ConnectionDequeuedCallback
		{
			get
			{
				return this.connectionDequeuedCallback;
			}
			set
			{
				this.connectionDequeuedCallback = value;
			}
		}

		protected bool IsClosed
		{
			get
			{
				return this.isClosed;
			}
		}

		protected int MaxContentTypeSize
		{
			get
			{
				return this.maxContentTypeSize;
			}
		}

		protected int MaxViaSize
		{
			get
			{
				return this.maxViaSize;
			}
		}

		private object ThisLock
		{
			get
			{
				return this;
			}
		}

		protected InitialServerConnectionReader(Microsoft.ServiceBus.Channels.IConnection connection, Microsoft.ServiceBus.Channels.ConnectionClosedCallback closedCallback) : this(connection, closedCallback, 2048, 256)
		{
		}

		protected InitialServerConnectionReader(Microsoft.ServiceBus.Channels.IConnection connection, Microsoft.ServiceBus.Channels.ConnectionClosedCallback closedCallback, int maxViaSize, int maxContentTypeSize)
		{
			if (connection == null)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("connection");
			}
			if (closedCallback == null)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("closedCallback");
			}
			this.connection = connection;
			this.closedCallback = closedCallback;
			this.maxContentTypeSize = maxContentTypeSize;
			this.maxViaSize = maxViaSize;
		}

		protected void Abort()
		{
			this.Abort(null);
		}

		protected void Abort(Exception e)
		{
			lock (this.ThisLock)
			{
				if (!this.isClosed)
				{
					this.isClosed = true;
				}
				else
				{
					return;
				}
			}
			try
			{
				if (e != null && Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceError)
				{
					TraceUtility.TraceEvent(TraceEventType.Error, TraceCode.ChannelConnectionDropped, this, e);
				}
				this.connection.Abort();
			}
			finally
			{
				if (this.closedCallback != null)
				{
					this.closedCallback(this);
				}
				if (this.connectionDequeuedCallback != null)
				{
					this.connectionDequeuedCallback();
				}
			}
		}

		public static IAsyncResult BeginUpgradeConnection(Microsoft.ServiceBus.Channels.IConnection connection, StreamUpgradeAcceptor upgradeAcceptor, IDefaultCommunicationTimeouts defaultTimeouts, AsyncCallback callback, object state)
		{
			return new Microsoft.ServiceBus.Channels.InitialServerConnectionReader.UpgradeConnectionAsyncResult(connection, upgradeAcceptor, defaultTimeouts, callback, state);
		}

		protected void Close(TimeSpan timeout)
		{
			lock (this.ThisLock)
			{
				if (!this.isClosed)
				{
					this.isClosed = true;
				}
				else
				{
					return;
				}
			}
			bool flag = false;
			try
			{
				this.connection.Close(timeout);
				flag = true;
			}
			finally
			{
				if (!flag)
				{
					this.connection.Abort();
				}
				if (this.closedCallback != null)
				{
					this.closedCallback(this);
				}
				if (this.connectionDequeuedCallback != null)
				{
					this.connectionDequeuedCallback();
				}
			}
		}

		public void CloseFromPool(TimeSpan timeout)
		{
			try
			{
				this.Close(timeout);
			}
			catch (CommunicationException communicationException1)
			{
				CommunicationException communicationException = communicationException1;
				if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceInformation)
				{
					Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.TraceHandledException(communicationException, TraceEventType.Information);
				}
			}
			catch (TimeoutException timeoutException1)
			{
				TimeoutException timeoutException = timeoutException1;
				if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceInformation)
				{
					Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.TraceHandledException(timeoutException, TraceEventType.Information);
				}
			}
		}

		public void Dispose()
		{
			lock (this.ThisLock)
			{
				if (!this.isClosed)
				{
					this.isClosed = true;
				}
				else
				{
					return;
				}
			}
			Microsoft.ServiceBus.Channels.IConnection connection = this.connection;
			if (connection != null)
			{
				connection.Abort();
			}
			if (this.connectionDequeuedCallback != null)
			{
				this.connectionDequeuedCallback();
			}
			GC.SuppressFinalize(this);
		}

		public static Microsoft.ServiceBus.Channels.IConnection EndUpgradeConnection(IAsyncResult result)
		{
			return Microsoft.ServiceBus.Channels.InitialServerConnectionReader.UpgradeConnectionAsyncResult.End(result);
		}

		public Action GetConnectionDequeuedCallback()
		{
			Action action = this.connectionDequeuedCallback;
			this.connectionDequeuedCallback = null;
			return action;
		}

		public void ReleaseConnection()
		{
			this.isClosed = true;
			this.connection = null;
		}

		internal static void SendFault(Microsoft.ServiceBus.Channels.IConnection connection, string faultString, byte[] drainBuffer, TimeSpan sendTimeout, int maxRead)
		{
			Microsoft.ServiceBus.Channels.EncodedFault encodedFault = new Microsoft.ServiceBus.Channels.EncodedFault(faultString);
			TimeoutHelper timeoutHelper = new TimeoutHelper(sendTimeout);
			try
			{
				connection.Write(encodedFault.EncodedBytes, 0, (int)encodedFault.EncodedBytes.Length, true, timeoutHelper.RemainingTime());
				connection.Shutdown(timeoutHelper.RemainingTime());
			}
			catch (CommunicationException communicationException1)
			{
				CommunicationException communicationException = communicationException1;
				if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceInformation)
				{
					Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.TraceHandledException(communicationException, TraceEventType.Information);
				}
				connection.Abort();
				return;
			}
			catch (TimeoutException timeoutException1)
			{
				TimeoutException timeoutException = timeoutException1;
				if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceInformation)
				{
					Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.TraceHandledException(timeoutException, TraceEventType.Information);
				}
				connection.Abort();
				return;
			}
			int num = 0;
			int num1 = 0;
			do
			{
				try
				{
					num = connection.Read(drainBuffer, 0, (int)drainBuffer.Length, timeoutHelper.RemainingTime());
				}
				catch (CommunicationException communicationException3)
				{
					CommunicationException communicationException2 = communicationException3;
					if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceInformation)
					{
						Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.TraceHandledException(communicationException2, TraceEventType.Information);
					}
					connection.Abort();
					return;
				}
				catch (TimeoutException timeoutException3)
				{
					TimeoutException timeoutException2 = timeoutException3;
					if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceInformation)
					{
						Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.TraceHandledException(timeoutException2, TraceEventType.Information);
					}
					connection.Abort();
					return;
				}
				if (num == 0)
				{
					Microsoft.ServiceBus.Channels.ConnectionUtilities.CloseNoThrow(connection, timeoutHelper.RemainingTime());
					return;
				}
				num1 = num1 + num;
			}
			while (num1 <= maxRead && !(timeoutHelper.RemainingTime() <= TimeSpan.Zero));
			connection.Abort();
		}

		public static Microsoft.ServiceBus.Channels.IConnection UpgradeConnection(Microsoft.ServiceBus.Channels.IConnection connection, StreamUpgradeAcceptor upgradeAcceptor, IDefaultCommunicationTimeouts defaultTimeouts)
		{
			Microsoft.ServiceBus.Channels.ConnectionStream connectionStream = new Microsoft.ServiceBus.Channels.ConnectionStream(connection, defaultTimeouts);
			Stream stream = upgradeAcceptor.AcceptUpgrade(connectionStream);
			if (upgradeAcceptor is StreamSecurityUpgradeAcceptor && Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceInformation)
			{
				TraceUtility.TraceEvent(TraceEventType.Information, TraceCode.StreamSecurityUpgradeAccepted, new StringTraceRecord("Type", upgradeAcceptor.GetType().ToString()), connection, null);
			}
			return new Microsoft.ServiceBus.Channels.StreamConnection(stream, connectionStream);
		}

		private class UpgradeConnectionAsyncResult : AsyncResult
		{
			private Microsoft.ServiceBus.Channels.ConnectionStream connectionStream;

			private static AsyncCallback onAcceptUpgrade;

			private Microsoft.ServiceBus.Channels.IConnection connection;

			private StreamUpgradeAcceptor upgradeAcceptor;

			static UpgradeConnectionAsyncResult()
			{
				Microsoft.ServiceBus.Channels.InitialServerConnectionReader.UpgradeConnectionAsyncResult.onAcceptUpgrade = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(Microsoft.ServiceBus.Channels.InitialServerConnectionReader.UpgradeConnectionAsyncResult.OnAcceptUpgrade));
			}

			public UpgradeConnectionAsyncResult(Microsoft.ServiceBus.Channels.IConnection connection, StreamUpgradeAcceptor upgradeAcceptor, IDefaultCommunicationTimeouts defaultTimeouts, AsyncCallback callback, object state) : base(callback, state)
			{
				this.upgradeAcceptor = upgradeAcceptor;
				this.connectionStream = new Microsoft.ServiceBus.Channels.ConnectionStream(connection, defaultTimeouts);
				bool flag = false;
				IAsyncResult asyncResult = upgradeAcceptor.BeginAcceptUpgrade(this.connectionStream, Microsoft.ServiceBus.Channels.InitialServerConnectionReader.UpgradeConnectionAsyncResult.onAcceptUpgrade, this);
				if (asyncResult.CompletedSynchronously)
				{
					this.CompleteAcceptUpgrade(asyncResult);
					flag = true;
				}
				if (flag)
				{
					base.Complete(true);
				}
			}

			private void CompleteAcceptUpgrade(IAsyncResult result)
			{
				Stream stream;
				bool flag = false;
				try
				{
					stream = this.upgradeAcceptor.EndAcceptUpgrade(result);
					flag = true;
				}
				finally
				{
					if (this.upgradeAcceptor is StreamSecurityUpgradeAcceptor && Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceInformation && flag)
					{
						TraceUtility.TraceEvent(TraceEventType.Information, TraceCode.StreamSecurityUpgradeAccepted, new StringTraceRecord("Type", this.upgradeAcceptor.GetType().ToString()), this, null);
					}
				}
				this.connection = new Microsoft.ServiceBus.Channels.StreamConnection(stream, this.connectionStream);
			}

			public static new Microsoft.ServiceBus.Channels.IConnection End(IAsyncResult result)
			{
				return AsyncResult.End<Microsoft.ServiceBus.Channels.InitialServerConnectionReader.UpgradeConnectionAsyncResult>(result).connection;
			}

			private static void OnAcceptUpgrade(IAsyncResult result)
			{
				if (result.CompletedSynchronously)
				{
					return;
				}
				Microsoft.ServiceBus.Channels.InitialServerConnectionReader.UpgradeConnectionAsyncResult asyncState = (Microsoft.ServiceBus.Channels.InitialServerConnectionReader.UpgradeConnectionAsyncResult)result.AsyncState;
				Exception exception = null;
				try
				{
					asyncState.CompleteAcceptUpgrade(result);
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
				asyncState.Complete(false, exception);
			}
		}
	}
}