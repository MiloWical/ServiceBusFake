using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Messaging.Channels
{
	internal sealed class ServiceBusChannelFactory : ChannelFactoryBase<IOutputChannel>
	{
		private System.ServiceModel.Channels.BufferManager bufferManager;

		internal System.ServiceModel.Channels.BufferManager BufferManager
		{
			get
			{
				return this.bufferManager;
			}
		}

		internal bool ManualAddressing
		{
			get;
			private set;
		}

		internal long MaxBufferPoolSize
		{
			get;
			private set;
		}

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

		internal Microsoft.ServiceBus.Messaging.MessagingFactorySettings MessagingFactorySettings
		{
			get;
			private set;
		}

		public ServiceBusChannelFactory(BindingContext context, NetMessagingTransportBindingElement transport) : base(context.Binding)
		{
			this.MessagingFactorySettings = transport.CreateMessagingFactorySettings(context);
			this.ManualAddressing = transport.ManualAddressing;
			this.MaxBufferPoolSize = transport.MaxBufferPoolSize;
			this.MaxBufferSize = (int)Math.Min(transport.MaxReceivedMessageSize, (long)2147483647);
			MessageEncodingBindingElement messageEncodingBindingElement = context.BindingParameters.Find<MessageEncodingBindingElement>();
			if (messageEncodingBindingElement == null)
			{
				messageEncodingBindingElement = Microsoft.ServiceBus.Messaging.Channels.TransportDefaults.CreateDefaultEncoder();
			}
			this.MessageEncoderFactory = messageEncodingBindingElement.CreateMessageEncoderFactory();
		}

		private void ClearBufferManager()
		{
			if (this.bufferManager != null)
			{
				this.bufferManager.Clear();
			}
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

		protected override void OnAbort()
		{
			this.ClearBufferManager();
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult(callback, state);
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			this.OnOpen(timeout);
			return new CompletedAsyncResult(callback, state);
		}

		protected override void OnClose(TimeSpan timeout)
		{
			this.ClearBufferManager();
		}

		protected override IOutputChannel OnCreateChannel(EndpointAddress endpointAddress, Uri via)
		{
			ServiceBusChannelFactory.ValidateScheme(via);
			return new ServiceBusOutputChannel(this, endpointAddress, via);
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
			this.ClearBufferManager();
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
		}

		protected override void OnOpen(TimeSpan timeout)
		{
			this.bufferManager = System.ServiceModel.Channels.BufferManager.CreateBufferManager(this.MaxBufferPoolSize, this.MaxBufferSize);
		}

		private static void ValidateScheme(Uri via)
		{
			if (!string.Equals(via.Scheme, "sb", StringComparison.OrdinalIgnoreCase))
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.Argument("via", SRClient.InvalidUriScheme(via.Scheme, "sb"));
			}
		}
	}
}