using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace Microsoft.ServiceBus
{
	[ServiceBehavior(ConcurrencyMode=ConcurrencyMode.Multiple, InstanceContextMode=InstanceContextMode.Single, AddressFilterMode=AddressFilterMode.Prefix)]
	internal class RelayedSocketListener : Microsoft.ServiceBus.Channels.IConnectionListener, IRelayedConnectionControl
	{
		private int bufferSize;

		private Dictionary<string, RelayedConnectionSession> connectionSessions;

		private bool isClosed;

		private object mutex;

		private ServiceHost serviceHost;

		private InputQueue<Microsoft.ServiceBus.Channels.IConnection> socketQueue;

		private NameSettings nameSettings;

		private SocketSecurityRole socketSecurityMode;

		private TokenProvider tokenProvider;

		private Uri uri;

		private ConnectionStatusBehavior innerConnectionStatus;

		private ConnectivitySettingsEndpointBehavior connectivitySettingsBehavior;

		public IConnectionStatus ConnectionStatus
		{
			get
			{
				return this.innerConnectionStatus;
			}
		}

		private object ThisLock
		{
			get
			{
				return this.mutex;
			}
		}

		public RelayedSocketListener(TokenProvider tokenProvider, NameSettings nameSettings, SocketSecurityRole socketSecurityMode, ConnectivitySettingsEndpointBehavior connectivitySettings)
		{
			this.tokenProvider = tokenProvider;
			this.nameSettings = nameSettings;
			this.socketSecurityMode = socketSecurityMode;
			this.serviceHost = new ConfigurationlessServiceHost(this, new Uri[0]);
			this.connectionSessions = new Dictionary<string, RelayedConnectionSession>();
			this.socketQueue = new InputQueue<Microsoft.ServiceBus.Channels.IConnection>();
			this.mutex = new object();
			this.innerConnectionStatus = new ConnectionStatusBehavior();
			this.connectivitySettingsBehavior = connectivitySettings;
		}

		public void Abort()
		{
			List<RelayedConnectionSession> relayedConnectionSessions;
			lock (this.ThisLock)
			{
				if (!this.isClosed)
				{
					this.isClosed = true;
					relayedConnectionSessions = new List<RelayedConnectionSession>(this.connectionSessions.Values);
					this.connectionSessions.Clear();
				}
				else
				{
					return;
				}
			}
			for (int i = 0; i < relayedConnectionSessions.Count; i++)
			{
				relayedConnectionSessions[i].Close();
			}
			this.serviceHost.Abort();
			this.socketQueue.Close();
		}

		public IAsyncResult BeginAccept(AsyncCallback callback, object state)
		{
			return this.socketQueue.BeginDequeue(TimeSpan.MaxValue, callback, state);
		}

		public void Close(TimeSpan timeout)
		{
			List<RelayedConnectionSession> relayedConnectionSessions;
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			lock (this.ThisLock)
			{
				if (!this.isClosed)
				{
					this.isClosed = true;
					relayedConnectionSessions = new List<RelayedConnectionSession>(this.connectionSessions.Values);
					this.connectionSessions.Clear();
				}
				else
				{
					return;
				}
			}
			for (int i = 0; i < relayedConnectionSessions.Count; i++)
			{
				relayedConnectionSessions[i].Close();
			}
			this.serviceHost.Close(timeoutHelper.RemainingTime());
			this.socketQueue.Close();
		}

		public Microsoft.ServiceBus.Channels.IConnection EndAccept(IAsyncResult result)
		{
			return this.socketQueue.EndDequeue(result);
		}

		public void Failure(object sender, Exception exception)
		{
			RelayedConnectionSession relayedConnectionSession = (RelayedConnectionSession)sender;
			MessagingClientEventSource provider = MessagingClientEtwProvider.Provider;
			EventTraceActivity activity = relayedConnectionSession.Activity;
			string absoluteUri = this.uri.AbsoluteUri;
			Guid id = relayedConnectionSession.Id;
			provider.RelayListenerClientAcceptFailed(activity, absoluteUri, id.ToString(), exception.ToString());
			lock (this.ThisLock)
			{
				this.connectionSessions.Remove(relayedConnectionSession.Id.ToString());
			}
			relayedConnectionSession.Close();
		}

		public void Initialize(int bufferSize, Uri uri, Binding innerBinding)
		{
			this.bufferSize = bufferSize;
			this.uri = uri;
			ServiceEndpoint serviceEndpoint = this.serviceHost.AddServiceEndpoint(typeof(IRelayedConnectionControl), innerBinding, this.uri);
			serviceEndpoint.Behaviors.Add(this.nameSettings);
			TransportClientEndpointBehavior transportClientEndpointBehavior = new TransportClientEndpointBehavior(this.tokenProvider);
			serviceEndpoint.Behaviors.Add(transportClientEndpointBehavior);
			serviceEndpoint.Behaviors.Add(this.innerConnectionStatus);
			if (this.connectivitySettingsBehavior != null)
			{
				serviceEndpoint.Behaviors.Add(this.connectivitySettingsBehavior);
			}
			ServiceErrorHandlerBehavior serviceErrorHandlerBehavior = new ServiceErrorHandlerBehavior();
			serviceErrorHandlerBehavior.HandleError += new EventHandler<ServiceErrorEventArgs>((object s, ServiceErrorEventArgs e) => Fx.Exception.TraceHandled(e.Exception, "RelayedSocketListener.IErrorHandler.HandleError", null));
			this.serviceHost.Description.Behaviors.Add(serviceErrorHandlerBehavior);
		}

		IAsyncResult Microsoft.ServiceBus.IRelayedConnectionControl.BeginConnect(RelayedConnectMessage request, AsyncCallback callback, object state)
		{
			RelayedConnectionSession relayedConnectionSession;
			Guid guid = new Guid(request.Id);
			EventTraceActivity eventTraceActivity = new EventTraceActivity(guid);
			EventTraceActivity eventTraceActivity1 = new EventTraceActivity();
			MessagingClientEtwProvider.Provider.RelayListenerRelayedConnectReceived(eventTraceActivity, this.uri.AbsoluteUri, request.Id);
			MessagingClientEtwProvider.Provider.RelayChannelConnectionTransfer(eventTraceActivity, eventTraceActivity1);
			lock (this.ThisLock)
			{
				if (this.isClosed)
				{
					throw Fx.Exception.AsError(new FaultException(SRClient.EndpointNotFoundFault), eventTraceActivity);
				}
				if (this.connectionSessions.ContainsKey(request.Id))
				{
					throw Fx.Exception.AsError(new FaultException(SRClient.DuplicateConnectionIDFault), eventTraceActivity);
				}
				relayedConnectionSession = new RelayedConnectionSession(this.bufferSize, this.uri, this.tokenProvider, this.socketSecurityMode, guid, this, eventTraceActivity1);
				this.connectionSessions.Add(request.Id, relayedConnectionSession);
			}
			return relayedConnectionSession.BeginConnect(request, callback, state);
		}

		void Microsoft.ServiceBus.IRelayedConnectionControl.EndConnect(IAsyncResult result)
		{
			RelayedConnectionSession.End(result);
		}

		public void Open(TimeSpan timeout)
		{
			this.serviceHost.Open(timeout);
		}

		public void Success(object sender, Microsoft.ServiceBus.Channels.IConnection connection)
		{
			RelayedConnectionSession relayedConnectionSession = (RelayedConnectionSession)sender;
			MessagingClientEtwProvider.Provider.RelayListenerClientAccepted(relayedConnectionSession.Activity, this.uri.AbsoluteUri, relayedConnectionSession.Id.ToString());
			lock (this.ThisLock)
			{
				this.connectionSessions.Remove(relayedConnectionSession.Id.ToString());
			}
			this.socketQueue.EnqueueAndDispatch(connection);
			relayedConnectionSession.Close();
		}
	}
}