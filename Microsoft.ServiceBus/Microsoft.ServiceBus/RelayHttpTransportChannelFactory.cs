using Microsoft.ServiceBus.Channels;
using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus
{
	internal class RelayHttpTransportChannelFactory : Microsoft.ServiceBus.Channels.LayeredChannelFactory<IRequestChannel>
	{
		private IChannelFactory<IRequestChannel> innerChannelFactory;

		private TokenProvider tokenProvider;

		public RelayHttpTransportChannelFactory(BindingContext context, IChannelFactory<IRequestChannel> innerChannelFactory) : base(context.Binding, innerChannelFactory)
		{
			this.innerChannelFactory = innerChannelFactory;
			this.tokenProvider = TokenProviderUtility.CreateTokenProvider(context);
		}

		public override T GetProperty<T>()
		where T : class
		{
			return this.innerChannelFactory.GetProperty<T>();
		}

		protected override IRequestChannel OnCreateChannel(EndpointAddress address, Uri via)
		{
			IRequestChannel requestChannel = this.innerChannelFactory.CreateChannel(address, via);
			if (requestChannel == null)
			{
				return null;
			}
			return new RelayedHttpRequestChannel(this, this.tokenProvider, requestChannel);
		}

		protected override void OnOpening()
		{
			this.innerChannelFactory = (IChannelFactory<IRequestChannel>)base.InnerChannelFactory;
			base.OnOpening();
		}
	}
}