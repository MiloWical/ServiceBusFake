using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Channels.Security;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Properties;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IdentityModel.Tokens;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Security;
using System.Threading;
using System.Xml;

namespace Microsoft.ServiceBus
{
	internal abstract class RelayedOnewayTcpClient : CommunicationObject, IConnectionStatus
	{
		private const int MaxRedirectDepth = 3;

		private const double SecondRetrySleepSeconds = 2.5;

		private const double MaxRetrySeconds = 10;

		private readonly static Action<object> retryCallback;

		private readonly string appliesTo;

		private readonly BindingContext context;

		private readonly Microsoft.ServiceBus.Channels.IRequestReplyCorrelator correlator;

		private readonly System.ServiceModel.Channels.MessageVersion messageVersion;

		private readonly IOThreadTimer retryTimer;

		private readonly IDefaultCommunicationTimeouts timeouts;

		private readonly Microsoft.ServiceBus.TokenProvider tokenProvider;

		private readonly System.Uri uri;

		private readonly RelayedOnewayTransportBindingElement bindingElement;

		private RelayedOnewayTcpClient.RelayedOnewayConnection connection;

		private IChannelFactory<IDuplexSessionChannel> channelFactory;

		private Exception connectionError;

		private RelayedOnewayTcpClient.RelayedOnewayStatus connectionStatus;

		private RelayedOnewayTcpClient.ReceivePump receivePump;

		private InternalConnectivityMode connectivityMode;

		private bool transportProtectionEnabled;

		private TimeSpan connectSleep;

		internal EventTraceActivity Activity
		{
			get;
			private set;
		}

		public string AppliesTo
		{
			get
			{
				return this.appliesTo;
			}
		}

		public IChannelFactory<IDuplexSessionChannel> ChannelFactory
		{
			get
			{
				return this.channelFactory;
			}
		}

		private Microsoft.ServiceBus.Channels.IRequestReplyCorrelator Correlator
		{
			get
			{
				return this.correlator;
			}
		}

		protected override TimeSpan DefaultCloseTimeout
		{
			get
			{
				return Microsoft.ServiceBus.ServiceDefaults.CloseTimeout;
			}
		}

		protected override TimeSpan DefaultOpenTimeout
		{
			get
			{
				return Microsoft.ServiceBus.ServiceDefaults.OpenTimeout;
			}
		}

		public bool IsListener
		{
			get;
			protected set;
		}

		public bool IsOnline
		{
			get
			{
				return this.connectionStatus == RelayedOnewayTcpClient.RelayedOnewayStatus.Online;
			}
		}

		public Exception LastError
		{
			get
			{
				return this.connectionError;
			}
		}

		public System.ServiceModel.Channels.MessageVersion MessageVersion
		{
			get
			{
				return this.messageVersion;
			}
		}

		public IDefaultCommunicationTimeouts Timeouts
		{
			get
			{
				return this.timeouts;
			}
		}

		public Microsoft.ServiceBus.TokenProvider TokenProvider
		{
			get
			{
				return this.tokenProvider;
			}
		}

		public System.Uri Uri
		{
			get
			{
				return this.uri;
			}
		}

		public System.Uri Via
		{
			get;
			private set;
		}

		static RelayedOnewayTcpClient()
		{
			RelayedOnewayTcpClient.retryCallback = new Action<object>(RelayedOnewayTcpClient.RetryCallback);
		}

		protected RelayedOnewayTcpClient(BindingContext context, RelayedOnewayTransportBindingElement transportBindingElement, System.Uri uri, bool transportProtectionEnabled, EventTraceActivity activity)
		{
			this.context = context;
			this.uri = uri;
			this.bindingElement = transportBindingElement;
			this.transportProtectionEnabled = transportProtectionEnabled;
			this.messageVersion = context.Binding.MessageVersion;
			this.timeouts = context.Binding;
			this.Activity = activity;
			this.tokenProvider = TokenProviderUtility.CreateTokenProvider(context);
			if (this.tokenProvider != null)
			{
				this.transportProtectionEnabled = true;
			}
			this.appliesTo = RelayedHttpUtility.ConvertToHttpUri(uri).AbsoluteUri;
			this.retryTimer = new IOThreadTimer(RelayedOnewayTcpClient.retryCallback, this, false);
			this.correlator = new Microsoft.ServiceBus.Channels.RequestReplyCorrelator();
			this.connectionStatus = RelayedOnewayTcpClient.RelayedOnewayStatus.Connecting;
			this.SetConnectivityModeRelatedParameters(this.GetInternalConnectivityMode());
		}

		public IAsyncResult BeginRequest(Message message, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new RelayedOnewayTcpClient.AsyncRequestReplyContext(this, message, timeout, callback, state);
		}

		public IAsyncResult BeginSend(Message message, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.BeginRequest(message, timeout, callback, state);
		}

		private void ChannelClosed(object sender, EventArgs args)
		{
			RelayedOnewayTcpClient.RelayedOnewayConnection relayedOnewayConnection = this.connection;
			if (relayedOnewayConnection != null)
			{
				this.Disconnected(relayedOnewayConnection.LastError, false);
			}
		}

		private void ChannelFaulted(object sender, EventArgs args)
		{
			RelayedOnewayTcpClient.RelayedOnewayConnection relayedOnewayConnection = this.connection;
			if (relayedOnewayConnection != null)
			{
				relayedOnewayConnection.Abort();
			}
		}

		protected abstract RelayedOnewayTcpClient.RelayedOnewayConnection Connect(TimeSpan timeout);

		private void Connected(RelayedOnewayTcpClient.RelayedOnewayConnection connection)
		{
			this.connectSleep = TimeSpan.Zero;
			this.connection = connection;
			if (this.connection == null)
			{
				throw Fx.Exception.AsError(new ArgumentException(SRClient.InvalidChannelType), null);
			}
			this.connection.Closed += new EventHandler(this.ChannelClosed);
			this.connection.Faulted += new EventHandler(this.ChannelFaulted);
			if (this.connection.State == CommunicationState.Closed)
			{
				this.ChannelClosed(this.connection, EventArgs.Empty);
				return;
			}
			if (this.connection.State == CommunicationState.Faulted)
			{
				this.ChannelFaulted(this.connection, EventArgs.Empty);
				return;
			}
			MessagingClientEtwProvider.Provider.RelayClientConnected(this.Activity, this.uri.AbsoluteUri, this.IsListener);
			this.SetStatus(RelayedOnewayTcpClient.RelayedOnewayStatus.Online, null);
		}

