using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using Microsoft.ServiceBus.Messaging.Amqp.Transport;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.ServiceModel;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal sealed class AmqpTransportManager : TransportManager, ITransportManagerRegistration, IRuntimeProvider, IConnectionFactory, ISessionFactory, ILinkFactory
	{
		internal readonly static UriPrefixTable<ITransportManagerRegistration> TransportManagerTable;

		private readonly static AsyncCallback sessionOpenCallback;

		private readonly UriPrefixTable<AmqpChannelListenerBase> addressTable;

		private readonly AmqpSettings amqpSettings;

		private readonly AmqpConnectionSettings amqpConnectionSettings;

		private readonly string id;

		private AmqpTransportListener amqpTransportListener;

		private bool IsOpen
		{
			get;
			set;
		}

		public Uri ListenUri
		{
			get
			{
				return JustDecompileGenerated_get_ListenUri();
			}
			set
			{
				JustDecompileGenerated_set_ListenUri(value);
			}
		}

		private Uri JustDecompileGenerated_ListenUri_k__BackingField;

		public Uri JustDecompileGenerated_get_ListenUri()
		{
			return this.JustDecompileGenerated_ListenUri_k__BackingField;
		}

		private void JustDecompileGenerated_set_ListenUri(Uri value)
		{
			this.JustDecompileGenerated_ListenUri_k__BackingField = value;
		}

		System.ServiceModel.HostNameComparisonMode Microsoft.ServiceBus.Channels.ITransportManagerRegistration.HostNameComparisonMode
		{
			get
			{
				return System.ServiceModel.HostNameComparisonMode.StrongWildcard;
			}
		}

		public TimeSpan OpenTimeout
		{
			get;
			private set;
		}

		internal override string Scheme
		{
			get
			{
				return "amqp";
			}
		}

		static AmqpTransportManager()
		{
			AmqpTransportManager.TransportManagerTable = new UriPrefixTable<ITransportManagerRegistration>(true);
			AmqpTransportManager.sessionOpenCallback = new AsyncCallback(AmqpTransportManager.SessionOpenCallback);
		}

		public AmqpTransportManager(Uri listenUri, AmqpSettings amqpSettings, TimeSpan openTimeout)
		{
			this.ListenUri = new Uri(listenUri.GetLeftPart(UriPartial.Authority));
			this.addressTable = new UriPrefixTable<AmqpChannelListenerBase>(false);
			this.OpenTimeout = openTimeout;
			this.amqpSettings = amqpSettings.Clone();
			this.amqpSettings.RuntimeProvider = this;
			this.amqpConnectionSettings = new AmqpConnectionSettings()
			{
				ContainerId = Guid.NewGuid().ToString("N")
			};
			this.id = string.Concat(base.GetType().Name, this.GetHashCode());
			MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteAmqpLogOperation(this, TraceOperation.Create, this.ListenUri));
		}

		public IAsyncResult BeginOpenLink(AmqpLink link, TimeSpan timeout, AsyncCallback callback, object state)
		{
			Uri uri;
			AmqpChannelListenerBase amqpChannelListenerBase;
			string str = ((Target)link.Settings.Target).Address.ToString();
			if (!Uri.TryCreate(str, UriKind.Absolute, out uri))
			{
				uri = (new UriBuilder("amqp", Environment.MachineName, -1, str)).Uri;
			}
			if (!this.addressTable.TryLookupUri(uri, System.ServiceModel.HostNameComparisonMode.StrongWildcard, out amqpChannelListenerBase))
			{
				throw Fx.Exception.AsWarning(new AmqpException(AmqpError.NotFound, string.Concat("No link with name '", link.Name, "' could be found.")), null);
			}
			return new AmqpTransportManager.OpenLinkAsyncResult(amqpChannelListenerBase, link, timeout, callback, state);
		}

		public AmqpConnection CreateConnection(TransportBase transport, ProtocolHeader protocolHeader, bool isInitiator, AmqpSettings amqpSettings, AmqpConnectionSettings connectionSettings)
		{
			AmqpConnection amqpConnection = new AmqpConnection(transport, protocolHeader, isInitiator, amqpSettings, connectionSettings)
			{
				SessionFactory = this
			};
			MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteAmqpLogOperation(this, TraceOperation.Connect, amqpConnection));
			return amqpConnection;
		}

		public AmqpLink CreateLink(AmqpSession session, AmqpLinkSettings settings)
		{
			return new ReceivingAmqpLink(session, settings);
		}

		public AmqpSession CreateSession(AmqpConnection connection, AmqpSessionSettings settings)
		{
			settings.DispositionInterval = TimeSpan.Zero;
			AmqpSession amqpSession = new AmqpSession(connection, settings, this);
			MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteAmqpLogOperation(this, TraceOperation.Create, amqpSession));
			amqpSession.BeginOpen(this.OpenTimeout, AmqpTransportManager.sessionOpenCallback, amqpSession);
			return amqpSession;
		}

		private AmqpTransportListener CreateTransportListener()
		{
			TransportListener transportListener;
			TcpTransportSettings tcpTransportSetting = new TcpTransportSettings();
			int port = this.ListenUri.Port;
			if (port <= 0)
			{
				port = 5672;
			}
			tcpTransportSetting.Host = this.ListenUri.Host;
			tcpTransportSetting.Port = port;
			if (!this.ListenUri.Scheme.Equals("amqps", StringComparison.OrdinalIgnoreCase))
			{
				transportListener = tcpTransportSetting.CreateListener();
			}
			else
			{
				TlsTransportProvider transportProvider = this.amqpSettings.GetTransportProvider<TlsTransportProvider>();
				if (transportProvider == null)
				{
					throw Fx.Exception.Argument("TlsSecurityProvider", "Tls provider must be set.");
				}
				TlsTransportSettings tlsTransportSetting = new TlsTransportSettings(tcpTransportSetting, false)
				{
					Certificate = transportProvider.Settings.Certificate
				};
				transportListener = tlsTransportSetting.CreateListener();
			}
			return new AmqpTransportListener(new TransportListener[] { transportListener }, this.amqpSettings);
		}

		public void EndOpenLink(IAsyncResult result)
		{
			AsyncResult<AmqpTransportManager.OpenLinkAsyncResult>.End(result);
		}

		private bool IsCompatible(TransportChannelListener channelListener)
		{
			return true;
		}

		IList<TransportManager> Microsoft.ServiceBus.Channels.ITransportManagerRegistration.Select(TransportChannelListener channelListener)
		{
			IList<TransportManager> transportManagers = null;
			if (this.IsCompatible(channelListener))
			{
				transportManagers = new List<TransportManager>()
				{
					this
				};
			}
			return transportManagers;
		}

		private void OnAcceptTransport(TransportAsyncCallbackArgs args)
		{
			AmqpConnectionSettings amqpConnectionSetting = this.amqpConnectionSettings;
			AmqpConnection amqpConnection = null;
			try
			{
				amqpConnection = this.CreateConnection(args.Transport, (ProtocolHeader)args.UserToken, false, this.amqpSettings, amqpConnectionSetting);
				(new AmqpTransportManager.ConnectionHandler(amqpConnection, this.OpenTimeout)).Start();
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				Fx.Exception.TraceHandled(exception, "AmqpTransportManager.OnAcceptTransport", null);
				if (amqpConnection != null)
				{
					amqpConnection.SafeClose(exception);
				}
			}
		}

		private void OnAmqpTransportListenerClosed(object sender, EventArgs args)
		{
			if (this.IsOpen)
			{
				Fx.AssertAndFailFastService("AmqpTransportListener shutdown unexpectedly.");
			}
		}

		internal override void OnClose(TimeSpan timeout)
		{
			this.IsOpen = false;
			MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteAmqpLogOperation(this, TraceOperation.Close, this.ListenUri));
			AmqpTransportListener amqpTransportListener = this.amqpTransportListener;
			if (amqpTransportListener != null)
			{
				amqpTransportListener.Closed -= new EventHandler(this.OnAmqpTransportListenerClosed);
				amqpTransportListener.Close(timeout);
			}
		}

		internal override void OnOpen(TimeSpan timeout)
		{
			MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteAmqpLogOperation(this, TraceOperation.Open, this.ListenUri));
			this.amqpTransportListener = this.CreateTransportListener();
			this.amqpTransportListener.Closed += new EventHandler(this.OnAmqpTransportListenerClosed);
			this.amqpTransportListener.Open(timeout);
			this.amqpTransportListener.Listen(new Action<TransportAsyncCallbackArgs>(this.OnAcceptTransport));
			this.IsOpen = true;
		}

		internal override void Register(TimeSpan timeout, TransportChannelListener channelListener)
		{
			this.addressTable.RegisterUri(channelListener.Uri, channelListener.HostNameComparisonModeInternal, (AmqpChannelListenerBase)channelListener);
		}

		private static void SessionOpenCallback(IAsyncResult asyncResult)
		{
			AmqpSession asyncState = (AmqpSession)asyncResult.AsyncState;
			try
			{
				asyncState.EndOpen(asyncResult);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				Fx.Exception.TraceHandled(exception, "AmqpTransportManager.SessionOpenCallback", null);
				asyncState.SafeClose(exception);
			}
		}

		public override string ToString()
		{
			return this.id;
		}

		internal override void Unregister(TimeSpan timeout, TransportChannelListener channelListener)
		{
			TransportManager.EnsureRegistered<AmqpChannelListenerBase>(this.addressTable, (AmqpChannelListenerBase)channelListener);
			this.addressTable.UnregisterUri(channelListener.Uri, channelListener.HostNameComparisonModeInternal);
		}

		private sealed class ConnectionHandler
		{
			private readonly static AsyncCallback openCallback;

			private readonly AmqpConnection connection;

			private readonly TimeSpan openTimeout;

			static ConnectionHandler()
			{
				AmqpTransportManager.ConnectionHandler.openCallback = new AsyncCallback(AmqpTransportManager.ConnectionHandler.OpenCallback);
			}

			public ConnectionHandler(AmqpConnection connection, TimeSpan openTimeout)
			{
				this.connection = connection;
				this.openTimeout = openTimeout;
			}

			private static void OpenCallback(IAsyncResult asyncResult)
			{
				AmqpTransportManager.ConnectionHandler asyncState = (AmqpTransportManager.ConnectionHandler)asyncResult.AsyncState;
				try
				{
					asyncState.OpenComplete(asyncResult);
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					Fx.Exception.TraceHandled(exception, "AmqpTransportManager.ConnectionHandler.OpenCallback", null);
					asyncState.connection.SafeClose(exception);
				}
			}

			private void OpenComplete(IAsyncResult asyncResult)
			{
				this.connection.EndOpen(asyncResult);
			}

			public void Start()
			{
				try
				{
					IAsyncResult asyncResult = this.connection.BeginOpen(this.openTimeout, AmqpTransportManager.ConnectionHandler.openCallback, this);
					if (asyncResult.CompletedSynchronously)
					{
						this.OpenComplete(asyncResult);
					}
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					Fx.Exception.TraceHandled(exception, "AmqpTransportManager.ConnectionHandler.Start", null);
					this.connection.SafeClose(exception);
				}
			}
		}

		private sealed class OpenLinkAsyncResult : AsyncResult<AmqpTransportManager.OpenLinkAsyncResult>
		{
			private readonly static AsyncResult.AsyncCompletion openLinkComplete;

			private readonly ILinkFactory linkFactory;

			static OpenLinkAsyncResult()
			{
				AmqpTransportManager.OpenLinkAsyncResult.openLinkComplete = new AsyncResult.AsyncCompletion(AmqpTransportManager.OpenLinkAsyncResult.OpenLinkComplete);
			}

			public OpenLinkAsyncResult(ILinkFactory linkFactory, AmqpLink link, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				this.linkFactory = linkFactory;
				if (base.SyncContinue(this.linkFactory.BeginOpenLink(link, timeout, base.PrepareAsyncCompletion(AmqpTransportManager.OpenLinkAsyncResult.openLinkComplete), this)))
				{
					base.Complete(true);
				}
			}

			private static bool OpenLinkComplete(IAsyncResult result)
			{
				((AmqpTransportManager.OpenLinkAsyncResult)result.AsyncState).linkFactory.EndOpenLink(result);
				return true;
			}
		}
	}
}