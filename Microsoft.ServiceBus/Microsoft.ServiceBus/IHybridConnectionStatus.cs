using System;

namespace Microsoft.ServiceBus
{
	public interface IHybridConnectionStatus
	{
		HybridConnectionState ConnectionState
		{
			get;
		}

		event EventHandler<HybridConnectionStateChangedArgs> ConnectionStateChanged;
	}
}