		private Binding CreateNetTcpOnewayBinding(BindingContext context, RelayedOnewayTransportBindingElement transportBindingElement)
		{
			Binding customBinding;
			BinaryMessageEncodingBindingElement binaryMessageEncodingBindingElement = ClientMessageUtility.CreateInnerEncodingBindingElement(context);
			if (this.connectivityMode == InternalConnectivityMode.HttpsWebSocket)
			{
				SocketConnectionBindingElement socketConnectionBindingElement = new SocketConnectionBindingElement(new WebSocketOnewayClientConnectionElement(SocketSecurityRole.None, "wsrelayedoneway"), false)
				{
					ChannelInitializationTimeout = transportBindingElement.ChannelInitializationTimeout,
					ConnectionBufferSize = transportBindingElement.ConnectionBufferSize,
					ListenBacklog = transportBindingElement.ListenBacklog,
					MaxBufferPoolSize = transportBindingElement.MaxBufferPoolSize,
					MaxBufferSize = transportBindingElement.MaxBufferSize,
					MaxOutputDelay = transportBindingElement.MaxOutputDelay,
					MaxPendingAccepts = transportBindingElement.MaxPendingAccepts,
					MaxPendingConnections = transportBindingElement.MaxPendingConnections,
					MaxReceivedMessageSize = transportBindingElement.MaxReceivedMessageSize,
					TransferMode = TransferMode.Buffered,
					ManualAddressing = true
				};
				SocketConnectionBindingElement groupName = socketConnectionBindingElement;
				groupName.ConnectionPoolSettings.GroupName = transportBindingElement.ConnectionPoolSettings.GroupName;
				groupName.ConnectionPoolSettings.IdleTimeout = transportBindingElement.ConnectionPoolSettings.IdleTimeout;
				groupName.ConnectionPoolSettings.LeaseTimeout = transportBindingElement.ConnectionPoolSettings.LeaseTimeout;
				groupName.ConnectionPoolSettings.MaxOutboundConnectionsPerEndpoint = transportBindingElement.ConnectionPoolSettings.MaxOutboundConnectionsPerEndpoint;
				customBinding = new CustomBinding(new BindingElement[] { binaryMessageEncodingBindingElement, groupName });
			}
			else if (this.connectivityMode == InternalConnectivityMode.Http || this.connectivityMode == InternalConnectivityMode.Https)
			{
				bool flag = this.connectivityMode == InternalConnectivityMode.Https;
				SocketConnectionBindingElement socketConnectionBindingElement1 = new SocketConnectionBindingElement(new WebStreamOnewayClientConnectionElement((flag ? SocketSecurityRole.None : SocketSecurityRole.SslClient), "oneway", flag), false)
				{
					ChannelInitializationTimeout = transportBindingElement.ChannelInitializationTimeout,
					ConnectionBufferSize = transportBindingElement.ConnectionBufferSize,
					ListenBacklog = transportBindingElement.ListenBacklog,
					MaxBufferPoolSize = transportBindingElement.MaxBufferPoolSize,
					MaxBufferSize = transportBindingElement.MaxBufferSize,
					MaxOutputDelay = transportBindingElement.MaxOutputDelay,
					MaxPendingAccepts = transportBindingElement.MaxPendingAccepts,
					MaxPendingConnections = transportBindingElement.MaxPendingConnections,
					MaxReceivedMessageSize = transportBindingElement.MaxReceivedMessageSize,
					TransferMode = TransferMode.Buffered,
					ManualAddressing = true
				};
				SocketConnectionBindingElement idleTimeout = socketConnectionBindingElement1;
				idleTimeout.ConnectionPoolSettings.GroupName = transportBindingElement.ConnectionPoolSettings.GroupName;
				idleTimeout.ConnectionPoolSettings.IdleTimeout = transportBindingElement.ConnectionPoolSettings.IdleTimeout;
				idleTimeout.ConnectionPoolSettings.LeaseTimeout = transportBindingElement.ConnectionPoolSettings.LeaseTimeout;
				idleTimeout.ConnectionPoolSettings.MaxOutboundConnectionsPerEndpoint = transportBindingElement.ConnectionPoolSettings.MaxOutboundConnectionsPerEndpoint;
				customBinding = new CustomBinding(new BindingElement[] { binaryMessageEncodingBindingElement, idleTimeout });
			}
			else
			{
				TcpTransportBindingElement tcpTransportBindingElement = new TcpTransportBindingElement()
				{
					PortSharingEnabled = false,
					ListenBacklog = transportBindingElement.ListenBacklog,
					ChannelInitializationTimeout = transportBindingElement.ChannelInitializationTimeout,
					ConnectionBufferSize = transportBindingElement.ConnectionBufferSize,
					MaxBufferPoolSize = transportBindingElement.MaxBufferPoolSize,
					MaxBufferSize = transportBindingElement.MaxBufferSize,
					MaxOutputDelay = transportBindingElement.MaxOutputDelay,
					MaxPendingAccepts = transportBindingElement.MaxPendingAccepts,
					MaxPendingConnections = transportBindingElement.MaxPendingConnections,
					MaxReceivedMessageSize = transportBindingElement.MaxReceivedMessageSize,
					TransferMode = TransferMode.Buffered,
					ManualAddressing = true
				};
				TcpTransportBindingElement leaseTimeout = tcpTransportBindingElement;
				leaseTimeout.ConnectionPoolSettings.GroupName = transportBindingElement.ConnectionPoolSettings.GroupName;
				leaseTimeout.ConnectionPoolSettings.IdleTimeout = transportBindingElement.ConnectionPoolSettings.IdleTimeout;
				leaseTimeout.ConnectionPoolSettings.LeaseTimeout = transportBindingElement.ConnectionPoolSettings.LeaseTimeout;
				leaseTimeout.ConnectionPoolSettings.MaxOutboundConnectionsPerEndpoint = transportBindingElement.ConnectionPoolSettings.MaxOutboundConnectionsPerEndpoint;
				customBinding = new CustomBinding(new BindingElement[] { binaryMessageEncodingBindingElement, leaseTimeout });
			}
			customBinding.ReceiveTimeout = context.Binding.ReceiveTimeout;
			customBinding.CloseTimeout = context.Binding.CloseTimeout;
			customBinding.OpenTimeout = context.Binding.OpenTimeout;
			customBinding.SendTimeout = context.Binding.SendTimeout;
			customBinding.Name = context.Binding.Name;
			customBinding.Namespace = context.Binding.Namespace;
			return customBinding;
		}

