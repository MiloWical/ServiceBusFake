using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace Microsoft.ServiceBus
{
	[ServiceBehavior(ConcurrencyMode=ConcurrencyMode.Multiple, InstanceContextMode=InstanceContextMode.Single, AddressFilterMode=AddressFilterMode.Prefix)]
	internal class WebSocketRelayedConnectionListener : WebSocketConnectionListener, IRelayedConnectionControl
	{
		private readonly ServiceHost serviceHost;

		private readonly NameSettings nameSettings;

		private readonly TokenProvider tokenProvider;

		private readonly ConnectionStatusBehavior innerConnectionStatus;

		private readonly ConnectivitySettings connectivitySettings;

		private readonly HttpConnectivitySettings httpConnectivitySettings;

		private int bufferSize;

		private SocketMessageHelper messageHelper;

		private Uri uri;

		public IConnectionStatus ConnectionStatus
		{
			get
			{
				return this.innerConnectionStatus;
			}
		}

		public WebSocketRelayedConnectionListener(TokenProvider tokenProvider, SocketSecurityRole socketSecurityMode, NameSettings nameSettings, BindingContext context)
		{
			this.tokenProvider = tokenProvider;
			this.nameSettings = nameSettings;
			this.serviceHost = new ConfigurationlessServiceHost(this, new Uri[0]);
			this.innerConnectionStatus = new ConnectionStatusBehavior();
			this.connectivitySettings = context.BindingParameters.Find<ConnectivitySettings>();
			this.httpConnectivitySettings = context.BindingParameters.Find<HttpConnectivitySettings>();
		}

		public override void Abort()
		{
			this.serviceHost.Abort();
		}

		public IAsyncResult BeginConnect(RelayedConnectMessage request, AsyncCallback callback, object state)
		{
			EventTraceActivity eventTraceActivity = new EventTraceActivity(Guid.Parse(request.Id));
			MessagingClientEtwProvider.Provider.RelayListenerRelayedConnectReceived(eventTraceActivity, string.Concat("WebSocket: ", this.uri.AbsoluteUri), request.Id);
			return (new WebSocketRelayedConnectionListener.ConnectAsyncResult(request, this, eventTraceActivity, callback, state)).Start();
		}

		private void BeginConnectCallback(object state)
		{
			Microsoft.ServiceBus.Channels.IConnection connection = (Microsoft.ServiceBus.Channels.IConnection)state;
			this.messageHelper.BeginReceiveMessage(connection, TimeSpan.MaxValue, new AsyncCallback(this.EndConnectCallback), connection);
		}

		public override void Close(TimeSpan timeout)
		{
			try
			{
				this.serviceHost.Close(timeout);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				Fx.Exception.TraceHandled(exception, "WebSocketRelayedConnectionListener.Close", null);
				this.serviceHost.Abort();
			}
		}

		public void EndConnect(IAsyncResult result)
		{
			WebSocketRelayedConnectionListener.ConnectAsyncResult connectAsyncResult = (WebSocketRelayedConnectionListener.ConnectAsyncResult)result;
			try
			{
				AsyncResult<WebSocketRelayedConnectionListener.ConnectAsyncResult>.End(result);
				MessagingClientEtwProvider.Provider.RelayListenerClientAccepted(connectAsyncResult.Activity, string.Concat("WebSocket: ", this.uri.AbsoluteUri), connectAsyncResult.Request.Id);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					MessagingClientEtwProvider.Provider.RelayListenerClientAcceptFailed(connectAsyncResult.Activity, string.Concat("WebSocket: ", this.uri.AbsoluteUri), connectAsyncResult.Request.Id, exception.ToString());
					throw Fx.Exception.AsWarning(new FaultException(exception.ToString()), null);
				}
				throw;
			}
		}

		private void EndConnectCallback(IAsyncResult ar)
		{
			try
			{
				Microsoft.ServiceBus.Channels.IConnection asyncState = (Microsoft.ServiceBus.Channels.IConnection)ar.AsyncState;
				Message message = this.messageHelper.EndReceiveMessage(ar);
				if (message.IsFault)
				{
					MessageFault messageFault = MessageFault.CreateFault(message, 65536);
					throw Fx.Exception.AsError(ErrorUtility.ConvertToError(messageFault), null);
				}
				base.EnqueueConnection(asyncState, false);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				Fx.Exception.TraceHandled(exception, "WebSocketRelayedConnectionListener.EndConnectCallback", null);
			}
		}

		public void Initialize(Uri uri, int bufferSize, Binding innerBinding)
		{
			this.uri = uri;
			this.bufferSize = bufferSize;
			ServiceEndpoint serviceEndpoint = this.serviceHost.AddServiceEndpoint(typeof(IRelayedConnectionControl), innerBinding, this.uri);
			serviceEndpoint.Behaviors.Add(this.nameSettings);
			TransportClientEndpointBehavior transportClientEndpointBehavior = new TransportClientEndpointBehavior(this.tokenProvider);
			serviceEndpoint.Behaviors.Add(transportClientEndpointBehavior);
			serviceEndpoint.Behaviors.Add(this.innerConnectionStatus);
			if (this.connectivitySettings != null || this.httpConnectivitySettings != null)
			{
				WebSocketRelayedConnectionListener.ConnectivitySettingsEndpointBehavior connectivitySettingsEndpointBehavior = new WebSocketRelayedConnectionListener.ConnectivitySettingsEndpointBehavior(this.connectivitySettings, this.httpConnectivitySettings);
				serviceEndpoint.Behaviors.Add(connectivitySettingsEndpointBehavior);
			}
			ServiceErrorHandlerBehavior serviceErrorHandlerBehavior = new ServiceErrorHandlerBehavior();
			serviceErrorHandlerBehavior.HandleError += new EventHandler<ServiceErrorEventArgs>((object s, ServiceErrorEventArgs e) => Fx.Exception.TraceHandled(e.Exception, "WebSocketRelayedConnectionListener.IErrorHandler.HandleError", null));
			this.serviceHost.Description.Behaviors.Add(serviceErrorHandlerBehavior);
			this.messageHelper = new SocketMessageHelper();
		}

		public override void Open(TimeSpan timeout)
		{
			this.serviceHost.Open(timeout);
		}

		private class ConnectAsyncResult : IteratorAsyncResult<WebSocketRelayedConnectionListener.ConnectAsyncResult>
		{
			private readonly WebSocketRelayedConnectionListener relayedConnectionListener;

			private readonly EventTraceActivity activity;

			private Microsoft.ServiceBus.Channels.IConnection connection;

			private Message message;

			protected internal override EventTraceActivity Activity
			{
				get
				{
					return this.activity;
				}
			}

			public RelayedConnectMessage Request
			{
				get;
				private set;
			}

			public ConnectAsyncResult(RelayedConnectMessage request, WebSocketRelayedConnectionListener relayedConnectionListener, EventTraceActivity activity, AsyncCallback callback, object state) : base(ConnectConstants.ConnectionInitiateTimeout, callback, state)
			{
				this.Request = request;
				this.activity = activity;
				this.relayedConnectionListener = relayedConnectionListener;
			}

			protected override IEnumerator<IteratorAsyncResult<WebSocketRelayedConnectionListener.ConnectAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				Uri uri = ServiceBusUriHelper.CreateServiceUri(Uri.UriSchemeHttps, this.Request.HttpsEndpoint.ToString(), "/");
				ServiceBusClientWebSocket serviceBusClientWebSocket = new ServiceBusClientWebSocket("wsrelayedconnection");
				yield return base.CallAsync((WebSocketRelayedConnectionListener.ConnectAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => serviceBusClientWebSocket.BeginConnect(uri.Host, uri.Port, ConnectConstants.ConnectionInitiateTimeout, c, s), (WebSocketRelayedConnectionListener.ConnectAsyncResult thisPtr, IAsyncResult r) => serviceBusClientWebSocket.EndConnect(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				this.connection = new ClientWebSocketConnection(serviceBusClientWebSocket, this.relayedConnectionListener.bufferSize, this.relayedConnectionListener.uri, this.Activity);
				this.message = Message.CreateMessage(this.relayedConnectionListener.messageHelper.MessageVersion, "RelayedAccept", new AcceptMessage(this.Request.Id));
				this.message.Headers.To = EndpointAddress.AnonymousUri;
				WebSocketRelayedConnectionListener.ConnectAsyncResult connectAsyncResult = this;
				IteratorAsyncResult<WebSocketRelayedConnectionListener.ConnectAsyncResult>.BeginCall beginCall = (WebSocketRelayedConnectionListener.ConnectAsyncResult thisRef, TimeSpan t, AsyncCallback c, object s) => thisRef.relayedConnectionListener.messageHelper.BeginSendMessage(thisRef.connection, thisRef.message, t, c, s);
				yield return connectAsyncResult.CallAsync(beginCall, (WebSocketRelayedConnectionListener.ConnectAsyncResult thisRef, IAsyncResult r) => thisRef.relayedConnectionListener.messageHelper.EndSendMessage(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				IOThreadScheduler.ScheduleCallbackNoFlow(new Action<object>(this.relayedConnectionListener.BeginConnectCallback), this.connection);
			}
		}

		private class ConnectivitySettingsEndpointBehavior : IEndpointBehavior
		{
			private readonly ConnectivitySettings connectivitySettings;

			private readonly HttpConnectivitySettings httpConnectivitySettings;

			public ConnectivitySettingsEndpointBehavior(ConnectivitySettings connectivitySettings, HttpConnectivitySettings httpConnectivitySettings)
			{
				this.connectivitySettings = connectivitySettings;
				this.httpConnectivitySettings = httpConnectivitySettings;
			}

			public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
			{
				if (this.connectivitySettings != null)
				{
					bindingParameters.Add(this.connectivitySettings);
				}
				if (this.httpConnectivitySettings != null)
				{
					bindingParameters.Add(this.httpConnectivitySettings);
				}
			}

			public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
			{
			}

			public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
			{
			}

			public void Validate(ServiceEndpoint endpoint)
			{
			}
		}
	}
}