using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging.Amqp;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
	internal sealed class TlsTransportInitiator : TransportInitiator
	{
		private readonly static AsyncCallback onTransportOpened;

		private TlsTransportSettings transportSettings;

		private TransportAsyncCallbackArgs callbackArgs;

		private TimeoutHelper timeoutHelper;

		static TlsTransportInitiator()
		{
			TlsTransportInitiator.onTransportOpened = new AsyncCallback(TlsTransportInitiator.OnTransportOpened);
		}

		public TlsTransportInitiator(TlsTransportSettings transportSettings)
		{
			this.transportSettings = transportSettings;
		}

		private void Complete()
		{
			if (this.callbackArgs.Exception != null && this.callbackArgs.Transport != null)
			{
				this.callbackArgs.Transport.SafeClose(this.callbackArgs.Exception);
				this.callbackArgs.Transport = null;
			}
			this.callbackArgs.CompletedCallback(this.callbackArgs);
		}

		public override bool ConnectAsync(TimeSpan timeout, TransportAsyncCallbackArgs callbackArgs)
		{
			this.callbackArgs = callbackArgs;
			this.timeoutHelper = new TimeoutHelper(timeout);
			TransportInitiator transportInitiator = this.transportSettings.InnerTransportSettings.CreateInitiator();
			TransportAsyncCallbackArgs transportAsyncCallbackArg = new TransportAsyncCallbackArgs()
			{
				CompletedCallback = new Action<TransportAsyncCallbackArgs>(TlsTransportInitiator.OnInnerTransportConnected),
				UserToken = this
			};
			if (transportInitiator.ConnectAsync(timeout, transportAsyncCallbackArg))
			{
				return true;
			}
			this.HandleInnerTransportConnected(transportAsyncCallbackArg);
			return !this.callbackArgs.CompletedSynchronously;
		}

		private void HandleInnerTransportConnected(TransportAsyncCallbackArgs innerArgs)
		{
			this.callbackArgs.CompletedSynchronously = innerArgs.CompletedSynchronously;
			if (innerArgs.Exception != null)
			{
				this.callbackArgs.Exception = innerArgs.Exception;
				this.Complete();
				return;
			}
			this.callbackArgs.Transport = new TlsTransport(innerArgs.Transport, this.transportSettings);
			try
			{
				IAsyncResult asyncResult = this.callbackArgs.Transport.BeginOpen(this.timeoutHelper.RemainingTime(), TlsTransportInitiator.onTransportOpened, this);
				if (asyncResult.CompletedSynchronously)
				{
					this.HandleTransportOpened(asyncResult);
					this.Complete();
				}
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				this.callbackArgs.Exception = exception;
				this.Complete();
			}
		}

		private void HandleTransportOpened(IAsyncResult result)
		{
			this.callbackArgs.Transport.EndOpen(result);
			if (this.callbackArgs.CompletedSynchronously)
			{
				this.callbackArgs.CompletedSynchronously = result.CompletedSynchronously;
			}
		}

		private static void OnInnerTransportConnected(TransportAsyncCallbackArgs innerArgs)
		{
			((TlsTransportInitiator)innerArgs.UserToken).HandleInnerTransportConnected(innerArgs);
		}

		private static void OnTransportOpened(IAsyncResult result)
		{
			if (result.CompletedSynchronously)
			{
				return;
			}
			TlsTransportInitiator asyncState = (TlsTransportInitiator)result.AsyncState;
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
				asyncState.callbackArgs.Exception = exception;
			}
			asyncState.Complete();
		}

		public override string ToString()
		{
			return "tls-initiator";
		}
	}
}