		private Binding CreateSecureNetTcpOnewayBinding(BindingContext context, RelayedOnewayTransportBindingElement transportBindingElement, IdentityVerifier lenientDnsIdentityVerifier)
		{
			Binding customBinding;
			Binding binding;
			if (this.connectivityMode == InternalConnectivityMode.HttpsWebSocket)
			{
				return null;
			}
			BinaryMessageEncodingBindingElement binaryMessageEncodingBindingElement = ClientMessageUtility.CreateInnerEncodingBindingElement(context);
			SslStreamSecurityBindingElement sslStreamSecurityBindingElement = new SslStreamSecurityBindingElement()
			{
				RequireClientCertificate = false,
				IdentityVerifier = lenientDnsIdentityVerifier
			};
			SslStreamSecurityBindingElement sslStreamSecurityBindingElement1 = sslStreamSecurityBindingElement;
			if (this.connectivityMode == InternalConnectivityMode.Http || this.connectivityMode == InternalConnectivityMode.Https)
			{
				bool flag = this.connectivityMode == InternalConnectivityMode.Https;
				SocketConnectionBindingElement socketConnectionBindingElement = new SocketConnectionBindingElement(new WebStreamOnewayClientConnectionElement((flag ? SocketSecurityRole.None : SocketSecurityRole.SslClient), "oneway", flag), false)
				{
					ChannelInitializationTimeout = transportBindingElement.ChannelInitializationTimeout,
					ConnectionBufferSize = transportBindingElement.ConnectionBufferSize,
					ListenBacklog = transportBindingElement.ListenBacklog,
					MaxBufferPoolSize = transportBindingElement.MaxBufferPoolSize,
					MaxBufferSize = transportBindingElement.MaxBufferSize,
					MaxOutputDelay = transportBindingElement.MaxOutputDelay,
					MaxPendingAccepts = transportBindingElement.MaxPendingAccepts,
					MaxPendingConnections = transportBindingElement.MaxPendingConnections,
					MaxReceivedMessageSize = transportBindingElement.MaxReceivedMessageSize,
					TransferMode = TransferMode.Buffered,
					ManualAddressing = true
				};
				SocketConnectionBindingElement groupName = socketConnectionBindingElement;
				groupName.ConnectionPoolSettings.GroupName = transportBindingElement.ConnectionPoolSettings.GroupName;
				groupName.ConnectionPoolSettings.IdleTimeout = transportBindingElement.ConnectionPoolSettings.IdleTimeout;
				groupName.ConnectionPoolSettings.LeaseTimeout = transportBindingElement.ConnectionPoolSettings.LeaseTimeout;
				groupName.ConnectionPoolSettings.MaxOutboundConnectionsPerEndpoint = transportBindingElement.ConnectionPoolSettings.MaxOutboundConnectionsPerEndpoint;
				if (flag)
				{
					binding = new CustomBinding(new BindingElement[] { binaryMessageEncodingBindingElement, groupName });
				}
				else
				{
					BindingElement[] bindingElementArray = new BindingElement[] { sslStreamSecurityBindingElement1, binaryMessageEncodingBindingElement, groupName };
					binding = new CustomBinding(bindingElementArray);
				}
				customBinding = binding;
			}
			else
			{
				TcpTransportBindingElement tcpTransportBindingElement = new TcpTransportBindingElement()
				{
					PortSharingEnabled = false,
					ListenBacklog = transportBindingElement.ListenBacklog,
					ChannelInitializationTimeout = transportBindingElement.ChannelInitializationTimeout,
					ConnectionBufferSize = transportBindingElement.ConnectionBufferSize,
					MaxBufferPoolSize = transportBindingElement.MaxBufferPoolSize,
					MaxBufferSize = transportBindingElement.MaxBufferSize,
					MaxOutputDelay = transportBindingElement.MaxOutputDelay,
					MaxPendingAccepts = transportBindingElement.MaxPendingAccepts,
					MaxPendingConnections = transportBindingElement.MaxPendingConnections,
					MaxReceivedMessageSize = transportBindingElement.MaxReceivedMessageSize,
					ManualAddressing = true,
					TransferMode = TransferMode.Buffered
				};
				TcpTransportBindingElement idleTimeout = tcpTransportBindingElement;
				idleTimeout.ConnectionPoolSettings.GroupName = transportBindingElement.ConnectionPoolSettings.GroupName;
				idleTimeout.ConnectionPoolSettings.IdleTimeout = transportBindingElement.ConnectionPoolSettings.IdleTimeout;
				idleTimeout.ConnectionPoolSettings.LeaseTimeout = transportBindingElement.ConnectionPoolSettings.LeaseTimeout;
				idleTimeout.ConnectionPoolSettings.MaxOutboundConnectionsPerEndpoint = transportBindingElement.ConnectionPoolSettings.MaxOutboundConnectionsPerEndpoint;
				BindingElement[] bindingElementArray1 = new BindingElement[] { sslStreamSecurityBindingElement1, binaryMessageEncodingBindingElement, idleTimeout };
				customBinding = new CustomBinding(bindingElementArray1);
			}
			customBinding.ReceiveTimeout = context.Binding.ReceiveTimeout;
			customBinding.CloseTimeout = context.Binding.CloseTimeout;
			customBinding.OpenTimeout = context.Binding.OpenTimeout;
			customBinding.SendTimeout = context.Binding.SendTimeout;
			customBinding.Name = context.Binding.Name;
			customBinding.Namespace = context.Binding.Namespace;
			return customBinding;
		}

		private void Disconnected(Exception error, bool isRetry)
		{
			RelayedOnewayTcpClient.RelayedOnewayStatus relayedOnewayStatu;
			lock (base.ThisLock)
			{
				if (error == null)
				{
					error = new ConnectionLostException("The connection to the connect service was lost.");
				}
				MessagingClientEtwProvider.Provider.RelayClientDisconnected(this.Activity, this.uri.AbsoluteUri, this.IsListener, error.ToStringSlim());
				if (base.State != CommunicationState.Opened || !ErrorUtility.CanRetry(error))
				{
					relayedOnewayStatu = RelayedOnewayTcpClient.RelayedOnewayStatus.Offline;
				}
				else
				{
					relayedOnewayStatu = RelayedOnewayTcpClient.RelayedOnewayStatus.Connecting;
					if (isRetry)
					{
						NetworkDetector.Reset();
						try
						{
							InternalConnectivityMode internalConnectivityMode = this.GetInternalConnectivityMode();
							MessagingClientEtwProvider.Provider.RelayClientConnectivityModeDetected(this.Activity, this.uri.AbsoluteUri, this.IsListener, internalConnectivityMode.ToString());
							if (internalConnectivityMode != this.connectivityMode)
							{
								this.SetConnectivityModeRelatedParameters(internalConnectivityMode);
							}
						}
						catch (Exception exception1)
						{
							Exception exception = exception1;
							if (Fx.IsFatal(exception))
							{
								throw;
							}
							error = exception;
						}
					}
				}
			}
			this.SetStatus(relayedOnewayStatu, error);
			lock (base.ThisLock)
			{
				if (base.State != CommunicationState.Closing && base.State != CommunicationState.Closed && this.connectionStatus == RelayedOnewayTcpClient.RelayedOnewayStatus.Connecting)
				{
					this.retryTimer.Set(this.connectSleep);
				}
			}
		}

