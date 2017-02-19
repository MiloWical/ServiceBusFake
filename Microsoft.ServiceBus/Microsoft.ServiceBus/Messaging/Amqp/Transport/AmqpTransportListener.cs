using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
	internal sealed class AmqpTransportListener : TransportListener
	{
		private readonly List<TransportListener> innerListeners;

		private readonly Microsoft.ServiceBus.Messaging.Amqp.AmqpSettings settings;

		public Microsoft.ServiceBus.Messaging.Amqp.AmqpSettings AmqpSettings
		{
			get
			{
				return this.settings;
			}
		}

		public AmqpTransportListener(IEnumerable<TransportListener> listeners, Microsoft.ServiceBus.Messaging.Amqp.AmqpSettings settings) : base("tp-listener")
		{
			this.innerListeners = new List<TransportListener>(listeners);
			this.settings = settings;
		}

		protected override void AbortInternal()
		{
			base.State = AmqpObjectState.Faulted;
			TransportListener[] array = this.innerListeners.ToArray();
			for (int i = 0; i < (int)array.Length; i++)
			{
				array[i].Abort();
			}
		}

		protected override bool CloseInternal()
		{
			base.State = AmqpObjectState.CloseSent;
			TransportListener[] array = this.innerListeners.ToArray();
			for (int i = 0; i < (int)array.Length; i++)
			{
				array[i].Close();
			}
			return true;
		}

		public T Find<T>()
		where T : TransportListener
		{
			T t;
			List<TransportListener>.Enumerator enumerator = this.innerListeners.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					TransportListener current = enumerator.Current;
					if (typeof(T) != current.GetType())
					{
						continue;
					}
					t = (T)current;
					return t;
				}
				return default(T);
			}
			finally
			{
				((IDisposable)enumerator).Dispose();
			}
			return t;
		}

		private void OnAcceptTransport(TransportAsyncCallbackArgs args)
		{
			MessagingClientEtwProvider.TraceClient<AmqpTransportListener>((AmqpTransportListener source) => MessagingClientEtwProvider.Provider.EventWriteAmqpLogOperation(source, TraceOperation.Execute, "OnAcceptTransport"), this);
			AmqpTransportListener.TransportHandler.SpawnHandler(this, args);
		}

		private void OnHandleTransportComplete(TransportAsyncCallbackArgs args)
		{
			args.SetBuffer(null, 0, 0);
			args.CompletedCallback = null;
			if (args.Exception == null)
			{
				base.OnTransportAccepted(args);
				return;
			}
			args.Transport.SafeClose(args.Exception);
		}

		protected override void OnListen()
		{
			Action<TransportAsyncCallbackArgs> action = new Action<TransportAsyncCallbackArgs>(this.OnAcceptTransport);
			EventHandler eventHandler = new EventHandler(this.OnListenerClosed);
			foreach (TransportListener innerListener in this.innerListeners)
			{
				innerListener.Closed += eventHandler;
				innerListener.Listen(action);
			}
		}

		private void OnListenerClosed(object sender, EventArgs e)
		{
			if (!base.IsClosing())
			{
				TransportListener transportListener = (TransportListener)sender;
				MessagingClientEtwProvider.TraceClient<AmqpTransportListener, TransportListener>((AmqpTransportListener source, TransportListener listener) => MessagingClientEtwProvider.Provider.EventWriteAmqpLogError(source, "OnListenerClosed", listener.ToString()), this, transportListener);
				base.SafeClose(transportListener.TerminalException);
			}
		}

		private sealed class TransportHandler
		{
			private readonly static AsyncCallback onTransportOpened;

			private readonly AmqpTransportListener parent;

			private readonly TransportAsyncCallbackArgs args;

			private static Action<TransportAsyncCallbackArgs> readCompleteCallback;

			private static Action<TransportAsyncCallbackArgs> writeCompleteCallback;

			private static Action<object> startCallback;

			private AsyncIO.AsyncBufferReader bufferReader;

			private AsyncIO.AsyncBufferWriter bufferWriter;

			private byte[] buffer;

			private TimeoutHelper timeoutHelper;

			static TransportHandler()
			{
				AmqpTransportListener.TransportHandler.onTransportOpened = new AsyncCallback(AmqpTransportListener.TransportHandler.OnTransportOpened);
				AmqpTransportListener.TransportHandler.readCompleteCallback = new Action<TransportAsyncCallbackArgs>(AmqpTransportListener.TransportHandler.OnReadHeaderComplete);
				AmqpTransportListener.TransportHandler.writeCompleteCallback = new Action<TransportAsyncCallbackArgs>(AmqpTransportListener.TransportHandler.OnWriteHeaderComplete);
				AmqpTransportListener.TransportHandler.startCallback = new Action<object>(AmqpTransportListener.TransportHandler.Start);
			}

			private TransportHandler(AmqpTransportListener parent, TransportAsyncCallbackArgs args)
			{
				this.parent = parent;
				this.args = args;
				this.args.UserToken = this;
				this.buffer = new byte[8];
				this.bufferReader = new AsyncIO.AsyncBufferReader(args.Transport);
				this.bufferWriter = new AsyncIO.AsyncBufferWriter(args.Transport);
				this.timeoutHelper = new TimeoutHelper(TimeSpan.FromSeconds(60));
			}

			private void HandleTransportOpened(IAsyncResult result)
			{
				this.args.Transport.EndOpen(result);
				this.bufferReader = new AsyncIO.AsyncBufferReader(this.args.Transport);
				this.bufferWriter = new AsyncIO.AsyncBufferWriter(this.args.Transport);
				this.ReadProtocolHeader();
			}

			private void OnProtocolHeader(ByteBuffer buffer)
			{
				AmqpVersion amqpVersion;
				ProtocolHeader protocolHeader = new ProtocolHeader();
				protocolHeader.Decode(buffer);
				MessagingClientEtwProvider.TraceClient<AmqpTransportListener.TransportHandler, ProtocolHeader>((AmqpTransportListener.TransportHandler source, ProtocolHeader detail) => MessagingClientEtwProvider.Provider.EventWriteAmqpLogOperation(source, TraceOperation.Receive, detail), this, protocolHeader);
				TransportProvider transportProvider = null;
				if (!this.parent.settings.TryGetTransportProvider(protocolHeader, out transportProvider))
				{
					this.WriteReplyHeader(new ProtocolHeader(transportProvider.ProtocolId, transportProvider.DefaultVersion), true);
					return;
				}
				if (!transportProvider.TryGetVersion(protocolHeader.Version, out amqpVersion))
				{
					this.WriteReplyHeader(new ProtocolHeader(transportProvider.ProtocolId, amqpVersion), true);
					return;
				}
				TransportBase transportBase = transportProvider.CreateTransport(this.args.Transport, false);
				if (!object.ReferenceEquals(transportBase, this.args.Transport))
				{
					MessagingClientEtwProvider.TraceClient<AmqpTransportListener.TransportHandler, TransportBase, TransportBase>((AmqpTransportListener.TransportHandler source, TransportBase from, TransportBase to) => MessagingClientEtwProvider.Provider.EventWriteAmqpUpgradeTransport(source, from, to), this, this.args.Transport, transportBase);
					this.args.Transport = transportBase;
					this.WriteReplyHeader(protocolHeader, false);
					return;
				}
				if ((!this.parent.settings.RequireSecureTransport || transportBase.IsSecure) && (this.parent.settings.AllowAnonymousConnection || transportBase.IsAuthenticated))
				{
					this.args.UserToken = protocolHeader;
					this.parent.OnHandleTransportComplete(this.args);
					return;
				}
				MessagingClientEtwProvider.TraceClient<AmqpTransportListener, TransportBase>((AmqpTransportListener source, TransportBase transport) => MessagingClientEtwProvider.Provider.EventWriteAmqpInsecureTransport(source, transport, transport.IsSecure, transport.IsAuthenticated), this.parent, transportBase);
				this.WriteReplyHeader(this.parent.settings.GetDefaultHeader(), true);
			}

			private static void OnReadHeaderComplete(TransportAsyncCallbackArgs args)
			{
				AmqpTransportListener.TransportHandler userToken = (AmqpTransportListener.TransportHandler)args.UserToken;
				if (args.Exception != null)
				{
					userToken.parent.OnHandleTransportComplete(args);
					return;
				}
				ByteBuffer byteBuffer = new ByteBuffer(userToken.buffer, 0, (int)userToken.buffer.Length);
				try
				{
					userToken.OnProtocolHeader(byteBuffer);
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					MessagingClientEtwProvider.TraceClient<AmqpTransportListener.TransportHandler, Exception>((AmqpTransportListener.TransportHandler source, Exception ex) => MessagingClientEtwProvider.Provider.EventWriteAmqpLogError(source, "OnProtocolHeader", ex.Message), userToken, exception);
					args.Exception = exception;
					userToken.parent.OnHandleTransportComplete(args);
				}
			}

			private static void OnTransportOpened(IAsyncResult result)
			{
				if (result.CompletedSynchronously)
				{
					return;
				}
				AmqpTransportListener.TransportHandler asyncState = (AmqpTransportListener.TransportHandler)result.AsyncState;
				try
				{
					asyncState.HandleTransportOpened(result);
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					MessagingClientEtwProvider.TraceClient<AmqpTransportListener.TransportHandler, Exception>((AmqpTransportListener.TransportHandler source, Exception ex) => MessagingClientEtwProvider.Provider.EventWriteAmqpLogError(source, "HandleTransportOpened", ex.Message), asyncState, exception);
					asyncState.args.Exception = exception;
					asyncState.parent.OnHandleTransportComplete(asyncState.args);
				}
			}

			private static void OnWriteHeaderComplete(TransportAsyncCallbackArgs args)
			{
				AmqpTransportListener.TransportHandler userToken = (AmqpTransportListener.TransportHandler)args.UserToken;
				if (args.Exception != null)
				{
					userToken.parent.OnHandleTransportComplete(args);
					return;
				}
				try
				{
					IAsyncResult asyncResult = userToken.args.Transport.BeginOpen(userToken.timeoutHelper.RemainingTime(), AmqpTransportListener.TransportHandler.onTransportOpened, userToken);
					if (asyncResult.CompletedSynchronously)
					{
						userToken.HandleTransportOpened(asyncResult);
					}
				}
				catch (Exception exception)
				{
					args.Exception = exception;
					userToken.parent.OnHandleTransportComplete(args);
				}
			}

			private void ReadProtocolHeader()
			{
				MessagingClientEtwProvider.TraceClient<AmqpTransportListener.TransportHandler>((AmqpTransportListener.TransportHandler source) => {
				}, this);
				this.args.SetBuffer(this.buffer, 0, (int)this.buffer.Length);
				this.args.CompletedCallback = AmqpTransportListener.TransportHandler.readCompleteCallback;
				this.bufferReader.ReadBuffer(this.args);
			}

			public static void SpawnHandler(AmqpTransportListener parent, TransportAsyncCallbackArgs args)
			{
				AmqpTransportListener.TransportHandler transportHandler = new AmqpTransportListener.TransportHandler(parent, args);
				ActionItem.Schedule(AmqpTransportListener.TransportHandler.startCallback, transportHandler);
			}

			private static void Start(object state)
			{
				((AmqpTransportListener.TransportHandler)state).ReadProtocolHeader();
			}

			public override string ToString()
			{
				return "tp-handler";
			}

			private void WriteReplyHeader(ProtocolHeader header, bool fail)
			{
				Action<TransportAsyncCallbackArgs> action;
				MessagingClientEtwProvider.TraceClient<AmqpTransportListener.TransportHandler, ProtocolHeader>((AmqpTransportListener.TransportHandler source, ProtocolHeader detail) => MessagingClientEtwProvider.Provider.EventWriteAmqpLogOperation(source, TraceOperation.Send, detail), this, header);
				header.Encode(new ByteBuffer(this.buffer));
				this.args.SetBuffer(this.buffer, 0, (int)this.buffer.Length);
				TransportAsyncCallbackArgs transportAsyncCallbackArg = this.args;
				if (fail)
				{
					action = null;
				}
				else
				{
					action = AmqpTransportListener.TransportHandler.writeCompleteCallback;
				}
				transportAsyncCallbackArg.CompletedCallback = action;
				this.bufferWriter.WriteBuffer(this.args);
				if (fail)
				{
					this.args.Exception = new NotSupportedException(header.ToString());
					this.parent.OnHandleTransportComplete(this.args);
				}
			}
		}
	}
}