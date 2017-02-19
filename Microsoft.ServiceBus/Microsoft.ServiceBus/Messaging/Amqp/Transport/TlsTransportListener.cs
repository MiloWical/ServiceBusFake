using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
	internal sealed class TlsTransportListener : TransportListener
	{
		private readonly AsyncCallback onTransportOpened;

		private readonly TlsTransportSettings transportSettings;

		private TransportListener innerListener;

		public TlsTransportListener(TlsTransportSettings transportSettings) : base("tls-listener")
		{
			this.transportSettings = transportSettings;
			this.onTransportOpened = new AsyncCallback(this.OnTransportOpened);
		}

		protected override void AbortInternal()
		{
			if (this.innerListener != null)
			{
				this.innerListener.Abort();
			}
		}

		protected override bool CloseInternal()
		{
			if (this.innerListener != null)
			{
				this.innerListener.Close();
			}
			return true;
		}

		private void HandleTransportOpened(IAsyncResult result)
		{
			TransportAsyncCallbackArgs asyncState = (TransportAsyncCallbackArgs)result.AsyncState;
			asyncState.Transport.EndOpen(result);
			if (asyncState.CompletedSynchronously)
			{
				asyncState.CompletedSynchronously = result.CompletedSynchronously;
			}
			base.OnTransportAccepted(asyncState);
		}

		private void OnAcceptInnerTransport(TransportAsyncCallbackArgs innerArgs)
		{
			MessagingClientEtwProvider.TraceClient<TlsTransportListener, TransportBase>((TlsTransportListener source, TransportBase detail) => MessagingClientEtwProvider.Provider.EventWriteAmqpLogOperation(source, TraceOperation.Accept, detail), this, innerArgs.Transport);
			try
			{
				innerArgs.Transport = new TlsTransport(innerArgs.Transport, this.transportSettings);
				IAsyncResult asyncResult = innerArgs.Transport.BeginOpen(innerArgs.Transport.DefaultOpenTimeout, this.onTransportOpened, innerArgs);
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
				innerArgs.Transport.SafeClose(exception);
			}
		}

		private void OnInnerListenerClosed(object sender, EventArgs e)
		{
			if (!base.IsClosing())
			{
				base.SafeClose(((TransportListener)sender).TerminalException);
			}
		}

		protected override void OnListen()
		{
			this.innerListener = this.transportSettings.InnerTransportSettings.CreateListener();
			this.innerListener.Closed += new EventHandler(this.OnInnerListenerClosed);
			this.innerListener.Listen(new Action<TransportAsyncCallbackArgs>(this.OnAcceptInnerTransport));
		}

		private void OnTransportOpened(IAsyncResult result)
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
				((TransportAsyncCallbackArgs)result.AsyncState).Transport.SafeClose(exception);
			}
		}
	}
}