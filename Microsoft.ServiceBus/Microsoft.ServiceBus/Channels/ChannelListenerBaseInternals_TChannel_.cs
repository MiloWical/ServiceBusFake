using Microsoft.ServiceBus.Messaging.Channels;
using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Channels
{
	internal abstract class ChannelListenerBaseInternals<TChannel> : ChannelListenerBase<TChannel>, ICommunicationObjectInternals, ICommunicationObject
	where TChannel : class, IChannel
	{
		protected ChannelListenerBaseInternals(IDefaultCommunicationTimeouts timeouts) : base(timeouts)
		{
		}

		void Microsoft.ServiceBus.Messaging.Channels.ICommunicationObjectInternals.ThrowIfDisposed()
		{
			base.ThrowIfDisposed();
		}

		void Microsoft.ServiceBus.Messaging.Channels.ICommunicationObjectInternals.ThrowIfDisposedOrNotOpen()
		{
			base.ThrowIfDisposedOrNotOpen();
		}
	}
}