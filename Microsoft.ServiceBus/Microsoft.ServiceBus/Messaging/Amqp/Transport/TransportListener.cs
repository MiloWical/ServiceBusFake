using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging.Amqp;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
	internal abstract class TransportListener : AmqpObject
	{
		private Action<object> notifyAccept;

		private Action<TransportAsyncCallbackArgs> acceptCallback;

		protected TransportListener(string type) : base(type)
		{
		}

		protected override void AbortInternal()
		{
		}

		protected override bool CloseInternal()
		{
			return true;
		}

		public void Listen(Action<TransportAsyncCallbackArgs> callback)
		{
			this.notifyAccept = new Action<object>(this.NotifyAccept);
			this.acceptCallback = callback;
			this.OnListen();
		}

		private void NotifyAccept(object state)
		{
			TransportAsyncCallbackArgs transportAsyncCallbackArg = (TransportAsyncCallbackArgs)state;
			this.acceptCallback(transportAsyncCallbackArg);
		}

		protected override void OnClose(TimeSpan timeout)
		{
			this.CloseInternal();
			base.State = AmqpObjectState.End;
		}

		protected abstract void OnListen();

		protected override void OnOpen(TimeSpan timeout)
		{
			base.State = AmqpObjectState.Opened;
		}

		protected void OnTransportAccepted(TransportAsyncCallbackArgs args)
		{
			if (!args.CompletedSynchronously)
			{
				this.NotifyAccept(args);
				return;
			}
			ActionItem.Schedule(this.notifyAccept, args);
		}

		protected override bool OpenInternal()
		{
			return true;
		}
	}
}