		public static Message EndRequest(IAsyncResult result)
		{
			return AsyncResult<RelayedOnewayTcpClient.AsyncRequestReplyContext>.End(result).Reply;
		}

		public void EndSend(IAsyncResult result)
		{
			Message message = RelayedOnewayTcpClient.EndRequest(result);
			using (message)
			{
				if (message.IsFault)
				{
					throw ErrorUtility.ConvertToError(MessageFault.CreateFault(message, 65536));
				}
			}
		}

		private void EnsureConnected()
		{
			this.EnsureConnected(this.timeouts.OpenTimeout);
		}

		private RelayedOnewayTcpClient.RelayedOnewayConnection EnsureConnected(TimeSpan timeout)
		{
			return this.EnsureConnected(timeout, true);
		}

		private RelayedOnewayTcpClient.RelayedOnewayConnection EnsureConnected(TimeSpan timeout, bool isRetry)
		{
			RelayedOnewayTcpClient.RelayedOnewayConnection relayedOnewayConnection;
			lock (base.ThisLock)
			{
				if (this.connection != null)
				{
					if (this.connection.State != CommunicationState.Opened)
					{
						this.connection.Abort();
						this.connection = null;
					}
					else
					{
						relayedOnewayConnection = this.connection;
						return relayedOnewayConnection;
					}
				}
				try
				{
					MessagingClientEtwProvider.Provider.RelayClientConnecting(this.Activity, this.uri.AbsoluteUri, this.IsListener);
					RelayedOnewayTcpClient.RelayedOnewayConnection relayedOnewayConnection1 = this.Connect(this.timeouts.OpenTimeout);
					this.Connected(relayedOnewayConnection1);
					relayedOnewayConnection = relayedOnewayConnection1;
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					if (this.connectSleep == TimeSpan.Zero)
					{
						this.connectSleep = TimeSpan.FromSeconds(2.5);
					}
					else if (this.connectSleep < TimeSpan.FromSeconds(10))
					{
						this.connectSleep = TimeSpan.FromSeconds(this.connectSleep.TotalSeconds * 2);
					}
					this.Disconnected(exception, isRetry);
					throw;
				}
			}
			return relayedOnewayConnection;
		}

		private static Type GetErrorType(Exception error)
		{
			if (error == null)
			{
				return null;
			}
			return error.GetType();
		}

		private InternalConnectivityMode GetInternalConnectivityMode()
		{
			ConnectivitySettings connectivitySetting = this.context.BindingParameters.Find<ConnectivitySettings>();
			HttpConnectivitySettings httpConnectivitySetting = this.context.BindingParameters.Find<HttpConnectivitySettings>();
			return ConnectivityModeHelper.GetInternalConnectivityMode(connectivitySetting, httpConnectivitySetting, this.uri);
		}

		protected virtual RelayedOnewayTcpClient.RelayedOnewayConnection GetOrCreateConnection(System.Uri via, TimeSpan timeout)
		{
			RelayedOnewayTcpClient.RelayedOnewayConnection relayedOnewayConnection;
			lock (base.ThisLock)
			{
				if (this.connection != null)
				{
					if (!(this.connection.Via == via) || this.connection.State != CommunicationState.Opened)
					{
						this.connection.Abort();
					}
					else
					{
						relayedOnewayConnection = this.connection;
						return relayedOnewayConnection;
					}
				}
				base.ThrowIfDisposed();
				RelayedOnewayTcpClient.RelayedOnewayTcpConnection relayedOnewayTcpConnection = new RelayedOnewayTcpClient.RelayedOnewayTcpConnection(this, this.messageVersion, new EndpointAddress(this.uri, new AddressHeader[0]), via);
				try
				{
					relayedOnewayTcpConnection.Open(timeout);
				}
				catch (Exception exception)
				{
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					relayedOnewayTcpConnection.Abort();
					throw;
				}
				this.receivePump = new RelayedOnewayTcpClient.ReceivePump(relayedOnewayTcpConnection, this);
				this.receivePump.Open();
				relayedOnewayConnection = relayedOnewayTcpConnection;
			}
			return relayedOnewayConnection;
		}

		protected virtual void MessageReceived(Message message, Action dequeuedCallback)
		{
			message.Close();
			dequeuedCallback();
		}

		protected override void OnAbort()
		{
			if (this.connection != null)
			{
				this.connection.Abort();
			}
			this.channelFactory.Abort();
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
			if (this.connection != null)
			{
				this.connection.Close(timeoutHelper.RemainingTime());
			}
			return this.channelFactory.BeginClose(timeoutHelper.RemainingTime(), callback, state);
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			this.EnsureConnected(timeout, false);
			return new CompletedAsyncResult(callback, state);
		}

		protected override void OnClose(TimeSpan timeout)
		{
			Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
			if (this.connection != null)
			{
				this.connection.Close(timeoutHelper.RemainingTime());
			}
			this.channelFactory.Close(timeoutHelper.RemainingTime());
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			this.channelFactory.EndClose(result);
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
		}

		protected override void OnOpen(TimeSpan timeout)
		{
			this.EnsureConnected(timeout, false);
		}

		protected virtual void RaiseEvent()
		{
			EventHandler eventHandler = null;
			switch (this.connectionStatus)
			{
				case RelayedOnewayTcpClient.RelayedOnewayStatus.Connecting:
				{
					eventHandler = this.Connecting;
					break;
				}
				case RelayedOnewayTcpClient.RelayedOnewayStatus.Online:
				{
					eventHandler = this.Online;
					break;
				}
				case RelayedOnewayTcpClient.RelayedOnewayStatus.Offline:
				{
					eventHandler = this.Offline;
					break;
				}
			}
			if (eventHandler != null)
			{
				eventHandler(this, EventArgs.Empty);
			}
		}

		public Message Request(Message message, TimeSpan timeout)
		{
			Message message1;
			using (RelayedOnewayTcpClient.SyncRequestReplyContext syncRequestReplyContext = new RelayedOnewayTcpClient.SyncRequestReplyContext(this))
			{
				message1 = syncRequestReplyContext.Request(message, timeout);
			}
			return message1;
		}

		private static void RetryCallback(object state)
		{
			RelayedOnewayTcpClient relayedOnewayTcpClient = (RelayedOnewayTcpClient)state;
			try
			{
				relayedOnewayTcpClient.EnsureConnected();
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				Fx.Exception.TraceHandled(exception, "RelayedOnewayTcpClient.RetryCallback", relayedOnewayTcpClient.Activity);
			}
		}

		public void Send(Message message, TimeSpan timeout)
		{
			Message message1 = this.Request(message, timeout);
			using (message1)
			{
				if (message1.IsFault)
				{
					throw ErrorUtility.ConvertToError(MessageFault.CreateFault(message1, 65536));
				}
			}
		}

