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

namespace Microsoft.ServiceBus
{
	[ServiceBehavior(ConcurrencyMode=ConcurrencyMode.Multiple, InstanceContextMode=InstanceContextMode.Single, AddressFilterMode=AddressFilterMode.Prefix)]
	internal class WebStreamRelayedConnectionListener : WebStreamConnectionListener, IRelayedConnectionControl
	{
		private int bufferSize;

		private SocketMessageHelper messageHelper;

		private ServiceHost serviceHost;

		private NameSettings nameSettings;

		private TokenProvider tokenProvider;

		private SocketSecurityRole socketSecurityMode;

		private Uri uri;

		private ConnectionStatusBehavior innerConnectionStatus;

		private ConnectivitySettings connectivitySettings;

		private HttpConnectivitySettings httpConnectivitySettings;

		private bool useHttpsMode;

		public IConnectionStatus ConnectionStatus
		{
			get
			{
				return this.innerConnectionStatus;
			}
		}

		public WebStreamRelayedConnectionListener(TokenProvider tokenProvider, SocketSecurityRole socketSecurityMode, NameSettings nameSettings, BindingContext context, bool useHttpsMode)
		{
			this.tokenProvider = tokenProvider;
			this.socketSecurityMode = socketSecurityMode;
			this.nameSettings = nameSettings;
			this.serviceHost = new ConfigurationlessServiceHost(this, new Uri[0]);
			this.innerConnectionStatus = new ConnectionStatusBehavior();
			this.connectivitySettings = context.BindingParameters.Find<ConnectivitySettings>();
			this.httpConnectivitySettings = context.BindingParameters.Find<HttpConnectivitySettings>();
			this.useHttpsMode = useHttpsMode;
		}

		public override void Abort()
		{
			this.serviceHost.Abort();
		}

		public IAsyncResult BeginConnect(RelayedConnectMessage request, AsyncCallback callback, object state)
		{
			EventTraceActivity eventTraceActivity = new EventTraceActivity(Guid.Parse(request.Id));
			MessagingClientEtwProvider.Provider.RelayListenerRelayedConnectReceived(eventTraceActivity, string.Concat("WebStream: ", this.uri.AbsoluteUri), request.Id);
			return (new WebStreamRelayedConnectionListener.ConnectAsyncResult(request, this, eventTraceActivity, callback, state)).Start();
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
				Fx.Exception.TraceHandled(exception, "WebStreamRelayedConnectionListener.Close", null);
				this.serviceHost.Abort();
			}
		}

		public void EndConnect(IAsyncResult result)
		{
			WebStreamRelayedConnectionListener.ConnectAsyncResult connectAsyncResult = (WebStreamRelayedConnectionListener.ConnectAsyncResult)result;
			try
			{
				AsyncResult<WebStreamRelayedConnectionListener.ConnectAsyncResult>.End(result);
				MessagingClientEtwProvider.Provider.RelayListenerClientAccepted(connectAsyncResult.Activity, string.Concat("WebStream: ", this.uri.AbsoluteUri), connectAsyncResult.Request.Id);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					MessagingClientEtwProvider.Provider.RelayListenerClientAcceptFailed(connectAsyncResult.Activity, string.Concat("WebStream: ", this.uri.AbsoluteUri), connectAsyncResult.Request.Id, exception.ToStringSlim());
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
				Fx.Exception.TraceHandled(exception, "WebStreamRelayedConnectionListener.EndConnectCallback", null);
			}
		}

		public void Initialize(Uri uri, int bufferSize, Binding innerBinding)
		{
			this.uri = uri;
			this.bufferSize = bufferSize;
			ServiceEndpoint serviceEndpoint = this.serviceHost.AddServiceEndpoint(typeof(IRelayedConnectionControl), innerBinding, uri);
			serviceEndpoint.Behaviors.Add(this.nameSettings);
			TransportClientEndpointBehavior transportClientEndpointBehavior = new TransportClientEndpointBehavior(this.tokenProvider);
			serviceEndpoint.Behaviors.Add(transportClientEndpointBehavior);
			serviceEndpoint.Behaviors.Add(this.innerConnectionStatus);
			if (this.connectivitySettings != null || this.httpConnectivitySettings != null)
			{
				ConnectivitySettingsEndpointBehavior connectivitySettingsEndpointBehavior = new ConnectivitySettingsEndpointBehavior(this.connectivitySettings, this.httpConnectivitySettings);
				serviceEndpoint.Behaviors.Add(connectivitySettingsEndpointBehavior);
			}
			ServiceErrorHandlerBehavior serviceErrorHandlerBehavior = new ServiceErrorHandlerBehavior();
			serviceErrorHandlerBehavior.HandleError += new EventHandler<ServiceErrorEventArgs>((object s, ServiceErrorEventArgs e) => Fx.Exception.TraceHandled(e.Exception, "WebStreamRelayedConnectionListener.IErrorHandler.HandleError", null));
			this.serviceHost.Description.Behaviors.Add(serviceErrorHandlerBehavior);
			this.messageHelper = new SocketMessageHelper();
		}

