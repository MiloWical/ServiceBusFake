using System;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Channels
{
	internal class InputChannelAcceptor : Microsoft.ServiceBus.Channels.SingletonChannelAcceptor<IInputChannel, Microsoft.ServiceBus.Channels.InputChannel, Message>
	{
		public InputChannelAcceptor(ChannelManagerBase channelManager, Func<Exception> pendingExceptionGenerator) : base(channelManager, pendingExceptionGenerator)
		{
		}

		protected override Microsoft.ServiceBus.Channels.InputChannel OnCreateChannel()
		{
			return new Microsoft.ServiceBus.Channels.InputChannel(base.ChannelManager, null);
		}

		protected override void OnTraceMessageReceived(Message message)
		{
		}
	}
}