		private bool SetConnectionStatus(RelayedOnewayTcpClient.RelayedOnewayStatus status, Exception error)
		{
			if (this.connectionStatus == status && !(RelayedOnewayTcpClient.GetErrorType(this.connectionError) != RelayedOnewayTcpClient.GetErrorType(error)))
			{
				return false;
			}
			this.connectionStatus = status;
			this.connectionError = error;
			return true;
		}

		private void SetConnectivityModeRelatedParameters(InternalConnectivityMode mode)
		{
			bool flag = false;
			this.connectivityMode = mode;
			if (this.connectivityMode == InternalConnectivityMode.Http || this.connectivityMode == InternalConnectivityMode.Https)
			{
				flag = true;
			}
			UriBuilder uriBuilder = new UriBuilder(this.uri)
			{
				Scheme = (flag | mode == InternalConnectivityMode.HttpsWebSocket ? "sb" : System.Uri.UriSchemeNetTcp)
			};
			UriBuilder relayHttpsPort = uriBuilder;
			if (!flag && mode != InternalConnectivityMode.HttpsWebSocket)
			{
				relayHttpsPort.Port = (this.transportProtectionEnabled ? 9351 : 9350);
			}
			else if (this.connectivityMode != InternalConnectivityMode.Http)
			{
				relayHttpsPort.Port = RelayEnvironment.RelayHttpsPort;
				this.transportProtectionEnabled = false;
			}
			else
			{
				relayHttpsPort.Port = RelayEnvironment.RelayHttpPort;
			}
			this.Via = relayHttpsPort.Uri;
			if (this.channelFactory != null)
			{
				this.channelFactory.Abort();
			}
			if (!this.transportProtectionEnabled)
			{
				Binding binding = this.CreateNetTcpOnewayBinding(this.context, this.bindingElement);
				this.channelFactory = binding.BuildChannelFactory<IDuplexSessionChannel>(new object[0]);
				this.channelFactory.Open();
				return;
			}
			Binding binding1 = this.CreateSecureNetTcpOnewayBinding(this.context, this.bindingElement, new LenientDnsIdentityVerifier(this.uri.Host));
			ClientCredentials clientCredential = new ClientCredentials();
			clientCredential.ServiceCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.Custom;
			clientCredential.ServiceCertificate.Authentication.CustomCertificateValidator = RetriableCertificateValidator.Instance;
			this.channelFactory = binding1.BuildChannelFactory<IDuplexSessionChannel>(new BindingParameterCollection()
			{
				clientCredential
			});
			this.channelFactory.Open();
		}

		private void SetStatus(RelayedOnewayTcpClient.RelayedOnewayStatus status, Exception error)
		{
			if (this.SetConnectionStatus(status, error))
			{
				this.RaiseEvent();
			}
		}

		public event EventHandler Connecting;

		public event EventHandler Offline;

		public event EventHandler Online;

		private sealed class AsyncRequestReplyContext : AsyncResult<RelayedOnewayTcpClient.AsyncRequestReplyContext>, RelayedOnewayTcpClient.IRequestReplyContext
		{
			private readonly static AsyncCallback sendCallback;

			private readonly static Action<object> onExpiration;

			private readonly static Action<AsyncResult, Exception> onComplete;

			private readonly RelayedOnewayTcpClient client;

			private readonly Message message;

			private readonly IOThreadTimer expirationTimer;

			private Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper;

			private RelayedOnewayTcpClient.RelayedOnewayConnection connection;

			public Message Reply
			{
				get;
				private set;
			}

			static AsyncRequestReplyContext()
			{
				RelayedOnewayTcpClient.AsyncRequestReplyContext.sendCallback = new AsyncCallback(RelayedOnewayTcpClient.AsyncRequestReplyContext.SendCallback);
				RelayedOnewayTcpClient.AsyncRequestReplyContext.onExpiration = new Action<object>(RelayedOnewayTcpClient.AsyncRequestReplyContext.OnExpiration);
				RelayedOnewayTcpClient.AsyncRequestReplyContext.onComplete = new Action<AsyncResult, Exception>(RelayedOnewayTcpClient.AsyncRequestReplyContext.OnComplete);
			}

			public AsyncRequestReplyContext(RelayedOnewayTcpClient client, Message message, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				this.client = client;
				if (message.Headers.MessageId == null)
				{
					message.Headers.MessageId = new UniqueId();
				}
				this.message = message;
				this.timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
				RelayedOnewayTcpClient.AsyncRequestReplyContext asyncRequestReplyContext = this;
				asyncRequestReplyContext.OnCompleting = (Action<AsyncResult, Exception>)Delegate.Combine(asyncRequestReplyContext.OnCompleting, RelayedOnewayTcpClient.AsyncRequestReplyContext.onComplete);
				this.expirationTimer = new IOThreadTimer(RelayedOnewayTcpClient.AsyncRequestReplyContext.onExpiration, this, false);
				this.expirationTimer.Set(timeout);
				this.Start();
			}

			void Microsoft.ServiceBus.RelayedOnewayTcpClient.IRequestReplyContext.Complete(Message reply)
			{
				this.Reply = reply;
				base.TryComplete(false, null);
			}

			private static void OnComplete(AsyncResult result, Exception exception)
			{
				RelayedOnewayTcpClient.AsyncRequestReplyContext asyncRequestReplyContext = (RelayedOnewayTcpClient.AsyncRequestReplyContext)result;
				asyncRequestReplyContext.client.Correlator.Remove<RelayedOnewayTcpClient.IRequestReplyContext>(asyncRequestReplyContext.message);
				asyncRequestReplyContext.expirationTimer.Cancel();
			}

			private static void OnExpiration(object state)
			{
				((RelayedOnewayTcpClient.AsyncRequestReplyContext)state).TryComplete(false, new TimeoutException("Failed to receive reply within the specified timeout"));
			}

			private static void SendCallback(IAsyncResult result)
			{
				if (result.CompletedSynchronously)
				{
					return;
				}
				((RelayedOnewayTcpClient.AsyncRequestReplyContext)result.AsyncState).SendComplete(result, false);
			}

			private void SendComplete(IAsyncResult result, bool completedSynchronously)
			{
				try
				{
					this.connection.EndSend(result);
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					base.TryComplete(completedSynchronously, exception);
				}
			}

			private void Start()
			{
				try
				{
					this.connection = this.client.EnsureConnected(this.timeoutHelper.RemainingTime());
					this.client.Correlator.Add<RelayedOnewayTcpClient.IRequestReplyContext>(this.message, this);
					IAsyncResult asyncResult = this.connection.BeginSend(this.message, this.timeoutHelper.RemainingTime(), RelayedOnewayTcpClient.AsyncRequestReplyContext.sendCallback, this);
					if (asyncResult.CompletedSynchronously)
					{
						this.SendComplete(asyncResult, true);
					}
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					base.TryComplete(true, exception);
				}
			}
		}

