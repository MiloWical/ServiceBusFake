using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Messaging.Channels;
using System;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus
{
	internal class SocketConnectionDuplexChannelListener : SocketConnectionChannelListener<IDuplexSessionChannel, Microsoft.ServiceBus.Channels.InputQueueChannelAcceptor<IDuplexSessionChannel>>, Microsoft.ServiceBus.Channels.ISessionPreambleHandler
	{
		private Microsoft.ServiceBus.Channels.InputQueueChannelAcceptor<IDuplexSessionChannel> duplexAcceptor;

		protected override Microsoft.ServiceBus.Channels.InputQueueChannelAcceptor<IDuplexSessionChannel> ChannelAcceptor
		{
			get
			{
				return this.duplexAcceptor;
			}
		}

		public SocketConnectionDuplexChannelListener(SocketConnectionBindingElement bindingElement, BindingContext context) : base(bindingElement, context)
		{
			this.duplexAcceptor = new Microsoft.ServiceBus.Channels.InputQueueChannelAcceptor<IDuplexSessionChannel>(this, () => this.GetPendingException());
		}

		void Microsoft.ServiceBus.Channels.ISessionPreambleHandler.HandleServerSessionPreamble(Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader preambleReader, Microsoft.ServiceBus.Channels.ConnectionDemuxer connectionDemuxer)
		{
			IDuplexSessionChannel duplexSessionChannel = preambleReader.CreateDuplexSessionChannel(this, new EndpointAddress(this.Uri, new AddressHeader[0]), base.ExposeConnectionProperty, connectionDemuxer);
			this.duplexAcceptor.EnqueueAndDispatch(duplexSessionChannel, preambleReader.ConnectionDequeuedCallback);
		}
	}
}