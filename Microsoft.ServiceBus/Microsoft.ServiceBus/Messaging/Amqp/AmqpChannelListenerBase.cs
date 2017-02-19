using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using System;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal abstract class AmqpChannelListenerBase : Microsoft.ServiceBus.Channels.TransportChannelListener, ILinkFactory
	{
		protected readonly static System.ServiceModel.Channels.BufferManager GCBufferManager;

		protected Microsoft.ServiceBus.Messaging.Amqp.AmqpSettings AmqpSettings
		{
			get
			{
				return this.TransportBindingElement.AmqpSettings;
			}
		}

		public override string Scheme
		{
			get
			{
				return "amqp";
			}
		}

		protected AmqpTransportBindingElement TransportBindingElement
		{
			get;
			private set;
		}

		internal override Microsoft.ServiceBus.Channels.UriPrefixTable<Microsoft.ServiceBus.Channels.ITransportManagerRegistration> TransportManagerTable
		{
			get
			{
				return AmqpTransportManager.TransportManagerTable;
			}
		}

		static AmqpChannelListenerBase()
		{
			AmqpChannelListenerBase.GCBufferManager = System.ServiceModel.Channels.BufferManager.CreateBufferManager((long)0, 0);
		}

		protected AmqpChannelListenerBase(AmqpTransportBindingElement transportBindingElement, BindingContext context, HostNameComparisonMode hostNameComparisonMode) : base(transportBindingElement, context, hostNameComparisonMode)
		{
			this.TransportBindingElement = transportBindingElement;
		}

		internal override Microsoft.ServiceBus.Channels.ITransportManagerRegistration CreateTransportManagerRegistration(System.Uri listenUri)
		{
			return new AmqpTransportManager(listenUri, this.AmqpSettings, this.DefaultOpenTimeout);
		}

		IAsyncResult Microsoft.ServiceBus.Messaging.Amqp.ILinkFactory.BeginOpenLink(AmqpLink link, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.OnBeginOpenLink(link, timeout, callback, state);
		}

		AmqpLink Microsoft.ServiceBus.Messaging.Amqp.ILinkFactory.CreateLink(AmqpSession session, AmqpLinkSettings linkSettings)
		{
			throw Fx.AssertAndThrow("AmqpTransportManager should always create the Link!");
		}

		void Microsoft.ServiceBus.Messaging.Amqp.ILinkFactory.EndOpenLink(IAsyncResult result)
		{
			this.OnEndOpenLink(result);
		}

		protected abstract IAsyncResult OnBeginOpenLink(AmqpLink link, TimeSpan timeout, AsyncCallback callback, object state);

		protected abstract void OnEndOpenLink(IAsyncResult result);

		protected override bool OnWaitForChannel(TimeSpan timeout)
		{
			return base.EndWaitForChannel(base.BeginWaitForChannel(timeout, null, null));
		}
	}
}