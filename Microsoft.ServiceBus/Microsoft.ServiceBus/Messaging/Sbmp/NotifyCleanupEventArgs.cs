using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	internal class NotifyCleanupEventArgs : EventArgs
	{
		public Uri ChannelAddress
		{
			get;
			private set;
		}

		public NotifyCleanupEventArgs(Uri channelAddress)
		{
			this.ChannelAddress = channelAddress;
		}
	}
}