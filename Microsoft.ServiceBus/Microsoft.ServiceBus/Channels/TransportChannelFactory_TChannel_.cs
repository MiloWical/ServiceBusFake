using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Channels
{
	internal abstract class TransportChannelFactory<TChannel> : ChannelFactoryBase<TChannel>, Microsoft.ServiceBus.Channels.ITransportFactorySettings, IDefaultCommunicationTimeouts
	{
		private System.ServiceModel.Channels.BufferManager bufferManager;

		private long maxBufferPoolSize;

		private long maxReceivedMessageSize;

		private System.ServiceModel.Channels.MessageEncoderFactory messageEncoderFactory;

		private bool manualAddressing;

		private System.ServiceModel.Channels.MessageVersion messageVersion;

		public System.ServiceModel.Channels.BufferManager BufferManager
		{
			get
			{
				return this.bufferManager;
			}
		}

		public bool ManualAddressing
		{
			get
			{
				return this.manualAddressing;
			}
		}

		public long MaxBufferPoolSize
		{
			get
			{
				return this.maxBufferPoolSize;
			}
		}

		public long MaxReceivedMessageSize
		{
			get
			{
				return this.maxReceivedMessageSize;
			}
		}

		public System.ServiceModel.Channels.MessageEncoderFactory MessageEncoderFactory
		{
			get
			{
				return this.messageEncoderFactory;
			}
		}

		public System.ServiceModel.Channels.MessageVersion MessageVersion
		{
			get
			{
				return this.messageVersion;
			}
		}

		System.ServiceModel.Channels.BufferManager Microsoft.ServiceBus.Channels.ITransportFactorySettings.BufferManager
		{
			get
			{
				return this.BufferManager;
			}
		}

		bool Microsoft.ServiceBus.Channels.ITransportFactorySettings.ManualAddressing
		{
			get
			{
				return this.ManualAddressing;
			}
		}

		long Microsoft.ServiceBus.Channels.ITransportFactorySettings.MaxReceivedMessageSize
		{
			get
			{
				return this.MaxReceivedMessageSize;
			}
		}

		System.ServiceModel.Channels.MessageEncoderFactory Microsoft.ServiceBus.Channels.ITransportFactorySettings.MessageEncoderFactory
		{
			get
			{
				return this.MessageEncoderFactory;
			}
		}

		public abstract string Scheme
		{
			get;
		}

		protected TransportChannelFactory(TransportBindingElement bindingElement, BindingContext context) : this(bindingElement, context, Microsoft.ServiceBus.Channels.TransportDefaults.GetDefaultMessageEncoderFactory())
		{
		}

		protected TransportChannelFactory(TransportBindingElement bindingElement, BindingContext context, System.ServiceModel.Channels.MessageEncoderFactory defaultMessageEncoderFactory) : base(context.Binding)
		{
			this.manualAddressing = bindingElement.ManualAddressing;
			this.maxBufferPoolSize = bindingElement.MaxBufferPoolSize;
			this.maxReceivedMessageSize = bindingElement.MaxReceivedMessageSize;
			Collection<MessageEncodingBindingElement> messageEncodingBindingElements = context.BindingParameters.FindAll<MessageEncodingBindingElement>();
			if (messageEncodingBindingElements.Count > 1)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(Resources.MultipleMebesInParameters, new object[0])));
			}
			if (messageEncodingBindingElements.Count != 1)
			{
				this.messageEncoderFactory = defaultMessageEncoderFactory;
			}
			else
			{
				this.messageEncoderFactory = messageEncodingBindingElements[0].CreateMessageEncoderFactory();
				context.BindingParameters.Remove<MessageEncodingBindingElement>();
			}
			if (this.messageEncoderFactory == null)
			{
				this.messageVersion = System.ServiceModel.Channels.MessageVersion.None;
				return;
			}
			this.messageVersion = this.messageEncoderFactory.MessageVersion;
		}

		internal virtual int GetMaxBufferSize()
		{
			if (this.MaxReceivedMessageSize > (long)2147483647)
			{
				return 2147483647;
			}
			return (int)this.MaxReceivedMessageSize;
		}

		public override T GetProperty<T>()
		where T : class
		{
			if (typeof(T) == typeof(System.ServiceModel.Channels.MessageVersion))
			{
				return (T)this.MessageVersion;
			}
			if (typeof(T) != typeof(FaultConverter))
			{
				return base.GetProperty<T>();
			}
			if (this.MessageEncoderFactory == null)
			{
				return default(T);
			}
			return this.MessageEncoderFactory.Encoder.GetProperty<T>();
		}

		protected override void OnAbort()
		{
			this.OnCloseOrAbort();
			base.OnAbort();
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			this.OnCloseOrAbort();
			return base.OnBeginClose(timeout, callback, state);
		}

		protected override void OnClose(TimeSpan timeout)
		{
			this.OnCloseOrAbort();
			base.OnClose(timeout);
		}

		private void OnCloseOrAbort()
		{
			if (this.bufferManager != null)
			{
				this.bufferManager.Clear();
			}
		}

		protected override void OnOpening()
		{
			base.OnOpening();
			this.bufferManager = System.ServiceModel.Channels.BufferManager.CreateBufferManager(this.MaxBufferPoolSize, this.GetMaxBufferSize());
		}

		internal void ValidateScheme(Uri via)
		{
			if (via.Scheme != this.Scheme && string.Compare(via.Scheme, this.Scheme, StringComparison.OrdinalIgnoreCase) != 0)
			{
				ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
				string invalidUriScheme = Resources.InvalidUriScheme;
				object[] scheme = new object[] { via.Scheme, this.Scheme };
				throw exceptionUtility.ThrowHelperArgument("via", Microsoft.ServiceBus.SR.GetString(invalidUriScheme, scheme));
			}
		}
	}
}