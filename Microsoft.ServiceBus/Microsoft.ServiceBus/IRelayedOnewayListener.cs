using System;
using System.ServiceModel;

namespace Microsoft.ServiceBus
{
	internal interface IRelayedOnewayListener : ICommunicationObject, IConnectionStatus
	{
		Microsoft.ServiceBus.NameSettings NameSettings
		{
			get;
		}

		System.Uri Uri
		{
			get;
		}

		void Register(RelayedOnewayChannelListener channelListener);

		void Unregister(RelayedOnewayChannelListener channelListener);
	}
}