using System;

namespace Microsoft.ServiceBus
{
	public class HybridConnectionStateChangedArgs : EventArgs
	{
		private HybridConnectionState state;

		public HybridConnectionState ConnectionState
		{
			get
			{
				return this.state;
			}
		}

		public HybridConnectionStateChangedArgs(HybridConnectionState state)
		{
			this.state = state;
		}
	}
}