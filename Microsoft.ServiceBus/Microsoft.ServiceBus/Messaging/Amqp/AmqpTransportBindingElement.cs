using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging.Amqp.Transport;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal class AmqpTransportBindingElement : TransportBindingElement
	{
		internal Microsoft.ServiceBus.Messaging.Amqp.AmqpSettings AmqpSettings
		{
			get;
			set;
		}

		public override string Scheme
		{
			get
			{
				return "amqp";
			}
		}

		public AmqpTransportBindingElement()
		{
			AmqpTransportProvider amqpTransportProvider = new AmqpTransportProvider();
			amqpTransportProvider.Versions.Add(new AmqpVersion(1, 0, 0));
			this.AmqpSettings = new Microsoft.ServiceBus.Messaging.Amqp.AmqpSettings();
			this.AmqpSettings.TransportProviders.Add(amqpTransportProvider);
		}

		private AmqpTransportBindingElement(AmqpTransportBindingElement elementToBeCloned) : base(elementToBeCloned)
		{
			this.AmqpSettings = elementToBeCloned.AmqpSettings.Clone();
		}

		public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
		{
			if (context == null)
			{
				throw Fx.Exception.ArgumentNull("context");
			}
			if (!this.CanBuildChannelFactory<TChannel>(context))
			{
				throw Fx.Exception.Argument("TChannel", SRClient.UnsupportedChannelType(typeof(TChannel)));
			}
			return (IChannelFactory<TChannel>)(new AmqpChannelFactory(this, context));
		}

		public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
		where TChannel : class, IChannel
		{
			if (context == null)
			{
				throw Fx.Exception.ArgumentNull("context");
			}
			if (!this.CanBuildChannelListener<TChannel>(context))
			{
				throw Fx.Exception.Argument("TChannel", SRClient.UnsupportedChannelType(typeof(TChannel)));
			}
			return (IChannelListener<TChannel>)(new AmqpChannelListener(this, context));
		}

		public override bool CanBuildChannelFactory<TChannel>(BindingContext context)
		{
			return typeof(TChannel) == typeof(IOutputChannel);
		}

		public override bool CanBuildChannelListener<TChannel>(BindingContext context)
		where TChannel : class, IChannel
		{
			return typeof(TChannel) == typeof(IInputSessionChannel);
		}

		public override BindingElement Clone()
		{
			return new AmqpTransportBindingElement(this);
		}
	}
}