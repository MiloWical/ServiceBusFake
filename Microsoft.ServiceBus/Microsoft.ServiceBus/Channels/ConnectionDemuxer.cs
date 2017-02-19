using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Channels
{
	internal sealed class ConnectionDemuxer
	{
		private Microsoft.ServiceBus.Channels.ConnectionAcceptor acceptor;

		private List<Microsoft.ServiceBus.Channels.InitialServerConnectionReader> connectionReaders;

		private bool isClosed;

		private Microsoft.ServiceBus.Channels.ConnectionModeCallback onConnectionModeKnown;

		private Microsoft.ServiceBus.Channels.ConnectionModeCallback onCachedConnectionModeKnown;

		private Microsoft.ServiceBus.Channels.ConnectionClosedCallback onConnectionClosed;

		private Microsoft.ServiceBus.Channels.ServerSessionPreambleCallback onSessionPreambleKnown;

		private Microsoft.ServiceBus.Channels.ServerSingletonPreambleCallback onSingletonPreambleKnown;

		private Action<object> reuseConnectionCallback;

		private Microsoft.ServiceBus.Channels.ServerSessionPreambleDemuxCallback serverSessionPreambleCallback;

		private Microsoft.ServiceBus.Channels.SingletonPreambleDemuxCallback singletonPreambleCallback;

		private Microsoft.ServiceBus.Channels.TransportSettingsCallback transportSettingsCallback;

		private Action pooledConnectionDequeuedCallback;

		private OnViaDelegate viaDelegate;

		private TimeSpan channelInitializationTimeout;

		private TimeSpan idleTimeout;

		private int maxPooledConnections;

		private int pooledConnectionCount;

		private object ThisLock
		{
			get
			{
				return this;
			}
		}

		public ConnectionDemuxer(Microsoft.ServiceBus.Channels.IConnectionListener listener, int maxAccepts, int maxPendingConnections, TimeSpan channelInitializationTimeout, TimeSpan idleTimeout, int maxPooledConnections, Microsoft.ServiceBus.Channels.TransportSettingsCallback transportSettingsCallback, Microsoft.ServiceBus.Channels.SingletonPreambleDemuxCallback singletonPreambleCallback, Microsoft.ServiceBus.Channels.ServerSessionPreambleDemuxCallback serverSessionPreambleCallback, Microsoft.ServiceBus.Channels.ErrorCallback errorCallback)
		{
			this.connectionReaders = new List<Microsoft.ServiceBus.Channels.InitialServerConnectionReader>();
			this.acceptor = new Microsoft.ServiceBus.Channels.ConnectionAcceptor(listener, maxAccepts, maxPendingConnections, new Microsoft.ServiceBus.Channels.ConnectionAvailableCallback(this.OnConnectionAvailable), errorCallback);
			this.channelInitializationTimeout = channelInitializationTimeout;
			this.idleTimeout = idleTimeout;
			this.maxPooledConnections = maxPooledConnections;
			this.onConnectionClosed = new Microsoft.ServiceBus.Channels.ConnectionClosedCallback(this.OnConnectionClosed);
			this.transportSettingsCallback = transportSettingsCallback;
			this.singletonPreambleCallback = singletonPreambleCallback;
			this.serverSessionPreambleCallback = serverSessionPreambleCallback;
		}

		public void Abort()
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
			for (int i = 0; i < this.connectionReaders.Count; i++)
			{
				this.connectionReaders[i].Dispose();
			}
			this.connectionReaders.Clear();
			this.acceptor.Abort();
		}

		public void Close(TimeSpan timeout)
		{
			lock (this.ThisLock)
			{
				if (!this.isClosed)
				{
					for (int i = 0; i < this.connectionReaders.Count; i++)
					{
						this.connectionReaders[i].Dispose();
					}
					this.connectionReaders.Clear();
					this.acceptor.Close(timeout);
					this.isClosed = true;
				}
			}
		}

		private void OnCachedConnectionModeKnown(Microsoft.ServiceBus.Channels.ConnectionModeReader modeReader)
		{
			this.OnConnectionModeKnownCore(modeReader, true);
		}

		private void OnConnectionAvailable(Microsoft.ServiceBus.Channels.IConnection connection, Action connectionDequeuedCallback)
		{
			Microsoft.ServiceBus.Channels.ConnectionModeReader connectionModeReader = this.SetupModeReader(connection, false);
			if (connectionModeReader == null)
			{
				connectionDequeuedCallback();
				return;
			}
			connectionModeReader.StartReading(this.channelInitializationTimeout, connectionDequeuedCallback);
		}

		private void OnConnectionClosed(Microsoft.ServiceBus.Channels.InitialServerConnectionReader connectionReader)
		{
			lock (this.ThisLock)
			{
				if (!this.isClosed)
				{
					this.connectionReaders.Remove(connectionReader);
				}
			}
		}

		private void OnConnectionModeKnown(Microsoft.ServiceBus.Channels.ConnectionModeReader modeReader)
		{
			this.OnConnectionModeKnownCore(modeReader, false);
		}

		private void OnConnectionModeKnownCore(Microsoft.ServiceBus.Channels.ConnectionModeReader modeReader, bool isCached)
		{
			Microsoft.ServiceBus.Channels.FramingMode connectionMode;
			lock (this.ThisLock)
			{
				if (!this.isClosed)
				{
					this.connectionReaders.Remove(modeReader);
				}
				else
				{
					return;
				}
			}
			bool flag = true;
			try
			{
				try
				{
					try
					{
						connectionMode = modeReader.GetConnectionMode();
					}
					catch (CommunicationException communicationException1)
					{
						CommunicationException communicationException = communicationException1;
						TraceEventType exceptionEventType = modeReader.Connection.ExceptionEventType;
						if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTrace(exceptionEventType))
						{
							Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.TraceHandledException(communicationException, exceptionEventType);
						}
						return;
					}
					catch (TimeoutException timeoutException1)
					{
						TimeoutException timeoutException = timeoutException1;
						if (!isCached)
						{
							string channelInitializationTimeout = Resources.ChannelInitializationTimeout;
							object[] objArray = new object[] { this.channelInitializationTimeout };
							timeoutException = new TimeoutException(Microsoft.ServiceBus.SR.GetString(channelInitializationTimeout, objArray), timeoutException);
							ErrorBehavior.ThrowAndCatch(timeoutException);
						}
						TraceEventType traceEventType = modeReader.Connection.ExceptionEventType;
						if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTrace(traceEventType))
						{
							Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.TraceHandledException(timeoutException, traceEventType);
						}
						return;
					}
					switch (connectionMode)
					{
						case Microsoft.ServiceBus.Channels.FramingMode.Singleton:
						{
							this.OnSingletonConnection(modeReader.Connection, modeReader.ConnectionDequeuedCallback, modeReader.StreamPosition, modeReader.BufferOffset, modeReader.BufferSize, modeReader.GetRemainingTimeout());
							break;
						}
						case Microsoft.ServiceBus.Channels.FramingMode.Duplex:
						{
							this.OnDuplexConnection(modeReader.Connection, modeReader.ConnectionDequeuedCallback, modeReader.StreamPosition, modeReader.BufferOffset, modeReader.BufferSize, modeReader.GetRemainingTimeout());
							break;
						}
						default:
						{
							string framingModeNotSupported = Resources.FramingModeNotSupported;
							object[] objArray1 = new object[] { connectionMode };
							Exception invalidDataException = new InvalidDataException(Microsoft.ServiceBus.SR.GetString(framingModeNotSupported, objArray1));
							Exception protocolException = new ProtocolException(invalidDataException.Message, invalidDataException);
							Microsoft.ServiceBus.Channels.FramingEncodingString.AddFaultString(protocolException, "http://schemas.microsoft.com/ws/2006/05/framing/faults/UnsupportedMode");
							ErrorBehavior.ThrowAndCatch(protocolException);
							return;
						}
					}
					flag = false;
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					TraceEventType exceptionEventType1 = modeReader.Connection.ExceptionEventType;
					if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTrace(exceptionEventType1))
					{
						Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.TraceHandledException(exception, exceptionEventType1);
					}
				}
			}
			finally
			{
				if (flag)
				{
					modeReader.Dispose();
				}
			}
		}

		private void OnDuplexConnection(Microsoft.ServiceBus.Channels.IConnection connection, Action connectionDequeuedCallback, long streamPosition, int offset, int size, TimeSpan timeout)
		{
			if (this.onSessionPreambleKnown == null)
			{
				this.onSessionPreambleKnown = new Microsoft.ServiceBus.Channels.ServerSessionPreambleCallback(this.OnSessionPreambleKnown);
			}
			Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader serverSessionPreambleConnectionReader = new Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader(connection, connectionDequeuedCallback, streamPosition, offset, size, this.transportSettingsCallback, this.onConnectionClosed, this.onSessionPreambleKnown);
			lock (this.ThisLock)
			{
				if (!this.isClosed)
				{
					this.connectionReaders.Add(serverSessionPreambleConnectionReader);
				}
				else
				{
					serverSessionPreambleConnectionReader.Dispose();
					return;
				}
			}
			serverSessionPreambleConnectionReader.StartReading(this.viaDelegate, timeout);
		}

		private void OnSessionPreambleKnown(Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader serverSessionPreambleReader)
		{
			lock (this.ThisLock)
			{
				if (!this.isClosed)
				{
					this.connectionReaders.Remove(serverSessionPreambleReader);
				}
				else
				{
					return;
				}
			}
			this.serverSessionPreambleCallback(serverSessionPreambleReader, this);
		}

		private void OnSingletonConnection(Microsoft.ServiceBus.Channels.IConnection connection, Action connectionDequeuedCallback, long streamPosition, int offset, int size, TimeSpan timeout)
		{
			if (this.onSingletonPreambleKnown == null)
			{
				this.onSingletonPreambleKnown = new Microsoft.ServiceBus.Channels.ServerSingletonPreambleCallback(this.OnSingletonPreambleKnown);
			}
			Microsoft.ServiceBus.Channels.ServerSingletonPreambleConnectionReader serverSingletonPreambleConnectionReader = new Microsoft.ServiceBus.Channels.ServerSingletonPreambleConnectionReader(connection, connectionDequeuedCallback, streamPosition, offset, size, this.transportSettingsCallback, this.onConnectionClosed, this.onSingletonPreambleKnown);
			lock (this.ThisLock)
			{
				if (!this.isClosed)
				{
					this.connectionReaders.Add(serverSingletonPreambleConnectionReader);
				}
				else
				{
					serverSingletonPreambleConnectionReader.Dispose();
					return;
				}
			}
			serverSingletonPreambleConnectionReader.StartReading(this.viaDelegate, timeout);
		}

		private void OnSingletonPreambleKnown(Microsoft.ServiceBus.Channels.ServerSingletonPreambleConnectionReader serverSingletonPreambleReader)
		{
			lock (this.ThisLock)
			{
				if (!this.isClosed)
				{
					this.connectionReaders.Remove(serverSingletonPreambleReader);
				}
				else
				{
					return;
				}
			}
			Microsoft.ServiceBus.Channels.ISingletonChannelListener singletonChannelListener = this.singletonPreambleCallback(serverSingletonPreambleReader);
			Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DebugAssert(singletonChannelListener != null, "singletonPreambleCallback must return a listener or send a Fault/throw");
			TimeoutHelper timeoutHelper = new TimeoutHelper(singletonChannelListener.ReceiveTimeout);
			Microsoft.ServiceBus.Channels.IConnection connection = serverSingletonPreambleReader.CompletePreamble(timeoutHelper.RemainingTime());
			Microsoft.ServiceBus.Channels.ServerSingletonConnectionReader serverSingletonConnectionReader = new Microsoft.ServiceBus.Channels.ServerSingletonConnectionReader(serverSingletonPreambleReader, connection, this);
			RequestContext requestContext = serverSingletonConnectionReader.ReceiveRequest(timeoutHelper.RemainingTime());
			singletonChannelListener.ReceiveRequest(requestContext, serverSingletonPreambleReader.ConnectionDequeuedCallback, true);
		}

		public void Open(TimeSpan timeout)
		{
			this.StartDemuxing(timeout, null);
		}

		private void PooledConnectionDequeuedCallback()
		{
			lock (this.ThisLock)
			{
				Microsoft.ServiceBus.Channels.ConnectionDemuxer connectionDemuxer = this;
				connectionDemuxer.pooledConnectionCount = connectionDemuxer.pooledConnectionCount - 1;
				Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DebugAssert(this.pooledConnectionCount >= 0, "Connection Throttle should never be negative");
			}
		}

		public void ReuseConnection(Microsoft.ServiceBus.Channels.IConnection connection, TimeSpan closeTimeout)
		{
			connection.ExceptionEventType = TraceEventType.Information;
			Microsoft.ServiceBus.Channels.ConnectionModeReader connectionModeReader = this.SetupModeReader(connection, true);
			if (connectionModeReader != null)
			{
				if (this.reuseConnectionCallback == null)
				{
					this.reuseConnectionCallback = new Action<object>(this.ReuseConnectionCallback);
				}
				IOThreadScheduler.ScheduleCallbackNoFlow(this.reuseConnectionCallback, new Microsoft.ServiceBus.Channels.ConnectionDemuxer.ReuseConnectionState(connectionModeReader, closeTimeout));
			}
		}

		private void ReuseConnectionCallback(object state)
		{
			Microsoft.ServiceBus.Channels.ConnectionDemuxer.ReuseConnectionState reuseConnectionState = (Microsoft.ServiceBus.Channels.ConnectionDemuxer.ReuseConnectionState)state;
			bool flag = false;
			lock (this.ThisLock)
			{
				if (this.pooledConnectionCount < this.maxPooledConnections)
				{
					Microsoft.ServiceBus.Channels.ConnectionDemuxer connectionDemuxer = this;
					connectionDemuxer.pooledConnectionCount = connectionDemuxer.pooledConnectionCount + 1;
				}
				else
				{
					flag = true;
				}
			}
			if (!flag)
			{
				if (this.pooledConnectionDequeuedCallback == null)
				{
					this.pooledConnectionDequeuedCallback = new Action(this.PooledConnectionDequeuedCallback);
				}
				reuseConnectionState.ModeReader.StartReading(this.idleTimeout, this.pooledConnectionDequeuedCallback);
				return;
			}
			if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceWarning)
			{
				DiagnosticTrace diagnosticTrace = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DiagnosticTrace;
				string traceCodeServerMaxPooledConnectionsQuotaReached = Resources.TraceCodeServerMaxPooledConnectionsQuotaReached;
				object[] objArray = new object[] { this.maxPooledConnections };
				diagnosticTrace.TraceEvent(TraceEventType.Warning, TraceCode.ServerMaxPooledConnectionsQuotaReached, Microsoft.ServiceBus.SR.GetString(traceCodeServerMaxPooledConnectionsQuotaReached, objArray), new StringTraceRecord("MaxOutboundConnectionsPerEndpoint", this.maxPooledConnections.ToString(CultureInfo.InvariantCulture)), null, this);
			}
			reuseConnectionState.ModeReader.CloseFromPool(reuseConnectionState.CloseTimeout);
		}

		private Microsoft.ServiceBus.Channels.ConnectionModeReader SetupModeReader(Microsoft.ServiceBus.Channels.IConnection connection, bool isCached)
		{
			Microsoft.ServiceBus.Channels.ConnectionModeReader connectionModeReader;
			Microsoft.ServiceBus.Channels.ConnectionModeReader connectionModeReader1;
			if (!isCached)
			{
				if (this.onConnectionModeKnown == null)
				{
					this.onConnectionModeKnown = new Microsoft.ServiceBus.Channels.ConnectionModeCallback(this.OnConnectionModeKnown);
				}
				connectionModeReader = new Microsoft.ServiceBus.Channels.ConnectionModeReader(connection, this.onConnectionModeKnown, this.onConnectionClosed);
			}
			else
			{
				if (this.onCachedConnectionModeKnown == null)
				{
					this.onCachedConnectionModeKnown = new Microsoft.ServiceBus.Channels.ConnectionModeCallback(this.OnCachedConnectionModeKnown);
				}
				connectionModeReader = new Microsoft.ServiceBus.Channels.ConnectionModeReader(connection, this.onCachedConnectionModeKnown, this.onConnectionClosed);
			}
			lock (this.ThisLock)
			{
				if (!this.isClosed)
				{
					this.connectionReaders.Add(connectionModeReader);
					connectionModeReader1 = connectionModeReader;
				}
				else
				{
					connectionModeReader.Dispose();
					connectionModeReader1 = null;
				}
			}
			return connectionModeReader1;
		}

		public void StartDemuxing(TimeSpan timeout, OnViaDelegate viaDelegate)
		{
			this.viaDelegate = viaDelegate;
			this.acceptor.Open(timeout);
		}

		private class ReuseConnectionState
		{
			private Microsoft.ServiceBus.Channels.ConnectionModeReader modeReader;

			private TimeSpan closeTimeout;

			public TimeSpan CloseTimeout
			{
				get
				{
					return this.closeTimeout;
				}
			}

			public Microsoft.ServiceBus.Channels.ConnectionModeReader ModeReader
			{
				get
				{
					return this.modeReader;
				}
			}

			public ReuseConnectionState(Microsoft.ServiceBus.Channels.ConnectionModeReader modeReader, TimeSpan closeTimeout)
			{
				this.modeReader = modeReader;
				this.closeTimeout = closeTimeout;
			}
		}
	}
}