		protected class ConnectRequestReplyContext : RelayedOnewayTcpClient.IRequestReplyContext, IDisposable
		{
			private readonly RelayedOnewayTcpClient client;

			private readonly object mutex;

			private readonly AutoResetEvent waitEvent;

			private bool disposed;

			private Message reply;

			public ConnectRequestReplyContext(RelayedOnewayTcpClient client)
			{
				this.client = client;
				this.mutex = new object();
				this.waitEvent = new AutoResetEvent(false);
			}

			public void Complete(Message reply)
			{
				this.reply = reply;
				try
				{
					this.waitEvent.Set();
				}
				catch (ObjectDisposedException objectDisposedException)
				{
				}
			}

			public void Dispose()
			{
				lock (this.mutex)
				{
					if (!this.disposed)
					{
						this.disposed = true;
					}
					else
					{
						return;
					}
				}
				this.waitEvent.Close();
				GC.SuppressFinalize(this);
			}

			public void Send(Message message, TimeSpan timeout, out RelayedOnewayTcpClient.RelayedOnewayConnection channel)
			{
				Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
				if (message.Headers.MessageId == null)
				{
					message.Headers.MessageId = new UniqueId();
				}
				MessageBuffer messageBuffer = message.CreateBufferedCopy(65536);
				System.Uri via = this.client.Via;
				int num = 0;
			Label2:
				while (num < 3)
				{
					RelayedOnewayTcpClient.RelayedOnewayConnection orCreateConnection = this.client.GetOrCreateConnection(via, timeoutHelper.RemainingTime());
					try
					{
						message = messageBuffer.CreateMessage();
						this.client.Correlator.Add<RelayedOnewayTcpClient.IRequestReplyContext>(message, this);
						try
						{
							orCreateConnection.Send(message, timeout);
							if (!this.waitEvent.WaitOne(timeoutHelper.RemainingTime(), false))
							{
								throw Fx.Exception.AsError(new TimeoutException(), this.client.Activity);
							}
						}
						catch (Exception exception1)
						{
							Exception exception = exception1;
							if (Fx.IsFatal(exception))
							{
								throw;
							}
							MessagingClientEtwProvider.Provider.RelayClientConnectFailed(this.client.Activity, via.AbsoluteUri, exception.Message);
							this.client.Correlator.Remove<RelayedOnewayTcpClient.IRequestReplyContext>(message);
							throw;
						}
						Message message1 = this.reply;
						this.reply = null;
						using (message1)
						{
							if (message1.IsFault)
							{
								throw ErrorUtility.ConvertToError(MessageFault.CreateFault(message1, 65536));
							}
							if (message1.Headers.Action == "Redirect")
							{
								RedirectMessage body = message1.GetBody<RedirectMessage>();
								MessagingClientEtwProvider.Provider.RelayClientConnectRedirected(this.client.Activity, via.AbsoluteUri, body.Uri.AbsoluteUri);
								via = body.Uri;
							}
							else
							{
								channel = orCreateConnection;
								orCreateConnection = null;
								return;
							}
						}
						goto Label0;
					}
					finally
					{
						if (orCreateConnection != null)
						{
							try
							{
								orCreateConnection.Close(this.client.DefaultCloseTimeout);
							}
							catch (CommunicationException communicationException)
							{
								orCreateConnection.Abort();
							}
							catch (TimeoutException timeoutException)
							{
								orCreateConnection.Abort();
							}
						}
					}
					return;
				}
				throw Fx.Exception.AsError(new CommunicationException(SRClient.MaxRedirectsExceeded(3)), null);
			Label0:
				num++;
				goto Label2;
			}
		}

		private interface IRequestReplyContext
		{
			void Complete(Message response);
		}

		protected class ReceivePump
		{
			private readonly static Action<object> startReceivingStatic;

			private readonly static AsyncCallback receiveCallback;

			private readonly RelayedOnewayTcpClient.RelayedOnewayConnection connection;

			private readonly RelayedOnewayTcpClient client;

			static ReceivePump()
			{
				RelayedOnewayTcpClient.ReceivePump.startReceivingStatic = new Action<object>(RelayedOnewayTcpClient.ReceivePump.StartReceivingStatic);
				RelayedOnewayTcpClient.ReceivePump.receiveCallback = new AsyncCallback(RelayedOnewayTcpClient.ReceivePump.ReceiveCallback);
			}

			public ReceivePump(RelayedOnewayTcpClient.RelayedOnewayConnection connection, RelayedOnewayTcpClient client)
			{
				this.connection = connection;
				this.client = client;
			}

			private void OnMessageDequeued()
			{
				IOThreadScheduler.ScheduleCallbackNoFlow(RelayedOnewayTcpClient.ReceivePump.startReceivingStatic, this);
			}

			public void Open()
			{
				try
				{
					IAsyncResult asyncResult = this.connection.BeginReceive(TimeSpan.MaxValue, RelayedOnewayTcpClient.ReceivePump.receiveCallback, this);
					if (asyncResult.CompletedSynchronously)
					{
						this.ReceiveComplete(asyncResult);
					}
				}
				catch
				{
					if (this.connection.State == CommunicationState.Opened)
					{
						throw;
					}
					else
					{
						this.connection.Abort();
					}
				}
			}

			private static void ReceiveCallback(IAsyncResult result)
			{
				if (result.CompletedSynchronously)
				{
					return;
				}
				((RelayedOnewayTcpClient.ReceivePump)result.AsyncState).ReceiveComplete(result);
			}

			private void ReceiveComplete(IAsyncResult result)
			{
				try
				{
					Message message = this.connection.EndReceive(result);
					if (message != null)
					{
						if (message.Headers.RelatesTo != null)
						{
							RelayedOnewayTcpClient.IRequestReplyContext requestReplyContext = this.client.Correlator.Find<RelayedOnewayTcpClient.IRequestReplyContext>(message, true);
							if (requestReplyContext != null)
							{
								requestReplyContext.Complete(message);
								IOThreadScheduler.ScheduleCallbackNoFlow(RelayedOnewayTcpClient.ReceivePump.startReceivingStatic, this);
								return;
							}
						}
						this.client.MessageReceived(message, new Action(this.OnMessageDequeued));
					}
					else
					{
						this.connection.Close(this.client.DefaultCloseTimeout);
					}
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					if (this.connection.State == CommunicationState.Opened)
					{
						IOThreadScheduler.ScheduleCallbackNoFlow(RelayedOnewayTcpClient.ReceivePump.startReceivingStatic, this);
						CultureInfo invariantCulture = CultureInfo.InvariantCulture;
						object[] type = new object[] { exception.GetType(), exception.Message };
						Trace.WriteLine(string.Format(invariantCulture, "Unknown exception on server: {0}, {1}", type));
					}
					else
					{
						this.connection.Abort();
					}
				}
			}

