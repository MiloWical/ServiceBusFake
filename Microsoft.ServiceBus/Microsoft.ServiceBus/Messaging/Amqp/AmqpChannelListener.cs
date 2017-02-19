using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using Microsoft.ServiceBus.Messaging.Channels;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Xml;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal sealed class AmqpChannelListener : AmqpChannelListenerBase, IChannelListener<IInputSessionChannel>, IChannelListener, ICommunicationObject
	{
		private readonly InputQueue<IInputSessionChannel> availableChannels;

		private readonly string id;

		public AmqpChannelListener(AmqpTransportBindingElement transportBindingElement, BindingContext context) : base(transportBindingElement, context, HostNameComparisonMode.StrongWildcard)
		{
			this.availableChannels = new InputQueue<IInputSessionChannel>();
			this.id = string.Concat(base.GetType().Name, this.GetHashCode());
			MessagingClientEtwProvider.Provider.EventWriteAmqpLogOperation(this, TraceOperation.Create, this.Uri);
		}

		public IInputSessionChannel AcceptChannel()
		{
			return this.AcceptChannel(this.DefaultReceiveTimeout);
		}

		public IInputSessionChannel AcceptChannel(TimeSpan timeout)
		{
			this.ThrowIfNotOpened();
			return this.availableChannels.Dequeue(timeout);
		}

		public IAsyncResult BeginAcceptChannel(AsyncCallback callback, object state)
		{
			return this.BeginAcceptChannel(this.DefaultReceiveTimeout, callback, state);
		}

		public IAsyncResult BeginAcceptChannel(TimeSpan timeout, AsyncCallback callback, object state)
		{
			this.ThrowIfNotOpened();
			return this.availableChannels.BeginDequeue(timeout, callback, state);
		}

		public IInputSessionChannel EndAcceptChannel(IAsyncResult result)
		{
			return this.availableChannels.EndDequeue(result);
		}

		private void EnqueueLink(ReceivingAmqpLink link)
		{
			AmqpChannelListener.AmqpInputSessionChannel amqpInputSessionChannel = new AmqpChannelListener.AmqpInputSessionChannel(this, link);
			this.availableChannels.EnqueueAndDispatch(amqpInputSessionChannel, null, false);
		}

		protected override IAsyncResult OnBeginOpenLink(AmqpLink link, TimeSpan timeout, AsyncCallback callback, object state)
		{
			link.Opened += new EventHandler((object s, EventArgs e) => this.EnqueueLink((ReceivingAmqpLink)s));
			return new CompletedAsyncResult(callback, state);
		}

		protected override IAsyncResult OnBeginWaitForChannel(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.availableChannels.BeginWaitForItem(timeout, callback, state);
		}

		protected override void OnClosed()
		{
			base.OnClosed();
			MessagingClientEtwProvider.Provider.EventWriteAmqpLogOperation(this, TraceOperation.Close, this.Uri);
		}

		protected override void OnClosing()
		{
			base.OnClosing();
			this.availableChannels.Close();
		}

		protected override void OnEndOpenLink(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
		}

		protected override bool OnEndWaitForChannel(IAsyncResult result)
		{
			return this.availableChannels.EndWaitForItem(result);
		}

		protected override bool OnWaitForChannel(TimeSpan timeout)
		{
			return this.availableChannels.WaitForItem(timeout);
		}

		public override string ToString()
		{
			return this.id;
		}

		private sealed class AmqpInputSessionChannel : ChannelBase, IInputSessionChannel, IInputChannel, IChannel, ISessionChannel<IInputSession>, ICommunicationObjectInternals, ICommunicationObject
		{
			private readonly ReceivingAmqpLink link;

			private IInputSession inputSession;

			private readonly string id;

			internal AmqpChannelListener ChannelListener
			{
				get
				{
					return (AmqpChannelListener)base.Manager;
				}
			}

			public EndpointAddress LocalAddress
			{
				get
				{
					return get_LocalAddress();
				}
				set
				{
					set_LocalAddress(value);
				}
			}

			private EndpointAddress <LocalAddress>k__BackingField;

			public EndpointAddress get_LocalAddress()
			{
				return this.<LocalAddress>k__BackingField;
			}

			private void set_LocalAddress(EndpointAddress value)
			{
				this.<LocalAddress>k__BackingField = value;
			}

			public IInputSession Session
			{
				get
				{
					if (this.inputSession == null)
					{
						lock (base.ThisLock)
						{
							if (this.inputSession == null)
							{
								this.inputSession = new AmqpChannelListener.AmqpInputSessionChannel.InputSession(this);
							}
						}
					}
					return this.inputSession;
				}
			}

			public AmqpInputSessionChannel(AmqpChannelListener channelListener, ReceivingAmqpLink link) : base(channelListener)
			{
				this.link = link;
				this.LocalAddress = new EndpointAddress(channelListener.Uri, new AddressHeader[0]);
				this.id = string.Concat(base.GetType().Name, this.GetHashCode());
				MessagingClientEtwProvider.Provider.EventWriteAmqpLogOperation(this, TraceOperation.Create, this.LocalAddress);
			}

			private Message AmqpMessageToChannelMessage(AmqpMessage amqpMessage)
			{
				string empty;
				if (amqpMessage == null)
				{
					return null;
				}
				if (amqpMessage.ApplicationProperties == null || !amqpMessage.ApplicationProperties.Map.TryGetValue<string>("Action", out empty))
				{
					empty = string.Empty;
				}
				Message uri = null;
				if (amqpMessage.BodyType == SectionFlag.AmqpValue)
				{
					uri = Message.CreateMessage(System.ServiceModel.Channels.MessageVersion.Default, empty, amqpMessage.ValueBody.Value);
					amqpMessage.Dispose();
				}
				else if (amqpMessage.BodyType == SectionFlag.Data)
				{
					Data datum = null;
					foreach (Data dataBody in amqpMessage.DataBody)
					{
						Fx.AssertAndThrow(datum == null, "Serialization of AMQP messages with multiple body frames is not implemented.");
						datum = dataBody;
					}
					ArraySegment<byte> value = (ArraySegment<byte>)datum.Value;
					uri = this.ChannelListener.MessageEncoderFactory.Encoder.ReadMessage(value, AmqpChannelListenerBase.GCBufferManager);
					uri.Properties["AmqpMessageProperty"] = amqpMessage;
				}
				if (uri == null)
				{
					throw Fx.AssertAndThrow(string.Concat("Serialization of this type of AmqpMessage is not yet implemented: ", amqpMessage.BodyType.ToString()));
				}
				uri.Properties.Via = this.LocalAddress.Uri;
				if (amqpMessage.Properties != null)
				{
					if (amqpMessage.Properties.To != null)
					{
						uri.Headers.To = new System.Uri(amqpMessage.Properties.To.ToString());
					}
					if (amqpMessage.Properties.MessageId != null)
					{
						uri.Headers.MessageId = new UniqueId(amqpMessage.Properties.MessageId.ToString());
					}
				}
				return uri;
			}

			public IAsyncResult BeginReceive(AsyncCallback callback, object state)
			{
				return this.BeginReceive(base.DefaultReceiveTimeout, callback, state);
			}

			public IAsyncResult BeginReceive(TimeSpan timeout, AsyncCallback callback, object state)
			{
				return (new AmqpChannelListener.AmqpInputSessionChannel.TryReceiveAsyncResult(this, timeout, callback, state)).Start();
			}

			void Microsoft.ServiceBus.Messaging.Channels.ICommunicationObjectInternals.ThrowIfDisposed()
			{
				base.ThrowIfDisposed();
			}

			void Microsoft.ServiceBus.Messaging.Channels.ICommunicationObjectInternals.ThrowIfDisposedOrNotOpen()
			{
				base.ThrowIfDisposedOrNotOpen();
			}

			protected override void OnAbort()
			{
				this.link.SafeClose();
			}

			protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
			{
				return this.link.BeginClose(timeout, callback, state);
			}

			protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
			{
				return new CompletedAsyncResult(callback, state);
			}

			protected override void OnClose(TimeSpan timeout)
			{
				this.link.Close(timeout);
			}

			protected override void OnClosed()
			{
				base.OnClosed();
				MessagingClientEtwProvider.Provider.EventWriteAmqpLogOperation(this, TraceOperation.Close, this.LocalAddress);
			}

			protected override void OnEndClose(IAsyncResult result)
			{
				this.link.EndClose(result);
			}

			protected override void OnEndOpen(IAsyncResult result)
			{
				CompletedAsyncResult.End(result);
			}

			protected override void OnOpen(TimeSpan timeout)
			{
			}

			public Message Receive()
			{
				return this.Receive(base.DefaultReceiveTimeout);
			}

			public Message Receive(TimeSpan timeout)
			{
				AmqpChannelListener.AmqpInputSessionChannel.TryReceiveAsyncResult tryReceiveAsyncResult = new AmqpChannelListener.AmqpInputSessionChannel.TryReceiveAsyncResult(this, timeout, null, null);
				tryReceiveAsyncResult.RunSynchronously();
				if (!tryReceiveAsyncResult.Outcome)
				{
					throw new TimeoutException(SRCore.TimeoutOnOperation(timeout));
				}
				return tryReceiveAsyncResult.Message;
			}

			IAsyncResult System.ServiceModel.Channels.IInputChannel.BeginTryReceive(TimeSpan timeout, AsyncCallback callback, object state)
			{
				return (new AmqpChannelListener.AmqpInputSessionChannel.TryReceiveAsyncResult(this, timeout, callback, state)).Start();
			}

			IAsyncResult System.ServiceModel.Channels.IInputChannel.BeginWaitForMessage(TimeSpan timeout, AsyncCallback callback, object state)
			{
				base.ThrowIfDisposedOrNotOpen();
				throw new NotImplementedException();
			}

			Message System.ServiceModel.Channels.IInputChannel.EndReceive(IAsyncResult result)
			{
				AmqpChannelListener.AmqpInputSessionChannel.TryReceiveAsyncResult tryReceiveAsyncResult = AsyncResult<AmqpChannelListener.AmqpInputSessionChannel.TryReceiveAsyncResult>.End(result);
				if (!tryReceiveAsyncResult.Outcome)
				{
					throw new TimeoutException(SRCore.TimeoutOnOperation(tryReceiveAsyncResult.OriginalTimeout));
				}
				return tryReceiveAsyncResult.Message;
			}

			bool System.ServiceModel.Channels.IInputChannel.EndTryReceive(IAsyncResult result, out Message message)
			{
				AmqpChannelListener.AmqpInputSessionChannel.TryReceiveAsyncResult tryReceiveAsyncResult = AsyncResult<AmqpChannelListener.AmqpInputSessionChannel.TryReceiveAsyncResult>.End(result);
				message = tryReceiveAsyncResult.Message;
				return tryReceiveAsyncResult.Outcome;
			}

			bool System.ServiceModel.Channels.IInputChannel.EndWaitForMessage(IAsyncResult result)
			{
				throw new NotImplementedException();
			}

			public override string ToString()
			{
				return this.id;
			}

			public bool TryReceive(TimeSpan timeout, out Message message)
			{
				AmqpChannelListener.AmqpInputSessionChannel.TryReceiveAsyncResult tryReceiveAsyncResult = new AmqpChannelListener.AmqpInputSessionChannel.TryReceiveAsyncResult(this, timeout, null, null);
				tryReceiveAsyncResult.RunSynchronously();
				message = tryReceiveAsyncResult.Message;
				return tryReceiveAsyncResult.Outcome;
			}

			public bool WaitForMessage(TimeSpan timeout)
			{
				base.ThrowIfDisposedOrNotOpen();
				throw new NotImplementedException();
			}

			private sealed class InputSession : IInputSession, ISession
			{
				public string Id
				{
					get
					{
						return get_Id();
					}
					set
					{
						set_Id(value);
					}
				}

				private string <Id>k__BackingField;

				public string get_Id()
				{
					return this.<Id>k__BackingField;
				}

				private void set_Id(string value)
				{
					this.<Id>k__BackingField = value;
				}

				public InputSession(AmqpChannelListener.AmqpInputSessionChannel channel)
				{
					CultureInfo invariantCulture = CultureInfo.InvariantCulture;
					object[] connection = new object[] { channel.link.Session.Connection, channel.link.Session, channel.link };
					this.Id = string.Format(invariantCulture, "{0},{1},{2}", connection);
				}
			}

			private sealed class TryReceiveAsyncResult : IteratorAsyncResult<AmqpChannelListener.AmqpInputSessionChannel.TryReceiveAsyncResult>
			{
				private readonly AmqpChannelListener.AmqpInputSessionChannel inputChannel;

				private AmqpMessage amqpMessage;

				public Message Message
				{
					get;
					private set;
				}

				public bool Outcome
				{
					get;
					private set;
				}

				public TryReceiveAsyncResult(AmqpChannelListener.AmqpInputSessionChannel inputChannel, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
				{
					this.inputChannel = inputChannel;
				}

				protected override IEnumerator<IteratorAsyncResult<AmqpChannelListener.AmqpInputSessionChannel.TryReceiveAsyncResult>.AsyncStep> GetAsyncSteps()
				{
					if (!this.inputChannel.DoneReceivingInCurrentState())
					{
						AmqpChannelListener.AmqpInputSessionChannel.TryReceiveAsyncResult tryReceiveAsyncResult = this;
						IteratorAsyncResult<AmqpChannelListener.AmqpInputSessionChannel.TryReceiveAsyncResult>.BeginCall beginCall = (AmqpChannelListener.AmqpInputSessionChannel.TryReceiveAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.inputChannel.link.BeginReceiveMessage(t, c, s);
						yield return tryReceiveAsyncResult.CallAsync(beginCall, (AmqpChannelListener.AmqpInputSessionChannel.TryReceiveAsyncResult thisPtr, IAsyncResult a) => thisPtr.Outcome = thisPtr.inputChannel.link.EndReceiveMessage(a, out thisPtr.amqpMessage), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
						this.Message = this.inputChannel.AmqpMessageToChannelMessage(this.amqpMessage);
						if (this.amqpMessage != null)
						{
							this.inputChannel.link.DisposeMessage(this.amqpMessage, AmqpConstants.AcceptedOutcome, true, this.amqpMessage.Batchable);
						}
					}
					else
					{
						this.Message = null;
						this.Outcome = true;
					}
				}
			}
		}
	}
}