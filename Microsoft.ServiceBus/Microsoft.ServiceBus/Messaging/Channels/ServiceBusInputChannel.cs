using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Messaging.Channels
{
	internal sealed class ServiceBusInputChannel : ServiceBusInputChannelBase<IInputChannel>
	{
		public ServiceBusInputChannel(ServiceBusChannelListener<IInputChannel> parent) : base(parent)
		{
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			ServiceBusInputChannel.CreateAndOpenMessageReceiverAsyncResult createAndOpenMessageReceiverAsyncResult = new ServiceBusInputChannel.CreateAndOpenMessageReceiverAsyncResult(this, timeout, callback, state);
			createAndOpenMessageReceiverAsyncResult.Start();
			return createAndOpenMessageReceiverAsyncResult;
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			AsyncResult<ServiceBusInputChannel.CreateAndOpenMessageReceiverAsyncResult>.End(result);
		}

		protected override void OnFaulted()
		{
			base.OnFaulted();
			base.Abort();
		}

		protected override void OnOpen(TimeSpan timeout)
		{
			(new ServiceBusInputChannel.CreateAndOpenMessageReceiverAsyncResult(this, timeout, null, null)).RunSynchronously();
		}

		private class CreateAndOpenMessageReceiverAsyncResult : IteratorAsyncResult<ServiceBusInputChannel.CreateAndOpenMessageReceiverAsyncResult>
		{
			private readonly ServiceBusInputChannel inputChannel;

			private readonly ServiceBusChannelListener<IInputChannel> channelListener;

			private readonly MessagingFactory messagingFactory;

			private readonly string entityName;

			private readonly ReceiveMode receiveMode;

			private readonly int prefetchCount;

			public CreateAndOpenMessageReceiverAsyncResult(ServiceBusInputChannel inputChannel, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.inputChannel = inputChannel;
				this.channelListener = inputChannel.ChannelListener;
				this.messagingFactory = this.channelListener.MessagingFactory;
				this.entityName = this.channelListener.MessagingAddress.EntityName;
				this.receiveMode = this.channelListener.ReceiveMode;
				this.prefetchCount = this.channelListener.PrefetchCount;
			}

			protected override IEnumerator<IteratorAsyncResult<ServiceBusInputChannel.CreateAndOpenMessageReceiverAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				ServiceBusInputChannel.CreateAndOpenMessageReceiverAsyncResult createAndOpenMessageReceiverAsyncResult = this;
				IteratorAsyncResult<ServiceBusInputChannel.CreateAndOpenMessageReceiverAsyncResult>.BeginCall beginCall = (ServiceBusInputChannel.CreateAndOpenMessageReceiverAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.messagingFactory.BeginCreateMessageReceiver(thisPtr.entityName, thisPtr.receiveMode, t, c, s);
				yield return createAndOpenMessageReceiverAsyncResult.CallAsync(beginCall, (ServiceBusInputChannel.CreateAndOpenMessageReceiverAsyncResult thisPtr, IAsyncResult a) => thisPtr.inputChannel.MessageReceiver = thisPtr.messagingFactory.EndCreateMessageReceiver(a), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
				if (this.prefetchCount >= 0)
				{
					this.inputChannel.MessageReceiver.PrefetchCount = this.prefetchCount;
				}
				if (base.LastAsyncStepException == null)
				{
					ServiceBusInputChannel.CreateAndOpenMessageReceiverAsyncResult createAndOpenMessageReceiverAsyncResult1 = this;
					IteratorAsyncResult<ServiceBusInputChannel.CreateAndOpenMessageReceiverAsyncResult>.BeginCall beginCall1 = (ServiceBusInputChannel.CreateAndOpenMessageReceiverAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.inputChannel.MessageReceiver.BeginOpen(t, c, s);
					IteratorAsyncResult<ServiceBusInputChannel.CreateAndOpenMessageReceiverAsyncResult>.EndCall endCall = (ServiceBusInputChannel.CreateAndOpenMessageReceiverAsyncResult thisPtr, IAsyncResult a) => thisPtr.inputChannel.MessageReceiver.EndOpen(a);
					yield return createAndOpenMessageReceiverAsyncResult1.CallAsync(beginCall1, endCall, (ServiceBusInputChannel.CreateAndOpenMessageReceiverAsyncResult thisPtr, TimeSpan t) => thisPtr.inputChannel.MessageReceiver.Open(t), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
				}
				Exception lastAsyncStepException = base.LastAsyncStepException;
				if (lastAsyncStepException != null)
				{
					if (lastAsyncStepException is MessagingEntityNotFoundException)
					{
						this.channelListener.InternalFault();
						base.LastAsyncStepException = new EndpointNotFoundException(lastAsyncStepException.Message, lastAsyncStepException);
					}
					else if (lastAsyncStepException is UnauthorizedAccessException)
					{
						this.channelListener.InternalFault();
					}
					base.Complete(base.LastAsyncStepException);
				}
			}
		}
	}
}