using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Common.Parallel;
using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using Microsoft.ServiceBus.Messaging.Amqp.Sasl;
using Microsoft.ServiceBus.Messaging.Amqp.Transport;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus
{
	internal sealed class AmqpRelay : IConnectionStatus
	{
		private readonly EventHandler onAmqpObjectClosed;

		private readonly Uri serviceBusUri;

		private readonly AmqpRelay.TokenRenewer tokenRenewer;

		private readonly ConnectivitySettings connectivitySettings;

		private readonly HttpConnectivitySettings httpConnectivitySettings;

		private readonly EventTraceActivity activity;

		private AmqpConnection connection;

		private DuplexAmqpLink link;

		private Action<DuplexAmqpLink, AmqpMessage> messageListener;

		public string ClientAgent
		{
			get;
			set;
		}

		public string DisplayName
		{
			get;
			set;
		}

		public bool IsDynamic
		{
			get;
			set;
		}

		public bool IsOnline
		{
			get
			{
				return JustDecompileGenerated_get_IsOnline();
			}
			set
			{
				JustDecompileGenerated_set_IsOnline(value);
			}
		}

		private bool JustDecompileGenerated_IsOnline_k__BackingField;

		public bool JustDecompileGenerated_get_IsOnline()
		{
			return this.JustDecompileGenerated_IsOnline_k__BackingField;
		}

		private void JustDecompileGenerated_set_IsOnline(bool value)
		{
			this.JustDecompileGenerated_IsOnline_k__BackingField = value;
		}

		public Exception LastError
		{
			get
			{
				return JustDecompileGenerated_get_LastError();
			}
			set
			{
				JustDecompileGenerated_set_LastError(value);
			}
		}

		private Exception JustDecompileGenerated_LastError_k__BackingField;

		public Exception JustDecompileGenerated_get_LastError()
		{
			return this.JustDecompileGenerated_LastError_k__BackingField;
		}

		private void JustDecompileGenerated_set_LastError(Exception value)
		{
			this.JustDecompileGenerated_LastError_k__BackingField = value;
		}

		public Microsoft.ServiceBus.ListenerType ListenerType
		{
			get;
			set;
		}

		public bool PublishToRegistry
		{
			get;
			set;
		}

		public bool RelayClientAuthorizationRequired
		{
			get;
			set;
		}

		public AmqpObjectState State
		{
			get;
			private set;
		}

		private object ThisLock
		{
			get;
			set;
		}

		public bool TransportSecurityRequired
		{
			get;
			set;
		}

		public AmqpRelay(Uri serviceBusUri, TokenProvider tokenProvider, ConnectivitySettings connectivitySettings = null, HttpConnectivitySettings httpConnectivitySettings = null)
		{
			this.serviceBusUri = serviceBusUri;
			if (tokenProvider != null)
			{
				this.tokenRenewer = new AmqpRelay.TokenRenewer(tokenProvider, this.serviceBusUri.AbsoluteUri, "Listen");
			}
			this.RelayClientAuthorizationRequired = true;
			this.IsDynamic = true;
			this.State = AmqpObjectState.Start;
			this.ListenerType = Microsoft.ServiceBus.ListenerType.RoutedHttp;
			this.onAmqpObjectClosed = new EventHandler(this.OnAmqpObjectClosed);
			this.ThisLock = new object();
			this.connectivitySettings = connectivitySettings;
			this.httpConnectivitySettings = httpConnectivitySettings;
			this.activity = new EventTraceActivity();
		}

		public void Abort()
		{
			lock (this.ThisLock)
			{
				switch (this.State)
				{
					case AmqpObjectState.CloseReceived:
					case AmqpObjectState.End:
					{
						return;
					}
				}
				this.State = AmqpObjectState.CloseReceived;
			}
			(new AmqpRelay.CloseAbortTask(this, true, TimeSpan.Zero)).Start().Fork();
		}

		public Task CloseAsync(TimeSpan timeout)
		{
			Task @default;
			lock (this.ThisLock)
			{
				switch (this.State)
				{
					case AmqpObjectState.OpenReceived:
					case AmqpObjectState.Opened:
					{
						this.State = AmqpObjectState.CloseReceived;
						return (new AmqpRelay.CloseAbortTask(this, false, timeout)).Start();
					}
					case AmqpObjectState.ClosePipe:
					case AmqpObjectState.CloseSent:
					{
						throw new InvalidOperationException();
					}
					case AmqpObjectState.CloseReceived:
					case AmqpObjectState.End:
					{
						@default = CompletedTask.Default;
						break;
					}
					default:
					{
						throw new InvalidOperationException();
					}
				}
			}
			return @default;
		}

		private void OnAmqpObjectClosed(object sender, EventArgs args)
		{
			AmqpObject amqpObject = (AmqpObject)sender;
			amqpObject.Closed -= this.onAmqpObjectClosed;
			Exception terminalException = amqpObject.TerminalException;
			if (terminalException == null)
			{
				terminalException = new ConnectionLostException("The connection to the connect service was lost.");
			}
			this.OnDisconnected(ExceptionHelper.ToRelayContract(terminalException));
		}

		private void OnDisconnected(Exception lastError)
		{
			lock (this.ThisLock)
			{
				if (lastError != null)
				{
					this.LastError = lastError;
				}
				this.IsOnline = false;
			}
			if (lastError is RelayNotFoundException || lastError is AddressAlreadyInUseException || this.State != AmqpObjectState.OpenReceived && this.State != AmqpObjectState.Opened)
			{
				MessagingClientEtwProvider.Provider.RelayClientStopConnecting(this.activity, this.serviceBusUri.AbsoluteUri, this.ListenerType.ToString());
				if (this.tokenRenewer != null)
				{
					this.tokenRenewer.Close();
				}
				EventHandler eventHandler = this.Offline;
				if (eventHandler != null)
				{
					eventHandler(this, EventArgs.Empty);
					return;
				}
			}
			else
			{
				MessagingClientEtwProvider.Provider.RelayClientReconnecting(this.activity, this.serviceBusUri.AbsoluteUri, this.ListenerType.ToString());
				EventHandler eventHandler1 = this.Connecting;
				if (eventHandler1 != null)
				{
					eventHandler1(this, EventArgs.Empty);
				}
				(new AmqpRelay.ConnectTask(this, TimeSpan.FromSeconds(120))).Start().Fork();
			}
		}

		private void OnOnline()
		{
			MessagingClientEtwProvider.Provider.RelayClientGoingOnline(this.activity, this.serviceBusUri.AbsoluteUri);
			lock (this.ThisLock)
			{
				if (!this.IsOnline)
				{
					this.LastError = null;
					this.IsOnline = true;
				}
				else
				{
					return;
				}
			}
			EventHandler eventHandler = this.Online;
			if (eventHandler != null)
			{
				eventHandler(this, EventArgs.Empty);
			}
		}

		private void OnTokenRenewed(object sender, AmqpRelay.TokenEventArgs args)
		{
			DuplexAmqpLink duplexAmqpLink = this.link;
			if (duplexAmqpLink != null)
			{
				Fields field = new Fields()
				{
					{ AmqpConstants.SimpleWebTokenPropertyName, args.Token.Token }
				};
				duplexAmqpLink.SendProperties(field);
			}
		}

		public Task OpenAsync(TimeSpan openTimeout)
		{
			lock (this.ThisLock)
			{
				if (this.State != AmqpObjectState.Start)
				{
					throw new InvalidOperationException();
				}
				this.State = AmqpObjectState.OpenReceived;
			}
			return (new AmqpRelay.OpenTask(this, openTimeout)).Start();
		}

		public void RegisterMessageListener(Action<DuplexAmqpLink, AmqpMessage> newListener)
		{
			if (Interlocked.Exchange<Action<DuplexAmqpLink, AmqpMessage>>(ref this.messageListener, newListener) != null)
			{
				throw new InvalidOperationException(SRClient.MessageListenerAlreadyRegistered);
			}
		}

		public event EventHandler Connecting;

		public event EventHandler Offline;

		public event EventHandler Online;

		private sealed class CloseAbortTask : IteratorTask
		{
			private readonly AmqpRelay relay;

			private readonly bool aborting;

			private readonly TimeSpan timeout;

			public CloseAbortTask(AmqpRelay relay, bool aborting, TimeSpan timeout)
			{
				this.relay = relay;
				this.aborting = aborting;
				this.timeout = timeout;
			}

			protected override IEnumerator<IteratorTask<object>.TaskStep> GetTasks()
			{
				if (this.relay.tokenRenewer != null)
				{
					this.relay.tokenRenewer.Close();
				}
				AmqpConnection amqpConnection = this.relay.connection;
				if (amqpConnection != null)
				{
					if (this.aborting)
					{
						amqpConnection.SafeClose();
					}
					else
					{
						yield return base.CallTask(amqpConnection.CloseAsync(this.timeout), IteratorTask<TResult>.ExceptionPolicy.Transfer);
					}
				}
				this.relay.State = AmqpObjectState.End;
			}
		}

		private sealed class ConnectTask : IteratorTask
		{
			private readonly static Action<IteratorTask<object>, Exception> onException;

			private readonly AmqpRelay relay;

			private readonly TimeSpan timeout;

			static ConnectTask()
			{
				AmqpRelay.ConnectTask.onException = new Action<IteratorTask<object>, Exception>(AmqpRelay.ConnectTask.OnException);
			}

			public ConnectTask(AmqpRelay relay, TimeSpan timeout)
			{
				this.relay = relay;
				this.timeout = timeout;
				this.OnSetException += AmqpRelay.ConnectTask.onException;
			}

			private static AmqpSettings CreateAmqpSettings()
			{
				AmqpSettings amqpSetting = new AmqpSettings();
				SaslTransportProvider saslTransportProvider = new SaslTransportProvider();
				saslTransportProvider.Versions.Add(AmqpConstants.DefaultProtocolVersion);
				saslTransportProvider.AddHandler(new SaslExternalHandler());
				amqpSetting.TransportProviders.Add(saslTransportProvider);
				AmqpTransportProvider amqpTransportProvider = new AmqpTransportProvider();
				amqpTransportProvider.Versions.Add(AmqpConstants.DefaultProtocolVersion);
				amqpSetting.TransportProviders.Add(amqpTransportProvider);
				return amqpSetting;
			}

			protected override IEnumerator<IteratorTask<object>.TaskStep> GetTasks()
			{
				ConnectivityMode connectivityMode;
				object obj = null;
				bool flag = false;
				try
				{
					object thisLock = this.relay.ThisLock;
					object obj1 = thisLock;
					obj = thisLock;
					Monitor.Enter(obj1, ref flag);
					if (this.relay.State != AmqpObjectState.OpenReceived && this.relay.State != AmqpObjectState.Opened)
					{
						goto Label0;
					}
				}
				finally
				{
					if (flag)
					{
						Monitor.Exit(obj);
					}
				}
				Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(this.timeout);
				string host = this.relay.serviceBusUri.Host;
				AmqpSettings amqpSetting = AmqpRelay.ConnectTask.CreateAmqpSettings();
				connectivityMode = (this.relay.connectivitySettings != null ? this.relay.connectivitySettings.Mode : ServiceBusEnvironment.SystemConnectivity.Mode);
				ConnectivityMode connectivityMode1 = connectivityMode;
				if (connectivityMode1 == ConnectivityMode.Tcp)
				{
					TcpTransportSettings tcpTransportSetting = new TcpTransportSettings()
					{
						Host = host,
						Port = 5671
					};
					TlsTransportSettings tlsTransportSetting = new TlsTransportSettings(tcpTransportSetting)
					{
						TargetHost = host
					};
					AmqpTransportInitiator amqpTransportInitiator = new AmqpTransportInitiator(amqpSetting, tlsTransportSetting);
					yield return base.CallTask(amqpTransportInitiator.ConnectTaskAsync(timeoutHelper.RemainingTime()), IteratorTask<TResult>.ExceptionPolicy.Transfer);
				}
				else if (connectivityMode1 == ConnectivityMode.Http || this.relay.httpConnectivitySettings != null)
				{
					WebSocketTransportSettings webSocketTransportSetting = new WebSocketTransportSettings(this.relay.serviceBusUri);
					AmqpTransportInitiator amqpTransportInitiator1 = new AmqpTransportInitiator(amqpSetting, webSocketTransportSetting);
					yield return base.CallTask(amqpTransportInitiator1.ConnectTaskAsync(timeoutHelper.RemainingTime()), IteratorTask<TResult>.ExceptionPolicy.Transfer);
				}
				else
				{
					TcpTransportSettings tcpTransportSetting1 = new TcpTransportSettings()
					{
						Host = host,
						Port = 5671
					};
					TlsTransportSettings tlsTransportSetting1 = new TlsTransportSettings(tcpTransportSetting1)
					{
						TargetHost = host
					};
					AmqpTransportInitiator amqpTransportInitiator2 = new AmqpTransportInitiator(amqpSetting, tlsTransportSetting1);
					yield return base.CallTask(amqpTransportInitiator2.ConnectTaskAsync(Microsoft.ServiceBus.Common.TimeoutHelper.Divide(timeoutHelper.RemainingTime(), 2)), IteratorTask<TResult>.ExceptionPolicy.Continue);
					if (base.LastTask.Exception != null)
					{
						if (timeoutHelper.RemainingTime() == TimeSpan.Zero)
						{
							throw base.LastTask.Exception;
						}
						WebSocketTransportSettings webSocketTransportSetting1 = new WebSocketTransportSettings(this.relay.serviceBusUri);
						AmqpTransportInitiator amqpTransportInitiator3 = new AmqpTransportInitiator(amqpSetting, webSocketTransportSetting1);
						yield return base.CallTask(amqpTransportInitiator3.ConnectTaskAsync(timeoutHelper.RemainingTime()), IteratorTask<TResult>.ExceptionPolicy.Transfer);
					}
				}
				TransportBase transportBase = base.LastTaskResult<TransportBase>();
				string[] strArrays = host.Split(new char[] { '.' });
				strArrays[0] = string.Concat(strArrays[0], "-relay");
				AmqpConnectionSettings amqpConnectionSetting = new AmqpConnectionSettings()
				{
					ContainerId = Guid.NewGuid().ToString(),
					HostName = string.Join(".", strArrays)
				};
				this.relay.connection = new AmqpConnection(transportBase, amqpSetting, amqpConnectionSetting);
				yield return base.CallTask(this.relay.connection.OpenAsync(timeoutHelper.RemainingTime()), IteratorTask<TResult>.ExceptionPolicy.Transfer);
				AmqpSessionSettings amqpSessionSetting = new AmqpSessionSettings();
				AmqpSession amqpSession = this.relay.connection.CreateSession(amqpSessionSetting);
				yield return base.CallTask(amqpSession.OpenAsync(timeoutHelper.RemainingTime()), IteratorTask<TResult>.ExceptionPolicy.Transfer);
				AmqpLinkSettings amqpLinkSetting = new AmqpLinkSettings()
				{
					Role = new bool?(false),
					InitialDeliveryCount = new uint?(0),
					LinkName = string.Concat("HttpRelayServer_Link_", Guid.NewGuid()),
					Target = new Target(this.relay.serviceBusUri),
					Source = new Source(this.relay.serviceBusUri),
					TotalLinkCredit = 1000,
					AutoSendFlow = true
				};
				AmqpLinkSettings amqpLinkSetting1 = amqpLinkSetting;
				if (this.relay.tokenRenewer != null)
				{
					amqpLinkSetting1.AddProperty(AmqpConstants.SimpleWebTokenPropertyName, this.relay.tokenRenewer.CurrentToken.Token);
				}
				if (!this.relay.TransportSecurityRequired)
				{
					amqpLinkSetting1.AddProperty(ClientConstants.TransportSecurityRequiredName, false);
				}
				if (!this.relay.RelayClientAuthorizationRequired)
				{
					amqpLinkSetting1.AddProperty(ClientConstants.ClientAuthenticationRequiredName, false);
				}
				if (this.relay.PublishToRegistry)
				{
					amqpLinkSetting1.AddProperty(ClientConstants.RequiresPublicRegistry, true);
				}
				if (!string.IsNullOrEmpty(this.relay.ClientAgent))
				{
					amqpLinkSetting1.AddProperty(ClientConstants.ClientAgent, this.relay.ClientAgent);
				}
				if (!string.IsNullOrEmpty(this.relay.DisplayName))
				{
					amqpLinkSetting1.AddProperty(ClientConstants.DisplayName, this.relay.DisplayName);
				}
				amqpLinkSetting1.AddProperty(ClientConstants.DynamicRelay, this.relay.IsDynamic);
				amqpLinkSetting1.AddProperty(ClientConstants.ListenerTypeName, this.relay.ListenerType.ToString());
				this.relay.link = new DuplexAmqpLink(amqpSession, amqpLinkSetting1);
				yield return base.CallTask(this.relay.link.OpenAsync(timeoutHelper.RemainingTime()), IteratorTask<TResult>.ExceptionPolicy.Transfer);
				this.relay.link.SafeAddClosed(this.relay.onAmqpObjectClosed);
				this.relay.link.RegisterMessageListener((AmqpMessage msg) => this.relay.messageListener(this.relay.link, msg));
				this.relay.OnOnline();
			Label0:
				yield break;
			}

			private static void OnException(IteratorTask<object> iterator, Exception exception)
			{
				AmqpRelay.ConnectTask connectTask = (AmqpRelay.ConnectTask)iterator;
				exception = ExceptionHelper.ToRelayContract(exception);
				connectTask.relay.OnDisconnected(exception);
				throw Fx.Exception.AsWarning(exception, connectTask.relay.activity);
			}
		}

		private sealed class OpenTask : IteratorTask
		{
			private readonly AmqpRelay relay;

			private readonly TimeSpan timeout;

			public OpenTask(AmqpRelay relay, TimeSpan timeout)
			{
				this.relay = relay;
				this.timeout = timeout;
			}

			protected override IEnumerator<IteratorTask<object>.TaskStep> GetTasks()
			{
				Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(this.timeout);
				if (this.relay.tokenRenewer != null)
				{
					yield return base.CallTask(this.relay.tokenRenewer.GetTokenAsync(timeoutHelper.RemainingTime()), IteratorTask<TResult>.ExceptionPolicy.Transfer);
					this.relay.tokenRenewer.TokenRenewed += new EventHandler<AmqpRelay.TokenEventArgs>(this.relay.OnTokenRenewed);
				}
				yield return base.CallTask((new AmqpRelay.ConnectTask(this.relay, timeoutHelper.RemainingTime())).Start(), IteratorTask<TResult>.ExceptionPolicy.Transfer);
				this.relay.State = AmqpObjectState.Opened;
			}
		}

		private class TokenEventArgs : EventArgs
		{
			public SimpleWebSecurityToken Token
			{
				get;
				internal set;
			}

			public TokenEventArgs()
			{
			}
		}

		private class TokenRenewer
		{
			private readonly static TimeSpan GetTokenTimeout;

			private readonly static TimeSpan TokenRefreshBuffer;

			private readonly static Action<object> onRenewToken;

			private readonly IOThreadTimer renewTimer;

			private readonly TokenProvider tokenProvider;

			private readonly string action;

			private readonly string appliesTo;

			private readonly object syncRoot;

			private volatile SimpleWebSecurityToken currentToken;

			public SimpleWebSecurityToken CurrentToken
			{
				get
				{
					return this.currentToken;
				}
				private set
				{
					lock (this.syncRoot)
					{
						this.currentToken = value;
						this.ScheduleRenewTimer();
					}
					this.OnTokenRenewed();
				}
			}

			static TokenRenewer()
			{
				AmqpRelay.TokenRenewer.GetTokenTimeout = TimeSpan.FromMinutes(5);
				AmqpRelay.TokenRenewer.TokenRefreshBuffer = TimeSpan.FromSeconds(10);
				AmqpRelay.TokenRenewer.onRenewToken = new Action<object>(AmqpRelay.TokenRenewer.OnRenewToken);
			}

			public TokenRenewer(TokenProvider tokenProvider, string appliesTo, string action)
			{
				this.tokenProvider = tokenProvider;
				this.appliesTo = appliesTo;
				this.action = action;
				this.renewTimer = new IOThreadTimer(AmqpRelay.TokenRenewer.onRenewToken, this, false);
				this.syncRoot = new object();
			}

			private void CancelRenewTimer()
			{
				this.renewTimer.Cancel();
			}

			public void Close()
			{
				this.CancelRenewTimer();
			}

			public Task GetTokenAsync(TimeSpan timeout)
			{
				return (new AmqpRelay.TokenRenewer.GetTokenTask(this, timeout)).Start();
			}

			private static void OnRenewToken(object state)
			{
				AmqpRelay.TokenRenewer tokenRenewer = (AmqpRelay.TokenRenewer)state;
				try
				{
					tokenRenewer.GetTokenAsync(AmqpRelay.TokenRenewer.GetTokenTimeout).Fork();
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					Fx.Exception.TraceHandled(exception, "TokenRenewer.OnRenewToken", null);
				}
			}

			private void OnTokenRenewed()
			{
				EventHandler<AmqpRelay.TokenEventArgs> eventHandler = this.TokenRenewed;
				if (eventHandler != null)
				{
					eventHandler(this, new AmqpRelay.TokenEventArgs()
					{
						Token = this.CurrentToken
					});
				}
			}

			private void ScheduleRenewTimer()
			{
				TimeSpan tokenRefreshBuffer = this.currentToken.ValidTo.Subtract(DateTime.UtcNow);
				if (tokenRefreshBuffer < TimeSpan.Zero)
				{
					return;
				}
				tokenRefreshBuffer = tokenRefreshBuffer + AmqpRelay.TokenRenewer.TokenRefreshBuffer;
				tokenRefreshBuffer = (tokenRefreshBuffer < ClientConstants.ClientMinimumTokenRefreshInterval ? ClientConstants.ClientMinimumTokenRefreshInterval : tokenRefreshBuffer);
				this.renewTimer.Set(tokenRefreshBuffer);
			}

			public event EventHandler<AmqpRelay.TokenEventArgs> TokenRenewed;

			private class GetTokenTask : IteratorTask
			{
				private readonly AmqpRelay.TokenRenewer tokenRenewer;

				private readonly string appliesTo;

				private readonly string action;

				private readonly TimeSpan timeout;

				public GetTokenTask(AmqpRelay.TokenRenewer tokenRenewer, TimeSpan timeout)
				{
					this.tokenRenewer = tokenRenewer;
					this.appliesTo = tokenRenewer.appliesTo;
					this.action = tokenRenewer.action;
					this.timeout = timeout;
				}

				protected override IEnumerator<IteratorTask<object>.TaskStep> GetTasks()
				{
					yield return base.CallTask(this.tokenRenewer.tokenProvider.GetTokenAsync(this.appliesTo, this.action, false, this.timeout), IteratorTask<TResult>.ExceptionPolicy.Transfer);
					SimpleWebSecurityToken simpleWebSecurityToken = (SimpleWebSecurityToken)base.LastTaskResult<SecurityToken>();
					this.tokenRenewer.CurrentToken = simpleWebSecurityToken;
				}
			}
		}
	}
}