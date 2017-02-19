using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Xml;

namespace Microsoft.ServiceBus.Messaging.Channels
{
	internal sealed class ServiceBusOutputChannel : ChannelBase, IOutputChannel, IChannel, ICommunicationObject
	{
		private MessageEncoder encoder;

		private int maxBufferSize;

		private MessagingAddress messagingAddress;

		private Microsoft.ServiceBus.Messaging.MessageSender messageSender;

		private EndpointAddress to;

		private MessagingFactory messagingFactory;

		private MessagingFactorySettings messagingFactorySettings;

		private ServiceBusChannelFactory ChannelFactory
		{
			get;
			set;
		}

		private Microsoft.ServiceBus.Messaging.MessageSender MessageSender
		{
			get
			{
				return this.messageSender;
			}
			set
			{
				this.messageSender = value;
				this.messageSender.SafeAddFaulted(new EventHandler(this.OnMessageSenderFaulted));
			}
		}

		public EndpointAddress RemoteAddress
		{
			get
			{
				return this.to;
			}
		}

		public Uri Via
		{
			get
			{
				return JustDecompileGenerated_get_Via();
			}
			set
			{
				JustDecompileGenerated_set_Via(value);
			}
		}

		private Uri JustDecompileGenerated_Via_k__BackingField;

		public Uri JustDecompileGenerated_get_Via()
		{
			return this.JustDecompileGenerated_Via_k__BackingField;
		}

		private void JustDecompileGenerated_set_Via(Uri value)
		{
			this.JustDecompileGenerated_Via_k__BackingField = value;
		}

		public ServiceBusOutputChannel(ServiceBusChannelFactory factory, EndpointAddress to, Uri via) : base(factory)
		{
			this.ChannelFactory = factory;
			this.encoder = factory.MessageEncoderFactory.Encoder;
			this.maxBufferSize = factory.MaxBufferSize;
			this.to = to;
			this.Via = via;
			this.messagingFactorySettings = factory.MessagingFactorySettings;
			this.messagingAddress = new MessagingAddress(via, this.messagingFactorySettings.NetMessagingTransportSettings.GatewayMode);
			if (this.messagingAddress.Type != MessagingAddressType.Entity)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.Argument("via", SRClient.EntityNameNotFound(via));
			}
		}

		private void AddHeadersTo(Message wcfMessage)
		{
			if (!this.ChannelFactory.ManualAddressing && this.to != null)
			{
				this.to.ApplyTo(wcfMessage);
			}
		}