			private void StartReceiving()
			{
				try
				{
					IAsyncResult asyncResult = this.connection.BeginReceive(TimeSpan.MaxValue, RelayedOnewayTcpClient.ReceivePump.receiveCallback, this);
					if (asyncResult.CompletedSynchronously)
					{
						this.ReceiveComplete(asyncResult);
					}
				}
				catch
				{
					if (this.connection.State == CommunicationState.Opened)
					{
						IOThreadScheduler.ScheduleCallbackNoFlow(RelayedOnewayTcpClient.ReceivePump.startReceivingStatic, this);
					}
					else
					{
						this.connection.Abort();
					}
				}
			}

			private static void StartReceivingStatic(object state)
			{
				((RelayedOnewayTcpClient.ReceivePump)state).StartReceiving();
			}
		}

		protected abstract class RelayedOnewayConnection
		{
			public abstract Exception LastError
			{
				get;
			}

			public abstract CommunicationState State
			{
				get;
			}

			public System.Uri Via
			{
				get;
				private set;
			}

			protected RelayedOnewayConnection(System.Uri via)
			{
				this.Via = via;
			}

			public abstract void Abort();

			public abstract IAsyncResult BeginReceive(TimeSpan timeout, AsyncCallback callback, object state);

			public abstract IAsyncResult BeginSend(Message message, TimeSpan timeout, AsyncCallback callback, object state);

			public abstract void Close(TimeSpan timeout);

			public abstract Message EndReceive(IAsyncResult result);

			public abstract void EndSend(IAsyncResult result);

			protected void RaiseClosed(object sender, EventArgs args)
			{
				EventHandler eventHandler = this.Closed;
				if (eventHandler != null)
				{
					eventHandler(sender, args);
				}
			}

			protected void RaiseFaulted(object sender, EventArgs args)
			{
				EventHandler eventHandler = this.Faulted;
				if (eventHandler != null)
				{
					eventHandler(sender, args);
				}
			}

			public virtual void Send(Message message, TimeSpan timeout)
			{
				this.EndSend(this.BeginSend(message, timeout, null, null));
			}

			public event EventHandler Closed;

			public event EventHandler Faulted;
		}

		private enum RelayedOnewayStatus
		{
			Connecting,
			Online,
			Offline
		}

		private sealed class RelayedOnewayTcpConnection : RelayedOnewayTcpClient.RelayedOnewayConnection
		{
			private readonly static Action<object> pingCallbackStatic;

			private readonly RelayedOnewayTcpClient client;

			private readonly IDuplexSessionChannel innerChannel;

			private readonly System.ServiceModel.Channels.MessageVersion messageVersion;

			private readonly IOThreadTimer pingTimer;

			private readonly EndpointAddress to;

			private readonly string requiredTokenAction;

			private Exception lastError;

			private int messagesInFlight;

			private SecurityToken activeToken;

			private int getTokenRetryCounter;

			public override Exception LastError
			{
				get
				{
					return this.lastError;
				}
			}

			private System.ServiceModel.Channels.MessageVersion MessageVersion
			{
				get
				{
					return this.messageVersion;
				}
			}

			public override CommunicationState State
			{
				get
				{
					return this.innerChannel.State;
				}
			}

			private object ThisLock
			{
				get;
				set;
			}

			static RelayedOnewayTcpConnection()
			{
				RelayedOnewayTcpClient.RelayedOnewayTcpConnection.pingCallbackStatic = new Action<object>(RelayedOnewayTcpClient.RelayedOnewayTcpConnection.PingCallbackStatic);
			}

			public RelayedOnewayTcpConnection(RelayedOnewayTcpClient client, System.ServiceModel.Channels.MessageVersion messageVersion, EndpointAddress to, System.Uri via) : base(via)
			{
				this.client = client;
				this.messageVersion = messageVersion;
				this.to = to;
				this.innerChannel = client.ChannelFactory.CreateChannel(to, via);
				this.innerChannel.Closed += new EventHandler(this.OnInnerChannelClosed);
				this.innerChannel.Closing += new EventHandler(this.OnInnerChannelClosing);
				this.innerChannel.Faulted += new EventHandler(this.OnInnerChannelFaulted);
				this.pingTimer = new IOThreadTimer(RelayedOnewayTcpClient.RelayedOnewayTcpConnection.pingCallbackStatic, this, false);
				this.ThisLock = new object();
				this.requiredTokenAction = (this.client.IsListener ? "Listen" : "Send");
			}

			public override void Abort()
			{
				this.pingTimer.Cancel();
				this.innerChannel.Abort();
			}

			public override IAsyncResult BeginReceive(TimeSpan timeout, AsyncCallback callback, object state)
			{
				return this.innerChannel.BeginReceive(timeout, callback, state);
			}

			public override IAsyncResult BeginSend(Message message, TimeSpan timeout, AsyncCallback callback, object state)
			{
				IAsyncResult asyncResult;
				if (Interlocked.Increment(ref this.messagesInFlight) == 1)
				{
					this.pingTimer.Cancel();
				}
				try
				{
					asyncResult = this.innerChannel.BeginSend(message, timeout, callback, state);
				}
				catch
				{
					Interlocked.Decrement(ref this.messagesInFlight);
					throw;
				}
				return asyncResult;
			}

			public override void Close(TimeSpan timeout)
			{
				this.innerChannel.Close(timeout);
			}

			public override Message EndReceive(IAsyncResult result)
			{
				return this.innerChannel.EndReceive(result);
			}

			public override void EndSend(IAsyncResult result)
			{
				try
				{
					this.innerChannel.EndSend(result);
				}
				finally
				{
					if (Interlocked.Decrement(ref this.messagesInFlight) == 0)
					{
						this.SetPing();
					}
				}
			}

			private void OnInnerChannelClosed(object sender, EventArgs args)
			{
				base.RaiseClosed(this, args);
			}

			private void OnInnerChannelClosing(object sender, EventArgs args)
			{
				this.pingTimer.Cancel();
			}

			private void OnInnerChannelFaulted(object sender, EventArgs args)
			{
				this.pingTimer.Cancel();
				if (this.lastError == null)
				{
					string communicationObjectFaultedStack2 = Resources.CommunicationObjectFaultedStack2;
					object[] type = new object[] { sender.GetType(), new StackTrace() };
					this.lastError = new CommunicationObjectFaultedException(Microsoft.ServiceBus.SR.GetString(communicationObjectFaultedStack2, type));
				}
				base.RaiseFaulted(this, args);
			}

