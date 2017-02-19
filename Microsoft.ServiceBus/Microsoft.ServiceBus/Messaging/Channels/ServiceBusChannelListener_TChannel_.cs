using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Channels;
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
	internal abstract class ServiceBusChannelListener<TChannel> : ChannelListenerBaseInternals<TChannel>
	where TChannel : class, IChannel
	{
		private System.Uri listenUri;

		internal int MaxBufferSize
		{
			get;
			private set;
		}

		internal System.ServiceModel.Channels.MessageEncoderFactory MessageEncoderFactory
		{
			get;
			private set;
		}

		internal Microsoft.ServiceBus.Messaging.MessagingAddress MessagingAddress
		{
			get;
			private set;
		}

		internal Microsoft.ServiceBus.Messaging.MessagingFactory MessagingFactory
		{
			get;
			private set;
		}

		internal Microsoft.ServiceBus.Messaging.MessagingFactorySettings MessagingFactorySettings
		{
			get;
			private set;
		}

		internal int PrefetchCount
		{
			get;
			private set;
		}

		internal bool ReceiveContextEnabled
		{
			get;
			private set;
		}

		internal Microsoft.ServiceBus.Messaging.ReceiveMode ReceiveMode
		{
			get
			{
				if (!this.ReceiveContextEnabled)
				{
					return Microsoft.ServiceBus.Messaging.ReceiveMode.ReceiveAndDelete;
				}
				return Microsoft.ServiceBus.Messaging.ReceiveMode.PeekLock;
			}
		}

		internal NetMessagingTransportBindingElement TransportBindingElement
		{
			get;
			private set;
		}

		public override System.Uri Uri
		{
			get
			{
				return this.listenUri;
			}
		}

		protected ServiceBusChannelListener(BindingContext context, NetMessagingTransportBindingElement transport) : base(context.Binding)
		{
			this.listenUri = context.ListenUriBaseAddress;
			if (!string.IsNullOrEmpty(context.ListenUriRelativeAddress))
			{
				this.listenUri = new System.Uri(context.ListenUriBaseAddress, context.ListenUriRelativeAddress);
			}
			this.MaxBufferSize = (int)Math.Min(transport.MaxReceivedMessageSize, (long)2147483647);
			this.ReceiveContextEnabled = transport.ReceiveContextEnabled;
			MessageEncodingBindingElement messageEncodingBindingElement = context.BindingParameters.Find<MessageEncodingBindingElement>();
			if (messageEncodingBindingElement == null)
			{
				messageEncodingBindingElement = Microsoft.ServiceBus.Messaging.Channels.TransportDefaults.CreateDefaultEncoder();
			}
			this.MessageEncoderFactory = messageEncodingBindingElement.CreateMessageEncoderFactory();
			this.MessagingFactorySettings = transport.CreateMessagingFactorySettings(context);
			this.MessagingAddress = new Microsoft.ServiceBus.Messaging.MessagingAddress(this.Uri, this.MessagingFactorySettings.NetMessagingTransportSettings.GatewayMode);
			if (this.MessagingAddress.Type != MessagingAddressType.Entity)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.Argument("ListenUriBaseAddress", SRClient.EntityNameNotFound(this.MessagingAddress.ToString()));
			}
			this.TransportBindingElement = transport;
			this.PrefetchCount = transport.PrefetchCount;
		}

		private Exception ConvertException(MessagingException messagingException)
		{
			bool flag;
			Exception communicationException = MessagingExceptionHelper.ConvertToCommunicationException(messagingException, out flag);
			if (flag)
			{
				base.Fault();
			}
			return communicationException;
		}

		public override T GetProperty<T>()
		where T : class
		{
			if (typeof(T) == typeof(MessageVersion))
			{
				return (T)this.MessageEncoderFactory.MessageVersion;
			}
			if (typeof(T) != typeof(FaultConverter))
			{
				return base.GetProperty<T>();
			}
			return this.MessageEncoderFactory.Encoder.GetProperty<T>();
		}

		internal void InternalFault()
		{
			base.Fault();
		}

		protected override void OnAbort()
		{
			Microsoft.ServiceBus.Messaging.MessagingFactory messagingFactory = this.MessagingFactory;
			if (messagingFactory != null)
			{
				messagingFactory.Abort();
			}
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult asyncResult;
			try
			{
				asyncResult = this.MessagingFactory.BeginClose(timeout, callback, state);
			}
			catch (MessagingException messagingException1)
			{
				MessagingException messagingException = messagingException1;
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(this.ConvertException(messagingException), null);
			}
			catch (OperationCanceledException operationCanceledException1)
			{
				OperationCanceledException operationCanceledException = operationCanceledException1;
				ExceptionTrace exception = Microsoft.ServiceBus.Messaging.FxTrace.Exception;
				string entityClosedOrAborted = SRClient.EntityClosedOrAborted;
				Exception innerException = operationCanceledException.InnerException;
				if (innerException == null)
				{
					innerException = operationCanceledException;
				}
				throw exception.AsError(new CommunicationObjectAbortedException(entityClosedOrAborted, innerException), null);
			}
			return asyncResult;
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			ServiceBusChannelListener<TChannel>.OpenAsyncResult openAsyncResult = new ServiceBusChannelListener<TChannel>.OpenAsyncResult(this, timeout, callback, state);
			openAsyncResult.Start();
			return openAsyncResult;
		}

		protected override void OnClose(TimeSpan timeout)
		{
			try
			{
				this.MessagingFactory.Close(timeout);
			}
			catch (MessagingException messagingException1)
			{
				MessagingException messagingException = messagingException1;
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(this.ConvertException(messagingException), null);
			}
			catch (OperationCanceledException operationCanceledException1)
			{
				OperationCanceledException operationCanceledException = operationCanceledException1;
				ExceptionTrace exception = Microsoft.ServiceBus.Messaging.FxTrace.Exception;
				string entityClosedOrAborted = SRClient.EntityClosedOrAborted;
				Exception innerException = operationCanceledException.InnerException;
				if (innerException == null)
				{
					innerException = operationCanceledException;
				}
				throw exception.AsError(new CommunicationObjectAbortedException(entityClosedOrAborted, innerException), null);
			}
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			try
			{
				this.MessagingFactory.EndClose(result);
			}
			catch (MessagingException messagingException1)
			{
				MessagingException messagingException = messagingException1;
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(this.ConvertException(messagingException), null);
			}
			catch (OperationCanceledException operationCanceledException1)
			{
				OperationCanceledException operationCanceledException = operationCanceledException1;
				ExceptionTrace exception = Microsoft.ServiceBus.Messaging.FxTrace.Exception;
				string entityClosedOrAborted = SRClient.EntityClosedOrAborted;
				Exception innerException = operationCanceledException.InnerException;
				if (innerException == null)
				{
					innerException = operationCanceledException;
				}
				throw exception.AsError(new CommunicationObjectAbortedException(entityClosedOrAborted, innerException), null);
			}
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			AsyncResult<ServiceBusChannelListener<TChannel>.OpenAsyncResult>.End(result);
		}

		private void OnMessagingFactoryFaulted(object sender, EventArgs args)
		{
			base.Fault();
		}

		protected override void OnOpen(TimeSpan timeout)
		{
			(new ServiceBusChannelListener<TChannel>.OpenAsyncResult(this, timeout, null, null)).RunSynchronously();
		}

		private class OpenAsyncResult : IteratorAsyncResult<ServiceBusChannelListener<TChannel>.OpenAsyncResult>
		{
			private readonly ServiceBusChannelListener<TChannel> channelListener;

			private readonly System.Uri messagingFactoryAddress;

			private readonly Microsoft.ServiceBus.Messaging.MessagingFactorySettings factorySettings;

			public OpenAsyncResult(ServiceBusChannelListener<TChannel> channelListener, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.channelListener = channelListener;
				this.factorySettings = this.channelListener.MessagingFactorySettings;
				if (!this.factorySettings.NetMessagingTransportSettings.GatewayMode)
				{
					this.messagingFactoryAddress = this.channelListener.MessagingAddress.NamingAuthority;
					return;
				}
				this.messagingFactoryAddress = this.channelListener.MessagingAddress.ResourceAddress;
			}

			private bool ContineAfterMessagingException(Exception exception)
			{
				if (exception == null)
				{
					return true;
				}
				MessagingException messagingException = exception as MessagingException;
				if (messagingException == null)
				{
					base.Complete(exception);
				}
				else
				{
					base.Complete(MessagingExceptionHelper.ConvertToCommunicationException(messagingException));
				}
				return !base.IsCompleted;
			}

			protected override IEnumerator<IteratorAsyncResult<ServiceBusChannelListener<TChannel>.OpenAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				ServiceBusChannelListener<TChannel>.OpenAsyncResult openAsyncResult = this;
				IteratorAsyncResult<ServiceBusChannelListener<TChannel>.OpenAsyncResult>.BeginCall beginCall = (ServiceBusChannelListener<TChannel>.OpenAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => Microsoft.ServiceBus.Messaging.MessagingFactory.BeginCreate(thisPtr.messagingFactoryAddress, thisPtr.factorySettings, t, c, s);
				IteratorAsyncResult<ServiceBusChannelListener<TChannel>.OpenAsyncResult>.EndCall messagingFactory = (ServiceBusChannelListener<TChannel>.OpenAsyncResult thisPtr, IAsyncResult a) => thisPtr.channelListener.MessagingFactory = Microsoft.ServiceBus.Messaging.MessagingFactory.EndCreate(a);
				yield return openAsyncResult.CallAsync(beginCall, messagingFactory, (ServiceBusChannelListener<TChannel>.OpenAsyncResult thisPtr, TimeSpan t) => thisPtr.channelListener.MessagingFactory = Microsoft.ServiceBus.Messaging.MessagingFactory.Create(thisPtr.messagingFactoryAddress, thisPtr.factorySettings, t), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
				if (this.ContineAfterMessagingException(base.LastAsyncStepException))
				{
					this.channelListener.MessagingFactory.SafeAddFaulted(new EventHandler(this.channelListener.OnMessagingFactoryFaulted));
					ServiceBusChannelListener<TChannel>.OpenAsyncResult openAsyncResult1 = this;
					IteratorAsyncResult<ServiceBusChannelListener<TChannel>.OpenAsyncResult>.BeginCall beginCall1 = (ServiceBusChannelListener<TChannel>.OpenAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.channelListener.MessagingFactory.BeginOpen(t, c, s);
					IteratorAsyncResult<ServiceBusChannelListener<TChannel>.OpenAsyncResult>.EndCall endCall = (ServiceBusChannelListener<TChannel>.OpenAsyncResult thisPtr, IAsyncResult a) => thisPtr.channelListener.MessagingFactory.EndOpen(a);
					yield return openAsyncResult1.CallAsync(beginCall1, endCall, (ServiceBusChannelListener<TChannel>.OpenAsyncResult thisPtr, TimeSpan t) => thisPtr.channelListener.MessagingFactory.Open(t), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
					this.ContineAfterMessagingException(base.LastAsyncStepException);
				}
			}
		}
	}
}