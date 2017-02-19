using System;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Channels
{
	internal class ReplyChannelAcceptor : Microsoft.ServiceBus.Channels.SingletonChannelAcceptor<IReplyChannel, Microsoft.ServiceBus.Channels.ReplyChannel, RequestContext>
	{
		public ReplyChannelAcceptor(ChannelManagerBase channelManager, Func<Exception> pendingExceptionGenerator) : base(channelManager, pendingExceptionGenerator)
		{
		}

		protected override Microsoft.ServiceBus.Channels.ReplyChannel OnCreateChannel()
		{
			return new Microsoft.ServiceBus.Channels.ReplyChannel(base.ChannelManager, null);
		}

		protected override void OnTraceMessageReceived(RequestContext requestContext)
		{
		}
	}
}