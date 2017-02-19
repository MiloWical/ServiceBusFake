using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using Microsoft.ServiceBus.Messaging.Amqp.Transport;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using System.Xml;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal sealed class AmqpChannelFactory : Microsoft.ServiceBus.Channels.TransportChannelFactory<IOutputChannel>
	{
		private readonly AmqpTransportBindingElement transportBindingElement;

		private readonly ConcurrentDictionary<string, AmqpChannelFactory.SharedAmqpConnection> sharedAmqpConnections;

		private readonly ConcurrentDictionary<string, AmqpChannelFactory.SharedAmqpLink> sharedAmqpLinks;

		private readonly AmqpChannelFactory.AmqpChannelEvents amqpChannelEvents;

		public override string Scheme
		{
			get
			{
				return "amqp";
			}
		}

		public AmqpChannelFactory(AmqpTransportBindingElement transportBindingElement, BindingContext context) : base(transportBindingElement, context)
		{
			this.transportBindingElement = transportBindingElement;
			this.sharedAmqpConnections = new ConcurrentDictionary<string, AmqpChannelFactory.SharedAmqpConnection>();
			this.sharedAmqpLinks = new ConcurrentDictionary<string, AmqpChannelFactory.SharedAmqpLink>();
			this.amqpChannelEvents = new AmqpChannelFactory.AmqpChannelEvents(this);
		}

		private static string CreateSharedAmqpConnectionsKey(string host, int port)
		{
			return string.Concat(host, ":", port);
		}

		public override T GetProperty<T>()
		where T : class
		{
			if (typeof(T) != typeof(IAmqpChannelEvents))
			{
				return base.GetProperty<T>();
			}
			return (T)this.amqpChannelEvents;
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult(callback, state);
		}

		protected override IOutputChannel OnCreateChannel(EndpointAddress address, Uri via)
		{
			AmqpChannelFactory.SharedAmqpLink orAdd;
			AmqpChannelFactory.SharedAmqpConnection sharedAmqpConnection;
			if (!this.sharedAmqpLinks.TryGetValue(via.AbsoluteUri, out orAdd))
			{
				string str = AmqpChannelFactory.CreateSharedAmqpConnectionsKey(via.Host, via.Port);
				if (!this.sharedAmqpConnections.TryGetValue(str, out sharedAmqpConnection))
				{
					sharedAmqpConnection = this.sharedAmqpConnections.GetOrAdd(str, new AmqpChannelFactory.SharedAmqpConnection(this, via.Host, via.Port, this.transportBindingElement.AmqpSettings));
				}
				orAdd = this.sharedAmqpLinks.GetOrAdd(via.AbsoluteUri, new AmqpChannelFactory.SharedAmqpLink(this, sharedAmqpConnection, via));
			}
			return new AmqpChannelFactory.AmqpOutputChannel(orAdd, this, address, via);
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
		}

		protected override void OnFaulted()
		{
			Fx.AssertAndFailFastService("AmqpChannelFactory is not designed to fault. If this is required, please update ConnectBindings and the users of ConnectBindings.P2PChannelFactory");
			base.OnFaulted();
		}

		protected override void OnOpen(TimeSpan timeout)
		{
		}

		private sealed class AmqpChannelEvents : IAmqpChannelEvents
		{
			private readonly object sender;

			public AmqpChannelEvents(object sender)
			{
				this.sender = sender;
			}

			public void OnLinkClosed()
			{
				EventHandler eventHandler = this.LinkClosed;
				if (eventHandler != null)
				{
					eventHandler(this.sender, EventArgs.Empty);
				}
			}

			public void OnLinkOpened()
			{
				EventHandler eventHandler = this.LinkOpened;
				if (eventHandler != null)
				{
					eventHandler(this.sender, EventArgs.Empty);
				}
			}

			public void OnSessionClosed()
			{
				EventHandler eventHandler = this.SessionClosed;
				if (eventHandler != null)
				{
					eventHandler(this.sender, EventArgs.Empty);
				}
			}

			public void OnSessionOpened()
			{
				EventHandler eventHandler = this.SessionOpened;
				if (eventHandler != null)
				{
					eventHandler(this.sender, EventArgs.Empty);
				}
			}

			public event EventHandler LinkClosed;

			public event EventHandler LinkOpened;

			public event EventHandler SessionClosed;

			public event EventHandler SessionOpened;
		}

		private sealed class AmqpOutputChannel : ChannelBase, IOutputChannel, IChannel, ICommunicationObject
		{
			private static int instanceIdCounter;

			private static int messageDeliveryTag;

			private readonly AmqpChannelFactory.SharedAmqpLink sharedAmqpLink;

			private readonly string id;

			private readonly EventHandler onAmqpObjectClosed;

			private AmqpConnection connection;

			private SendingAmqpLink link;

			private AmqpChannelFactory ChannelFactory
			{
				get
				{
					return (AmqpChannelFactory)base.Manager;
				}
			}

			private AmqpConnection Connection
			{
				get
				{
					return this.connection;
				}
				set
				{
					if (value != null)
					{
						value.Closed += this.onAmqpObjectClosed;
						if (value.State != AmqpObjectState.Opened)
						{
							base.Fault();
						}
					}
					this.connection = value;
				}
			}

			private SendingAmqpLink Link
			{
				get
				{
					return this.link;
				}
				set
				{
					if (value != null)
					{
						value.Closed += this.onAmqpObjectClosed;
						if (value.State != AmqpObjectState.Opened)
						{
							base.Fault();
						}
					}
					this.link = value;
				}
			}

			private MessageEncoder MessageEncoder
			{
				get
				{
					return this.ChannelFactory.MessageEncoderFactory.Encoder;
				}
			}

			public EndpointAddress RemoteAddress
			{
				get
				{
					return get_RemoteAddress();
				}
				set
				{
					set_RemoteAddress(value);
				}
			}

			private EndpointAddress <RemoteAddress>k__BackingField;

			public EndpointAddress get_RemoteAddress()
			{
				return this.<RemoteAddress>k__BackingField;
			}

			private void set_RemoteAddress(EndpointAddress value)
			{
				this.<RemoteAddress>k__BackingField = value;
			}

			public Uri Via
			{
				get
				{
					return get_Via();
				}
				set
				{
					set_Via(value);
				}
			}

			private Uri <Via>k__BackingField;

			public Uri get_Via()
			{
				return this.<Via>k__BackingField;
			}

			private void set_Via(Uri value)
			{
				this.<Via>k__BackingField = value;
			}

			public AmqpOutputChannel(AmqpChannelFactory.SharedAmqpLink sharedAmqpLink, AmqpChannelFactory channelFactory, EndpointAddress address, Uri via) : base(channelFactory)
			{
				this.sharedAmqpLink = sharedAmqpLink;
				this.RemoteAddress = address;
				this.Via = via;
				this.id = string.Concat(base.GetType().Name, Interlocked.Increment(ref AmqpChannelFactory.AmqpOutputChannel.instanceIdCounter));
				this.onAmqpObjectClosed = new EventHandler(this.OnAmqpObjectClosed);
			}

			private void AddHeadersTo(Message channelMessage)
			{
				if (!this.ChannelFactory.ManualAddressing && this.RemoteAddress != null)
				{
					this.RemoteAddress.ApplyTo(channelMessage);
				}
			}

			public IAsyncResult BeginSend(Message message, AsyncCallback callback, object state)
			{
				return this.BeginSend(message, base.DefaultSendTimeout, callback, state);
			}

			public IAsyncResult BeginSend(Message message, TimeSpan timeout, AsyncCallback callback, object state)
			{
				base.ThrowIfDisposedOrNotOpen();
				this.AddHeadersTo(message);
				int maxBufferSize = this.ChannelFactory.GetMaxBufferSize();
				ArraySegment<byte> nums = this.MessageEncoder.WriteMessage(message, maxBufferSize, this.ChannelFactory.BufferManager);
				AmqpMessage amqpMessage = AmqpChannelFactory.AmqpOutputChannel.FinishEncodingMessage(nums, message);
				SendingAmqpLink link = this.Link;
				ArraySegment<byte> deliveryTag = AmqpChannelFactory.AmqpOutputChannel.GetDeliveryTag();
				ArraySegment<byte> nums1 = new ArraySegment<byte>();
				return link.BeginSendMessage(amqpMessage, deliveryTag, nums1, timeout, callback, state);
			}

			public void EndSend(IAsyncResult result)
			{
				SendingAmqpLink sendingAmqpLink;
				Outcome outcome = this.Link.EndSendMessage(result);
				if (outcome.DescriptorCode != Accepted.Code)
				{
					if (this.sharedAmqpLink != null)
					{
						this.sharedAmqpLink.Invalidate(out sendingAmqpLink);
					}
					Rejected rejected = outcome as Rejected;
					if (rejected == null)
					{
						base.Fault();
						throw Fx.Exception.AsWarning(new CommunicationObjectFaultedException(outcome.ToString()), null);
					}
					throw ExceptionHelper.ToCommunicationContract(rejected.Error, null);
				}
			}

			private static AmqpMessage FinishEncodingMessage(ArraySegment<byte> buffer, Message channelMessage)
			{
				Data[] dataArray = new Data[1];
				Data datum = new Data()
				{
					Value = buffer
				};
				dataArray[0] = datum;
				AmqpMessage absoluteUri = AmqpMessage.Create(dataArray);
				if (channelMessage.Headers.To != null)
				{
					absoluteUri.Properties.To = channelMessage.Headers.To.AbsoluteUri;
				}
				if (channelMessage.Headers.MessageId != null)
				{
					absoluteUri.Properties.MessageId = channelMessage.Headers.MessageId.ToString();
				}
				if (channelMessage.Headers.ReplyTo != null)
				{
					absoluteUri.Properties.ReplyTo = channelMessage.Headers.ReplyTo.Uri.AbsoluteUri;
				}
				return absoluteUri;
			}

			private static ArraySegment<byte> GetDeliveryTag()
			{
				int num = Interlocked.Increment(ref AmqpChannelFactory.AmqpOutputChannel.messageDeliveryTag);
				return new ArraySegment<byte>(BitConverter.GetBytes(num));
			}

			protected override void OnAbort()
			{
				SendingAmqpLink sendingAmqpLink;
				MessagingClientEtwProvider.Provider.EventWriteAmqpLogOperation(this.id, TraceOperation.Abort, this.Via.AbsolutePath);
				if (this.sharedAmqpLink != null && this.sharedAmqpLink.Invalidate(out sendingAmqpLink))
				{
					sendingAmqpLink.SafeClose();
				}
			}

			private void OnAmqpObjectClosed(object sender, EventArgs args)
			{
				base.Fault();
			}

			protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
			{
				return new CompletedAsyncResult(callback, state);
			}

			protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
			{
				return (new AmqpChannelFactory.AmqpOutputChannel.EstablishConnectionAsyncResult(this.sharedAmqpLink, this, timeout, callback, state)).Start();
			}

			protected override void OnClose(TimeSpan timeout)
			{
			}

			protected override void OnClosing()
			{
				SendingAmqpLink link = this.Link;
				if (link != null)
				{
					link.Closed -= this.onAmqpObjectClosed;
				}
				AmqpConnection connection = this.Connection;
				if (connection != null)
				{
					connection.Closed -= this.onAmqpObjectClosed;
				}
				base.OnClosing();
			}

			protected override void OnEndClose(IAsyncResult result)
			{
				CompletedAsyncResult.End(result);
			}

			protected override void OnEndOpen(IAsyncResult result)
			{
				AsyncResult<AmqpChannelFactory.AmqpOutputChannel.EstablishConnectionAsyncResult>.End(result);
			}

			protected override void OnFaulted()
			{
				MessagingClientEtwProvider.Provider.EventWriteChannelFaulted(this.id);
				base.OnFaulted();
			}

			protected override void OnOpen(TimeSpan timeout)
			{
				(new AmqpChannelFactory.AmqpOutputChannel.EstablishConnectionAsyncResult(this.sharedAmqpLink, this, timeout, null, null)).RunSynchronously();
			}

			public void Send(Message message)
			{
				this.Send(message, base.DefaultSendTimeout);
			}

			public void Send(Message message, TimeSpan timeout)
			{
				this.EndSend(this.BeginSend(message, timeout, null, null));
			}

			private sealed class EstablishConnectionAsyncResult : IteratorAsyncResult<AmqpChannelFactory.AmqpOutputChannel.EstablishConnectionAsyncResult>
			{
				private readonly static Action<AsyncResult, Exception> onFinally;

				private readonly AmqpChannelFactory.AmqpOutputChannel channel;

				private readonly AmqpChannelFactory.SharedAmqpLink sharedAmqpLink;

				static EstablishConnectionAsyncResult()
				{
					AmqpChannelFactory.AmqpOutputChannel.EstablishConnectionAsyncResult.onFinally = new Action<AsyncResult, Exception>(AmqpChannelFactory.AmqpOutputChannel.EstablishConnectionAsyncResult.OnFinally);
				}

				public EstablishConnectionAsyncResult(AmqpChannelFactory.SharedAmqpLink sharedAmqpLink, AmqpChannelFactory.AmqpOutputChannel channel, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
				{
					this.channel = channel;
					this.sharedAmqpLink = sharedAmqpLink;
					AmqpChannelFactory.AmqpOutputChannel.EstablishConnectionAsyncResult establishConnectionAsyncResult = this;
					establishConnectionAsyncResult.OnCompleting = (Action<AsyncResult, Exception>)Delegate.Combine(establishConnectionAsyncResult.OnCompleting, AmqpChannelFactory.AmqpOutputChannel.EstablishConnectionAsyncResult.onFinally);
				}

				protected override IEnumerator<IteratorAsyncResult<AmqpChannelFactory.AmqpOutputChannel.EstablishConnectionAsyncResult>.AsyncStep> GetAsyncSteps()
				{
					AmqpChannelFactory.AmqpOutputChannel.EstablishConnectionAsyncResult establishConnectionAsyncResult = this;
					IteratorAsyncResult<AmqpChannelFactory.AmqpOutputChannel.EstablishConnectionAsyncResult>.BeginCall beginCall = (AmqpChannelFactory.AmqpOutputChannel.EstablishConnectionAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.sharedAmqpLink.BeginGetInstance(t, c, s);
					yield return establishConnectionAsyncResult.CallAsync(beginCall, (AmqpChannelFactory.AmqpOutputChannel.EstablishConnectionAsyncResult thisPtr, IAsyncResult a) => thisPtr.channel.Link = thisPtr.sharedAmqpLink.EndGetInstance(a), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
					this.channel.Connection = this.channel.Link.Session.Connection;
				}

				private static void OnFinally(AsyncResult asyncResult, Exception exception)
				{
					if (exception != null)
					{
						throw ExceptionHelper.ToCommunicationContract(exception);
					}
				}
			}
		}

		private sealed class SharedAmqpConnection : SingletonManager<Tuple<AmqpConnection, AmqpSession>>
		{
			private readonly AmqpChannelFactory amqpChannelFactory;

			private readonly string host;

			private readonly int port;

			private readonly AmqpSettings settings;

			public SharedAmqpConnection(AmqpChannelFactory amqpChannelFactory, string host, int port, AmqpSettings settings) : base(new object())
			{
				this.amqpChannelFactory = amqpChannelFactory;
				this.host = host;
				this.port = port;
				this.settings = settings;
			}

			private void Cleanup()
			{
				AmqpChannelFactory.SharedAmqpConnection sharedAmqpConnection;
				string str = AmqpChannelFactory.CreateSharedAmqpConnectionsKey(this.host, this.port);
				this.amqpChannelFactory.sharedAmqpConnections.TryRemove(str, out sharedAmqpConnection);
				this.amqpChannelFactory.amqpChannelEvents.OnSessionClosed();
			}

			protected override IAsyncResult OnBeginCreateInstance(TimeSpan timeout, AsyncCallback callback, object state)
			{
				return (new AmqpChannelFactory.SharedAmqpConnection.OpenConnectionAsyncResult(this, this.host, this.port, this.settings, timeout, callback, state)).Start();
			}

			protected override Tuple<AmqpConnection, AmqpSession> OnEndCreateInstance(IAsyncResult asyncResult)
			{
				AmqpChannelFactory.SharedAmqpConnection.OpenConnectionAsyncResult openConnectionAsyncResult = AsyncResult<AmqpChannelFactory.SharedAmqpConnection.OpenConnectionAsyncResult>.End(asyncResult);
				Tuple<AmqpConnection, AmqpSession> tuple = new Tuple<AmqpConnection, AmqpSession>(openConnectionAsyncResult.Connection, openConnectionAsyncResult.Session);
				this.amqpChannelFactory.amqpChannelEvents.OnSessionOpened();
				return tuple;
			}

			protected override void OnGetInstance(Tuple<AmqpConnection, AmqpSession> connectionAndSession)
			{
				if ((connectionAndSession.Item1.State != AmqpObjectState.Opened || connectionAndSession.Item2.State != AmqpObjectState.Opened) && base.Invalidate(connectionAndSession))
				{
					connectionAndSession.Item1.SafeClose();
				}
			}

			private void OnSessionClosed(object sender, EventArgs e)
			{
				this.Cleanup();
			}

			private sealed class ConnectAsyncResult : AsyncResult<AmqpChannelFactory.SharedAmqpConnection.ConnectAsyncResult>
			{
				private readonly AmqpSettings amqpSettings;

				public AmqpConnection AmqpConnection
				{
					get;
					private set;
				}

				public ConnectAsyncResult(string host, int port, AmqpSettings amqpSettings, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
				{
					if (port <= 0)
					{
						port = 5672;
					}
					this.amqpSettings = amqpSettings;
					TcpTransportSettings tcpTransportSetting = new TcpTransportSettings()
					{
						Host = host,
						Port = port
					};
					AmqpTransportInitiator amqpTransportInitiator = new AmqpTransportInitiator(this.amqpSettings, tcpTransportSetting);
					TransportAsyncCallbackArgs transportAsyncCallbackArg = new TransportAsyncCallbackArgs()
					{
						CompletedCallback = new Action<TransportAsyncCallbackArgs>(this.OnTransportCallback),
						UserToken = this
					};
					amqpTransportInitiator.ConnectAsync(timeout, transportAsyncCallbackArg);
				}

				private void OnTransportCallback(TransportAsyncCallbackArgs args)
				{
					if (args.Exception != null)
					{
						base.Complete(args.CompletedSynchronously, args.Exception);
						return;
					}
					AmqpConnectionSettings amqpConnectionSetting = new AmqpConnectionSettings()
					{
						ContainerId = Guid.NewGuid().ToString("N")
					};
					this.AmqpConnection = new AmqpConnection(args.Transport, this.amqpSettings, amqpConnectionSetting);
					base.Complete(args.CompletedSynchronously);
				}
			}

			private sealed class OpenConnectionAsyncResult : IteratorAsyncResult<AmqpChannelFactory.SharedAmqpConnection.OpenConnectionAsyncResult>
			{
				private readonly static Action<AsyncResult, Exception> onFinally;

				private readonly AmqpChannelFactory.SharedAmqpConnection sharedAmqpConnection;

				private readonly string host;

				private readonly int port;

				private readonly AmqpSettings settings;

				public AmqpConnection Connection
				{
					get;
					private set;
				}

				public AmqpSession Session
				{
					get;
					private set;
				}

				static OpenConnectionAsyncResult()
				{
					AmqpChannelFactory.SharedAmqpConnection.OpenConnectionAsyncResult.onFinally = new Action<AsyncResult, Exception>(AmqpChannelFactory.SharedAmqpConnection.OpenConnectionAsyncResult.OnFinally);
				}

				public OpenConnectionAsyncResult(AmqpChannelFactory.SharedAmqpConnection sharedAmqpConnection, string host, int port, AmqpSettings settings, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
				{
					this.sharedAmqpConnection = sharedAmqpConnection;
					this.host = host;
					this.port = port;
					this.settings = settings;
					AmqpChannelFactory.SharedAmqpConnection.OpenConnectionAsyncResult openConnectionAsyncResult = this;
					openConnectionAsyncResult.OnCompleting = (Action<AsyncResult, Exception>)Delegate.Combine(openConnectionAsyncResult.OnCompleting, AmqpChannelFactory.SharedAmqpConnection.OpenConnectionAsyncResult.onFinally);
				}

				protected override IEnumerator<IteratorAsyncResult<AmqpChannelFactory.SharedAmqpConnection.OpenConnectionAsyncResult>.AsyncStep> GetAsyncSteps()
				{
					AmqpChannelFactory.SharedAmqpConnection.OpenConnectionAsyncResult openConnectionAsyncResult = this;
					IteratorAsyncResult<AmqpChannelFactory.SharedAmqpConnection.OpenConnectionAsyncResult>.BeginCall connectAsyncResult = (AmqpChannelFactory.SharedAmqpConnection.OpenConnectionAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => new AmqpChannelFactory.SharedAmqpConnection.ConnectAsyncResult(thisPtr.host, thisPtr.port, thisPtr.settings, t, c, s);
					yield return openConnectionAsyncResult.CallAsync(connectAsyncResult, (AmqpChannelFactory.SharedAmqpConnection.OpenConnectionAsyncResult thisPtr, IAsyncResult a) => thisPtr.Connection = AsyncResult<AmqpChannelFactory.SharedAmqpConnection.ConnectAsyncResult>.End(a).AmqpConnection, IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
					this.Connection.Closed += new EventHandler(this.sharedAmqpConnection.OnSessionClosed);
					AmqpChannelFactory.SharedAmqpConnection.OpenConnectionAsyncResult openConnectionAsyncResult1 = this;
					IteratorAsyncResult<AmqpChannelFactory.SharedAmqpConnection.OpenConnectionAsyncResult>.BeginCall beginCall = (AmqpChannelFactory.SharedAmqpConnection.OpenConnectionAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.Connection.BeginOpen(t, c, s);
					IteratorAsyncResult<AmqpChannelFactory.SharedAmqpConnection.OpenConnectionAsyncResult>.EndCall endCall = (AmqpChannelFactory.SharedAmqpConnection.OpenConnectionAsyncResult thisPtr, IAsyncResult a) => thisPtr.Connection.EndOpen(a);
					yield return openConnectionAsyncResult1.CallAsync(beginCall, endCall, (AmqpChannelFactory.SharedAmqpConnection.OpenConnectionAsyncResult thisPtr, TimeSpan t) => thisPtr.Connection.Open(t), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
					AmqpSessionSettings amqpSessionSetting = new AmqpSessionSettings()
					{
						DispositionInterval = TimeSpan.Zero
					};
					this.Session = this.Connection.CreateSession(amqpSessionSetting);
					this.Session.Closed += new EventHandler(this.sharedAmqpConnection.OnSessionClosed);
					AmqpChannelFactory.SharedAmqpConnection.OpenConnectionAsyncResult openConnectionAsyncResult2 = this;
					IteratorAsyncResult<AmqpChannelFactory.SharedAmqpConnection.OpenConnectionAsyncResult>.BeginCall beginCall1 = (AmqpChannelFactory.SharedAmqpConnection.OpenConnectionAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.Session.BeginOpen(t, c, s);
					IteratorAsyncResult<AmqpChannelFactory.SharedAmqpConnection.OpenConnectionAsyncResult>.EndCall endCall1 = (AmqpChannelFactory.SharedAmqpConnection.OpenConnectionAsyncResult thisPtr, IAsyncResult r) => thisPtr.Session.EndOpen(r);
					yield return openConnectionAsyncResult2.CallAsync(beginCall1, endCall1, (AmqpChannelFactory.SharedAmqpConnection.OpenConnectionAsyncResult thisPtr, TimeSpan t) => thisPtr.Session.Open(t), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				}

				private static void OnFinally(AsyncResult asyncResult, Exception exception)
				{
					AmqpChannelFactory.SharedAmqpConnection.OpenConnectionAsyncResult openConnectionAsyncResult = (AmqpChannelFactory.SharedAmqpConnection.OpenConnectionAsyncResult)asyncResult;
					if (exception != null)
					{
						openConnectionAsyncResult.sharedAmqpConnection.Cleanup();
						if (openConnectionAsyncResult.Session != null)
						{
							openConnectionAsyncResult.Session.Closed -= new EventHandler(openConnectionAsyncResult.sharedAmqpConnection.OnSessionClosed);
						}
						if (openConnectionAsyncResult.Connection != null)
						{
							openConnectionAsyncResult.Connection.Closed -= new EventHandler(openConnectionAsyncResult.sharedAmqpConnection.OnSessionClosed);
							openConnectionAsyncResult.Connection.SafeClose();
						}
					}
				}
			}
		}

		private sealed class SharedAmqpLink : SingletonManager<SendingAmqpLink>
		{
			private readonly AmqpChannelFactory amqpChannelFactory;

			private readonly AmqpChannelFactory.SharedAmqpConnection sharedAmqpConnection;

			private readonly Uri via;

			public SharedAmqpLink(AmqpChannelFactory amqpChannelFactory, AmqpChannelFactory.SharedAmqpConnection sharedConnection, Uri via) : base(new object())
			{
				this.amqpChannelFactory = amqpChannelFactory;
				this.sharedAmqpConnection = sharedConnection;
				this.via = via;
			}

			private void Cleanup()
			{
				AmqpChannelFactory.SharedAmqpLink sharedAmqpLink;
				this.amqpChannelFactory.sharedAmqpLinks.TryRemove(this.via.AbsoluteUri, out sharedAmqpLink);
				this.amqpChannelFactory.amqpChannelEvents.OnLinkClosed();
			}

			protected override IAsyncResult OnBeginCreateInstance(TimeSpan timeout, AsyncCallback callback, object state)
			{
				return (new AmqpChannelFactory.SharedAmqpLink.OpenLinkAsyncResult(this.sharedAmqpConnection, this, this.via, timeout, callback, state)).Start();
			}

			protected override SendingAmqpLink OnEndCreateInstance(IAsyncResult asyncResult)
			{
				SendingAmqpLink link = AsyncResult<AmqpChannelFactory.SharedAmqpLink.OpenLinkAsyncResult>.End(asyncResult).Link;
				this.amqpChannelFactory.amqpChannelEvents.OnLinkOpened();
				return link;
			}

			private void OnLinkClosed(object sender, EventArgs e)
			{
				this.Cleanup();
			}

			private sealed class OpenLinkAsyncResult : IteratorAsyncResult<AmqpChannelFactory.SharedAmqpLink.OpenLinkAsyncResult>
			{
				private readonly static Action<AsyncResult, Exception> onFinally;

				private readonly AmqpChannelFactory.SharedAmqpConnection sharedAmqpConnection;

				private readonly AmqpChannelFactory.SharedAmqpLink sharedAmqpLink;

				private readonly Uri via;

				private Tuple<AmqpConnection, AmqpSession> amqpConnectionSession;

				public SendingAmqpLink Link
				{
					get;
					private set;
				}

				static OpenLinkAsyncResult()
				{
					AmqpChannelFactory.SharedAmqpLink.OpenLinkAsyncResult.onFinally = new Action<AsyncResult, Exception>(AmqpChannelFactory.SharedAmqpLink.OpenLinkAsyncResult.OnFinally);
				}

				public OpenLinkAsyncResult(AmqpChannelFactory.SharedAmqpConnection sharedAmqpConnection, AmqpChannelFactory.SharedAmqpLink sharedAmqpLink, Uri via, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
				{
					this.sharedAmqpConnection = sharedAmqpConnection;
					this.sharedAmqpLink = sharedAmqpLink;
					this.via = via;
					AmqpChannelFactory.SharedAmqpLink.OpenLinkAsyncResult openLinkAsyncResult = this;
					openLinkAsyncResult.OnCompleting = (Action<AsyncResult, Exception>)Delegate.Combine(openLinkAsyncResult.OnCompleting, AmqpChannelFactory.SharedAmqpLink.OpenLinkAsyncResult.onFinally);
				}

				protected override IEnumerator<IteratorAsyncResult<AmqpChannelFactory.SharedAmqpLink.OpenLinkAsyncResult>.AsyncStep> GetAsyncSteps()
				{
					AmqpChannelFactory.SharedAmqpLink.OpenLinkAsyncResult openLinkAsyncResult = this;
					IteratorAsyncResult<AmqpChannelFactory.SharedAmqpLink.OpenLinkAsyncResult>.BeginCall beginCall = (AmqpChannelFactory.SharedAmqpLink.OpenLinkAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.sharedAmqpConnection.BeginGetInstance(t, c, s);
					yield return openLinkAsyncResult.CallAsync(beginCall, (AmqpChannelFactory.SharedAmqpLink.OpenLinkAsyncResult thisPtr, IAsyncResult a) => thisPtr.amqpConnectionSession = thisPtr.sharedAmqpConnection.EndGetInstance(a), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
					AmqpLinkSettings amqpLinkSetting = new AmqpLinkSettings()
					{
						LinkName = string.Concat(this.via.PathAndQuery, "/", Guid.NewGuid())
					};
					AmqpLinkSettings amqpLinkSetting1 = amqpLinkSetting;
					AmqpSymbol timeoutName = ClientConstants.TimeoutName;
					TimeSpan timeSpan = base.RemainingTime();
					amqpLinkSetting1.AddProperty(timeoutName, (uint)timeSpan.TotalMilliseconds);
					amqpLinkSetting.Role = new bool?(false);
					amqpLinkSetting.InitialDeliveryCount = new uint?(0);
					AmqpLinkSettings amqpLinkSetting2 = amqpLinkSetting;
					Target target = new Target()
					{
						Address = this.via.PathAndQuery
					};
					amqpLinkSetting2.Target = target;
					this.Link = new SendingAmqpLink(this.amqpConnectionSession.Item2, amqpLinkSetting);
					this.Link.Closed += new EventHandler(this.sharedAmqpLink.OnLinkClosed);
					AmqpChannelFactory.SharedAmqpLink.OpenLinkAsyncResult openLinkAsyncResult1 = this;
					IteratorAsyncResult<AmqpChannelFactory.SharedAmqpLink.OpenLinkAsyncResult>.BeginCall beginCall1 = (AmqpChannelFactory.SharedAmqpLink.OpenLinkAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.Link.BeginOpen(t, c, s);
					IteratorAsyncResult<AmqpChannelFactory.SharedAmqpLink.OpenLinkAsyncResult>.EndCall endCall = (AmqpChannelFactory.SharedAmqpLink.OpenLinkAsyncResult thisPtr, IAsyncResult r) => thisPtr.Link.EndOpen(r);
					yield return openLinkAsyncResult1.CallAsync(beginCall1, endCall, (AmqpChannelFactory.SharedAmqpLink.OpenLinkAsyncResult thisPtr, TimeSpan t) => thisPtr.Link.Open(t), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				}

				private static void OnFinally(AsyncResult asyncResult, Exception exception)
				{
					AmqpChannelFactory.SharedAmqpLink.OpenLinkAsyncResult openLinkAsyncResult = (AmqpChannelFactory.SharedAmqpLink.OpenLinkAsyncResult)asyncResult;
					if (exception != null)
					{
						if (openLinkAsyncResult.Link != null)
						{
							openLinkAsyncResult.Link.Closed -= new EventHandler(openLinkAsyncResult.sharedAmqpLink.OnLinkClosed);
							openLinkAsyncResult.Link.SafeClose();
						}
						openLinkAsyncResult.sharedAmqpLink.Cleanup();
					}
				}
			}
		}
	}
}