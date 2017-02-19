using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Xml;

namespace Microsoft.ServiceBus.Messaging.Channels
{
	internal abstract class ServiceBusInputChannelBase<TChannel> : ChannelBase, IInputChannel, IChannel, ICommunicationObjectInternals, ICommunicationObject
	where TChannel : class, IChannel
	{
		private Microsoft.ServiceBus.Messaging.MessageReceiver messageReceiver;

		protected ServiceBusChannelListener<TChannel> ChannelListener
		{
			get;
			private set;
		}

		public EndpointAddress LocalAddress
		{
			get
			{
				return JustDecompileGenerated_get_LocalAddress();
			}
			set
			{
				JustDecompileGenerated_set_LocalAddress(value);
			}
		}

		private EndpointAddress JustDecompileGenerated_LocalAddress_k__BackingField;

		public EndpointAddress JustDecompileGenerated_get_LocalAddress()
		{
			return this.JustDecompileGenerated_LocalAddress_k__BackingField;
		}

		private void JustDecompileGenerated_set_LocalAddress(EndpointAddress value)
		{
			this.JustDecompileGenerated_LocalAddress_k__BackingField = value;
		}

		protected Microsoft.ServiceBus.Messaging.MessageReceiver MessageReceiver
		{
			get
			{
				return this.messageReceiver;
			}
			set
			{
				this.messageReceiver = value;
				value.SafeAddFaulted(new EventHandler(this.OnReceiverFaulted));
			}
		}

		public ServiceBusInputChannelBase(ServiceBusChannelListener<TChannel> channelListener) : base(channelListener)
		{
			this.ChannelListener = channelListener;
			this.LocalAddress = new EndpointAddress(channelListener.Uri, new AddressHeader[0]);
		}

		public IAsyncResult BeginReceive(TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult asyncResult;
			base.ThrowIfDisposedOrNotOpen();
			try
			{
				asyncResult = this.MessageReceiver.BeginReceive(timeout, callback, state);
			}
			catch (Exception exception)
			{
				this.OnException(exception);
				throw;
			}
			return asyncResult;
		}

		public IAsyncResult BeginReceive(AsyncCallback callback, object state)
		{
			return this.BeginReceive(base.DefaultReceiveTimeout, callback, state);
		}

		private Message ConvertToWcfMessage(BrokeredMessage brokeredMessage)
		{
			MessageEncoder encoder = this.ChannelListener.MessageEncoderFactory.Encoder;
			Message uri = encoder.ReadMessage(brokeredMessage.BodyStream, this.ChannelListener.MaxBufferSize);
			BrokeredMessageProperty.AddPropertyToWcfMessage(brokeredMessage, uri);
			if (this.ChannelListener.ReceiveContextEnabled)
			{
				ServiceBusInputChannelBase<TChannel>.AfmsReceiveContext.ApplyTo(uri, this.messageReceiver, brokeredMessage.LockToken);
			}
			uri.Properties.Via = this.LocalAddress.Uri;
			return uri;
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
			Microsoft.ServiceBus.Messaging.MessageReceiver messageReceiver = this.messageReceiver;
			if (messageReceiver != null)
			{
				messageReceiver.Abort();
			}
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult closeEntityCollectionAsyncResult;
			try
			{
				ClientEntity[] clientEntityArray = new ClientEntity[] { this.messageReceiver };
				closeEntityCollectionAsyncResult = new CloseEntityCollectionAsyncResult(clientEntityArray, timeout, callback, state);
			}
			catch (Exception exception)
			{
				this.OnException(exception);
				throw;
			}
			return closeEntityCollectionAsyncResult;
		}

		protected virtual IAsyncResult OnBeginTryReceive(TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult asyncResult;
			try
			{
				asyncResult = this.MessageReceiver.BeginTryReceive((timeout == TimeSpan.MaxValue ? base.DefaultReceiveTimeout : timeout), callback, state);
			}
			catch (Exception exception)
			{
				this.OnException(exception);
				throw;
			}
			return asyncResult;
		}

		protected override void OnClose(TimeSpan timeout)
		{
			try
			{
				Microsoft.ServiceBus.Messaging.MessageReceiver messageReceiver = this.messageReceiver;
				if (messageReceiver != null)
				{
					messageReceiver.Close(timeout);
				}
			}
			catch (Exception exception)
			{
				this.OnException(exception);
				throw;
			}
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			try
			{
				CloseEntityCollectionAsyncResult.End(result);
			}
			catch (Exception exception)
			{
				this.OnException(exception);
				throw;
			}
		}

		protected virtual bool OnEndTryReceive(IAsyncResult result, out BrokeredMessage brokeredMessage)
		{
			bool flag;
			try
			{
				flag = this.MessageReceiver.EndTryReceive(result, out brokeredMessage);
			}
			catch (Exception exception)
			{
				this.OnException(exception);
				throw;
			}
			return flag;
		}

		private void OnException(Exception exception)
		{
			bool flag;
			if (Fx.IsFatal(exception))
			{
				return;
			}
			if (exception is MessagingEntityNotFoundException)
			{
				base.Fault();
				this.ChannelListener.InternalFault();
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new EndpointNotFoundException(exception.Message, exception), null);
			}
			if (exception is OperationCanceledException)
			{
				MessagingException innerException = exception.InnerException as MessagingException;
				if (innerException != null)
				{
					MessagingExceptionHelper.ConvertToCommunicationException(innerException, out flag);
					if (flag)
					{
						base.Fault();
						this.ChannelListener.InternalFault();
					}
				}
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new CommunicationObjectAbortedException(SRClient.EntityClosedOrAborted, exception.InnerException ?? exception), null);
			}
			if (exception is UnauthorizedAccessException)
			{
				base.Fault();
				this.ChannelListener.InternalFault();
				return;
			}
			MessagingException messagingException = exception as MessagingException;
			MessagingException messagingException1 = messagingException;
			if (messagingException != null)
			{
				CommunicationException communicationException = MessagingExceptionHelper.ConvertToCommunicationException(messagingException1);
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(communicationException, null);
			}
			if (exception is InvalidOperationException)
			{
				base.Fault();
				this.ChannelListener.InternalFault();
			}
		}

		private void OnReceiverFaulted(object sender, EventArgs args)
		{
			MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteChannelFaulted(this.GetCommunicationObjectType().ToString()));
			base.Fault();
		}

		protected virtual bool OnTryReceive(TimeSpan timeout, out BrokeredMessage brokeredMessage)
		{
			bool flag;
			try
			{
				flag = this.MessageReceiver.TryReceive(timeout, out brokeredMessage);
			}
			catch (Exception exception)
			{
				this.OnException(exception);
				throw;
			}
			return flag;
		}

		public Message Receive(TimeSpan timeout)
		{
			Message message;
			base.ThrowIfDisposedOrNotOpen();
			try
			{
				BrokeredMessage brokeredMessage = this.MessageReceiver.Receive(timeout);
				if (brokeredMessage == null)
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new TimeoutException(), null);
				}
				Message wcfMessage = this.ConvertToWcfMessage(brokeredMessage);
				this.TraceReceivedMessage(wcfMessage);
				message = wcfMessage;
			}
			catch (Exception exception)
			{
				this.OnException(exception);
				throw;
			}
			return message;
		}

		public Message Receive()
		{
			return this.Receive(base.DefaultReceiveTimeout);
		}

		IAsyncResult System.ServiceModel.Channels.IInputChannel.BeginTryReceive(TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult doneAsyncResult;
			if (this.DoneReceivingInCurrentState())
			{
				return new DoneAsyncResult(true, callback, state);
			}
			try
			{
				doneAsyncResult = this.OnBeginTryReceive(timeout, callback, state);
			}
			catch (OperationCanceledException operationCanceledException)
			{
				doneAsyncResult = new DoneAsyncResult(true, callback, state);
			}
			return doneAsyncResult;
		}

		IAsyncResult System.ServiceModel.Channels.IInputChannel.BeginWaitForMessage(TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new NotSupportedException(), null);
		}

		Message System.ServiceModel.Channels.IInputChannel.EndReceive(IAsyncResult result)
		{
			Message message;
			try
			{
				BrokeredMessage brokeredMessage = this.MessageReceiver.EndReceive(result);
				if (brokeredMessage == null)
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new TimeoutException(), null);
				}
				Message wcfMessage = this.ConvertToWcfMessage(brokeredMessage);
				this.TraceReceivedMessage(wcfMessage);
				message = wcfMessage;
			}
			catch (Exception exception)
			{
				this.OnException(exception);
				throw;
			}
			return message;
		}

		bool System.ServiceModel.Channels.IInputChannel.EndTryReceive(IAsyncResult result, out Message wcfMessage)
		{
			BrokeredMessage brokeredMessage;
			if (result is DoneAsyncResult)
			{
				wcfMessage = null;
				return CompletedAsyncResult<bool>.End(result);
			}
			bool flag = this.OnEndTryReceive(result, out brokeredMessage);
			if (brokeredMessage == null)
			{
				wcfMessage = null;
			}
			else
			{
				wcfMessage = this.ConvertToWcfMessage(brokeredMessage);
				this.TraceReceivedMessage(wcfMessage);
			}
			return flag;
		}

		bool System.ServiceModel.Channels.IInputChannel.EndWaitForMessage(IAsyncResult result)
		{
			throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new NotSupportedException(), null);
		}

		bool System.ServiceModel.Channels.IInputChannel.TryReceive(TimeSpan timeout, out Message wcfMessage)
		{
			BrokeredMessage brokeredMessage;
			if (this.DoneReceivingInCurrentState())
			{
				wcfMessage = null;
				return true;
			}
			bool flag = this.OnTryReceive(timeout, out brokeredMessage);
			if (brokeredMessage == null)
			{
				wcfMessage = null;
			}
			else
			{
				wcfMessage = this.ConvertToWcfMessage(brokeredMessage);
				this.TraceReceivedMessage(wcfMessage);
			}
			return flag;
		}

		private void TraceReceivedMessage(Message wcfMessage)
		{
			string empty = string.Empty;
			MessageHeaders headers = wcfMessage.Headers;
			if (headers.MessageVersion.Addressing != AddressingVersion.None && headers.MessageId != null)
			{
				headers.MessageId.ToString();
			}
			MessagingClientEtwProvider.TraceClient(() => {
			});
		}

		public bool WaitForMessage(TimeSpan timeout)
		{
			throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new NotSupportedException(), null);
		}

		private sealed class AfmsReceiveContext : System.ServiceModel.Channels.ReceiveContext
		{
			private Guid lockToken;

			private Microsoft.ServiceBus.Messaging.MessageReceiver receiver;

			private AfmsReceiveContext()
			{
			}

			public static ServiceBusInputChannelBase<TChannel>.AfmsReceiveContext ApplyTo(Message message, Microsoft.ServiceBus.Messaging.MessageReceiver receiver, Guid lockToken)
			{
				ServiceBusInputChannelBase<TChannel>.AfmsReceiveContext afmsReceiveContext = new ServiceBusInputChannelBase<TChannel>.AfmsReceiveContext()
				{
					receiver = receiver,
					lockToken = lockToken
				};
				message.Properties.Add(System.ServiceModel.Channels.ReceiveContext.Name, afmsReceiveContext);
				return afmsReceiveContext;
			}

			protected override void OnAbandon(TimeSpan timeout)
			{
				this.TraceAbandon();
				this.receiver.Abandon(new Guid[] { this.lockToken }, timeout);
			}

			protected override IAsyncResult OnBeginAbandon(TimeSpan timeout, AsyncCallback callback, object state)
			{
				this.TraceAbandon();
				Microsoft.ServiceBus.Messaging.MessageReceiver messageReceiver = this.receiver;
				Guid[] guidArray = new Guid[] { this.lockToken };
				return messageReceiver.BeginAbandon(guidArray, timeout, callback, state);
			}

			protected override IAsyncResult OnBeginComplete(TimeSpan timeout, AsyncCallback callback, object state)
			{
				this.TraceComplete();
				Microsoft.ServiceBus.Messaging.MessageReceiver messageReceiver = this.receiver;
				Guid[] guidArray = new Guid[] { this.lockToken };
				return messageReceiver.BeginComplete(guidArray, timeout, callback, state);
			}

			protected override void OnComplete(TimeSpan timeout)
			{
				this.TraceComplete();
				this.receiver.Complete(new Guid[] { this.lockToken }, timeout);
			}

			protected override void OnEndAbandon(IAsyncResult result)
			{
				this.receiver.EndAbandon(result);
			}

			protected override void OnEndComplete(IAsyncResult result)
			{
				this.receiver.EndComplete(result);
			}

			private void TraceAbandon()
			{
				MessagingClientEtwProvider.TraceClient(() => {
				});
			}

			private void TraceComplete()
			{
				MessagingClientEtwProvider.TraceClient(() => {
				});
			}
		}
	}
}