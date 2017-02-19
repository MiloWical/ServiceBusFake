using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
	internal sealed class AmqpTransportInitiator : TransportInitiator
	{
		private AmqpSettings settings;

		private TransportSettings transportSettings;

		private AsyncIO.AsyncBufferWriter writer;

		private AsyncIO.AsyncBufferReader reader;

		private TimeoutHelper timeoutHelper;

		private int providerIndex;

		private ProtocolHeader sentHeader;

		public AmqpTransportInitiator(AmqpSettings settings, TransportSettings transportSettings)
		{
			settings.ValidateInitiatorSettings();
			this.settings = settings;
			this.transportSettings = transportSettings;
		}

		public IAsyncResult BeginConnect(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new AmqpTransportInitiator.ConnectAsyncResult(this, timeout, callback, state);
		}

		private void Complete(TransportAsyncCallbackArgs args)
		{
			if (args.Exception != null && args.Transport != null)
			{
				args.Transport.SafeClose(args.Exception);
				args.Transport = null;
			}
			TransportAsyncCallbackArgs userToken = (TransportAsyncCallbackArgs)args.UserToken;
			userToken.Transport = args.Transport;
			userToken.Exception = args.Exception;
			userToken.CompletedCallback(userToken);
		}

		public override bool ConnectAsync(TimeSpan timeout, TransportAsyncCallbackArgs callbackArgs)
		{
			MessagingClientEtwProvider.TraceClient<AmqpTransportInitiator>((AmqpTransportInitiator source) => MessagingClientEtwProvider.Provider.EventWriteAmqpLogOperation(source, TraceOperation.Connect, source.transportSettings), this);
			TransportInitiator transportInitiator = this.transportSettings.CreateInitiator();
			TransportAsyncCallbackArgs transportAsyncCallbackArg = new TransportAsyncCallbackArgs()
			{
				CompletedCallback = new Action<TransportAsyncCallbackArgs>(this.OnConnectComplete),
				UserToken = callbackArgs
			};
			callbackArgs.CompletedSynchronously = false;
			this.timeoutHelper = new TimeoutHelper(timeout);
			transportInitiator.ConnectAsync(timeout, transportAsyncCallbackArg);
			return true;
		}

		internal Task<TransportBase> ConnectTaskAsync(TimeSpan timeout)
		{
			TaskCompletionSource<TransportBase> taskCompletionSource = new TaskCompletionSource<TransportBase>();
			TransportAsyncCallbackArgs transportAsyncCallbackArg = new TransportAsyncCallbackArgs()
			{
				CompletedCallback = (TransportAsyncCallbackArgs a) => {
					if (a.Exception != null)
					{
						taskCompletionSource.SetException(a.Exception);
						return;
					}
					taskCompletionSource.SetResult(a.Transport);
				}
			};
			this.ConnectAsync(timeout, transportAsyncCallbackArg);
			return taskCompletionSource.Task;
		}

		public TransportBase EndConnect(IAsyncResult result)
		{
			return AmqpTransportInitiator.ConnectAsyncResult.End(result);
		}

		private void HandleTransportOpened(IAsyncResult result)
		{
			TransportAsyncCallbackArgs asyncState = (TransportAsyncCallbackArgs)result.AsyncState;
			asyncState.Transport.EndOpen(result);
			AmqpTransportInitiator amqpTransportInitiator = this;
			amqpTransportInitiator.providerIndex = amqpTransportInitiator.providerIndex + 1;
			if (this.providerIndex == this.settings.TransportProviders.Count || this.settings.TransportProviders[this.providerIndex].ProtocolId == ProtocolId.Amqp)
			{
				this.writer = null;
				this.reader = null;
				this.providerIndex = 0;
				this.Complete(asyncState);
				return;
			}
			this.writer = new AsyncIO.AsyncBufferWriter(asyncState.Transport);
			this.reader = new AsyncIO.AsyncBufferReader(asyncState.Transport);
			this.WriteSecurityHeader(asyncState);
		}

		private void OnConnectComplete(TransportAsyncCallbackArgs args)
		{
			if (args.Exception != null)
			{
				this.Complete(args);
				return;
			}
			if (this.settings.TransportProviders[this.providerIndex].ProtocolId == ProtocolId.Amqp)
			{
				this.Complete(args);
				return;
			}
			this.writer = new AsyncIO.AsyncBufferWriter(args.Transport);
			this.reader = new AsyncIO.AsyncBufferReader(args.Transport);
			this.WriteSecurityHeader(args);
		}

		private void OnReadHeaderComplete(TransportAsyncCallbackArgs args)
		{
			if (args.Exception != null)
			{
				MessagingClientEtwProvider.TraceClient<AmqpTransportInitiator, Exception>((AmqpTransportInitiator source, Exception ex) => MessagingClientEtwProvider.Provider.EventWriteAmqpLogError(source, "ReadHeader", ex.Message), this, args.Exception);
				this.Complete(args);
				return;
			}
			try
			{
				ProtocolHeader protocolHeader = new ProtocolHeader();
				protocolHeader.Decode(new ByteBuffer(args.Buffer, args.Offset, args.Count));
				if (!protocolHeader.Equals(this.sentHeader))
				{
					throw new AmqpException(AmqpError.NotImplemented, SRAmqp.AmqpProtocolVersionNotSupported(this.sentHeader, protocolHeader));
				}
				TransportBase transportBase = this.settings.TransportProviders[this.providerIndex].CreateTransport(args.Transport, true);
				MessagingClientEtwProvider.TraceClient<AmqpTransportInitiator, TransportBase, TransportBase>((AmqpTransportInitiator source, TransportBase from, TransportBase to) => MessagingClientEtwProvider.Provider.EventWriteAmqpUpgradeTransport(source, from, to), this, args.Transport, transportBase);
				args.Transport = transportBase;
				IAsyncResult asyncResult = args.Transport.BeginOpen(this.timeoutHelper.RemainingTime(), new AsyncCallback(this.OnTransportOpenCompete), args);
				if (asyncResult.CompletedSynchronously)
				{
					this.HandleTransportOpened(asyncResult);
				}
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				MessagingClientEtwProvider.TraceClient<AmqpTransportInitiator, Exception>((AmqpTransportInitiator source, Exception ex) => MessagingClientEtwProvider.Provider.EventWriteAmqpLogError(source, "OnProtocolHeader", ex.Message), this, exception);
				args.Exception = exception;
				this.Complete(args);
			}
		}

		private void OnTransportOpenCompete(IAsyncResult result)
		{
			if (result.CompletedSynchronously)
			{
				return;
			}
			try
			{
				this.HandleTransportOpened(result);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				TransportAsyncCallbackArgs asyncState = (TransportAsyncCallbackArgs)result.AsyncState;
				asyncState.Exception = exception;
				this.Complete(asyncState);
			}
		}

		private void OnWriteHeaderComplete(TransportAsyncCallbackArgs args)
		{
			if (args.Exception != null)
			{
				this.Complete(args);
				return;
			}
			MessagingClientEtwProvider.TraceClient<AmqpTransportInitiator>((AmqpTransportInitiator source) => MessagingClientEtwProvider.Provider.EventWriteAmqpLogOperation(source, TraceOperation.Execute, "ReadHeader"), this);
			byte[] numArray = new byte[8];
			args.SetBuffer(numArray, 0, (int)numArray.Length);
			args.CompletedCallback = new Action<TransportAsyncCallbackArgs>(this.OnReadHeaderComplete);
			this.reader.ReadBuffer(args);
		}

		public override string ToString()
		{
			return "tp-initiator";
		}

		private void WriteSecurityHeader(TransportAsyncCallbackArgs args)
		{
			TransportProvider item = this.settings.TransportProviders[this.providerIndex];
			this.sentHeader = new ProtocolHeader(item.ProtocolId, item.DefaultVersion);
			ByteBuffer byteBuffer = new ByteBuffer(new byte[8]);
			this.sentHeader.Encode(byteBuffer);
			args.SetBuffer(byteBuffer.Buffer, byteBuffer.Offset, byteBuffer.Length);
			args.CompletedCallback = new Action<TransportAsyncCallbackArgs>(this.OnWriteHeaderComplete);
			this.writer.WriteBuffer(args);
		}

		private sealed class ConnectAsyncResult : AsyncResult
		{
			private static Action<TransportAsyncCallbackArgs> onConnect;

			private TransportBase transport;

			static ConnectAsyncResult()
			{
				AmqpTransportInitiator.ConnectAsyncResult.onConnect = new Action<TransportAsyncCallbackArgs>(AmqpTransportInitiator.ConnectAsyncResult.OnConnect);
			}

			public ConnectAsyncResult(AmqpTransportInitiator initiator, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				TransportAsyncCallbackArgs transportAsyncCallbackArg = new TransportAsyncCallbackArgs()
				{
					CompletedCallback = AmqpTransportInitiator.ConnectAsyncResult.onConnect,
					UserToken = this
				};
				if (!initiator.ConnectAsync(timeout, transportAsyncCallbackArg))
				{
					AmqpTransportInitiator.ConnectAsyncResult.OnConnect(transportAsyncCallbackArg);
				}
			}

			public static new TransportBase End(IAsyncResult result)
			{
				return AsyncResult.End<AmqpTransportInitiator.ConnectAsyncResult>(result).transport;
			}

			private static void OnConnect(TransportAsyncCallbackArgs args)
			{
				AmqpTransportInitiator.ConnectAsyncResult userToken = (AmqpTransportInitiator.ConnectAsyncResult)args.UserToken;
				if (args.Exception != null)
				{
					userToken.Complete(args.CompletedSynchronously, args.Exception);
					return;
				}
				userToken.transport = args.Transport;
				userToken.Complete(args.CompletedSynchronously);
			}
		}
	}
}