		public override void Open(TimeSpan timeout)
		{
			this.serviceHost.Open(timeout);
		}

		private class ConnectAsyncResult : IteratorAsyncResult<WebStreamRelayedConnectionListener.ConnectAsyncResult>
		{
			private readonly WebStreamRelayedConnectionListener relayedConnectionListener;

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

			public ConnectAsyncResult(RelayedConnectMessage request, WebStreamRelayedConnectionListener relayedConnectionListener, EventTraceActivity activity, AsyncCallback callback, object state) : base(ConnectConstants.ConnectionInitiateTimeout, callback, state)
			{
				this.Request = request;
				this.activity = activity;
				this.relayedConnectionListener = relayedConnectionListener;
			}

			protected override IEnumerator<IteratorAsyncResult<WebStreamRelayedConnectionListener.ConnectAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				Uri uri;
				uri = (!this.relayedConnectionListener.useHttpsMode ? ServiceBusUriHelper.CreateServiceUri(Uri.UriSchemeHttp, this.Request.HttpEndpoint.ToString(), "/") : ServiceBusUriHelper.CreateServiceUri(Uri.UriSchemeHttps, this.Request.HttpsEndpoint.ToString(), "/"));
				WebStream webStream = (new WebStream(uri, "connection", this.relayedConnectionListener.useHttpsMode, this.Activity, this.relayedConnectionListener.uri)).Open();
				this.connection = new WebStreamConnection(uri, this.relayedConnectionListener.bufferSize, this.Activity, webStream, this.relayedConnectionListener.uri);
				WebStreamRelayedConnectionListener.ConnectAsyncResult connectAsyncResult = this;
				IteratorAsyncResult<WebStreamRelayedConnectionListener.ConnectAsyncResult>.BeginCall beginCall = (WebStreamRelayedConnectionListener.ConnectAsyncResult thisRef, TimeSpan t, AsyncCallback c, object s) => SecureSocketUtil.BeginInitiateSecureClientUpgradeIfNeeded(thisRef.connection, null, thisRef.relayedConnectionListener.socketSecurityMode, thisRef.relayedConnectionListener.uri.Host, t, c, s);
				yield return connectAsyncResult.CallAsync(beginCall, (WebStreamRelayedConnectionListener.ConnectAsyncResult thisRef, IAsyncResult r) => thisRef.connection = SecureSocketUtil.EndInitiateSecureClientUpgradeIfNeeded(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				this.message = Message.CreateMessage(this.relayedConnectionListener.messageHelper.MessageVersion, "RelayedAccept", new AcceptMessage(this.Request.Id));
				this.message.Headers.To = EndpointAddress.AnonymousUri;
				WebStreamRelayedConnectionListener.ConnectAsyncResult connectAsyncResult1 = this;
				IteratorAsyncResult<WebStreamRelayedConnectionListener.ConnectAsyncResult>.BeginCall beginCall1 = (WebStreamRelayedConnectionListener.ConnectAsyncResult thisRef, TimeSpan t, AsyncCallback c, object s) => thisRef.relayedConnectionListener.messageHelper.BeginSendMessage(thisRef.connection, thisRef.message, t, c, s);
				yield return connectAsyncResult1.CallAsync(beginCall1, (WebStreamRelayedConnectionListener.ConnectAsyncResult thisRef, IAsyncResult r) => thisRef.relayedConnectionListener.messageHelper.EndSendMessage(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				IOThreadScheduler.ScheduleCallbackNoFlow(new Action<object>(this.relayedConnectionListener.BeginConnectCallback), this.connection);
			}
		}
	}
}