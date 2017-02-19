using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus
{
	internal class RelayedOnewayListener : RefcountedCommunicationObject, IRelayedOnewayListener, ICommunicationObject, IConnectionStatus
	{
		private readonly RelayedOnewayTcpClient client;

		private readonly Microsoft.ServiceBus.Channels.UriPrefixTable<RelayedOnewayListener.RelayedOnewayChannelListenerCollection> listenerTable;

		private readonly Microsoft.ServiceBus.NameSettings nameSettings;

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

		public bool IsOnline
		{
			get
			{
				return this.client.IsOnline;
			}
		}

		public Exception LastError
		{
			get
			{
				return this.client.LastError;
			}
		}

		public bool Multicast
		{
			get
			{
				return this.nameSettings.ServiceSettings.IsMulticastListener;
			}
		}

		public Microsoft.ServiceBus.NameSettings NameSettings
		{
			get
			{
				return this.nameSettings;
			}
		}

		public Microsoft.ServiceBus.TokenProvider TokenProvider
		{
			get
			{
				return this.client.TokenProvider;
			}
		}

		public System.Uri Uri
		{
			get
			{
				return this.client.Uri;
			}
		}

		public System.Uri Via
		{
			get
			{
				return this.client.Via;
			}
		}

		public RelayedOnewayListener(BindingContext context, RelayedOnewayTransportBindingElement transportBindingElement, System.Uri uri, EventTraceActivity activity)
		{
			this.nameSettings = context.BindingParameters.Find<Microsoft.ServiceBus.NameSettings>();
			if (this.nameSettings == null)
			{
				this.nameSettings = new Microsoft.ServiceBus.NameSettings();
				this.nameSettings.ServiceSettings.ListenerType = ListenerType.Unicast;
			}
			this.listenerTable = new Microsoft.ServiceBus.Channels.UriPrefixTable<RelayedOnewayListener.RelayedOnewayChannelListenerCollection>();
			this.client = new RelayedOnewayListener.RelayedOnewayAmqpListenerClient(context, transportBindingElement, uri, this, activity);
			this.client.Connecting += new EventHandler(this.OnConnecting);
			this.client.Online += new EventHandler(this.OnOnline);
			this.client.Offline += new EventHandler(this.OnOffline);
		}

		public RelayedOnewayChannelListener[] Lookup(System.Uri uri)
		{
			RelayedOnewayListener.RelayedOnewayChannelListenerCollection relayedOnewayChannelListenerCollection;
			RelayedOnewayChannelListener[] listeners;
			lock (base.ThisLock)
			{
				if (!this.listenerTable.TryLookupUri(uri, HostNameComparisonMode.Exact, out relayedOnewayChannelListenerCollection))
				{
					listeners = null;
				}
				else
				{
					listeners = relayedOnewayChannelListenerCollection.GetListeners();
				}
			}
			return listeners;
		}

		protected override void OnAbort()
		{
			this.client.Abort();
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.client.BeginClose(timeout, callback, state);
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.client.BeginOpen(timeout, callback, state);
		}

		protected override void OnClose(TimeSpan timeout)
		{
			this.client.Close(timeout);
		}

		private void OnConnecting(object sender, EventArgs args)
		{
			EventHandler eventHandler = this.Connecting;
			if (eventHandler != null)
			{
				eventHandler(this, EventArgs.Empty);
			}
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			this.client.EndClose(result);
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			this.client.EndOpen(result);
		}

		private void OnOffline(object sender, EventArgs args)
		{
			EventHandler eventHandler = this.Offline;
			if (eventHandler != null)
			{
				eventHandler(this, EventArgs.Empty);
			}
		}

		private void OnOnline(object sender, EventArgs args)
		{
			EventHandler eventHandler = this.Online;
			if (eventHandler != null)
			{
				eventHandler(this, EventArgs.Empty);
			}
		}

		protected override void OnOpen(TimeSpan timeout)
		{
			this.client.Open(timeout);
		}

		public void Register(RelayedOnewayChannelListener channelListener)
		{
			RelayedOnewayListener.RelayedOnewayChannelListenerCollection relayedOnewayChannelListenerCollection;
			lock (base.ThisLock)
			{
				if (!this.listenerTable.TryLookupUri(channelListener.Uri, HostNameComparisonMode.Exact, out relayedOnewayChannelListenerCollection) || !(relayedOnewayChannelListenerCollection.Uri == channelListener.Uri))
				{
					relayedOnewayChannelListenerCollection = new RelayedOnewayListener.RelayedOnewayChannelListenerCollection(channelListener.Uri, channelListener.NameSettings.ServiceSettings.ListenerType == ListenerType.Multicast);
					this.listenerTable.RegisterUri(channelListener.Uri, HostNameComparisonMode.Exact, relayedOnewayChannelListenerCollection);
				}
				else if (!relayedOnewayChannelListenerCollection.Multicast)
				{
					throw Fx.Exception.AsError(new AddressAlreadyInUseException(), null);
				}
				relayedOnewayChannelListenerCollection.AddListener(channelListener);
				base.AddRef();
			}
		}

		public void Unregister(RelayedOnewayChannelListener channelListener)
		{
			RelayedOnewayListener.RelayedOnewayChannelListenerCollection relayedOnewayChannelListenerCollection;
			lock (base.ThisLock)
			{
				if (this.listenerTable.TryLookupUri(channelListener.Uri, HostNameComparisonMode.Exact, out relayedOnewayChannelListenerCollection) && relayedOnewayChannelListenerCollection.Uri == channelListener.Uri)
				{
					relayedOnewayChannelListenerCollection.RemoveListener(channelListener);
					if (relayedOnewayChannelListenerCollection.Count == 0)
					{
						this.listenerTable.UnregisterUri(channelListener.Uri, HostNameComparisonMode.Exact);
					}
				}
			}
		}

		public event EventHandler Connecting;

		public event EventHandler Offline;

		public event EventHandler Online;

		private class RelayedOnewayAmqpListenerClient : RelayedOnewayTcpClient
		{
			private readonly RelayedOnewayListener listener;

			private readonly MessageWrapper messageWrapper;

			private readonly MessageEncoder encoder;

			private readonly BufferManager bufferManager;

			private readonly ConnectivitySettings connectivitySettings;

			private readonly HttpConnectivitySettings httpConnectivitySettings;

			public RelayedOnewayAmqpListenerClient(BindingContext context, RelayedOnewayTransportBindingElement transportBindingElement, System.Uri uri, RelayedOnewayListener listener, EventTraceActivity activity) : base(context, transportBindingElement, uri, true, activity)
			{
				this.listener = listener;
				BinaryMessageEncodingBindingElement binaryMessageEncodingBindingElement = ClientMessageUtility.CreateInnerEncodingBindingElement(context);
				this.encoder = binaryMessageEncodingBindingElement.CreateMessageEncoderFactory().Encoder;
				this.messageWrapper = new MessageWrapper(this.encoder);
				this.bufferManager = BufferManager.CreateBufferManager(transportBindingElement.MaxBufferPoolSize, transportBindingElement.MaxBufferSize);
				base.IsListener = true;
				this.connectivitySettings = context.BindingParameters.Find<ConnectivitySettings>();
				this.httpConnectivitySettings = context.BindingParameters.Find<HttpConnectivitySettings>();
			}

			protected override RelayedOnewayTcpClient.RelayedOnewayConnection Connect(TimeSpan timeout)
			{
				return this.GetOrCreateConnection(base.Via, timeout);
			}

			protected override RelayedOnewayTcpClient.RelayedOnewayConnection GetOrCreateConnection(System.Uri via, TimeSpan timeout)
			{
				RelayedOnewayListener.RelayedOnewayAmqpListenerClient.AmqpRelayedConnection amqpRelayedConnection = new RelayedOnewayListener.RelayedOnewayAmqpListenerClient.AmqpRelayedConnection(this, via, this.connectivitySettings, this.httpConnectivitySettings);
				amqpRelayedConnection.Open(timeout);
				(new RelayedOnewayTcpClient.ReceivePump(amqpRelayedConnection, this)).Open();
				return amqpRelayedConnection;
			}

			protected override void MessageReceived(Message message, Action dequeuedCallback)
			{
				RelayViaHeader relayViaHeader = RelayViaHeader.ReadHeader(message);
				message = this.messageWrapper.UnwrapMessage(message);
				if (relayViaHeader != null)
				{
					message.Properties.Via = relayViaHeader.Via;
				}
				RelayedOnewayChannelListener[] relayedOnewayChannelListenerArray = this.listener.Lookup(message.Properties.Via);
				if (relayedOnewayChannelListenerArray == null)
				{
					MessagingClientEtwProvider.Provider.RelayListenerFailedToDispatchMessage(base.Activity, base.Uri.AbsoluteUri, message.Properties.Via.AbsoluteUri);
					message.Close();
					dequeuedCallback();
					return;
				}
				if ((int)relayedOnewayChannelListenerArray.Length == 1)
				{
					relayedOnewayChannelListenerArray[0].EnqueueAndDispatch(message, dequeuedCallback);
					return;
				}
				Action action = dequeuedCallback;
				MessageBuffer messageBuffer = message.CreateBufferedCopy(65536);
				for (int i = 0; i < (int)relayedOnewayChannelListenerArray.Length; i++)
				{
					relayedOnewayChannelListenerArray[i].EnqueueAndDispatch(messageBuffer.CreateMessage(), action);
					action = null;
				}
			}

			private class AmqpRelayedConnection : RelayedOnewayTcpClient.RelayedOnewayConnection
			{
				private readonly RelayedOnewayListener.RelayedOnewayAmqpListenerClient client;

				private readonly AmqpRelay amqpRelay;

				private readonly InputQueue<Message> receivedMessages;

				private CommunicationState communicationState;

				public override Exception LastError
				{
					get
					{
						return this.amqpRelay.LastError;
					}
				}

				public override CommunicationState State
				{
					get
					{
						return this.communicationState;
					}
				}

				public AmqpRelayedConnection(RelayedOnewayListener.RelayedOnewayAmqpListenerClient client, System.Uri via, ConnectivitySettings connectivitySettings, HttpConnectivitySettings httpConnectivitySettings) : base(via)
				{
					this.communicationState = CommunicationState.Created;
					this.client = client;
					this.receivedMessages = new InputQueue<Message>();
					this.amqpRelay = new AmqpRelay(client.Uri, client.TokenProvider, connectivitySettings, httpConnectivitySettings);
					this.amqpRelay.Connecting += new EventHandler((object s, EventArgs e) => base.RaiseClosed(this, e));
					this.amqpRelay.Offline += new EventHandler((object s, EventArgs e) => base.RaiseFaulted(this, e));
					ServiceSettings serviceSettings = client.listener.nameSettings.ServiceSettings;
					if (serviceSettings.RelayClientAuthenticationType == RelayClientAuthenticationType.None)
					{
						this.amqpRelay.RelayClientAuthorizationRequired = false;
					}
					this.amqpRelay.DisplayName = client.listener.nameSettings.DisplayName;
					this.amqpRelay.ListenerType = serviceSettings.ListenerType;
					this.amqpRelay.IsDynamic = serviceSettings.IsDynamic;
					this.amqpRelay.ClientAgent = serviceSettings.ClientAgent;
					this.amqpRelay.PublishToRegistry = serviceSettings.IsDiscoverable;
					this.amqpRelay.TransportSecurityRequired = serviceSettings.TransportProtection == RelayTransportProtectionMode.EndToEnd;
				}

				public override void Abort()
				{
					try
					{
						this.amqpRelay.Abort();
					}
					finally
					{
						this.receivedMessages.Close();
						this.OnClosed();
					}
				}

				public override IAsyncResult BeginReceive(TimeSpan timeout, AsyncCallback callback, object state)
				{
					return this.receivedMessages.BeginDequeue(timeout, callback, state);
				}

				public override IAsyncResult BeginSend(Message message, TimeSpan timeout, AsyncCallback callback, object state)
				{
					throw new NotImplementedException();
				}

				public override void Close(TimeSpan timeout)
				{
					try
					{
						try
						{
							this.communicationState = CommunicationState.Closing;
							this.amqpRelay.CloseAsync(timeout).Wait();
						}
						catch (AggregateException aggregateException1)
						{
							AggregateException aggregateException = aggregateException1;
							throw Fx.Exception.AsWarning(aggregateException.GetBaseException(), this.client.Activity);
						}
					}
					finally
					{
						this.receivedMessages.Close();
						this.OnClosed();
					}
				}

				public override Message EndReceive(IAsyncResult result)
				{
					return this.receivedMessages.EndDequeue(result);
				}

				public override void EndSend(IAsyncResult result)
				{
					throw new NotImplementedException();
				}

				private void OnClosed()
				{
					this.communicationState = CommunicationState.Closed;
					base.RaiseClosed(this, EventArgs.Empty);
				}

				private void OnFaulted()
				{
					this.communicationState = CommunicationState.Faulted;
					base.RaiseFaulted(this, EventArgs.Empty);
				}

				private void OnMessageReceived(DuplexAmqpLink link, AmqpMessage amqpMessage)
				{
					try
					{
						Fx.AssertAndThrow(amqpMessage.BodyType == SectionFlag.Data, "Only 'Data' type is supported!");
						Data datum = null;
						foreach (Data dataBody in amqpMessage.DataBody)
						{
							Fx.AssertAndThrow(datum == null, "Serialization of AMQP messages with multiple body frames is not implemented.");
							datum = dataBody;
						}
						ArraySegment<byte> value = (ArraySegment<byte>)datum.Value;
						Message message = this.client.encoder.ReadMessage(value, this.client.bufferManager);
						this.receivedMessages.EnqueueAndDispatch(message);
						amqpMessage.Link.DisposeDelivery(amqpMessage, true, AmqpConstants.AcceptedOutcome);
					}
					catch (Exception exception1)
					{
						Exception exception = exception1;
						if (Fx.IsFatal(exception))
						{
							throw;
						}
						Fx.Exception.TraceHandled(exception, "AmqpRelayedListenerChannel.OnMessageReceived", null);
					}
				}

				private void OnOpened()
				{
					this.communicationState = CommunicationState.Opened;
				}

				public void Open(TimeSpan timeout)
				{
					this.communicationState = CommunicationState.Opening;
					this.amqpRelay.RegisterMessageListener(new Action<DuplexAmqpLink, AmqpMessage>(this.OnMessageReceived));
					bool flag = false;
					try
					{
						try
						{
							this.amqpRelay.OpenAsync(timeout).Wait();
							flag = true;
							this.OnOpened();
						}
						catch (AggregateException aggregateException1)
						{
							AggregateException aggregateException = aggregateException1;
							throw Fx.Exception.AsWarning(aggregateException.GetBaseException(), this.client.Activity);
						}
					}
					finally
					{
						if (!flag)
						{
							this.OnFaulted();
						}
					}
				}
			}
		}

		private class RelayedOnewayChannelListenerCollection
		{
			private bool multicast;

			private List<RelayedOnewayChannelListener> listeners;

			private System.Uri uri;

			public int Count
			{
				get
				{
					return this.listeners.Count;
				}
			}

			public bool Multicast
			{
				get
				{
					return this.multicast;
				}
			}

			public System.Uri Uri
			{
				get
				{
					return this.uri;
				}
			}

			public RelayedOnewayChannelListenerCollection(System.Uri uri, bool multicast)
			{
				this.uri = uri;
				this.multicast = multicast;
				this.listeners = new List<RelayedOnewayChannelListener>();
			}

			public void AddListener(RelayedOnewayChannelListener channelListener)
			{
				this.listeners.Add(channelListener);
			}

			public RelayedOnewayChannelListener[] GetListeners()
			{
				return this.listeners.ToArray();
			}

			public void RemoveListener(RelayedOnewayChannelListener channelListener)
			{
				this.listeners.Remove(channelListener);
			}
		}
	}
}