			public void Open(TimeSpan timeout)
			{
				try
				{
					Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
					this.innerChannel.Open(timeoutHelper.RemainingTime());
					this.SetPing();
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (!Fx.IsFatal(exception))
					{
						this.lastError = exception;
					}
					throw;
				}
			}

			private static void PingCallbackStatic(object state)
			{
				RelayedOnewayTcpClient.RelayedOnewayTcpConnection relayedOnewayTcpConnection = (RelayedOnewayTcpClient.RelayedOnewayTcpConnection)state;
				if (relayedOnewayTcpConnection.State == CommunicationState.Opened)
				{
					SecurityToken securityToken = relayedOnewayTcpConnection.TryGetNewToken(relayedOnewayTcpConnection.client.timeouts.SendTimeout);
					relayedOnewayTcpConnection.SendPingMessage(securityToken, relayedOnewayTcpConnection.client.timeouts.SendTimeout);
				}
			}

			private void RenewTokenIfNeeded(TimeSpan timeout)
			{
				bool flag;
				Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
				SecurityToken securityToken = this.TryGetNewToken(timeoutHelper.RemainingTime());
				lock (this.ThisLock)
				{
					flag = securityToken != this.activeToken;
				}
				if (flag)
				{
					this.SendPingMessage(securityToken, timeoutHelper.RemainingTime());
				}
			}

			public override void Send(Message message, TimeSpan timeout)
			{
				Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
				if (message.Headers.Action != "http://schemas.microsoft.com/netservices/2009/05/servicebus/connect/OnewayPing" && message.Headers.FindHeader("RelayAccessToken", "http://schemas.microsoft.com/netservices/2009/05/servicebus/connect") == -1)
				{
					this.RenewTokenIfNeeded(timeoutHelper.RemainingTime());
				}
				if (Interlocked.Increment(ref this.messagesInFlight) == 1)
				{
					this.pingTimer.Cancel();
				}
				try
				{
					this.innerChannel.Send(message, timeoutHelper.RemainingTime());
				}
				finally
				{
					if (Interlocked.Decrement(ref this.messagesInFlight) == 0)
					{
						this.SetPing();
					}
				}
			}

			private void SendPingMessage(SecurityToken token, TimeSpan timeout)
			{
				bool flag;
				Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
				lock (this.ThisLock)
				{
					flag = token != this.activeToken;
				}
				Exception exception = null;
				try
				{
					Message via = Message.CreateMessage(this.MessageVersion, "http://schemas.microsoft.com/netservices/2009/05/servicebus/connect/OnewayPing", new OnewayPingMessage());
					this.to.ApplyTo(via);
					via.Properties.Via = base.Via;
					via.Headers.MessageId = new UniqueId();
					via.Headers.ReplyTo = EndpointAddress2.AnonymousAddress;
					if (flag && token != null)
					{
						via.Headers.Add(new RelayTokenHeader(token));
					}
					this.Send(via, timeoutHelper.RemainingTime());
					lock (this.ThisLock)
					{
						if (flag)
						{
							this.activeToken = token;
						}
					}
				}
				catch (ObjectDisposedException objectDisposedException)
				{
					exception = objectDisposedException;
				}
				catch (CommunicationException communicationException)
				{
					exception = communicationException;
				}
				catch (TimeoutException timeoutException)
				{
					exception = timeoutException;
				}
				if (exception != null)
				{
					this.Abort();
					MessagingClientEtwProvider.Provider.RelayClientPingFailed(this.client.Activity, base.Via.AbsoluteUri, this.client.IsListener, exception.ToStringSlim());
				}
			}

			private void SetPing()
			{
				if (this.State == CommunicationState.Opened)
				{
					this.pingTimer.Set(TimeSpan.FromSeconds(30));
				}
			}

			private SecurityToken TryGetNewToken(TimeSpan timeout)
			{
				try
				{
					if (this.client.TokenProvider != null)
					{
						SecurityToken token = this.client.TokenProvider.GetToken(this.client.AppliesTo, this.requiredTokenAction, false, timeout);
						this.getTokenRetryCounter = 0;
						return token;
					}
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					if (this.getTokenRetryCounter <= 3)
					{
						MessagingClientEtwProvider.Provider.RelayClientFailedToAcquireToken(this.client.Activity, base.Via.AbsoluteUri, this.client.IsListener, "Retry", exception.ToStringSlim());
					}
					else
					{
						MessagingClientEtwProvider.Provider.RelayClientFailedToAcquireToken(this.client.Activity, base.Via.AbsoluteUri, this.client.IsListener, "CloseAndReconnect", exception.ToStringSlim());
						try
						{
							this.Close(this.client.timeouts.CloseTimeout);
						}
						catch
						{
							this.Abort();
						}
					}
					RelayedOnewayTcpClient.RelayedOnewayTcpConnection relayedOnewayTcpConnection = this;
					relayedOnewayTcpConnection.getTokenRetryCounter = relayedOnewayTcpConnection.getTokenRetryCounter + 1;
				}
				return null;
			}
		}

		private class SyncRequestReplyContext : RelayedOnewayTcpClient.IRequestReplyContext, IDisposable
		{
			private readonly RelayedOnewayTcpClient client;

			private readonly object mutex;

			private readonly AutoResetEvent waitEvent;

			private bool disposed;

			private Message reply;

			public SyncRequestReplyContext(RelayedOnewayTcpClient client)
			{
				this.client = client;
				this.mutex = new object();
				this.waitEvent = new AutoResetEvent(false);
			}

			public void Complete(Message reply)
			{
				this.reply = reply;
				try
				{
					this.waitEvent.Set();
				}
				catch (ObjectDisposedException objectDisposedException)
				{
				}
			}

			public void Dispose()
			{
				lock (this.mutex)
				{
					if (!this.disposed)
					{
						this.disposed = true;
					}
					else
					{
						return;
					}
				}
				this.waitEvent.Close();
				GC.SuppressFinalize(this);
			}

			public Message Request(Message message, TimeSpan timeout)
			{
				Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
				if (message.Headers.MessageId == null)
				{
					message.Headers.MessageId = new UniqueId();
				}
				RelayedOnewayTcpClient.RelayedOnewayConnection relayedOnewayConnection = this.client.EnsureConnected(timeoutHelper.RemainingTime());
				this.client.Correlator.Add<RelayedOnewayTcpClient.IRequestReplyContext>(message, this);
				try
				{
					relayedOnewayConnection.Send(message, timeoutHelper.RemainingTime());
					if (!this.waitEvent.WaitOne(timeoutHelper.RemainingTime(), false))
					{
						throw Fx.Exception.AsError(new TimeoutException(), null);
					}
				}
				catch
				{
					this.client.Correlator.Remove<RelayedOnewayTcpClient.IRequestReplyContext>(message);
					throw;
				}
				return this.reply;
			}
		}
	}
}