		public IAsyncResult BeginSend(Message message, TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (message == null)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("message");
			}
			TimeoutHelper.ThrowIfNegativeArgument(timeout);
			base.ThrowIfDisposedOrNotOpen();
			return new ServiceBusOutputChannel.SendAsyncResult(this, message, timeout, callback, state);
		}

		public IAsyncResult BeginSend(Message message, AsyncCallback callback, object state)
		{
			return this.BeginSend(message, base.DefaultSendTimeout, callback, state);
		}

		private BrokeredMessage ConvertToBrokerMessage(Message wcfMessage, out byte[] bufferToReturn)
		{
			BrokeredMessage brokeredMessage;
			BrokeredMessageProperty brokeredMessageProperty;
			BrokeredMessage brokeredMessage1;
			bufferToReturn = null;
			if (!BrokeredMessageProperty.TryGet(wcfMessage.Properties, out brokeredMessageProperty))
			{
				brokeredMessage = new BrokeredMessage();
				EndpointAddress replyTo = wcfMessage.Headers.ReplyTo;
				if (replyTo != null)
				{
					brokeredMessage.ReplyTo = replyTo.Uri.AbsoluteUri;
				}
			}
			else
			{
				brokeredMessage = brokeredMessageProperty.Message;
			}
			byte[] array = null;
			try
			{
				ArraySegment<byte> nums = this.encoder.WriteMessage(wcfMessage, this.maxBufferSize, this.ChannelFactory.BufferManager);
				array = nums.Array;
				MemoryStream memoryStream = new MemoryStream(nums.Array, nums.Offset, nums.Count, true, true);
				brokeredMessage.BodyStream = memoryStream;
				brokeredMessage.ContentType = this.encoder.ContentType;
				bufferToReturn = array;
				array = null;
				brokeredMessage1 = brokeredMessage;
			}
			catch (Exception exception)
			{
				if (array != null)
				{
					this.ChannelFactory.BufferManager.ReturnBuffer(array);
				}
				throw;
			}
			return brokeredMessage1;
		}

		public void EndSend(IAsyncResult result)
		{
			ServiceBusOutputChannel.SendAsyncResult.End(result);
		}

		public sealed override T GetProperty<T>()
		where T : class
		{
			if (typeof(T) == typeof(IOutputChannel))
			{
				return (T)this;
			}
			return base.GetProperty<T>();
		}

		protected sealed override void OnAbort()
		{
			if (this.messageSender != null)
			{
				this.messageSender.Abort();
			}
			if (this.messagingFactory != null)
			{
				this.messagingFactory.Abort();
			}
		}

		protected sealed override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			List<MessageClientEntity> messageClientEntities = new List<MessageClientEntity>();
			if (this.messageSender != null)
			{
				messageClientEntities.Add(this.messageSender);
			}
			if (this.messagingFactory != null)
			{
				messageClientEntities.Add(this.messagingFactory);
			}
			return new CloseEntityCollectionIteratedAsyncResult(messageClientEntities, timeout, callback, state);
		}

		protected sealed override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new ServiceBusOutputChannel.OpenMessagingFactoryAndMessageSenderAsyncResult(this, timeout, callback, state);
		}

		protected sealed override void OnClose(TimeSpan timeout)
		{
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			if (this.messageSender != null)
			{
				this.messageSender.Close(timeoutHelper.RemainingTime());
			}
			if (this.messagingFactory != null)
			{
				this.messagingFactory.Close(timeoutHelper.RemainingTime());
			}
		}

		protected sealed override void OnEndClose(IAsyncResult result)
		{
			AsyncResult<CloseEntityCollectionIteratedAsyncResult>.End(result);
		}

		protected sealed override void OnEndOpen(IAsyncResult result)
		{
			this.MessageSender = ServiceBusOutputChannel.OpenMessagingFactoryAndMessageSenderAsyncResult.End(result);
		}

		private void OnMessageSenderFaulted(object sender, EventArgs args)
		{
			MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteChannelFaulted(this.GetCommunicationObjectType().ToString()));
			base.Fault();
		}

		protected sealed override void OnOpen(TimeSpan timeout)
		{
			ServiceBusOutputChannel.OpenMessagingFactoryAndMessageSenderAsyncResult openMessagingFactoryAndMessageSenderAsyncResult = new ServiceBusOutputChannel.OpenMessagingFactoryAndMessageSenderAsyncResult(this, timeout, null, null);
			this.MessageSender = ServiceBusOutputChannel.OpenMessagingFactoryAndMessageSenderAsyncResult.End(openMessagingFactoryAndMessageSenderAsyncResult);
		}

		public void Send(Message message, TimeSpan timeout)
		{
			bool flag;
			bool flag1;
			if (message == null)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("message");
			}
			TimeoutHelper.ThrowIfNegativeArgument(timeout);
			base.ThrowIfDisposedOrNotOpen();
			this.AddHeadersTo(message);
			this.TraceSendingMessage(message);
			byte[] numArray = null;
			BrokeredMessage brokerMessage = null;
			try
			{
				try
				{
					brokerMessage = this.ConvertToBrokerMessage(message, out numArray);
					this.MessageSender.Send(new BrokeredMessage[] { brokerMessage }, timeout);
				}
				catch (MessagingException messagingException)
				{
					CommunicationException communicationException = MessagingExceptionHelper.ConvertToCommunicationException(messagingException, out flag);
					if (flag)
					{
						base.Fault();
					}
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(communicationException, null);
				}
				catch (OperationCanceledException operationCanceledException1)
				{
					OperationCanceledException operationCanceledException = operationCanceledException1;
					MessagingException innerException = operationCanceledException.InnerException as MessagingException;
					if (innerException != null)
					{
						MessagingExceptionHelper.ConvertToCommunicationException(innerException, out flag1);
						if (flag1)
						{
							base.Fault();
						}
					}
					ExceptionTrace exception = Microsoft.ServiceBus.Messaging.FxTrace.Exception;
					string entityClosedOrAborted = SRClient.EntityClosedOrAborted;
					Exception innerException1 = operationCanceledException.InnerException;
					if (innerException1 == null)
					{
						innerException1 = operationCanceledException;
					}
					throw exception.AsError(new CommunicationObjectAbortedException(entityClosedOrAborted, innerException1), null);
				}
			}
			finally
			{
				if (brokerMessage != null && numArray != null)
				{
					this.ChannelFactory.BufferManager.ReturnBuffer(numArray);
				}
			}
		}

		public void Send(Message message)
		{
			this.Send(message, base.DefaultSendTimeout);
		}

		private void TraceSendingMessage(Message wcfMessage)
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

		private sealed class OpenMessagingFactoryAndMessageSenderAsyncResult : AsyncResult
		{
			private readonly static AsyncResult.AsyncCompletion createFactoryComplete;

			private readonly static AsyncResult.AsyncCompletion createSenderComplete;

			private readonly static AsyncResult.AsyncCompletion openSenderComplete;

			private TimeoutHelper timeoutHelper;

			private ServiceBusOutputChannel outputChannel;

			private Microsoft.ServiceBus.Messaging.MessageSender messageSender;

			static OpenMessagingFactoryAndMessageSenderAsyncResult()
			{
				ServiceBusOutputChannel.OpenMessagingFactoryAndMessageSenderAsyncResult.createFactoryComplete = new AsyncResult.AsyncCompletion(ServiceBusOutputChannel.OpenMessagingFactoryAndMessageSenderAsyncResult.CreateFactoryComplete);
				ServiceBusOutputChannel.OpenMessagingFactoryAndMessageSenderAsyncResult.createSenderComplete = new AsyncResult.AsyncCompletion(ServiceBusOutputChannel.OpenMessagingFactoryAndMessageSenderAsyncResult.CreateSenderComplete);
				ServiceBusOutputChannel.OpenMessagingFactoryAndMessageSenderAsyncResult.openSenderComplete = new AsyncResult.AsyncCompletion(ServiceBusOutputChannel.OpenMessagingFactoryAndMessageSenderAsyncResult.OpenSenderComplete);
			}

			public OpenMessagingFactoryAndMessageSenderAsyncResult(ServiceBusOutputChannel outputChannel, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				this.timeoutHelper = new TimeoutHelper(timeout);
				this.outputChannel = outputChannel;
				try
				{
					if (base.SyncContinue(MessagingFactory.BeginCreate(this.outputChannel.messagingAddress.ResourceAddress, this.outputChannel.messagingFactorySettings, this.timeoutHelper.RemainingTime(), base.PrepareAsyncCompletion(ServiceBusOutputChannel.OpenMessagingFactoryAndMessageSenderAsyncResult.createFactoryComplete), this)))
					{
						base.Complete(true);
					}
				}
				catch (MessagingException messagingException1)
				{
					MessagingException messagingException = messagingException1;
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(MessagingExceptionHelper.ConvertToCommunicationException(messagingException), null);
				}
			}

			private static bool CreateFactoryComplete(IAsyncResult result)
			{
				ServiceBusOutputChannel.OpenMessagingFactoryAndMessageSenderAsyncResult asyncState = (ServiceBusOutputChannel.OpenMessagingFactoryAndMessageSenderAsyncResult)result.AsyncState;
				asyncState.outputChannel.messagingFactory = MessagingFactory.EndCreate(result);
				IAsyncResult asyncResult = asyncState.outputChannel.messagingFactory.BeginCreateMessageSender(asyncState.outputChannel.messagingAddress.EntityName, asyncState.timeoutHelper.RemainingTime(), asyncState.PrepareAsyncCompletion(ServiceBusOutputChannel.OpenMessagingFactoryAndMessageSenderAsyncResult.createSenderComplete), asyncState);
				return asyncState.SyncContinue(asyncResult);
			}

			private static bool CreateSenderComplete(IAsyncResult result)
			{
				ServiceBusOutputChannel.OpenMessagingFactoryAndMessageSenderAsyncResult asyncState = (ServiceBusOutputChannel.OpenMessagingFactoryAndMessageSenderAsyncResult)result.AsyncState;
				asyncState.messageSender = asyncState.outputChannel.messagingFactory.EndCreateMessageSender(result);
				IAsyncResult asyncResult = asyncState.messageSender.BeginOpen(asyncState.timeoutHelper.RemainingTime(), asyncState.PrepareAsyncCompletion(ServiceBusOutputChannel.OpenMessagingFactoryAndMessageSenderAsyncResult.openSenderComplete), asyncState);
				return asyncState.SyncContinue(asyncResult);
			}

			public static new Microsoft.ServiceBus.Messaging.MessageSender End(IAsyncResult result)
			{
				Microsoft.ServiceBus.Messaging.MessageSender messageSender;
				try
				{
					messageSender = AsyncResult.End<ServiceBusOutputChannel.OpenMessagingFactoryAndMessageSenderAsyncResult>(result).messageSender;
				}
				catch (MessagingException messagingException1)
				{
					MessagingException messagingException = messagingException1;
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(MessagingExceptionHelper.ConvertToCommunicationException(messagingException), null);
				}
				return messageSender;
			}

			private static bool OpenSenderComplete(IAsyncResult result)
			{
				((ServiceBusOutputChannel.OpenMessagingFactoryAndMessageSenderAsyncResult)result.AsyncState).messageSender.EndOpen(result);
				return true;
			}
		}

		private sealed class SendAsyncResult : AsyncResult
		{
			private static AsyncResult.AsyncCompletion onSendComplete;

			private byte[] bufferToReturn;

			private ServiceBusOutputChannel outputChannel;

			static SendAsyncResult()
			{
				ServiceBusOutputChannel.SendAsyncResult.onSendComplete = new AsyncResult.AsyncCompletion(ServiceBusOutputChannel.SendAsyncResult.OnSendComplete);
			}

			public SendAsyncResult(ServiceBusOutputChannel outputChannel, Message message, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				bool flag;
				bool flag1;
				this.outputChannel = outputChannel;
				outputChannel.AddHeadersTo(message);
				outputChannel.TraceSendingMessage(message);
				BrokeredMessage brokerMessage = null;
				try
				{
					brokerMessage = outputChannel.ConvertToBrokerMessage(message, out this.bufferToReturn);
					Microsoft.ServiceBus.Messaging.MessageSender messageSender = outputChannel.MessageSender;
					BrokeredMessage[] brokeredMessageArray = new BrokeredMessage[] { brokerMessage };
					if (base.SyncContinue(messageSender.BeginSend(brokeredMessageArray, timeout, base.PrepareAsyncCompletion(ServiceBusOutputChannel.SendAsyncResult.onSendComplete), this)))
					{
						base.Complete(true);
					}
				}
				catch (MessagingException messagingException)
				{
					CommunicationException communicationException = MessagingExceptionHelper.ConvertToCommunicationException(messagingException, out flag);
					if (flag)
					{
						this.outputChannel.Fault();
					}
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(communicationException, null);
				}
				catch (OperationCanceledException operationCanceledException1)
				{
					OperationCanceledException operationCanceledException = operationCanceledException1;
					MessagingException innerException = operationCanceledException.InnerException as MessagingException;
					if (innerException != null)
					{
						MessagingExceptionHelper.ConvertToCommunicationException(innerException, out flag1);
						if (flag1)
						{
							this.outputChannel.Fault();
						}
					}
					ExceptionTrace exception = Microsoft.ServiceBus.Messaging.FxTrace.Exception;
					string entityClosedOrAborted = SRClient.EntityClosedOrAborted;
					Exception innerException1 = operationCanceledException.InnerException;
					if (innerException1 == null)
					{
						innerException1 = operationCanceledException;
					}
					throw exception.AsError(new CommunicationObjectAbortedException(entityClosedOrAborted, innerException1), null);
				}
				catch (Exception exception1)
				{
					if (this.bufferToReturn != null)
					{
						this.outputChannel.ChannelFactory.BufferManager.ReturnBuffer(this.bufferToReturn);
					}
					throw;
				}
			}

			public static new void End(IAsyncResult result)
			{
				AsyncResult.End<ServiceBusOutputChannel.SendAsyncResult>(result);
			}

			private static bool OnSendComplete(IAsyncResult result)
			{
				bool flag;
				bool flag1;
				bool flag2;
				ServiceBusOutputChannel.SendAsyncResult asyncState = (ServiceBusOutputChannel.SendAsyncResult)result.AsyncState;
				try
				{
					try
					{
						asyncState.outputChannel.MessageSender.EndSend(result);
						flag2 = true;
					}
					catch (MessagingException messagingException)
					{
						CommunicationException communicationException = MessagingExceptionHelper.ConvertToCommunicationException(messagingException, out flag);
						if (flag)
						{
							asyncState.outputChannel.Fault();
						}
						throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(communicationException, null);
					}
					catch (OperationCanceledException operationCanceledException1)
					{
						OperationCanceledException operationCanceledException = operationCanceledException1;
						MessagingException innerException = operationCanceledException.InnerException as MessagingException;
						if (innerException != null)
						{
							MessagingExceptionHelper.ConvertToCommunicationException(innerException, out flag1);
							if (flag1)
							{
								asyncState.outputChannel.Fault();
							}
						}
						ExceptionTrace exception = Microsoft.ServiceBus.Messaging.FxTrace.Exception;
						string entityClosedOrAborted = SRClient.EntityClosedOrAborted;
						Exception innerException1 = operationCanceledException.InnerException;
						if (innerException1 == null)
						{
							innerException1 = operationCanceledException;
						}
						throw exception.AsError(new CommunicationObjectAbortedException(entityClosedOrAborted, innerException1), null);
					}
				}
				finally
				{
					if (asyncState.bufferToReturn != null)
					{
						asyncState.outputChannel.ChannelFactory.BufferManager.ReturnBuffer(asyncState.bufferToReturn);
					}
				}
				return flag2;
			}
		}
	}
}