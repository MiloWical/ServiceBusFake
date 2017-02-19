using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.ServiceBus.Messaging.Amqp.Transaction
{
	internal sealed class Controller
	{
		private SendingAmqpLink sendLink;

		private long messageTag;

		public Controller()
		{
		}

		public IAsyncResult BeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.sendLink.BeginClose(timeout, callback, state);
		}

		public IAsyncResult BeginDeclare(TimeSpan timeout, AsyncCallback callback, object state)
		{
			MessagingClientEtwProvider.TraceClient<Controller>((Controller source) => MessagingClientEtwProvider.Provider.EventWriteAmqpLogOperation(source, TraceOperation.Execute, "BeginDeclare"), this);
			AmqpMessage amqpMessage = Controller.CreateCommandMessage(new Declare());
			return this.sendLink.BeginSendMessage(amqpMessage, this.GetDeliveryTag(), AmqpConstants.NullBinary, timeout, callback, state);
		}

		public IAsyncResult BeginDischange(ArraySegment<byte> txnId, bool fail, TimeSpan timeout, AsyncCallback callback, object state)
		{
			MessagingClientEtwProvider.TraceClient<Controller>((Controller source) => MessagingClientEtwProvider.Provider.EventWriteAmqpLogOperation(source, TraceOperation.Execute, "BeginDischange"), this);
			Discharge discharge = new Discharge()
			{
				TxnId = txnId,
				Fail = new bool?(fail)
			};
			AmqpMessage amqpMessage = Controller.CreateCommandMessage(discharge);
			return this.sendLink.BeginSendMessage(amqpMessage, this.GetDeliveryTag(), AmqpConstants.NullBinary, timeout, callback, state);
		}

		public IAsyncResult BeginOpen(AmqpSession session, TimeSpan timeout, AsyncCallback callback, object state)
		{
			string str = Guid.NewGuid().ToString("N");
			Source source = new Source()
			{
				Address = str,
				DistributionMode = DistributionMode.Move
			};
			Coordinator coordinator = new Coordinator();
			AmqpLinkSettings amqpLinkSetting = new AmqpLinkSettings()
			{
				Source = source,
				Target = coordinator,
				LinkName = str,
				Role = new bool?(false)
			};
			this.sendLink = new SendingAmqpLink(session, amqpLinkSetting);
			return this.sendLink.BeginOpen(timeout, callback, state);
		}

		public void Close(TimeSpan timeout)
		{
			this.EndClose(this.BeginClose(timeout, null, null));
		}

		public static AmqpMessage CreateCommandMessage(IAmqpSerializable command)
		{
			return AmqpMessage.Create(new AmqpValue()
			{
				Value = command
			});
		}

		public void EndClose(IAsyncResult result)
		{
			this.sendLink.EndClose(result);
			this.sendLink = null;
		}

		public ArraySegment<byte> EndDeclare(IAsyncResult result)
		{
			DeliveryState deliveryState = this.sendLink.EndSendMessage(result);
			this.ThrowIfRejected(deliveryState);
			MessagingClientEtwProvider.TraceClient<Controller>((Controller source) => MessagingClientEtwProvider.Provider.EventWriteAmqpLogOperation(source, TraceOperation.Execute, "EndDeclare"), this);
			return ((Declared)deliveryState).TxnId;
		}

		public void EndDischarge(IAsyncResult result)
		{
			this.ThrowIfRejected(this.sendLink.EndSendMessage(result));
			MessagingClientEtwProvider.TraceClient<Controller>((Controller source) => MessagingClientEtwProvider.Provider.EventWriteAmqpLogOperation(source, TraceOperation.Execute, "EndDischange"), this);
		}

		public void EndOpen(IAsyncResult result)
		{
			this.sendLink.EndOpen(result);
		}

		private ArraySegment<byte> GetDeliveryTag()
		{
			long num = Interlocked.Increment(ref this.messageTag);
			return new ArraySegment<byte>(BitConverter.GetBytes(num));
		}

		public void Open(AmqpSession session, TimeSpan timeout)
		{
			this.EndOpen(this.BeginOpen(session, timeout, null, null));
		}

		private void ThrowIfRejected(DeliveryState deliveryState)
		{
			if (deliveryState.DescriptorCode == Rejected.Code)
			{
				throw AmqpException.FromError(((Rejected)deliveryState).Error);
			}
		}

		public override string ToString()
		{
			return "controller";
		}
	}
}