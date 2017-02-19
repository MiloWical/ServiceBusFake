using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace Microsoft.ServiceBus.Channels
{
	internal interface ITransportManagerRegistration
	{
		System.ServiceModel.HostNameComparisonMode HostNameComparisonMode
		{
			get;
		}

		Uri ListenUri
		{
			get;
		}

		IList<TransportManager> Select(TransportChannelListener factory);
	}
}